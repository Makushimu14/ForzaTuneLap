using CommunityToolkit.Mvvm.ComponentModel;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>
/// Écran provisoire des voitures : affiche le nombre de voitures, de réglages et de chronos pour vérifier
/// que la base de données est bien accessible. La liste des voitures et de leurs réglages viendra ensuite.
/// </summary>
public partial class VoituresViewModel : ObservableObject
{
    private readonly FabriqueUniteDeTravail fabrique;

    public VoituresViewModel(FabriqueUniteDeTravail fabrique)
    {
        this.fabrique = fabrique;
    }

    [ObservableProperty]
    private string resume = string.Empty;

    public async Task ChargerAsync()
    {
        try
        {
            using var travail = fabrique.Creer();
            var contexte = travail.Contexte;

            var voitures = await contexte.Voitures.CountAsync();
            var reglages = await contexte.Reglages.CountAsync(r => !r.EstOrigine);
            var chronos = await contexte.Chronos.CountAsync();

            Resume = string.Format(Texte.Obtenir("Voitures_Resume"), voitures, reglages, chronos);
        }
        catch (Exception exception)
        {
            // Appelée depuis une méthode « async void » : une exception non interceptée arrêterait l'application.
            JournalErreurs.Ecrire(exception, "Chargement de l'écran des voitures");
            Resume = $"{exception.GetType().Name} : {exception.Message}";
        }
    }
}
