using System.Net.Http.Headers;
using System.Text.Json;

namespace RenartCaseBackend.Services;

public class GoldPriceService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GoldPriceService(HttpClient http, IConfiguration config)
    {
        _http   = http;
        _config = config;
    }

    public async Task<decimal?> GetGramPriceAsync()
    {
        var url   = _config["GoldApi:Url"];
        var key   = _config["GoldApi:Key"];
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)) return null;

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("x-access-token", key);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("price_gram_24k").GetDecimal();
    }
}
