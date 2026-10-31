using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Un critère numérique ajouté aux filtres : un minimum et un maximum saisis dans l'unité choisie par l'utilisateur.
/// Sans borne, le critère ne filtre rien mais reste affiché sur les lignes du résultat.
/// </summary>
public sealed class CritereDeFiltre : INotifyPropertyChanged
{
    private readonly PreferencesUnites unites;
    private readonly Action modifie;

    private string minimumTexte = string.Empty;
    private string maximumTexte = string.Empty;

    internal CritereDeFiltre(DescriptionCritere critere, PreferencesUnites unites, Action modifie)
    {
        Critere = critere;
        this.unites = unites;
        this.modifie = modifie;

        Libelle = ServiceLibelles.Obtenir(critere);
        Symbole = unites.ObtenirUnite(critere.Grandeur).Symbole;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DescriptionCritere Critere { get; }

    public string Libelle { get; }

    /// <summary>Symbole de l'unité choisie (vide pour une valeur sans unité).</summary>
    public string Symbole { get; }

    public string MinimumTexte
    {
        get => minimumTexte;
        set => Definir(ref minimumTexte, value);
    }

    public string MaximumTexte
    {
        get => maximumTexte;
        set => Definir(ref maximumTexte, value);
    }

    /// <summary>Chaque borne est vide ou un nombre.</summary>
    public bool EstValide => BorneValide(minimumTexte) && BorneValide(maximumTexte);

    public bool EstInvalide => !EstValide;

    /// <summary>Le filtre en unité canonique. À n'appeler que si <see cref="EstValide"/>.</summary>
    internal FiltreNumerique Construire() => new(
        Critere.Cle,
        LireBorne(minimumTexte),
        LireBorne(maximumTexte));

    private double? LireBorne(string texte) =>
        Nombres.TryParse(texte, out var valeur) ? unites.VersCanonique(Critere.Grandeur, valeur) : null;

    private static bool BorneValide(string texte) => string.IsNullOrWhiteSpace(texte) || Nombres.TryParse(texte, out _);

    private void Definir(ref string champ, string? valeur, [CallerMemberName] string? nom = null)
    {
        valeur ??= string.Empty;
        if (valeur == champ)
        {
            return;
        }

        champ = valeur;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstValide)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstInvalide)));
        modifie();
    }
}

/// <summary>
/// Les filtres saisis pour comparer des réglages ou des chronos : texte, marque, division, transmission, origine du
/// réglage, années, indice de performance, et autant de critères numériques du catalogue que l'on veut. Les bornes des
/// critères sont saisies dans l'unité choisie et converties en unité canonique à la construction du filtre.
/// <see cref="Modifie"/> est levé après chaque changement : l'écran relance alors la comparaison.
/// </summary>
public sealed class EditeurFiltreReglages : INotifyPropertyChanged
{
    private readonly PreferencesUnites unites;

    private string texte = string.Empty;
    private int? identifiantMarque;
    private Division? division;
    private Transmission? transmission;
    private OrigineDuReglage origine = OrigineDuReglage.Tous;
    private string anneeMinimumTexte = string.Empty;
    private string anneeMaximumTexte = string.Empty;
    private string indiceMinimumTexte = string.Empty;
    private string indiceMaximumTexte = string.Empty;

