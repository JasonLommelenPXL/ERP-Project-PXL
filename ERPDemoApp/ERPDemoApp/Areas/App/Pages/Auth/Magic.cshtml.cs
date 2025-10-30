using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ERPDemoApp.Areas.App.Pages.Auth;

public class MagicModel : PageModel
{
 [BindProperty(SupportsGet = true)] public string Tenant { get; set; } = "";
 [BindProperty(SupportsGet = true)] public string Token { get; set; } = "";

 public async Task<IActionResult> OnGet()
 {
 // TODO: valideer token tegen store (eenmalig, TTL)
 // if (!IsValid(Token, Tenant)) return Forbid();

 var email = "user@demo.local"; // haal uit token store
 var claims = new[]
 {
 new Claim(ClaimTypes.Name, email),
 new Claim("tenant", Tenant)
 };
 var id = new ClaimsIdentity(claims, authenticationType: "TenantScheme");
 await HttpContext.SignInAsync("TenantScheme", new ClaimsPrincipal(id));

 return RedirectToPage("/Admin/Index", new { area = "App", tenant = Tenant });
 }
}
