using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Liste des voitures du catalogue, avec recherche, filtres et tri. Toucher une voiture ouvre ses réglages.</summary>
public partial class VoituresViewModel : ObservableObject
{
    private static readonly TimeSpan DelaiDeRecherche = TimeSpan.FromMilliseconds(250);

    private readonly FabriqueUniteDeTravail fabrique;

    private IReadOnlyList<OptionMarque> optionsMarques = Array.Empty<OptionMarque>();
    private IReadOnlyList<Division> optionsDivisions = Array.Empty<Division>();

    // État des filtres. Les index des listes déroulantes sont dérivés de ces valeurs quand les options changent.
    private string recherche = string.Empty;
    private int? identifiantMarque;
    private Division? division;
    private int indexMarque;
    private int indexDivision;
    private int indexTri;
    private bool seulementMesReglages;

    private CancellationTokenSource? attente;
    private int versionRecherche;

    public VoituresViewModel(FabriqueUniteDeTravail fabrique)
    {
        this.fabrique = fabrique;

        // Même ordre que l'énumération TriVoitures : l'index du sélecteur est la valeur de l'énumération.
        Tris = new[]
        {
            Texte.Obtenir("Tri_MarqueEtNom"),
            Texte.Obtenir("Tri_AnneeCroissante"),
            Texte.Obtenir("Tri_AnneeDecroissante"),
            Texte.Obtenir("Tri_NombreDeReglages")
        };

        marques = new[] { Texte.Obtenir("Voitures_ToutesMarques") };
        divisions = new[] { Texte.Obtenir("Voitures_ToutesDivisions") };
        resumeResultat = string.Empty;
    }

    public IReadOnlyList<string> Tris { get; }

    [ObservableProperty]
    private IReadOnlyList<LigneVoitureViewModel> voitures = Array.Empty<LigneVoitureViewModel>();

    [ObservableProperty]
    private IReadOnlyList<string> marques;

    [ObservableProperty]
    private IReadOnlyList<string> divisions;

