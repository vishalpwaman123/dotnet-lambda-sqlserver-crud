using System.Net;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using ProductsApi.Models;
using ProductsApi.Repositories;

// Tells Lambda how to convert JSON to C# objects and back (using camelCase names like "name", "price").
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.CamelCaseLambdaJsonSerializer))]

namespace ProductsApi;

/// <summary>
/// All 5 CRUD Lambda functions for products.
/// Each method with [LambdaFunction] becomes its own Lambda function in AWS.
/// [HttpApi] connects the method to a URL in API Gateway.
/// </summary>
public class Functions
{
    // The permissions each Lambda function gets: write logs, and (optionally) run inside a VPC
    // so it can reach a private SQL Server. See "If your SQL Server is private" in README.md.
    private const string Policies = "AWSLambdaBasicExecutionRole, AWSLambdaVPCAccessExecutionRole";

    private readonly IProductRepository _repository;

    /// <summary>The repository is given to us by dependency injection (see Startup.cs).</summary>
    public Functions(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>POST /products — creates a new product and returns 201 with it.</summary>
    [LambdaFunction(ResourceName = "CreateProductFunction", Policies = Policies, MemorySize = 512, Timeout = 30)]
    [HttpApi(LambdaHttpMethod.Post, "/products")]
    public async Task<IHttpResult> CreateProduct([FromBody] Product product, ILambdaContext context)
    {
        var error = Validate(product);
        if (error != null)
        {
            return HttpResults.BadRequest(new { message = error });
        }

        // The server sets Id and CreatedAt, so we ignore any values sent by the client.
        product.Id = Guid.NewGuid().ToString();
        product.CreatedAt = DateTime.UtcNow;

        await _repository.AddAsync(product);
        context.Logger.LogInformation($"Created product {product.Id}");

        return HttpResults.Created($"/products/{product.Id}", product);
    }

    /// <summary>GET /products — returns all products.</summary>
    [LambdaFunction(ResourceName = "GetProductsFunction", Policies = Policies, MemorySize = 512, Timeout = 30)]
    [HttpApi(LambdaHttpMethod.Get, "/products")]
    public async Task<IHttpResult> GetProducts()
    {
        var products = await _repository.GetAllAsync();
        return HttpResults.Ok(products);
    }

    /// <summary>GET /products/{id} — returns one product, or 404 if not found.</summary>
    [LambdaFunction(ResourceName = "GetProductFunction", Policies = Policies, MemorySize = 512, Timeout = 30)]
    [HttpApi(LambdaHttpMethod.Get, "/products/{id}")]
    public async Task<IHttpResult> GetProduct(string id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
        {
            return HttpResults.NotFound(new { message = $"Product '{id}' was not found." });
        }

        return HttpResults.Ok(product);
    }

    /// <summary>PUT /products/{id} — updates Name and Price of a product, or 404 if not found.</summary>
    [LambdaFunction(ResourceName = "UpdateProductFunction", Policies = Policies, MemorySize = 512, Timeout = 30)]
    [HttpApi(LambdaHttpMethod.Put, "/products/{id}")]
    public async Task<IHttpResult> UpdateProduct(string id, [FromBody] Product changes, ILambdaContext context)
    {
        var error = Validate(changes);
        if (error != null)
        {
            return HttpResults.BadRequest(new { message = error });
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return HttpResults.NotFound(new { message = $"Product '{id}' was not found." });
        }

        // Only Name and Price can change. Id and CreatedAt stay the same.
        existing.Name = changes.Name;
        existing.Price = changes.Price;

        await _repository.UpdateAsync(existing);
        context.Logger.LogInformation($"Updated product {id}");

        return HttpResults.Ok(existing);
    }

    /// <summary>DELETE /products/{id} — deletes a product and returns 204 (no content).</summary>
    [LambdaFunction(ResourceName = "DeleteProductFunction", Policies = Policies, MemorySize = 512, Timeout = 30)]
    [HttpApi(LambdaHttpMethod.Delete, "/products/{id}")]
    public async Task<IHttpResult> DeleteProduct(string id, ILambdaContext context)
    {
        await _repository.DeleteAsync(id);
        context.Logger.LogInformation($"Deleted product {id}");

        return HttpResults.NewResult(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Checks the product sent by the client.
    /// Returns an error message if something is wrong, or null if everything is OK.
    /// </summary>
    private static string? Validate(Product? product)
    {
        if (product == null)
        {
            return "Request body is missing or is not valid JSON.";
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return "Name is required.";
        }

        if (product.Name.Length > 200)
        {
            return "Name must be 200 characters or fewer.";
        }

        if (product.Price <= 0)
        {
            return "Price must be greater than 0.";
        }

        return null;
    }
}
