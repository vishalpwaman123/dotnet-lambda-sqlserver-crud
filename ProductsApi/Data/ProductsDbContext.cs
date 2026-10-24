using Microsoft.EntityFrameworkCore;
using ProductsApi.Models;

namespace ProductsApi.Data;

/// <summary>
/// The Entity Framework Core "database context": our connection to SQL Server.
/// Each DbSet property is one table.
/// </summary>
public class ProductsDbContext : DbContext
{
    /// <summary>The options (like the connection string) are set in Startup.cs.</summary>
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
        : base(options)
    {
    }

    /// <summary>The "Products" table.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Extra mapping rules that can't be written as attributes on the Product class.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQL Server's datetime2 column doesn't remember that a date is UTC.
        // This marks CreatedAt as UTC when reading it, so the JSON ends with "Z" (e.g. "2026-10-24T10:15:30Z").
        modelBuilder.Entity<Product>()
            .Property(p => p.CreatedAt)
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
