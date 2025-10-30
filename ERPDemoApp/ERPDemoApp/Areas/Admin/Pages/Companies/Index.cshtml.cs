using ERPDemoApp.Data;
using ERPDemoApp.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ERPDemoApp.Areas.Admin.Pages.Companies
{
 public class IndexModel : PageModel
 {
 private readonly AppDbContext _db;
 public IndexModel(AppDbContext db) => _db = db;

 public string? q { get; set; }
 public string? country { get; set; }
 public string? status { get; set; }
 public string? plan { get; set; }
 public int page { get; set; } =1;
 public int PageSize { get; } =20;
 public int Total { get; private set; }
 public List<Company> Items { get; private set; } = new();

 public async Task OnGetAsync(string? q, string? country, string? status, string? plan, int page =1)
 {
 this.q = q; this.country = country; this.status = status; this.plan = plan; this.page = page;
 var qry = _db.Companies.AsQueryable();
 if (!string.IsNullOrWhiteSpace(q))
 {
 var term = q.Trim();
 qry = qry.Where(c => c.CompanyName.Contains(term) || c.VatNumber.Contains(term) || c.City.Contains(term));
 }
 if (!string.IsNullOrWhiteSpace(country)) qry = qry.Where(c => c.Country == country);
 if (!string.IsNullOrWhiteSpace(status)) qry = qry.Where(c => c.Status == status);
 if (!string.IsNullOrWhiteSpace(plan)) qry = qry.Where(c => c.Plan == plan);
 Total = await qry.CountAsync();
 Items = await qry.OrderByDescending(c => c.CreatedAt)
 .Skip((page -1) * PageSize)
 .Take(PageSize)
 .ToListAsync();
 }
 }
}
