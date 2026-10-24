using ProductsApi.Models;

namespace ProductsApi.Repositories;

/// <summary>
/// Describes what we can do with products in the database.
/// The Lambda functions only talk to this interface, not to SQL Server directly.
/// </summary>
public interface IProductRepository
{
    /// <summary>Inserts a new product.</summary>
    Task AddAsync(Product product);

    /// <summary>Saves changes to a product that was loaded with GetByIdAsync.</summary>
    Task UpdateAsync(Product product);

    /// <summary>Returns every product in the table.</summary>
    Task<List<Product>> GetAllAsync();

    /// <summary>Returns one product, or null if it does not exist.</summary>
    Task<Product?> GetByIdAsync(string id);

    /// <summary>Deletes a product by its Id. Does nothing if it does not exist.</summary>
    Task DeleteAsync(string id);
}
