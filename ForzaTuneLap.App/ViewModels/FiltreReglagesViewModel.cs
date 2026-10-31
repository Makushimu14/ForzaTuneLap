using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Un critère numérique ajouté aux filtres, avec son bouton « retirer ».</summary>
public sealed class LigneCritereFiltreViewModel : ObservableObject
{
    private readonly CritereDeFiltre critere;

    public LigneCritereFiltreViewModel(CritereDeFiltre critere, Action<LigneCritereFiltreViewModel> retirer)
    {
        this.critere = critere;
        RetirerCommand = new RelayCommand(() => retirer(this));

        critere.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CritereDeFiltre.EstInvalide))
            {
                OnPropertyChanged(nameof(EstInvalide));
            }
        };
    }

    public CritereDeFiltre Critere => critere;

    public string Libelle => critere.Libelle;

    public string Symbole => critere.Symbole;

    public bool AccepteLesNegatifs => critere.AccepteLesNegatifs;

    public string MinimumTexte
    {
        get => critere.MinimumTexte;
        set
        {
            critere.MinimumTexte = value;
            OnPropertyChanged();
        }
    }

    public string MaximumTexte
    {
        get => critere.MaximumTexte;
        set
        {
            critere.MaximumTexte = value;
            OnPropertyChanged();
        }
    }

    public bool EstInvalide => critere.EstInvalide;

    public IRelayCommand RetirerCommand { get; }
}

/// <summary>
/// Les filtres d'une comparaison (chronos ou réglages) : les listes déroulantes, la recherche, les années, l'indice de
/// performance et les critères numériques à la demande. Toute la logique est dans <see cref="EditeurFiltreReglages"/>.
/// <see cref="Modifie"/> est levé après chaque changement.
/// </summary>
public partial class FiltreReglagesViewModel : ObservableObject
{
    private readonly FabriqueUniteDeTravail fabrique;
    private IReadOnlyList<OptionMarque> optionsMarques = Array.Empty<OptionMarque>();
    private IReadOnlyList<Division> optionsDivisions = Array.Empty<Division>();

