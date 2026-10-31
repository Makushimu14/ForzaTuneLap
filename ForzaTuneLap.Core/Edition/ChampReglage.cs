using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Un champ numérique d'un réglage, tel que l'utilisateur le voit : la valeur est affichée et saisie dans l'unité choisie,
/// et enregistrée en unité canonique.
/// La valeur du réglage n'est réécrite que lorsque l'utilisateur modifie le texte : un champ qu'il ne touche pas
/// garde exactement sa valeur d'origine, sans arrondi lié à l'affichage.
/// </summary>
public sealed class ChampReglage : INotifyPropertyChanged
{
    private readonly DescriptionCritere critere;
    private readonly Reglage reglage;
    private readonly PreferencesUnites unites;
    private readonly CultureInfo cultureDeFormat;
    private readonly Action changementParLUtilisateur;

    private string texte = string.Empty;
    private bool estActif;
    private bool estValide = true;
    private bool estInhabituel;

    internal ChampReglage(
        DescriptionCritere critere,
        Reglage reglage,
        PreferencesUnites unites,
        CultureInfo cultureDeFormat,
        Action changementParLUtilisateur)
    {
        this.critere = critere;
        this.reglage = reglage;
        this.unites = unites;
        this.cultureDeFormat = cultureDeFormat;
        this.changementParLUtilisateur = changementParLUtilisateur;

        Libelle = ServiceLibelles.Obtenir(critere);
        Symbole = unites.ObtenirUnite(critere.Grandeur).Symbole;
        EstModifiable = !reglage.EstOrigine;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Cle => critere.Cle;

    public string Libelle { get; }

    /// <summary>Symbole de l'unité choisie (vide pour une valeur sans unité).</summary>
    public string Symbole { get; }

    public bool EstFacultatif => critere.EstFacultatif;

    /// <summary>La valeur peut être négative : l'écran choisit alors un clavier qui permet de taper le signe « moins ».</summary>
    public bool AccepteLesNegatifs => critere.AccepteLesNegatifs;

    /// <summary>Faux pour un réglage d'origine, qui est en lecture seule.</summary>
    public bool EstModifiable { get; }

    /// <summary>Faux quand la section du champ n'existe pas pour ce réglage.</summary>
    public bool EstActif
    {
        get => estActif;
        private set
        {
            if (estActif == value)
            {
                return;
            }

            estActif = value;
            Notifier(nameof(EstActif));
            Notifier(nameof(PeutEtreSaisi));
        }
    }

    public bool PeutEtreSaisi => EstActif && EstModifiable;

    public string Texte
    {
        get => texte;
        set
        {
            value ??= string.Empty;
            if (value == texte)
            {
                return;
            }

            texte = value;
            Notifier();

            if (PeutEtreSaisi)
            {
                Valider();
                changementParLUtilisateur();
            }
        }
    }

    public bool EstValide => estValide;

    public bool EstInvalide => !estValide;

    /// <summary>Valeur hors des bornes indicatives du catalogue. Un avertissement, jamais un blocage.</summary>
    public bool EstInhabituel => estInhabituel;

    /// <summary>Relit la valeur du réglage (au chargement, ou quand la section apparaît ou disparaît).</summary>
    internal void Actualiser(bool sectionPresente)
    {
        EstActif = sectionPresente;

        var valeur = sectionPresente ? critere.Lire(reglage) : null;
        texte = valeur is { } v ? Nombres.Formater(unites.VersAffichage(critere.Grandeur, v), cultureDeFormat) : string.Empty;
        estValide = true;
        estInhabituel = valeur is { } canonique && HorsBornes(canonique);

        Notifier(nameof(Texte));
        Notifier(nameof(EstValide));
        Notifier(nameof(EstInvalide));
        Notifier(nameof(EstInhabituel));
    }

    private void Valider()
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            // Vide : accepté seulement pour un champ facultatif, qui prend alors la valeur nulle.
            estValide = critere.EstFacultatif;
            estInhabituel = false;
            if (estValide)
            {
                critere.Ecrire!(reglage, null);
            }
        }
        else if (Nombres.TryParse(texte, out var saisie))
        {
            var canonique = unites.VersCanonique(critere.Grandeur, saisie);
            critere.Ecrire!(reglage, canonique);
            estValide = true;
            estInhabituel = HorsBornes(canonique);
        }
        else
        {
            estValide = false;
            estInhabituel = false;
        }

        Notifier(nameof(EstValide));
        Notifier(nameof(EstInvalide));
        Notifier(nameof(EstInhabituel));
    }

    private bool HorsBornes(double canonique) =>
        (critere.Minimum is { } minimum && canonique < minimum) || (critere.Maximum is { } maximum && canonique > maximum);

    private void Notifier([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
