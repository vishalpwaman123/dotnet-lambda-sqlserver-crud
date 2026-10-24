using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductsApi.Data;
using ProductsApi.Repositories;

namespace ProductsApi;

/// <summary>
/// Sets up dependency injection. Lambda Annotations runs this once when the function starts.
/// The services registered here are passed into the Functions class constructor.
/// </summary>
[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    /// <summary>Registers the EF Core database context and our repository.</summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Read the SQL Server connection string from an environment variable (set in serverless.template).
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The SQL_CONNECTION_STRING environment variable is not set.");
        }

        // Tell EF Core to use SQL Server.
        // EnableRetryOnFailure retries automatically if the connection briefly drops.
        services.AddDbContext<ProductsDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // Scoped = a new object for each Lambda call.
        // Lambda Annotations registers Functions as a singleton by default, which would keep
        // the same repository and DbContext for every call. Registering it as scoped here
        // replaces that, so every call gets a fresh Functions, repository and DbContext.
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<Functions>();
    }
}
