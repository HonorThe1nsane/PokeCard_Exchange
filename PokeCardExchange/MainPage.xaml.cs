
namespace PokeCardExchange;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnStartSearchClicked(object sender, EventArgs e)
    {
        // Navigate to the search page
        await Navigation.PushAsync(new StartTradeSearch());
    }
}