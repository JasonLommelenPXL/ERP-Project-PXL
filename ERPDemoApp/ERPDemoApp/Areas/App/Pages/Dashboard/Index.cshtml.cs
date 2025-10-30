using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ERPDemoApp.Areas.App.Pages.Dashboard
{
 public class IndexModel : PageModel
 {
 public string Tenant { get; private set; } = string.Empty;
 public void OnGet(string tenant)
 {
 Tenant = tenant;
 }
 }
}
