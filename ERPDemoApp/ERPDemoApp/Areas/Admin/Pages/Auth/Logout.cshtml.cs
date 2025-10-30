using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ERPDemoApp.Areas.Admin.Pages.Auth
{
 public class LogoutModel : PageModel
 {
 public async Task<IActionResult> OnGet()
 {
 await HttpContext.SignOutAsync("AdminScheme");
 return RedirectToPage("/Auth/Login", new { area = "Admin" });
 }
 }
}
