using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ERPDemoApp.Areas.App.Pages.Onboarding;

public class AccountModel : PageModel
{
 private readonly ITenantResolver _tenantResolver;
 private readonly IEmailSender _email;

 public AccountModel(ITenantResolver resolver, IEmailSender email)
 {
 _tenantResolver = resolver;
 _email = email;
 }

 [BindProperty(SupportsGet = true)] public string Tenant { get; set; } = "";
 [BindProperty] public string Email { get; set; } = "";
 [BindProperty] public string Mode { get; set; } = "password"; // password | magic
 [BindProperty] public string? Password { get; set; }

 public async Task<IActionResult> OnGet()
 => await _tenantResolver.ValidateAsync(Tenant) ? Page() : NotFound();

 public async Task<IActionResult> OnPost()
 {
 if (!await _tenantResolver.ValidateAsync(Tenant)) return NotFound();
 if (!ModelState.IsValid) return Page();

 if (Mode == "password")
 {
 // TODO: create user + hash password
 await SignInAsync(Tenant, Email);
 return RedirectToPage("/Admin/Index", new { area = "App", tenant = Tenant });
 }
 else
 {
 var token = Guid.NewGuid().ToString("n"); // TODO: persist + TTL
 var url = Url.Page("/Auth/Magic", pageHandler: null, values: new { area = "App", tenant = Tenant, token }, protocol: Request.Scheme, host: null, fragment: null)!;
 await _email.SendAsync(Email, "Jouw login link", $"Klik om in te loggen: {url}");
 TempData["Info"] = "We hebben een login link naar je e-mail gestuurd.";
 return RedirectToPage("/Auth/Login", new { area = "App", tenant = Tenant });
 }
 }

 private async Task SignInAsync(string tenant, string email)
 {
 var claims = new[]
 {
 new Claim(ClaimTypes.Name, email),
 new Claim("tenant", tenant)
 };
 // Use explicit scheme to match Program.cs configuration
 var id = new ClaimsIdentity(claims, authenticationType: "TenantScheme");
 await HttpContext.SignInAsync("TenantScheme", new ClaimsPrincipal(id));
 }
}
