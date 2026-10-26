using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageDemarrage : ContentPage
{
    private readonly DemarrageViewModel viewModel;
    private bool demarre;

    public PageDemarrage(DemarrageViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.Termine += (_, _) => ((App)Application.Current!).OuvrirApplication();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OnAppearing peut être appelé plusieurs fois : le démarrage ne se lance qu'une fois (le bouton « Réessayer » le relance).
        if (demarre)
        {
            return;
        }

        demarre = true;
        await viewModel.DemarrerCommand.ExecuteAsync(null);
    }
}
