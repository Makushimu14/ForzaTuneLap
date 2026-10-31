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

        // La navigation est surveillée tant que la page est affichée : ouvrir un chrono (la page disparaît) la libère,
        // et le retour la reprend.
        Shell.Current.Navigating += SurNavigation;

        await viewModel.ChargerAsync();
        await viewModel.ActualiserChronosAsync();
    }

    protected override void OnDisappearing()
    {
        Shell.Current.Navigating -= SurNavigation;
        base.OnDisappearing();
    }

    /// <summary>
    /// Quitter la page (retour, changement d'onglet) avec des modifications non enregistrées demande confirmation.
    /// Ouvrir une autre page par-dessus (un chrono à ajouter) n'est pas un départ : l'éditeur reste en mémoire.
    /// </summary>
    private async void SurNavigation(object? expediteur, ShellNavigatingEventArgs e)
    {
        var quitte = e.Source is ShellNavigationSource.Pop
            or ShellNavigationSource.PopToRoot
            or ShellNavigationSource.ShellItemChanged
            or ShellNavigationSource.ShellSectionChanged
            or ShellNavigationSource.ShellContentChanged;
        if (!quitte || !e.CanCancel)
        {
            return;
        }

        // Le report doit être demandé avant le premier « await » : la navigation attend alors la réponse de l'utilisateur.
        var report = e.GetDeferral();
        try
        {
            if (!await viewModel.ConfirmerLeDepartAsync())
            {
                e.Cancel();
            }
        }
        finally
        {
            report.Complete();
        }
    }
}
