using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Un chrono dans la liste : rang dans son parcours, temps, voiture, réglage et date.</summary>
public sealed class LigneChronoViewModel
{
    public LigneChronoViewModel(
        Guid identifiant,
        string rang,
        string duree,
        string voiture,
        string reglage,
        string date,
        string? commentaire,
        Func<LigneChronoViewModel, Task> ouvrir)
    {
        OuvrirCommand = new AsyncRelayCommand(() => ouvrir(this));
        Identifiant = identifiant;
        Rang = rang;
        Duree = duree;
        Voiture = voiture;
        Reglage = reglage;
        Date = date;
        Commentaire = commentaire;
    }

    public Guid Identifiant { get; }

    public IAsyncRelayCommand OuvrirCommand { get; }

    public string Rang { get; }

    public string Duree { get; }

    public string Voiture { get; }

    public string Reglage { get; }

    public string Date { get; }

    public string? Commentaire { get; }

    public bool ACommentaire => !string.IsNullOrWhiteSpace(Commentaire);
}

/// <summary>Les chronos d'un parcours. Le titre est le circuit, le sous-titre le parcours et sa longueur.</summary>
public sealed class GroupeChronosViewModel : ObservableCollection<LigneChronoViewModel>
{
    public GroupeChronosViewModel(string titre, string sousTitre, IEnumerable<LigneChronoViewModel> lignes)
        : base(lignes)
    {
        Titre = titre;
        SousTitre = sousTitre;
    }

    public string Titre { get; }

    public string SousTitre { get; }
}

/// <summary>L'onglet Chronos : tous les chronos regroupés par parcours, du plus rapide au plus lent.</summary>
public partial class ChronosViewModel : ObservableObject
{
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;

    public ChronosViewModel(FabriqueUniteDeTravail fabrique, ServicePreferences preferences, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.preferences = preferences;
        this.dialogues = dialogues;
    }

    [ObservableProperty]
    private IReadOnlyList<GroupeChronosViewModel> groupes = Array.Empty<GroupeChronosViewModel>();

    /// <summary>Recharge la liste. Appelée à chaque affichage de la page, pour refléter les ajouts et corrections.</summary>
    public async Task ChargerAsync()
    {
        try
        {
            IReadOnlyList<GroupeChronosParcours> donnees;
            using (var travail = fabrique.Creer())
            {
                donnees = await travail.Chronos.ListerParParcoursAsync();
            }

            var culture = preferences.CultureDeFormat;
            var uniteLongueur = preferences.Unites.ObtenirUnite(TypeDeGrandeur.DistanceLongue);

            Groupes = donnees
                .Select(groupe =>
                {
                    var longueur = groupe.Parcours.LongueurKilometres > 0
                        ? $" · {uniteLongueur.DepuisCanonique(groupe.Parcours.LongueurKilometres).ToString("N2", culture)} {uniteLongueur.Symbole}"
                        : string.Empty;

                    return new GroupeChronosViewModel(
                        groupe.Parcours.Circuit!.Nom,
                        NomsAffiches.Parcours(groupe.Parcours) + longueur,
                        groupe.Chronos.Select((chrono, index) => new LigneChronoViewModel(
                            chrono.Identifiant,
                            Rang(groupe.Chronos, index),
                            DureesDeTour.Formater(chrono.DureeMillisecondes, culture),
                            $"{chrono.Reglage!.Voiture!.Annee.ToString(CultureInfo.InvariantCulture)} {chrono.Reglage.Voiture.Marque!.Nom} {chrono.Reglage.Voiture.Nom}",
                            chrono.Reglage.Nom,
                            chrono.DateRealisation.ToString("d", culture),
                            chrono.Commentaire,
                            OuvrirAsync)));
                })
                .ToList();
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Onglet des chronos");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }

    /// <summary>Rang « à la compétition » : deux temps identiques partagent le même rang.</summary>
    private static string Rang(IReadOnlyList<Core.Modeles.Chrono> chronos, int index) =>
        (1 + chronos.Count(autre => autre.DureeMillisecondes < chronos[index].DureeMillisecondes))
            .ToString(CultureInfo.InvariantCulture);

    [RelayCommand]
    private async Task AjouterAsync() => await Naviguer("chrono");

    private async Task OuvrirAsync(LigneChronoViewModel ligne) =>
        await Naviguer($"chrono?id={ligne.Identifiant}");

    private async Task Naviguer(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Navigation vers un chrono");
            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
