using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RenartCaseBackend.Models;
using RenartCaseBackend.Services;
using System.Globalization;

namespace RenartCaseBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly string _jsonPath = Path.Combine("data", "products.json");
    private readonly GoldPriceService _goldService;

    public ProductsController(GoldPriceService goldService)
    {
        _goldService = goldService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minScore)
    {
        if (!System.IO.File.Exists(_jsonPath))
            return NotFound("products.json bulunamadı.");

        var json     = await System.IO.File.ReadAllTextAsync(_jsonPath);
        var options  = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var products = JsonSerializer.Deserialize<List<Product>>(json, options)!;

        var goldPrice = await _goldService.GetGramPriceAsync();
        if (goldPrice is null)
            return StatusCode(500, "Altın fiyatı alınamadı.");

        var result = products
            .Select(p =>
            {
                var rawPrice = ((decimal)p.Weight * goldPrice.Value * (decimal)(p.PopularityScore + 1));
                return new
                {
                    p.Name,
                    p.Images,
                    p.Weight,
                    PopularityScore = (p.PopularityScore * 5).ToString("0.0", CultureInfo.InvariantCulture),
                    PriceValue = rawPrice,
                    Price = rawPrice.ToString("0.00", CultureInfo.InvariantCulture) + " USD"
                };
            })
            .Where(p =>
                (!minPrice.HasValue || p.PriceValue >= minPrice.Value) &&
                (!maxPrice.HasValue || p.PriceValue <= maxPrice.Value) &&
                (!minScore.HasValue || double.Parse(p.PopularityScore, CultureInfo.InvariantCulture) >= (minScore.Value * 5))
            )
            .Select(p => new
            {
                p.Name,
                p.Images,
                p.Weight,
                p.PopularityScore,
                p.Price
            });

        return Ok(result);
    }
}
