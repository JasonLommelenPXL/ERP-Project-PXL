using ERPDemoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPDemoApp.Data;

public class AppDbContext : DbContext
{
 public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
 public DbSet<Company> Companies => Set<Company>();
}
