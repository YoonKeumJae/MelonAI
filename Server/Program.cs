var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast")
// .WithOpenApi();

app.MapPost("/products", async (NewProduct product, ProductService service) =>
{
    var addedProduct = await service.AddProduct(product);
    return addedProduct;
});

app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }



record NewProduct(string Title, decimal Price, string Discription, string Image, string Category);

internal class ProductService()
{
    readonly HttpClient client = new HttpClient();
    public async Task<NewProduct> AddProduct(NewProduct product)
    {
        var URI = "https://fakestoreapi.com/products";
        var item = JsonContent.Create(product);
        var response = await client.PostAsync(URI, item);
        response.EnsureSuccessStatusCode();
        var addedProduct = await response.Content.ReadFromJsonAsync<NewProduct>();
        return addedProduct;
    }
}