    [ObservableProperty]
    private string resumeResultat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TexteBoutonFiltres))]
    private bool filtresVisibles;

    public string TexteBoutonFiltres =>
        Texte.Obtenir(FiltresVisibles ? "Voitures_MasquerFiltres" : "Voitures_Filtres");

    public string Recherche
    {
        get => recherche;
        set
        {
            value ??= string.Empty;
            if (recherche == value)
            {
                return;
            }

            recherche = value;
            OnPropertyChanged();
            _ = RechargerApresDelaiAsync();
        }
    }

    public int IndexMarque
    {
        get => indexMarque;
        set
        {
            if (value < 0 || value > optionsMarques.Count)
            {
                OnPropertyChanged(); // le sélecteur s'est vidé : on lui redonne le choix actuel
                return;
            }

            if (value == indexMarque)
            {
                return;
            }

            indexMarque = value;
            identifiantMarque = value == 0 ? null : optionsMarques[value - 1].Identifiant;
            OnPropertyChanged();
            _ = RechargerAsync();
        }
    }

    public int IndexDivision
    {
        get => indexDivision;
        set
        {
            if (value < 0 || value > optionsDivisions.Count)
            {
                OnPropertyChanged();
                return;
            }

            if (value == indexDivision)
            {
                return;
            }

            indexDivision = value;
            division = value == 0 ? null : optionsDivisions[value - 1];
            OnPropertyChanged();
            _ = RechargerAsync();
        }
    }

    public int IndexTri
    {
        get => indexTri;
        set
        {
            if (value < 0 || value >= Tris.Count)
            {
                OnPropertyChanged();
                return;
            }

            if (value == indexTri)
            {
                return;
            }

            indexTri = value;
            OnPropertyChanged();
            _ = RechargerAsync();
        }
    }

    public bool SeulementMesReglages
    {
        get => seulementMesReglages;
        set
        {
            if (seulementMesReglages == value)
            {
                return;
            }

            seulementMesReglages = value;
            OnPropertyChanged();
            _ = RechargerAsync();
        }
    }

    /// <summary>
    /// Sélection dans la liste : toucher une voiture l'ouvre, puis la sélection est effacée
    /// pour que la même voiture puisse être touchée à nouveau au retour.
    /// </summary>
    public LigneVoitureViewModel? VoitureSelectionnee
    {
        get => null;
        set
        {
            if (value is null)
            {
                return;
            }

            OnPropertyChanged();
            _ = OuvrirVoitureAsync(value);
        }
    }

    /// <summary>Appelée à chaque affichage de la page : les nombres de réglages et de chronos ont pu changer.</summary>
    public async Task ChargerAsync()
    {
        try
        {
            using (var travail = fabrique.Creer())
            {
                AppliquerOptions(await travail.ListeVoitures.ObtenirOptionsAsync());
            }
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Options des filtres de voitures");
        }

        await RechargerAsync();
    }

    [RelayCommand]
    private void BasculerFiltres() => FiltresVisibles = !FiltresVisibles;

    [RelayCommand]
    private async Task ReinitialiserAsync()
    {
        recherche = string.Empty;
        identifiantMarque = null;
        division = null;
        indexMarque = 0;
        indexDivision = 0;
        indexTri = 0;
        seulementMesReglages = false;

        OnPropertyChanged(nameof(Recherche));
        OnPropertyChanged(nameof(IndexMarque));
        OnPropertyChanged(nameof(IndexDivision));
        OnPropertyChanged(nameof(IndexTri));
        OnPropertyChanged(nameof(SeulementMesReglages));

        await RechargerAsync();
    }

    private void AppliquerOptions(OptionsFiltresVoitures options)
    {
        optionsMarques = options.Marques;
        optionsDivisions = options.Divisions;

        // On ne remplace les listes que si elles ont changé : réaffecter une liste déroulante efface sa sélection.
        var nomsMarques = new[] { Texte.Obtenir("Voitures_ToutesMarques") }
            .Concat(options.Marques.Select(m => m.Nom))
            .ToList();
        if (!nomsMarques.SequenceEqual(Marques))
        {
            Marques = nomsMarques;
        }

        var nomsDivisions = new[] { Texte.Obtenir("Voitures_ToutesDivisions") }
            .Concat(options.Divisions.Select(d => ServiceLibelles.Obtenir(d)))
            .ToList();
        if (!nomsDivisions.SequenceEqual(Divisions))
        {
            Divisions = nomsDivisions;
        }

        // La sélection est restaurée à partir des identifiants, pas des positions.
        indexMarque = identifiantMarque is { } marque
            ? options.Marques.ToList().FindIndex(m => m.Identifiant == marque) + 1
            : 0;
        if (indexMarque == 0)
        {
            identifiantMarque = null;
        }

        indexDivision = division is { } choisie
            ? options.Divisions.ToList().IndexOf(choisie) + 1
            : 0;
        if (indexDivision == 0)
        {
            division = null;
        }

        OnPropertyChanged(nameof(IndexMarque));
        OnPropertyChanged(nameof(IndexDivision));
    }

    private async Task RechargerApresDelaiAsync()
    {
        attente?.Cancel();
        var source = attente = new CancellationTokenSource();

        try
        {
            await Task.Delay(DelaiDeRecherche, source.Token);
        }
        catch (TaskCanceledException)
        {
            return; // une frappe plus récente a pris le relais
        }

        await RechargerAsync();
    }

    private FiltreVoitures ConstruireFiltre()
    {
        var filtre = new FiltreVoitures
        {
            Texte = recherche,
            SeulementAvecReglagesCrees = seulementMesReglages
        };

        if (identifiantMarque is { } marque)
        {
            filtre.IdentifiantsMarques.Add(marque);
        }

        if (division is { } choisie)
        {
            filtre.Divisions.Add(choisie);
        }

        return filtre;
    }

    private async Task RechargerAsync()
    {
        var version = ++versionRecherche;

        try
        {
            using var travail = fabrique.Creer();
            var lignes = await travail.ListeVoitures.ListerAsync(ConstruireFiltre(), (TriVoitures)indexTri);

            if (version != versionRecherche)
            {
                return; // une recherche plus récente est en cours : ce résultat est périmé
            }

            Voitures = lignes.Select(ligne => new LigneVoitureViewModel(ligne)).ToList();
            ResumeResultat = lignes.Count == 0
                ? Texte.Obtenir("Voitures_Aucune")
                : string.Format(Texte.Obtenir("Voitures_NombreResultats"), lignes.Count);
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Liste des voitures");
            ResumeResultat = MessagesErreurs.Obtenir(exception);
        }
    }

    private async Task OuvrirVoitureAsync(LigneVoitureViewModel ligne)
    {
        try
        {
            await Shell.Current.GoToAsync($"voiture?id={ligne.Identifiant}");
        }
        catch (Exception exception)
        {
            JournalErreurs.Ecrire(exception, "Ouverture d'une voiture");
        }
    }
}
