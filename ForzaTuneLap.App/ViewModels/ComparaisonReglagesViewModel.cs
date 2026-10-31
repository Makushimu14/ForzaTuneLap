using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une ligne du tableau comparatif : son numéro, la valeur de tri, l'écart avec la première ligne, la voiture et le réglage.</summary>
public sealed class LigneComparaisonReglageViewModel
{
    public LigneComparaisonReglageViewModel(
        int numero,
        string valeur,
        string ecart,
        string voiture,
        string reglage,
        string criteres,
        Guid identifiantReglage,
        Func<LigneComparaisonReglageViewModel, Task> ouvrir)
    {
        Numero = numero.ToString(CultureInfo.InvariantCulture);
        Valeur = valeur;
        Ecart = ecart;
        Voiture = voiture;
        Reglage = reglage;
        Criteres = criteres;
        IdentifiantReglage = identifiantReglage;
        OuvrirCommand = new AsyncRelayCommand(() => ouvrir(this));
    }

    public Guid IdentifiantReglage { get; }

    /// <summary>Numéro de la ligne dans le tableau, à partir de 1.</summary>
    public string Numero { get; }

    /// <summary>La valeur du critère de tri, dans l'unité choisie (« — » si le réglage n'a pas cette valeur).</summary>
    public string Valeur { get; }

    /// <summary>« +12,5 kW (+3,1 %) » par rapport à la première ligne, vide pour elle et pour les lignes sans valeur.</summary>
    public string Ecart { get; }

    public bool AUnEcart => Ecart.Length > 0;

    public string Voiture { get; }

    public string Reglage { get; }

    /// <summary>Les valeurs des critères ajoutés aux filtres, par exemple « Poids : 1 400 kg · Couple : 520 N·m ».</summary>
    public string Criteres { get; }

    public bool ACriteres => Criteres.Length > 0;

    public IAsyncRelayCommand OuvrirCommand { get; }
}

/// <summary>
/// Comparaison de tous les réglages (d'origine et créés, toutes voitures) qui respectent les filtres, classés sur un
/// critère au choix. Les critères ajoutés aux filtres s'affichent aussi sur chaque ligne.
/// </summary>
public partial class ComparaisonReglagesViewModel : ObservableObject
{
    private static readonly TimeSpan Delai = TimeSpan.FromMilliseconds(300);

    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;

    private CancellationTokenSource? attente;
    private int version;

    public ComparaisonReglagesViewModel(
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

        CriteresDeTri = CatalogueCriteres.Tous
            .Select(c => $"{ServiceLibelles.Obtenir(c.Categorie)} · {ServiceLibelles.Obtenir(c)}")
            .ToList();

        // Même ordre que l'énumération SensDuTri : l'index du sélecteur est la valeur de l'énumération.
        Ordres = new[] { Texte.Obtenir("Tri_Croissant"), Texte.Obtenir("Tri_Decroissant") };

        // Au départ, le classement se fait sur l'indice de performance.
        var depart = CatalogueCriteres.Tous.ToList().FindIndex(c => c.Cle == "Generaux.IndiceDePerformance");
        indexTri = Math.Max(0, depart);
        indexOrdre = (int)SensParDefaut(CatalogueCriteres.Tous[indexTri]);

        resume = string.Empty;
    }

    public FiltreReglagesViewModel Filtre { get; }

    public IReadOnlyList<string> CriteresDeTri { get; }

    public IReadOnlyList<string> Ordres { get; }

    [ObservableProperty]
    private int indexTri;

    [ObservableProperty]
    private int indexOrdre;

    [ObservableProperty]
    private IReadOnlyList<LigneComparaisonReglageViewModel> lignes = Array.Empty<LigneComparaisonReglageViewModel>();

    [ObservableProperty]
    private string resume;

    /// <summary>Changer de critère remet le sens le plus utile pour lui (le meilleur d'abord) ; le sens reste modifiable ensuite.</summary>
    partial void OnIndexTriChanged(int value)
    {
        if (value >= 0 && value < CatalogueCriteres.Tous.Count)
        {
            IndexOrdre = (int)SensParDefaut(CatalogueCriteres.Tous[value]);
        }

        _ = RecalculerApresDelaiAsync();
    }

    partial void OnIndexOrdreChanged(int value) => _ = RecalculerApresDelaiAsync();

