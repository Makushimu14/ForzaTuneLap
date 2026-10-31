using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une voiture et la liste de ses réglages : créer, dupliquer, supprimer. L'édition d'un réglage viendra ensuite.</summary>
public partial class VoitureViewModel : ObservableObject, IQueryAttributable
{
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;
    private int identifiantVoiture;

    public VoitureViewModel(FabriqueUniteDeTravail fabrique, ServicePreferences preferences, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.preferences = preferences;
        this.dialogues = dialogues;

        titre = string.Empty;
        sousTitre = string.Empty;
    }

    [ObservableProperty]
    private string titre;

    [ObservableProperty]
    private string sousTitre;

    [ObservableProperty]
    private IReadOnlyList<LigneReglageViewModel> reglages = Array.Empty<LigneReglageViewModel>();

    [ObservableProperty]
    private IReadOnlyList<LigneChronoCourteViewModel> chronos = Array.Empty<LigneChronoCourteViewModel>();

    /// <summary>La page montre soit les réglages de la voiture, soit ses chronos.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReglagesAffiches))]
    private bool chronosAffiches;

    public bool ReglagesAffiches => !ChronosAffiches;

    [RelayCommand]
    private void AfficherReglages() => ChronosAffiches = false;

    [RelayCommand]
    private void AfficherChronos() => ChronosAffiches = true;

    [RelayCommand]
    private async Task AjouterChronoAsync() =>
        await ExecuterAsync(() => Shell.Current.GoToAsync($"chrono?voiture={identifiantVoiture.ToString(CultureInfo.InvariantCulture)}"));

    private async Task OuvrirChronoAsync(LigneChronoCourteViewModel ligne) =>
        await ExecuterAsync(() => Shell.Current.GoToAsync($"chrono?id={ligne.Identifiant}"));

    /// <summary>Reçoit l'identifiant de la voiture depuis la navigation (« voiture?id=12 »).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valeur)
            && int.TryParse(Convert.ToString(valeur, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var identifiant))
        {
            identifiantVoiture = identifiant;
        }
    }

    public async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();
            var detail = await travail.ListeVoitures.ObtenirDetailAsync(identifiantVoiture);

            Titre = $"{detail.NomMarque} {detail.Voiture.Nom}";
            SousTitre = $"{detail.Voiture.Annee.ToString(CultureInfo.InvariantCulture)} · {ServiceLibelles.Obtenir(detail.Voiture.Division)}";
            Reglages = detail.Reglages
                .Select(ligne => new LigneReglageViewModel(
                    ligne,
                    DecrireReglage(ligne),
                    ligne.EstOrigine ? Texte.Obtenir("Reglage_Origine") : DateLocale(ligne.DateModification),
                    OuvrirAsync,
                    DupliquerAsync,
                    SupprimerAsync))
                .ToList();

            Chronos = LigneChronoCourteViewModel.Creer(
                await travail.Chronos.ListerDeLaVoitureAsync(identifiantVoiture),
                preferences.CultureDeFormat,
                avecReglage: true,
                OuvrirChronoAsync);
        });
    }

    [RelayCommand]
    private async Task NouveauReglageAsync()
    {
        var nom = await dialogues.DemanderTexteAsync(Texte.Obtenir("Reglage_NouveauTitre"), Texte.Obtenir("Reglage_NomMessage"));
        if (string.IsNullOrWhiteSpace(nom))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using (var travail = fabrique.Creer())
            {
                // Pré-rempli avec les valeurs du réglage d'origine de la voiture, s'il existe.
                var brouillon = await travail.Reglages.CreerBrouillonAsync(identifiantVoiture);
                brouillon.Nom = nom;
                await travail.Reglages.EnregistrerNouveauAsync(brouillon);
            }

            await ChargerAsync();
        });
    }

    private async Task OuvrirAsync(LigneReglageViewModel ligne) =>
        await ExecuterAsync(() => Shell.Current.GoToAsync($"reglage?id={ligne.Identifiant}"));

    private async Task DupliquerAsync(LigneReglageViewModel ligne)
    {
        var nom = await dialogues.DemanderTexteAsync(
            Texte.Obtenir("Reglage_DupliquerTitre"),
            Texte.Obtenir("Reglage_NomMessage"),
            string.Format(Texte.Obtenir("Reglage_Copie"), ligne.Nom));
        if (string.IsNullOrWhiteSpace(nom))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using (var travail = fabrique.Creer())
            {
                await travail.Reglages.DupliquerAsync(ligne.Identifiant, nom);
            }

            await ChargerAsync();
        });
    }

    private async Task SupprimerAsync(LigneReglageViewModel ligne)
    {
        await ExecuterAsync(async () =>
        {
            int nombreDeChronos;
            using (var travail = fabrique.Creer())
            {
                nombreDeChronos = await travail.Reglages.CompterChronosAsync(ligne.Identifiant);
            }

            var message = nombreDeChronos > 0
                ? string.Format(Texte.Obtenir("Reglage_ConfirmerSuppressionAvecChronos"), ligne.Nom, nombreDeChronos)
                : string.Format(Texte.Obtenir("Reglage_ConfirmerSuppression"), ligne.Nom);

            if (!await dialogues.ConfirmerAsync(Texte.Obtenir("Reglage_SupprimerTitre"), message))
            {
                return;
            }

            using (var travail = fabrique.Creer())
            {
                await travail.Reglages.SupprimerAsync(ligne.Identifiant);
            }

            await ChargerAsync();
        });
    }

    /// <summary>Indice, puissance (dans l'unité choisie), transmission et nombre de chronos.</summary>
    private string DecrireReglage(LigneReglage ligne)
    {
        var elements = new List<string>();

        if (ligne.IndiceDePerformance > 0)
        {
            elements.Add(IndicesDePerformance.Formater(ligne.IndiceDePerformance));
        }

        if (ligne.Puissance > 0)
        {
            var unite = preferences.Unites.ObtenirUnite(TypeDeGrandeur.Puissance);
            elements.Add($"{unite.DepuisCanonique(ligne.Puissance):N0} {unite.Symbole}");
        }

        elements.Add(ServiceLibelles.Obtenir(ligne.Transmission));

        if (ligne.NombreDeChronos > 0)
        {
            elements.Add(string.Format(Texte.Obtenir("Voitures_NbChronos"), ligne.NombreDeChronos));
        }

        return string.Join(" · ", elements);
    }

    private static string DateLocale(DateTime dateUtc) =>
        DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc).ToLocalTime().ToString("d", CultureInfo.CurrentCulture);

    /// <summary>Exécute une action en transformant toute erreur en message, au lieu d'arrêter l'application.</summary>
    private async Task ExecuterAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Écran d'une voiture");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
