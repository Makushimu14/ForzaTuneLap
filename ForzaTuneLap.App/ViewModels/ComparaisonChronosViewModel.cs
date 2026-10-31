using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une ligne du classement : son numéro, le temps, l'écart avec le meilleur, la voiture, le réglage et les critères.</summary>
public sealed class LigneComparaisonChronoViewModel
{
    public LigneComparaisonChronoViewModel(
        int numero,
        string duree,
        string ecart,
        string voiture,
        string reglage,
        string criteres,
        Func<LigneComparaisonChronoViewModel, Task> ouvrir,
        Guid identifiantChrono)
    {
        Numero = numero.ToString(CultureInfo.InvariantCulture);
        Duree = duree;
        Ecart = ecart;
        Voiture = voiture;
        Reglage = reglage;
        Criteres = criteres;
        IdentifiantChrono = identifiantChrono;
        OuvrirCommand = new AsyncRelayCommand(() => ouvrir(this));
    }

    public Guid IdentifiantChrono { get; }

    /// <summary>Numéro de la ligne dans le classement, à partir de 1.</summary>
    public string Numero { get; }

    public string Duree { get; }

    /// <summary>« +1,234 s (+1,3 %) », vide pour le meilleur temps.</summary>
    public string Ecart { get; }

    public bool AUnEcart => Ecart.Length > 0;

    public string Voiture { get; }

    public string Reglage { get; }

    /// <summary>Les valeurs des critères ajoutés aux filtres, par exemple « Puissance : 450 kW · Poids : 1 400 kg ».</summary>
    public string Criteres { get; }

    public bool ACriteres => Criteres.Length > 0;

    public IAsyncRelayCommand OuvrirCommand { get; }
}

/// <summary>
/// Comparaison des chronos d'un parcours précis, avec des filtres sur la voiture et son réglage. Le classement est
/// recalculé à chaque changement du parcours ou d'un filtre.
/// </summary>
public partial class ComparaisonChronosViewModel : ObservableObject
{
    private static readonly TimeSpan Delai = TimeSpan.FromMilliseconds(300);

    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;

    private CancellationTokenSource? attente;
    private int version;
    private bool charge;

    public ComparaisonChronosViewModel(
        FabriqueUniteDeTravail fabrique,
        ServicePreferences preferences,
        IDialogues dialogues,
        FiltreReglagesViewModel filtre)
    {
        this.fabrique = fabrique;
        this.preferences = preferences;
        this.dialogues = dialogues;

        Filtre = filtre;
        Filtre.Modifie += (_, _) => _ = RecalculerApresDelaiAsync();

        resume = Texte.Obtenir("Comparaison_ChoisirParcours");
    }

    public FiltreReglagesViewModel Filtre { get; }

    [ObservableProperty]
    private IReadOnlyList<OptionCircuit> circuits = Array.Empty<OptionCircuit>();

    [ObservableProperty]
    private OptionCircuit? circuitSelectionne;

    [ObservableProperty]
    private IReadOnlyList<OptionParcours> parcours = Array.Empty<OptionParcours>();

    [ObservableProperty]
    private OptionParcours? parcoursSelectionne;

    [ObservableProperty]
    private IReadOnlyList<LigneComparaisonChronoViewModel> lignes = Array.Empty<LigneComparaisonChronoViewModel>();

    [ObservableProperty]
    private string resume;

    partial void OnCircuitSelectionneChanged(OptionCircuit? value)
    {
        Parcours = value?.Parcours ?? Array.Empty<OptionParcours>();
        ParcoursSelectionne = Parcours.Count == 1 ? Parcours[0] : null;
    }

    partial void OnParcoursSelectionneChanged(OptionParcours? value) => _ = RecalculerApresDelaiAsync();

    /// <summary>Charge les circuits et les options des filtres une fois, puis recalcule le classement à chaque affichage.</summary>
    public async Task ChargerAsync()
    {
        try
        {
            if (!charge)
            {
                using (var travail = fabrique.Creer())
                {
                    Circuits = await OptionsDeParcours.ChargerAsync(travail);
                }

                charge = true;
            }

            await Filtre.ChargerOptionsAsync();
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Comparaison des chronos : options");
            Resume = MessagesErreurs.Obtenir(exception);
            return;
        }

        // Les chronos ont pu changer depuis le dernier affichage (ajout, correction, suppression).
        await RecalculerAsync();
    }

