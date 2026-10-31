using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageEditionReglage : ContentPage
{
    private readonly EditionReglageViewModel viewModel;

    public PageEditionReglage(EditionReglageViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.ChargerAsync();
        await viewModel.ActualiserChronosAsync();
    }
}
