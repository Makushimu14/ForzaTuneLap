using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageVoitures : ContentPage
{
    private readonly VoituresViewModel viewModel;

    public PageVoitures(VoituresViewModel viewModel)
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
