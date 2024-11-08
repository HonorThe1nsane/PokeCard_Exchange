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
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        private CancellationTokenSource _cts;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiKey = "1db0128418eb87d76be1e2667c69cb7cef7d77d7";  // Replace with your actual API key
        private const string ProductsApiUrl = "https://www.pricecharting.com/api/products";

        public StartTradeSearch()
        {
            InitializeComponent();
            BindingContext = this;  // Ensures Products binds to CollectionView
            _cts = new CancellationTokenSource();
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _cts?.Cancel();  // Cancel any ongoing requests
            _cts = new CancellationTokenSource();
            string query = e.NewTextValue;

            if (!string.IsNullOrWhiteSpace(query))
            {
                loadingLabel.IsVisible = true;

                try
                {
                    await Task.Delay(500, _cts.Token);  // Debounce search

                    var products = await SearchProducts(query, _cts.Token);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Products.Clear();  // Clear previous results
                        foreach (var product in products)
                        {
                            Products.Add(product);  // Add new products
                        }
                        OnPropertyChanged(nameof(Products));  // Notify UI of changes
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
                loadingLabel.IsVisible = false;
            }
        }

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
        public decimal LoosePrice { get; set; } = 0.0m;
    }

}
