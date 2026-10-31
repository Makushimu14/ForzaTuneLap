using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// État du formulaire d'une voiture personnalisée : nom, année, division, et, dans la zone « avancé », le prix et le
/// contenu téléchargeable. La marque est choisie par l'écran. L'enregistrement est fait avec
/// <see cref="Services.ServiceCatalogue"/>, une fois <see cref="PeutEnregistrer"/> vrai.
/// </summary>
public sealed class EditeurVoiturePersonnalisee : INotifyPropertyChanged
{
    public const int AnneeMinimum = 1900;
    public const int AnneeMaximum = 2100;

    private string nom = string.Empty;
    private string anneeTexte = string.Empty;
    private string valeurTexte = string.Empty;
    private Division division;
    private bool contenuTelechargeable;

    /// <summary>Nouvelle voiture : tout est vide, la division est la première de l'énumération.</summary>
    public EditeurVoiturePersonnalisee()
    {
        division = Enum.GetValues<Division>()[0];
    }

    /// <summary>Correction d'une voiture personnalisée existante.</summary>
    public EditeurVoiturePersonnalisee(Voiture voiture)
    {
        EstModification = true;
        IdentifiantVoiture = voiture.Identifiant;

        nom = voiture.Nom;
        anneeTexte = voiture.Annee.ToString(CultureInfo.InvariantCulture);
        valeurTexte = voiture.Valeur > 0 ? voiture.Valeur.ToString(CultureInfo.InvariantCulture) : string.Empty;
        division = voiture.Division;
        contenuTelechargeable = voiture.ContenuTelechargeable;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool EstModification { get; }

    /// <summary>Identifiant de la voiture corrigée, ou null pour une nouvelle voiture.</summary>
    public int? IdentifiantVoiture { get; }

    public string Nom
    {
        get => nom;
        set => Definir(ref nom, value ?? string.Empty);
    }

    public string AnneeTexte
    {
        get => anneeTexte;
        set => Definir(ref anneeTexte, value ?? string.Empty, nameof(Annee), nameof(AnneeInvalide));
    }

    /// <summary>L'année lue, ou null tant que le texte n'est pas une année entre 1900 et 2100.</summary>
    public int? Annee =>
        int.TryParse(anneeTexte.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var annee)
        && annee is >= AnneeMinimum and <= AnneeMaximum
            ? annee
            : null;

    /// <summary>L'année est saisie mais invalide. Un champ vide n'affiche pas d'erreur.</summary>
    public bool AnneeInvalide => !string.IsNullOrWhiteSpace(anneeTexte) && Annee is null;

    public Division Division
    {
        get => division;
        set => Definir(ref division, value);
    }

    /// <summary>Prix en crédits, facultatif (zone « avancé »).</summary>
    public string ValeurTexte
    {
        get => valeurTexte;
        set => Definir(ref valeurTexte, value ?? string.Empty, nameof(Valeur), nameof(ValeurInvalide));
    }

    /// <summary>Le prix lu : 0 quand le champ est vide, ou null s'il n'est pas un entier positif ou nul.</summary>
    public int? Valeur =>
        string.IsNullOrWhiteSpace(valeurTexte)
            ? 0
            : int.TryParse(valeurTexte.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var valeur) && valeur >= 0
                ? valeur
                : null;

    public bool ValeurInvalide => Valeur is null;

    public bool ContenuTelechargeable
    {
        get => contenuTelechargeable;
        set => Definir(ref contenuTelechargeable, value);
    }

    public bool NomVide => string.IsNullOrWhiteSpace(nom);

    public bool PeutEnregistrer => !NomVide && Annee is not null && Valeur is not null;

    private void Definir<T>(ref T champ, T valeur, string? autre1 = null, string? autre2 = null, [CallerMemberName] string? nomPropriete = null)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur))
        {
            return;
        }

        champ = valeur;
        Notifier(nomPropriete);
        Notifier(nameof(NomVide));
        Notifier(nameof(PeutEnregistrer));
        if (autre1 is not null)
        {
            Notifier(autre1);
        }

        if (autre2 is not null)
        {
            Notifier(autre2);
        }
    }

    private void Notifier(string? nomPropriete) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nomPropriete));
}
