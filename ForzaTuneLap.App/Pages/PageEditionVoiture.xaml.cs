using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageEditionVoiture : ContentPage
{
    private readonly EditionVoitureViewModel viewModel;

    public PageEditionVoiture(EditionVoitureViewModel viewModel)
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
