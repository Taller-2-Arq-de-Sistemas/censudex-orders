using System.Text.Json;
using CensudexOrders.Models;

namespace CensudexOrders.Data;

/// <summary>
/// Class to seed initial data into the database.
/// </summary>
public class Seed
{
    /// <summary>
    /// Seed the database with initial data from json files.
    /// </summary>
    /// <param name="context">Database context</param>
    public static void SeedData(OrdersContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        SeedUsers(context, options);
        SeedProducts(context, options);
        //SeedOrders(context, options);
        //SeedOrderProducts(context, options);
    }

    /// <summary>
    /// Seed the database with the users in the json file, then save changes in the database.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="options">Options to deserialize json</param>
    private static void SeedUsers(OrdersContext context, JsonSerializerOptions options)
    {
        var result = context.Users?.Any();
        if (result is true or null) return;

        var path = Path.Combine(AppContext.BaseDirectory, "Seeders", "UsersData.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "src", "Data", "Seeders", "UsersData.json");
        }
        var usersData = File.ReadAllText(path);
        var usersList = JsonSerializer.Deserialize<List<User>>(usersData, options) ??
            throw new Exception("UsersData.json is empty");
        context.Users?.AddRange(usersList);
        context.SaveChanges();
    }
    /// <summary>
    /// Seed the database with the products in the json file, then save changes in the database.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="options">Options to deserialize json</param>
    private static void SeedProducts(OrdersContext context, JsonSerializerOptions options)
    {
        var result = context.Products?.Any();
        if (result is true or null) return;

        var path = Path.Combine(AppContext.BaseDirectory, "Seeders", "ProductsData.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "src", "Data", "Seeders", "ProductsData.json");
        }
        var productsData = File.ReadAllText(path);
        var productsList = JsonSerializer.Deserialize<List<Product>>(productsData, options) ??
            throw new Exception("ProductsData.json is empty");
        context.Products?.AddRange(productsList);
        context.SaveChanges();
    }
    /// <summary>
    /// Seed the database with the orders in the json file, then save changes in the database.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="options">Options to deserialize json</param>
    /// <exception cref="Exception"></exception>
    private static void SeedOrders(OrdersContext context, JsonSerializerOptions options)
    {
        var result = context.Orders?.Any();
        if (result is true or null) return;
        var path = Path.Combine(AppContext.BaseDirectory, "Seeders", "OrdersData.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "src", "Data", "Seeders", "OrdersData.json");
        }
        var ordersData = File.ReadAllText(path);
        var ordersList = JsonSerializer.Deserialize<List<Order>>(ordersData, options) ??
            throw new Exception("OrdersData.json is empty");
        context.Orders?.AddRange(ordersList);
        context.SaveChanges();
    }

    /// <summary>
    /// Seed the database with the order products in the json file, then save changes in the database.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="options">Options to deserialize json</param>
    /// <exception cref="Exception"></exception>
    private static void SeedOrderProducts(OrdersContext context, JsonSerializerOptions options)
    {
        var result = context.OrderProducts?.Any();
        if (result is true or null) return;
        var path = Path.Combine(AppContext.BaseDirectory, "Seeders", "OrderProductsData.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "src", "Data", "Seeders", "OrderProductsData.json");
        }
        var orderProductsData = File.ReadAllText(path);
        var orderProductsList = JsonSerializer.Deserialize<List<OrderProducts>>(orderProductsData, options) ??
            throw new Exception("OrderProductsData.json is empty");
        context.OrderProducts?.AddRange(orderProductsList);
        context.SaveChanges();
    }
}