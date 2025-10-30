using ERPDemoApp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

public class StartNowModel : PageModel
{
    private readonly ITenantStore _tenantStore;

    public StartNowModel(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    [BindProperty]
    public StartNowInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1) Maak een slug voor de tenant
        var slug = ToSlug(Input.Company);

        // 2) Tenant aanmaken (vervang door jouw database/EF implementatie)
        await _tenantStore.CreateAsync(slug, Input.Company);

        // 3) Maak de eerste gebruiker + koppel aan tenant (hier: demo; vervang door Identity/db)
        //    Hash wachtwoord & bewaar user; hieronder loggen we direct in met tenant-claim.
        await SignInAsync(slug, Input.Email);

        // 4) Doorsturen naar het tenant-dashboard in je App-area
        return RedirectToPage("/Admin/Index", new { area = "App", tenant = slug });
    }

    private static string ToSlug(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "tenant";
        var normalized = Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(normalized)) normalized = "tenant";
        return normalized.Length > 63 ? normalized[..63].Trim('-') : normalized;
    }

    private async Task SignInAsync(string tenant, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim("tenant", tenant),
            new Claim("role","Owner") // bv. Tenant-eigenaar
        };
        var id = new ClaimsIdentity(claims, AuthSchemes.Tenant);
        await HttpContext.SignInAsync(AuthSchemes.Tenant, new ClaimsPrincipal(id));
    }
}

public class StartNowInput
{
    // Bedrijf
    [Required, Display(Name = "Bedrijfsnaam")]
    public string Company { get; set; } = "";

    [Display(Name = "BTW-nummer")]
    public string? Vat { get; set; }

    [Required, Display(Name = "Land")]
    public string Country { get; set; } = "";

    [Display(Name = "Provincie")]
    public string? Province { get; set; }

    [Display(Name = "Postcode")] public string? Zip { get; set; }
    [Display(Name = "Gemeente / Stad")] public string? City { get; set; }
    [Display(Name = "Straat")] public string? Street { get; set; }
    [Display(Name = "Nummer")] public string? Number { get; set; }
    [Display(Name = "Bus")] public string? Bus { get; set; }

    // Contactpersoon
    [Required, Display(Name = "Contactpersoon")]
    public string ContactName { get; set; } = "";

    [Required, Phone, Display(Name = "Telefoonnummer")]
    public string Phone { get; set; } = "";

    [Required, EmailAddress, Display(Name = "E-mailadres")]
    public string Email { get; set; } = "";

    [Display(Name = "Website / naam (optioneel)")]
    public string? WebsiteName { get; set; }

    // Wachtwoord
    [Required, DataType(DataType.Password), Display(Name = "Wachtwoord")]
    [MinLength(8, ErrorMessage = "Minstens 8 tekens.")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Wachtwoord herhalen")]
    [Compare(nameof(Password), ErrorMessage = "Wachtwoorden komen niet overeen.")]
    public string ConfirmPassword { get; set; } = "";
}