    private async Task RecalculerApresDelaiAsync()
    {
        attente?.Cancel();
        var source = attente = new CancellationTokenSource();

        try
        {
            await Task.Delay(Delai, source.Token);
        }
        catch (TaskCanceledException)
        {
            return; // un changement plus récent a pris le relais
        }

        await RecalculerAsync();
    }

    private async Task RecalculerAsync()
    {
        var numero = ++version;

        if (ParcoursSelectionne is not { } choisi)
        {
            Lignes = Array.Empty<LigneComparaisonChronoViewModel>();
            Resume = Texte.Obtenir("Comparaison_ChoisirParcours");
            return;
        }

        if (!Filtre.Editeur.EstValide)
        {
            Resume = Texte.Obtenir("Filtre_Invalide");
            return;
        }

        try
        {
            var filtre = Filtre.Editeur.ConstruireFiltreChronos();
            var criteres = Filtre.Editeur.Criteres.Where(c => c.EstValide).Select(c => c.Critere).ToList();

            ResultatComparaisonChronos resultat;
            using (var travail = fabrique.Creer())
            {
                resultat = await travail.ComparaisonChronos.ComparerAsync(choisi.Identifiant, filtre);
            }

            if (numero != version)
            {
                return; // un calcul plus récent est en cours : ce résultat est périmé
            }

            var culture = preferences.CultureDeFormat;
            Lignes = resultat.Lignes
                .Select((ligne, index) => new LigneComparaisonChronoViewModel(
                    index + 1,
                    DureesDeTour.Formater(ligne.Chrono.DureeMillisecondes, culture),
                    DecrireEcart(ligne, culture),
                    $"{ligne.Voiture.Annee.ToString(CultureInfo.InvariantCulture)} {ligne.Voiture.Marque!.Nom} {ligne.Voiture.Nom}",
                    ligne.Reglage.Nom,
                    DecrireCriteres(ligne, criteres, culture),
                    OuvrirAsync,
                    ligne.Chrono.Identifiant))
                .ToList();

            Resume = resultat.Lignes.Count == 0
                ? Texte.Obtenir("Comparaison_AucunChrono")
                : string.Format(Texte.Obtenir("Comparaison_NombreDeChronos"), resultat.Lignes.Count);
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Comparaison des chronos");
            }

            Resume = MessagesErreurs.Obtenir(exception);
        }
    }

    private static string DecrireEcart(LigneComparaisonChrono ligne, CultureInfo culture)
    {
        var ecart = DureesDeTour.FormaterEcart(ligne.EcartMillisecondes, culture);
        return ecart.Length == 0
            ? string.Empty
            : $"{ecart} (+{ligne.EcartPourcentage.ToString("0.0", culture)} %)";
    }

    /// <summary>La valeur de chaque critère ajouté aux filtres, dans l'unité choisie. « — » si le réglage n'a pas cette section.</summary>
    private string DecrireCriteres(LigneComparaisonChrono ligne, IReadOnlyList<DescriptionCritere> criteres, CultureInfo culture)
    {
        var morceaux = new List<string>(criteres.Count);
        foreach (var critere in criteres)
        {
            var libelle = ServiceLibelles.Obtenir(critere);
            if (critere.Lire(ligne.Reglage) is not { } valeur)
            {
                morceaux.Add($"{libelle} : —");
                continue;
            }

            var unite = preferences.Unites.ObtenirUnite(critere.Grandeur);
            var texte = Nombres.Formater(unite.DepuisCanonique(valeur), culture);
            morceaux.Add(unite.Symbole.Length > 0 ? $"{libelle} : {texte} {unite.Symbole}" : $"{libelle} : {texte}");
        }

        return string.Join(" · ", morceaux);
    }

    private async Task OuvrirAsync(LigneComparaisonChronoViewModel ligne)
    {
        try
        {
            await Shell.Current.GoToAsync($"chrono?id={ligne.IdentifiantChrono}");
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Ouverture d'un chrono depuis la comparaison");
            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
