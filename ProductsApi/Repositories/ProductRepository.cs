using Microsoft.EntityFrameworkCore;
using ProductsApi.Data;
using ProductsApi.Models;

namespace ProductsApi.Repositories;

/// <summary>
/// Reads and writes products in SQL Server using Entity Framework Core.
/// EF Core turns these C# calls into SQL statements for us.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductsDbContext _db;

    /// <summary>The ProductsDbContext is given to us by dependency injection (see Startup.cs).</summary>
    public ProductRepository(ProductsDbContext db)
    {
        _db = db;
    }

    /// <summary>Runs an INSERT for the new product.</summary>
    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    /// <summary>Runs an UPDATE that saves the product's current values.</summary>
    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    /// <summary>Runs SELECT * FROM Products.</summary>
    public async Task<List<Product>> GetAllAsync()
    {
        // AsNoTracking: we only read these products, so EF Core doesn't need to watch them for changes.
        return await _db.Products.AsNoTracking().ToListAsync();
    }

    /// <summary>Finds one product by its primary key. Returns null if it is not found.</summary>
    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _db.Products.FindAsync(id);
    }

    /// <summary>Runs DELETE FROM Products WHERE Id = @id.</summary>
    public async Task DeleteAsync(string id)
    {
        await _db.Products.Where(p => p.Id == id).ExecuteDeleteAsync();
    }
}
