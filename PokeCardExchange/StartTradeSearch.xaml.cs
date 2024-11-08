using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace PokeCardExchange;

public partial class StartTradeSearch : ContentPage
{
    public List<Product> Products { get; set; } = new List<Product>();
    private CancellationTokenSource _cts;
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string ApiKey = "1db0128418eb87d76be1e2667c69cb7cef7d77d7";  // Replace with your actual API key
    private const string ProductsApiUrl = "https://www.pricecharting.com/api/product";
    private const string OffersApiUrl = "https://www.pricecharting.com/api/offer-details";

    public StartTradeSearch()
    {
        InitializeComponent();
        BindingContext = this;
        _cts = new CancellationTokenSource();
    }

    // Main search logic
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _cts?.Cancel();  // Cancel previous search if it's still running
        _cts = new CancellationTokenSource();

        string query = e.NewTextValue;

        if (!string.IsNullOrWhiteSpace(query))
        {
            loadingLabel.IsVisible = true;

            try
            {
                // Debounce the search to avoid spamming requests
                await Task.Delay(500, _cts.Token);

                var products = await SearchProducts(query, _cts.Token);
                
                // Pull offer data (including images) for each product
                foreach (var product in products)
                {
                    var offerDetails = await GetOfferDetails(product.Id);
                    product.ImageUrl = offerDetails?.ImageUrl;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Products = products;
                    resultsListView.ItemsSource = Products;
                    loadingLabel.IsVisible = false;
                });
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Search operation was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Products.Clear();
            resultsListView.ItemsSource = Products;
            loadingLabel.IsVisible = false;
        }
    }

    // Product search API call
    private async Task<List<Product>> SearchProducts(string query, CancellationToken token)
    {
        string requestUrl = $"{ProductsApiUrl}?t={ApiKey}&q={Uri.EscapeDataString(query)}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, token);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Product API Response: {jsonResponse}");

                var apiResponse = JsonSerializer.Deserialize<ApiProductsResponse>(jsonResponse);
                return apiResponse?.Products ?? new List<Product>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Product search error: {ex.Message}");
        }

        return new List<Product>();
    }

    // Offer details API call to retrieve image URLs
    private async Task<OfferDetails> GetOfferDetails(string productId)
    {
        string requestUrl = $"{OffersApiUrl}?t={ApiKey}&offer-id={productId}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Offer Details API Response: {jsonResponse}");

                var offerDetails = JsonSerializer.Deserialize<OfferDetails>(jsonResponse);
                return offerDetails;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Offer details error: {ex.Message}");
        }

        return null;
    }
}

// Models
public class ApiProductsResponse
{
    public string Status { get; set; }
    public List<Product> Products { get; set; }
}

public class Product
{
    public string Id { get; set; }
    public string ProductName { get; set; }
    public string ConsoleName { get; set; }
    public decimal LoosePrice { get; set; }
    public string ImageUrl { get; set; }
}

public class OfferDetails
{
    public string OfferId { get; set; }
    public string ImageUrl { get; set; }
}
