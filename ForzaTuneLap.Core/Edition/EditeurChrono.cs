using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// État d'un chrono en cours de saisie, pour l'ajout comme pour la correction : la durée tapée dans un seul champ
/// « m:ss,mmm », la date de réalisation et le commentaire. L'enregistrement est fait par l'écran, avec
/// <see cref="Services.ServiceChronos"/>, une fois <see cref="PeutEnregistrer"/> vrai.
/// </summary>
public sealed class EditeurChrono : INotifyPropertyChanged
{
    private readonly CultureInfo cultureDeFormat;

    private string dureeTexte = string.Empty;
    private TimeSpan? duree;
    private DateTime dateRealisation;
    private string? commentaire;

    /// <summary>Nouveau chrono : durée vide (à saisir), date du jour.</summary>
    public EditeurChrono(CultureInfo cultureDeFormat)
    {
        this.cultureDeFormat = cultureDeFormat;
        dateRealisation = DateTime.UtcNow.Date;
    }

    /// <summary>Correction d'un chrono existant.</summary>
    public EditeurChrono(Chrono chrono, CultureInfo cultureDeFormat)
    {
        this.cultureDeFormat = cultureDeFormat;
        EstModification = true;
        IdentifiantChrono = chrono.Identifiant;

        duree = chrono.Duree;
        dureeTexte = DureesDeTour.Formater(chrono.DureeMillisecondes, cultureDeFormat);
        dateRealisation = chrono.DateRealisation;
        commentaire = chrono.Commentaire;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool EstModification { get; }

    /// <summary>Identifiant du chrono corrigé, ou null pour un nouveau chrono.</summary>
    public Guid? IdentifiantChrono { get; }

    public string DureeTexte
    {
        get => dureeTexte;
        set
        {
            value ??= string.Empty;
            if (value == dureeTexte)
            {
                return;
            }

            dureeTexte = value;
            duree = DureesDeTour.TryParse(value, out var lue) ? lue : null;

            Notifier();
            Notifier(nameof(EstValide));
            Notifier(nameof(EstInvalide));
            Notifier(nameof(PeutEnregistrer));
        }
    }

    /// <summary>La durée lue, ou null tant que le texte n'est pas valide.</summary>
    public TimeSpan? Duree => duree;

    public bool EstValide => duree is not null;

    /// <summary>Le texte n'est pas vide mais ne se lit pas : l'écran affiche une erreur. Un champ vide n'en affiche pas.</summary>
    public bool EstInvalide => duree is null && !string.IsNullOrWhiteSpace(dureeTexte);

    public DateTime DateRealisation
    {
        get => dateRealisation;
        set
        {
            if (value == dateRealisation)
            {
                return;
            }

            dateRealisation = value;
            Notifier();
        }
    }

    public string? Commentaire
    {
        get => commentaire;
        set
        {
            if (value == commentaire)
            {
                return;
            }

            commentaire = value;
            Notifier();
        }
    }

    public bool PeutEnregistrer => duree is not null;

    /// <summary>Texte affiché pour un exemple de saisie, dans la culture de l'utilisateur (« 1:32,450 »).</summary>
    public string Exemple => DureesDeTour.Formater(92_450, cultureDeFormat);

    private void Notifier([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
