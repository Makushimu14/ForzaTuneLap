using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageVoiture : ContentPage
{
    private readonly VoitureViewModel viewModel;

    public PageVoiture(VoitureViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.ChargerAsync();
    }
}
