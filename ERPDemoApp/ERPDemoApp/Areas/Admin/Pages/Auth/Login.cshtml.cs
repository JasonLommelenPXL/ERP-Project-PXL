using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ERPDemoApp.Areas.Admin.Pages.Auth
{
 public class LoginModel : PageModel
 {
 private const string AdminEmail = "jason.lommelen@gmail.com";
 private const string AdminPassword = "admin123";

 [BindProperty, Required, EmailAddress]
 public string Email { get; set; } = string.Empty;

 [BindProperty, Required]
 public string Password { get; set; } = string.Empty;

 [BindProperty]
 public bool RememberMe { get; set; }

 [BindProperty(SupportsGet = true)]
 public string? ReturnUrl { get; set; }

 public string? Error { get; set; }

 public void OnGet()
 {
 ReturnUrl ??= Url.Page("/Dashboard/Index", new { area = "Admin" }) ?? "/admin";
 }

 public async Task<IActionResult> OnPostAsync()
 {
 if (!ModelState.IsValid)
 {
 Error = "Vul alle velden in.";
 return Page();
 }

 var ok = Email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase) && Password == AdminPassword;
 if (!ok)
 {
 Error = "Ongeldige inloggegevens.";
 return Page();
 }

 var claims = new List<Claim>
 {
 new Claim(ClaimTypes.NameIdentifier, AdminEmail),
 new Claim(ClaimTypes.Name, "Beheerder"),
 new Claim(ClaimTypes.Email, AdminEmail),
 new Claim(ClaimTypes.Role, "Admin")
 };
 var identity = new ClaimsIdentity(claims, authenticationType: "AdminScheme");
 var principal = new ClaimsPrincipal(identity);
 var props = new AuthenticationProperties
 {
 IsPersistent = RememberMe,
 ExpiresUtc = RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8),
 AllowRefresh = true
 };
 await HttpContext.SignInAsync("AdminScheme", principal, props);
 return LocalRedirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/admin" : ReturnUrl);
 }
 }
}
