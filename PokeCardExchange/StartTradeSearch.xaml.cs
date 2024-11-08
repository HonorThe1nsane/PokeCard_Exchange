using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Text.Json.Serialization;

namespace PokeCardExchange
{
    public partial class StartTradeSearch : ContentPage
    {
        public List<Product> Products { get; set; } = new List<Product>();
        private CancellationTokenSource _cts;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiKey = "1db0128418eb87d76be1e2667c69cb7cef7d77d7";
        private const string ProductsApiUrl = "https://www.pricecharting.com/api/products";
        private const string WwwUrl = "https://www.pricecharting.com"; // Base URL for images

        public StartTradeSearch()
        {
            InitializeComponent();
            BindingContext = this;
            _cts = new CancellationTokenSource();
        }

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

                    // Retrieve products based on the search query
                    var products = await SearchProducts(query, _cts.Token);

                    // Assign image URLs to the products
                    await AssignImageUrls(products);

                    // Update the UI with products and hide the loading label
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

        // New method: Assigns image URLs to products by matching IDs with offers
        private async Task AssignImageUrls(List<Product> products)
        {
            string offersApiUrl = $"https://www.pricecharting.com/api/offers?t={ApiKey}&status=sold";
            HttpResponseMessage response = await _httpClient.GetAsync(offersApiUrl);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var offersResponse = JsonSerializer.Deserialize<ApiOffersResponse>(jsonResponse);

                if (offersResponse?.Offers != null)
                {
                    string defaultImageUrl = "default_image.jpg"; // Replace with a valid placeholder image URL

                    foreach (var product in products)
                    {
                        var offer = offersResponse.Offers.FirstOrDefault(o =>
                                        o.Id.ValueKind == JsonValueKind.String && o.Id.GetString() == product.Id);

                        if (offer != null && !string.IsNullOrEmpty(offer.ImageUrl))
                        {
                            product.ImageUrl = $"{WwwUrl}{offer.ImageUrl}";
                        }
                        else
                        {
                            Console.WriteLine($"No image URL found for product {product.Id}");
                            product.ImageUrl = defaultImageUrl; // Set placeholder image if no URL is found
                        }

                    }
                }
                else
                {
                    Console.WriteLine("No offers found in the offers API response.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve offers: {response.StatusCode}");
            }
        }
    }



    public class ApiOffersResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("offers")]
        public List<Offer> Offers { get; set; } = new List<Offer>();
    }

    public class Offer
    {
        [JsonPropertyName("id")]
        public JsonElement Id { get; set; } // Allows handling of both integer and string types

        [JsonPropertyName("image-url")]
        public string ImageUrl { get; set; } = string.Empty;
        public string GetIdAsString()
        {
            return Id.ValueKind switch
            {
                JsonValueKind.Number => Id.GetInt32().ToString(),
                JsonValueKind.String => Id.GetString() ?? string.Empty,
                _ => string.Empty // Default to an empty string if it's neither a number nor a string
            };
        }


    }



    public class ApiProductsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("products")]
        public List<Product> Products { get; set; } = new List<Product>();
    }
    public class Product
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("product-name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("console-name")]
        public string ConsoleName { get; set; } = string.Empty;

        [JsonPropertyName("loose-price")]
        public int LoosePriceInPennies { get; set; } = 0;  // Store the price in pennies

        public decimal LoosePriceInDollars => LoosePriceInPennies / 100m;

        [JsonPropertyName("image-url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
