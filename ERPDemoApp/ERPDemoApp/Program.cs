using ERPDemoApp;
using ERPDemoApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ERPDemoApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // HttpContext accessor for layouts/partials
            builder.Services.AddHttpContextAccessor();

            // Razor Pages + Areas + beveiligingsconventies
            builder.Services.AddRazorPages(options =>
            {
                // Old root-based Admin paths kept for compatibility during migration
                options.Conventions.AuthorizeFolder("/Admin", policy: "AdminOnly");
                options.Conventions.AllowAnonymousToPage("/Admin/Login");
                options.Conventions.AllowAnonymousToPage("/Admin/Logout");

                // New Areas-based routes for Admin
                options.Conventions.AuthorizeAreaFolder("Admin", "/", policy: "AdminOnly");
                options.Conventions.AllowAnonymousToAreaPage("Admin", "/Auth/Login");
                options.Conventions.AllowAnonymousToAreaPage("Admin", "/Auth/Logout");

                // Area 'App' krijgt extra route-variant: "{tenant}/<page>"
                options.Conventions.Add(new AppAreaTenantRouteConvention(areaName: "App", routePrefix: "{tenant}"));

                // Alles in Area 'App' is beschermd...
                options.Conventions.AuthorizeAreaFolder("App", "/", "TenantAuthenticated");

                // ... behalve onboarding + auth (open)
                options.Conventions.AllowAnonymousToAreaFolder("App", "/Onboarding");
                options.Conventions.AllowAnonymousToAreaFolder("App", "/Auth");
            });

            builder.Services.AddAuthentication(options =>
            {
                // Laat DefaultAuthenticateScheme en DefaultChallengeScheme leeg zodat de
                // Admin-cookie niet het hele ERP als ingelogd markeert.
            })
            .AddCookie(AuthSchemes.Admin, options =>
            {
                options.LoginPath = "/admin/login";
                options.LogoutPath = "/admin/logout";
                options.AccessDeniedPath = "/admin/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.Name = ".ERPDemo.Admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Scope de cookie tot de /admin path zodat hij niet naar de root wordt gestuurd
                options.Cookie.Path = "/admin";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            // Cookie voor tenant-login (frontend)
            .AddCookie(AuthSchemes.Tenant, options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/denied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Cookie.Name = ".ERPDemo.Tenant";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            // Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                // Bestaande Admin policy (ongewijzigd)
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.AddAuthenticationSchemes(AuthSchemes.Admin);
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Admin");
                });

                // Tenant policy: ingelogd met tenantcookie + optionele tenant-claim match
                options.AddPolicy("TenantAuthenticated", policy =>
                {
                    policy.AddAuthenticationSchemes(AuthSchemes.Tenant);
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(ctx =>
                    {
                        try
                        {
                            var http = ctx.Resource as HttpContext;
                            // In sommige gevallen is Resource geen HttpContext; laat dan door
                            string? routeTenant = http?.Request.RouteValues["tenant"]?.ToString();
                            string? userTenant = ctx.User.FindFirst("tenant")?.Value;
                            return string.IsNullOrEmpty(routeTenant) ||
                                string.Equals(routeTenant, userTenant, StringComparison.OrdinalIgnoreCase);
                        }
                        catch { return true; }
                    });
                });
            });

            // AppDbContext voor bedrijfsdata (InMemory voor nu)
            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("AppDb"));

            // Tenant infra + e-mail sender (demo)
            builder.Services.AddSingleton<ITenantStore, InMemoryTenantStore>();
            builder.Services.AddScoped<ITenantResolver, RouteTenantResolver>();
            builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Middleware: valideer tenant uit URL
            app.Use(async (context, next) =>
            {
                if (context.Request.RouteValues.TryGetValue("tenant", out var tenantObj) && tenantObj is string t)
                {
                    var resolver = context.RequestServices.GetRequiredService<ITenantResolver>();
                    var exists = await resolver.ValidateAsync(t);
                    if (!exists)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("Tenant not found");
                        return;
                    }
                    context.Items["tenant"] = t; // beschikbaar voor downstream services
                }

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Map Razor Pages including Areas
            app.MapRazorPages();

            // Health-check (afzonderlijke route)
            app.MapGet("/healthz", () => Results.Ok("OK"));

            // Demo API endpoints
            app.MapGet("/todoitems", async (TodoDb db) =>
                await db.Todos.ToListAsync());

            app.MapGet("/todoitems/complete", async (TodoDb db) =>
                await db.Todos.Where(t => t.IsComplete).ToListAsync());

            app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
                await db.Todos.FindAsync(id)
                    is Todo todo
                        ? Results.Ok(todo)
                        : Results.NotFound());

            app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
            {
                db.Todos.Add(todo);
                await db.SaveChangesAsync();

                return Results.Created($"/todoitems/{todo.Id}", todo);
            });

            app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
            {
                var todo = await db.Todos.FindAsync(id);

                if (todo is null) return Results.NotFound();

                todo.Name = inputTodo.Name;
                todo.IsComplete = inputTodo.IsComplete;

                await db.SaveChangesAsync();

                return Results.NoContent();
            });

            app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
            {
                if (await db.Todos.FindAsync(id) is Todo todo)
                {
                    db.Todos.Remove(todo);
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }

                return Results.NotFound();
            });

            app.Run();
        }
    }

    #region Helpers / Tenant infra (simple)

    public interface ITenantStore
    {
        Task<bool> ExistsAsync(string tenant);
        Task CreateAsync(string tenant, string companyName);
    }

    public class InMemoryTenantStore : ITenantStore
    {
        private readonly HashSet<string> _tenants = new(StringComparer.OrdinalIgnoreCase);
        public Task<bool> ExistsAsync(string tenant) => Task.FromResult(_tenants.Contains(tenant));
        public Task CreateAsync(string tenant, string companyName)
        {
            _tenants.Add(tenant);
            return Task.CompletedTask;
        }
    }

    public interface ITenantResolver
    {
        Task<bool> ValidateAsync(string tenant);
    }

    public class RouteTenantResolver : ITenantResolver
    {
        private readonly ITenantStore _store;
        private static readonly Regex Slug = new(@"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase);

        public RouteTenantResolver(ITenantStore store) { _store = store; }

        public async Task<bool> ValidateAsync(string tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant) || !Slug.IsMatch(tenant)) return false;
            return await _store.ExistsAsync(tenant);
        }
    }

    // Conventie: voeg voor Area='App' een extra route toe: "{tenant}/..."
    public sealed class AppAreaTenantRouteConvention : IPageRouteModelConvention
    {
        private static readonly Regex RouteParameterRegex = new(@"\{([^{}]+)\}");

        private readonly string _area;
        private readonly string _prefix;
        private readonly HashSet<string> _prefixParameterNames;

        public AppAreaTenantRouteConvention(string areaName, string routePrefix)
        {
            _area = areaName;
            _prefix = routePrefix.Trim('/');
            _prefixParameterNames = new HashSet<string>(ExtractParameterNames(_prefix), StringComparer.OrdinalIgnoreCase);
        }

        public void Apply(PageRouteModel model)
        {
            if (!string.Equals(model.AreaName, _area, StringComparison.OrdinalIgnoreCase))
                return;

            // Elke bestaande selector in de area krijgt een extra routevariant met {tenant}/
            foreach (var sel in model.Selectors.ToArray())
            {
                var template = sel.AttributeRouteModel?.Template ?? string.Empty; // bijv: "App/Onboarding/Account"

                // Strip het 'App/'-gedeelte en leidende slashes voor nette URLs: "{tenant}/Onboarding/Account"
                var clean = template.StartsWith(_area + "/", StringComparison.OrdinalIgnoreCase)
                    ? template.Substring(_area.Length + 1)
                    : template;
                clean = clean.Trim('/');

                if (SharesRouteParameterWithPrefix(clean))
                {
                    // Het bestaande template bevat al dezelfde parameter (bv. {tenant}); oversla de extra route.
                    continue;
                }

                var finalTemplate = string.IsNullOrEmpty(clean)
                    ? _prefix
                    : $"{_prefix}/{clean}";

                model.Selectors.Add(new SelectorModel
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = finalTemplate
                    }
                });
            }
        }

        private bool SharesRouteParameterWithPrefix(string template)
        {
            if (_prefixParameterNames.Count == 0 || string.IsNullOrEmpty(template))
                return false;

            foreach (var parameter in ExtractParameterNames(template))
            {
                if (_prefixParameterNames.Contains(parameter))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> ExtractParameterNames(string template)
        {
            if (string.IsNullOrEmpty(template)) yield break;

            foreach (Match match in RouteParameterRegex.Matches(template))
            {
                var content = match.Groups[1].Value;

                var colonIndex = content.IndexOf(':');
                if (colonIndex >= 0)
                {
                    content = content.Substring(0, colonIndex);
                }

                var equalsIndex = content.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    content = content.Substring(0, equalsIndex);
                }

                content = content.TrimEnd('?');
                content = content.TrimStart('*');
                content = content.Trim();

                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }

    // Minimale e-mail interface
    public interface IEmailSender { Task SendAsync(string to, string subject, string html); }
    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string html)
        {
            Console.WriteLine($"[EMAIL] to:{to} subject:{subject} body:{html}");
            return Task.CompletedTask;  
        }
    }

    #endregion
}