    public FiltreReglagesViewModel(FabriqueUniteDeTravail fabrique, ServicePreferences preferences)
    {
        this.fabrique = fabrique;

        Editeur = new EditeurFiltreReglages(preferences.Unites);
        Editeur.Modifie += (_, _) => Modifie?.Invoke(this, EventArgs.Empty);
        Editeur.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditeurFiltreReglages.EstValide))
            {
                OnPropertyChanged(nameof(EstInvalide));
            }
        };

        marques = new[] { Texte.Obtenir("Voitures_ToutesMarques") };
        divisions = new[] { Texte.Obtenir("Voitures_ToutesDivisions") };

        // Même ordre que l'énumération Transmission, précédée de « toutes ».
        Transmissions = new[] { Texte.Obtenir("Filtre_ToutesTransmissions") }
            .Concat(Enum.GetValues<Transmission>().Select(t => ServiceLibelles.Obtenir(t)))
            .ToList();

        // Même ordre que l'énumération OrigineDuReglage.
        Origines = new[]
        {
            Texte.Obtenir("Filtre_OrigineTous"),
            Texte.Obtenir("Filtre_OrigineOrigine"),
            Texte.Obtenir("Filtre_OrigineCreation")
        };

        CriteresDisponibles = CatalogueCriteres.Tous
            .Select(c => $"{ServiceLibelles.Obtenir(c.Categorie)} · {ServiceLibelles.Obtenir(c)}")
            .ToList();
    }

    /// <summary>Levé après chaque changement d'un filtre.</summary>
    public event EventHandler? Modifie;

    public EditeurFiltreReglages Editeur { get; }

    public IReadOnlyList<string> Transmissions { get; }

    public IReadOnlyList<string> Origines { get; }

    public IReadOnlyList<string> CriteresDisponibles { get; }

    [ObservableProperty]
    private IReadOnlyList<string> marques;

    [ObservableProperty]
    private IReadOnlyList<string> divisions;

    [ObservableProperty]
    private int indexMarque;

    [ObservableProperty]
    private int indexDivision;

    [ObservableProperty]
    private int indexTransmission;

    [ObservableProperty]
    private int indexOrigine;

    [ObservableProperty]
    private int indexCritereAjoute = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TexteBoutonFiltres))]
    private bool filtresVisibles;

    public ObservableCollection<LigneCritereFiltreViewModel> CriteresAffiches { get; } = new();

    public string TexteBoutonFiltres => Texte.Obtenir(FiltresVisibles ? "Voitures_MasquerFiltres" : "Voitures_Filtres");

    // Les champs de saisie sont liés à l'éditeur du Core par ces propriétés.
    public string Recherche
    {
        get => Editeur.Texte;
        set
        {
            Editeur.Texte = value;
            OnPropertyChanged();
        }
    }

    public string AnneeMinimum
    {
        get => Editeur.AnneeMinimumTexte;
        set
        {
            Editeur.AnneeMinimumTexte = value;
            OnPropertyChanged();
        }
    }

    public string AnneeMaximum
    {
        get => Editeur.AnneeMaximumTexte;
        set
        {
            Editeur.AnneeMaximumTexte = value;
            OnPropertyChanged();
        }
    }

    public string IndiceMinimum
    {
        get => Editeur.IndiceMinimumTexte;
        set
        {
            Editeur.IndiceMinimumTexte = value;
            OnPropertyChanged();
        }
    }

    public string IndiceMaximum
    {
        get => Editeur.IndiceMaximumTexte;
        set
        {
            Editeur.IndiceMaximumTexte = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Un nombre ou un champ vide est attendu : sert à afficher le message d'erreur.</summary>
    public bool EstInvalide => !Editeur.EstValide;

    partial void OnIndexMarqueChanged(int value) =>
        Editeur.IdentifiantMarque = value >= 1 && value <= optionsMarques.Count ? optionsMarques[value - 1].Identifiant : null;

    partial void OnIndexDivisionChanged(int value) =>
        Editeur.Division = value >= 1 && value <= optionsDivisions.Count ? optionsDivisions[value - 1] : null;

    partial void OnIndexTransmissionChanged(int value) =>
        Editeur.Transmission = value >= 1 && value <= Enum.GetValues<Transmission>().Length
            ? Enum.GetValues<Transmission>()[value - 1]
            : null;

    partial void OnIndexOrigineChanged(int value) =>
        Editeur.Origine = value >= 0 && value < Origines.Count ? (OrigineDuReglage)value : OrigineDuReglage.Tous;

    /// <summary>Charge les marques et les divisions qui existent dans les données. Peut être rappelée sans perdre la sélection.</summary>
    public async Task ChargerOptionsAsync()
    {
        OptionsFiltresVoitures options;
        using (var travail = fabrique.Creer())
        {
            options = await travail.ListeVoitures.ObtenirOptionsAsync();
        }

        optionsMarques = options.Marques;
        optionsDivisions = options.Divisions;

        // On ne remplace les listes que si elles ont changé : réaffecter une liste déroulante efface sa sélection.
        var nomsMarques = new[] { Texte.Obtenir("Voitures_ToutesMarques") }.Concat(options.Marques.Select(m => m.Nom)).ToList();
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
        var indexDeLaMarque = Editeur.IdentifiantMarque is { } marque
            ? options.Marques.ToList().FindIndex(m => m.Identifiant == marque) + 1
            : 0;
        var indexDeLaDivision = Editeur.Division is { } division
            ? options.Divisions.ToList().IndexOf(division) + 1
            : 0;

        IndexMarque = indexDeLaMarque;
        IndexDivision = indexDeLaDivision;
    }

    [RelayCommand]
    private void BasculerFiltres() => FiltresVisibles = !FiltresVisibles;

    [RelayCommand]
    private void AjouterCritere()
    {
        if (IndexCritereAjoute < 0 || IndexCritereAjoute >= CatalogueCriteres.Tous.Count)
        {
            return;
        }

        var ajoute = Editeur.AjouterCritere(CatalogueCriteres.Tous[IndexCritereAjoute]);
        if (CriteresAffiches.All(ligne => ligne.Critere != ajoute))
        {
            CriteresAffiches.Add(new LigneCritereFiltreViewModel(ajoute, RetirerCritere));
        }

        OnPropertyChanged(nameof(EstInvalide));
    }

    private void RetirerCritere(LigneCritereFiltreViewModel ligne)
    {
        CriteresAffiches.Remove(ligne);
        Editeur.RetirerCritere(ligne.Critere);
        OnPropertyChanged(nameof(EstInvalide));
    }

    [RelayCommand]
    private void Reinitialiser()
    {
        CriteresAffiches.Clear();
        Editeur.Reinitialiser();

        IndexMarque = 0;
        IndexDivision = 0;
        IndexTransmission = 0;
        IndexOrigine = 0;
        IndexCritereAjoute = -1;

        OnPropertyChanged(nameof(Recherche));
        OnPropertyChanged(nameof(AnneeMinimum));
        OnPropertyChanged(nameof(AnneeMaximum));
        OnPropertyChanged(nameof(IndiceMinimum));
        OnPropertyChanged(nameof(IndiceMaximum));
        OnPropertyChanged(nameof(EstInvalide));
    }
}
