using CommunityToolkit.Mvvm.ComponentModel;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une ligne de l'écran des paramètres : le choix de l'unité d'une grandeur (poids, vitesse…).</summary>
public sealed class ChoixUniteViewModel : ObservableObject
{
    private readonly TypeDeGrandeur grandeur;
    private readonly ServicePreferences preferences;
    private readonly IReadOnlyList<Unite> unites;
    private int indexUnite;

    public ChoixUniteViewModel(TypeDeGrandeur grandeur, ServicePreferences preferences)
    {
        this.grandeur = grandeur;
        this.preferences = preferences;
        unites = CatalogueUnites.ObtenirUnites(grandeur);

        Libelle = Texte.Obtenir($"Grandeur_{grandeur}");
        Symboles = unites.Select(unite => unite.Symbole).ToList();
        indexUnite = IndexDeLUniteChoisie();
    }

    public string Libelle { get; }

    public IReadOnlyList<string> Symboles { get; }

    public int IndexUnite
    {
        get => indexUnite;
        set
        {
            if (value < 0 || value >= unites.Count)
            {
                // Le sélecteur a été vidé (changement de liste) : on lui redonne le choix actuel.
                OnPropertyChanged();
                return;
            }

            if (value == indexUnite)
            {
                return;
            }

            indexUnite = value;
            preferences.DefinirUnite(grandeur, unites[value]);
            OnPropertyChanged();
        }
    }

    /// <summary>Relit le choix depuis les préférences (après « Tout en métrique » ou « Tout en impérial »).</summary>
    public void Actualiser()
    {
        indexUnite = IndexDeLUniteChoisie();
        OnPropertyChanged(nameof(IndexUnite));
    }

    private int IndexDeLUniteChoisie()
    {
        var cle = preferences.Unites.ObtenirUnite(grandeur).Cle;
        return unites.ToList().FindIndex(unite => unite.Cle == cle);
    }
}