    public EditeurFiltreReglages(PreferencesUnites unites)
    {
        this.unites = unites;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Levé après chaque changement d'un filtre.</summary>
    public event EventHandler? Modifie;

    public string Texte
    {
        get => texte;
        set => Definir(ref texte, value ?? string.Empty, nameof(Texte));
    }

    public int? IdentifiantMarque
    {
        get => identifiantMarque;
        set => Definir(ref identifiantMarque, value, nameof(IdentifiantMarque));
    }

    public Division? Division
    {
        get => division;
        set => Definir(ref division, value, nameof(Division));
    }

    public Transmission? Transmission
    {
        get => transmission;
        set => Definir(ref transmission, value, nameof(Transmission));
    }

    public OrigineDuReglage Origine
    {
        get => origine;
        set => Definir(ref origine, value, nameof(Origine));
    }

    public string AnneeMinimumTexte
    {
        get => anneeMinimumTexte;
        set => Definir(ref anneeMinimumTexte, value ?? string.Empty, nameof(AnneeMinimumTexte), avecValidite: true);
    }

    public string AnneeMaximumTexte
    {
        get => anneeMaximumTexte;
        set => Definir(ref anneeMaximumTexte, value ?? string.Empty, nameof(AnneeMaximumTexte), avecValidite: true);
    }

    public string IndiceMinimumTexte
    {
        get => indiceMinimumTexte;
        set => Definir(ref indiceMinimumTexte, value ?? string.Empty, nameof(IndiceMinimumTexte), avecValidite: true);
    }

    public string IndiceMaximumTexte
    {
        get => indiceMaximumTexte;
        set => Definir(ref indiceMaximumTexte, value ?? string.Empty, nameof(IndiceMaximumTexte), avecValidite: true);
    }

    /// <summary>Les critères numériques ajoutés, dans l'ordre d'ajout.</summary>
    public ObservableCollection<CritereDeFiltre> Criteres { get; } = new();

    /// <summary>Toutes les bornes sont vides ou des nombres. Tant que c'est faux, le filtre ne doit pas être appliqué.</summary>
    public bool EstValide =>
        EntierValide(anneeMinimumTexte) && EntierValide(anneeMaximumTexte)
        && EntierValide(indiceMinimumTexte) && EntierValide(indiceMaximumTexte)
        && Criteres.All(critere => critere.EstValide);

    /// <summary>Ajoute un critère aux filtres. Sans effet s'il y est déjà. Renvoie le critère ajouté ou existant.</summary>
    public CritereDeFiltre AjouterCritere(DescriptionCritere critere)
    {
        var existant = Criteres.FirstOrDefault(c => c.Critere.Cle == critere.Cle);
        if (existant is not null)
        {
            return existant;
        }

        var ajoute = new CritereDeFiltre(critere, unites, SurChangementDeCritere);
        Criteres.Add(ajoute);
        Notifier(nameof(EstValide));
        Modifie?.Invoke(this, EventArgs.Empty);
        return ajoute;
    }

    public void RetirerCritere(CritereDeFiltre critere)
    {
        if (Criteres.Remove(critere))
        {
            Notifier(nameof(EstValide));
            Modifie?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Efface tous les filtres, critères compris. <see cref="Modifie"/> n'est levé qu'une fois.</summary>
    public void Reinitialiser()
    {
        texte = string.Empty;
        identifiantMarque = null;
        division = null;
        transmission = null;
        origine = OrigineDuReglage.Tous;
        anneeMinimumTexte = anneeMaximumTexte = indiceMinimumTexte = indiceMaximumTexte = string.Empty;
        Criteres.Clear();

        foreach (var nom in new[]
        {
            nameof(Texte), nameof(IdentifiantMarque), nameof(Division), nameof(Transmission), nameof(Origine),
            nameof(AnneeMinimumTexte), nameof(AnneeMaximumTexte), nameof(IndiceMinimumTexte), nameof(IndiceMaximumTexte),
            nameof(EstValide)
        })
        {
            Notifier(nom);
        }

        Modifie?.Invoke(this, EventArgs.Empty);
    }

    public FiltreChronos ConstruireFiltreChronos()
    {
        var filtre = new FiltreChronos();
        Remplir(filtre);
        return filtre;
    }

    public FiltreReglages ConstruireFiltreReglages()
    {
        var filtre = new FiltreReglages();
        Remplir(filtre);
        return filtre;
    }

    private void Remplir(FiltreReglages filtre)
    {
        filtre.Texte = string.IsNullOrWhiteSpace(texte) ? null : texte;

        if (identifiantMarque is { } marque)
        {
            filtre.IdentifiantsMarques.Add(marque);
        }

        if (division is { } choisie)
        {
            filtre.Divisions.Add(choisie);
        }

        if (transmission is { } transmissionChoisie)
        {
            filtre.Transmissions.Add(transmissionChoisie);
        }

        filtre.Origine = origine;
        filtre.AnneeMinimum = LireEntier(anneeMinimumTexte);
        filtre.AnneeMaximum = LireEntier(anneeMaximumTexte);
        filtre.IndiceDePerformanceMinimum = LireEntier(indiceMinimumTexte);
        filtre.IndiceDePerformanceMaximum = LireEntier(indiceMaximumTexte);

        foreach (var critere in Criteres.Where(critere => critere.EstValide))
        {
            filtre.Criteres.Add(critere.Construire());
        }
    }

    private static bool EntierValide(string texte) =>
        string.IsNullOrWhiteSpace(texte)
        || int.TryParse(texte.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    private static int? LireEntier(string texte) =>
        int.TryParse(texte.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var valeur) ? valeur : null;

    private void SurChangementDeCritere()
    {
        Notifier(nameof(EstValide));
        Modifie?.Invoke(this, EventArgs.Empty);
    }

    private void Definir<T>(ref T champ, T valeur, string nom, bool avecValidite = false)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur))
        {
            return;
        }

        champ = valeur;
        Notifier(nom);
        if (avecValidite)
        {
            Notifier(nameof(EstValide));
        }

        Modifie?.Invoke(this, EventArgs.Empty);
    }

    private void Notifier([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
