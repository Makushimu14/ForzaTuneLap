using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Paramètres de l'application : langue et unités de mesure.</summary>
public partial class ParametresViewModel : ObservableObject
{
    /// <summary>Grandeurs pour lesquelles plusieurs unités existent.</summary>
    private static readonly TypeDeGrandeur[] GrandeursConfigurables =
    {
        TypeDeGrandeur.Masse,
        TypeDeGrandeur.Puissance,
        TypeDeGrandeur.Couple,
        TypeDeGrandeur.Vitesse,
        TypeDeGrandeur.Distance,
        TypeDeGrandeur.DistanceLongue,
        TypeDeGrandeur.Hauteur,
        TypeDeGrandeur.Pression,
        TypeDeGrandeur.RaideurDeRessort,
        TypeDeGrandeur.Force
    };

    private readonly ServicePreferences preferences;
    private int indexLangue;

    public ParametresViewModel(ServicePreferences preferences)
    {
        this.preferences = preferences;

        // Même ordre que l'énumération ChoixDeLangue : l'index du sélecteur est la valeur de l'énumération.
        Langues = new[]
        {
            Texte.Obtenir("Langue_Systeme"),
            Texte.Obtenir("Langue_Francais"),
            Texte.Obtenir("Langue_Anglais")
        };
        indexLangue = (int)preferences.Langue;

        Unites = GrandeursConfigurables.Select(grandeur => new ChoixUniteViewModel(grandeur, preferences)).ToList();
    }

    /// <summary>Levé après un changement de langue : la page demande alors à l'application de reconstruire son interface.</summary>
    public event EventHandler? LangueModifiee;

    public IReadOnlyList<string> Langues { get; }

    public IReadOnlyList<ChoixUniteViewModel> Unites { get; }

    public int IndexLangue
    {
        get => indexLangue;
        set
        {
            if (value < 0 || value >= Langues.Count)
            {
                OnPropertyChanged();
                return;
            }

            if (value == indexLangue)
            {
                return;
            }

            indexLangue = value;
            preferences.DefinirLangue((ChoixDeLangue)value);
            preferences.AppliquerLangue();
            OnPropertyChanged();
            LangueModifiee?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void UtiliserMetrique()
    {
        preferences.UtiliserSystemeMetrique();
        ActualiserUnites();
    }

    [RelayCommand]
    private void UtiliserImperial()
    {
        preferences.UtiliserSystemeImperial();
        ActualiserUnites();
    }

    private void ActualiserUnites()
    {
        foreach (var choix in Unites)
        {
            choix.Actualiser();
        }
    }
}
