using ForzaTuneLap.App.ViewModels;

namespace ForzaTuneLap.App.Pages;

public partial class PageParametres : ContentPage
{
    /// <summary>Position de l'onglet des paramètres dans la coquille.</summary>
    private const int OngletParametres = 3;

    public PageParametres(ParametresViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Les textes sont traduits à la construction des pages : un changement de langue reconstruit toute l'interface.
        viewModel.LangueModifiee += (_, _) => ((App)Application.Current!).RecreerApplication(OngletParametres);
    }
}
