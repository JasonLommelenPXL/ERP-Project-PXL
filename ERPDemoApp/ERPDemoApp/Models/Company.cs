namespace ERPDemoApp.Models;

public class Company
{
 public int Id { get; set; }
 public string CompanyName { get; set; } = string.Empty;
 public string VatNumber { get; set; } = string.Empty;
 public string Country { get; set; } = string.Empty;
 public string? Province { get; set; }
 public string PostalCode { get; set; } = string.Empty;
 public string City { get; set; } = string.Empty;
 public string Street { get; set; } = string.Empty;
 public string Number { get; set; } = string.Empty;
 public string? Unit { get; set; }

 // Admin fields
 public string Status { get; set; } = "Actief"; // Actief | In review | Geblokkeerd
 public string Plan { get; set; } = "Starter"; // Starter | Pro | Enterprise
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