    private static SensDuTri SensParDefaut(DescriptionCritere critere) =>
        critere.Sens == SensPrefere.PlusPetitEstMeilleur ? SensDuTri.Croissant : SensDuTri.Decroissant;

    /// <summary>Charge les options des filtres, puis recalcule le tableau (les réglages ont pu changer depuis le dernier affichage).</summary>
    public async Task ChargerAsync()
    {
        try
        {
            await Filtre.ChargerOptionsAsync();
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Comparaison des réglages : options");
            Resume = MessagesErreurs.Obtenir(exception);
            return;
        }

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

        if (IndexTri < 0 || IndexTri >= CatalogueCriteres.Tous.Count)
        {
            return;
        }

        if (!Filtre.Editeur.EstValide)
        {
            Resume = Texte.Obtenir("Filtre_Invalide");
            return;
        }

        try
        {
            var critereDeTri = CatalogueCriteres.Tous[IndexTri];
            var filtre = Filtre.Editeur.ConstruireFiltreReglages();
            var affiches = Filtre.Editeur.Criteres.Where(c => c.EstValide).Select(c => c.Critere.Cle).ToList();

            ResultatComparaisonReglages resultat;
            using (var travail = fabrique.Creer())
            {
                resultat = await travail.ComparaisonReglages.ComparerAsync(
                    critereDeTri.Cle,
                    filtre,
                    affiches,
                    (SensDuTri)IndexOrdre);
            }

            if (numero != version)
            {
                return; // un calcul plus récent est en cours : ce résultat est périmé
            }

            var culture = preferences.CultureDeFormat;
            var unite = preferences.Unites.ObtenirUnite(critereDeTri.Grandeur);
            var autresCriteres = resultat.CriteresAffiches.Where(c => c.Cle != critereDeTri.Cle).ToList();

            Lignes = resultat.Lignes
                .Select((ligne, index) => new LigneComparaisonReglageViewModel(
                    index + 1,
                    Formater(ligne.Valeurs[critereDeTri.Cle], critereDeTri, culture),
                    DecrireEcart(ligne, unite, culture),
                    $"{ligne.Voiture.Annee.ToString(CultureInfo.InvariantCulture)} {ligne.Voiture.Marque!.Nom} {ligne.Voiture.Nom}",
                    ligne.Reglage.Nom,
                    string.Join(" · ", autresCriteres.Select(c => $"{ServiceLibelles.Obtenir(c)} : {Formater(ligne.Valeurs[c.Cle], c, culture)}")),
                    ligne.Reglage.Identifiant,
                    OuvrirAsync))
                .ToList();

            Resume = resultat.Lignes.Count == 0
                ? Texte.Obtenir("Comparaison_AucunReglage")
                : string.Format(Texte.Obtenir("Comparaison_NombreDeReglages"), resultat.Lignes.Count);
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Comparaison des réglages");
            }

            Resume = MessagesErreurs.Obtenir(exception);
        }
    }

    /// <summary>Une valeur canonique dans l'unité choisie, avec son symbole. « — » si le réglage n'a pas la section.</summary>
    private string Formater(double? valeur, DescriptionCritere critere, CultureInfo culture)
    {
        if (valeur is not { } canonique)
        {
            return "—";
        }

        var unite = preferences.Unites.ObtenirUnite(critere.Grandeur);
        var texte = Nombres.Formater(unite.DepuisCanonique(canonique), culture);
        return unite.Symbole.Length > 0 ? $"{texte} {unite.Symbole}" : texte;
    }

    private static string DecrireEcart(LigneComparaisonReglage ligne, Unite unite, CultureInfo culture)
    {
        if (ligne.Ecart is not { } ecart || ecart == 0)
        {
            return string.Empty;
        }

        var texte = unite.DepuisCanonique(ecart).ToString("+0.####;-0.####;0", culture);
        var symbole = unite.Symbole.Length > 0 ? $" {unite.Symbole}" : string.Empty;
        var pourcentage = ligne.EcartPourcentage is { } p ? $" ({p.ToString("+0.0;-0.0;0", culture)} %)" : string.Empty;
        return texte + symbole + pourcentage;
    }

    private async Task OuvrirAsync(LigneComparaisonReglageViewModel ligne)
    {
        try
        {
            await Shell.Current.GoToAsync($"reglage?id={ligne.IdentifiantReglage}");
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Ouverture d'un réglage depuis la comparaison");
            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
