using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Édition d'une copie de réglage : l'utilisateur voit et saisit les valeurs dans ses unités, l'éditeur les range en unités
/// canoniques dans <see cref="Reglage"/> au fil de la saisie. À l'enregistrement, on passe <see cref="Reglage"/> à
/// <c>ServiceReglages.ModifierAsync</c>.
/// Un réglage d'origine s'affiche mais ne se modifie pas.
/// </summary>
public sealed class EditeurReglage : INotifyPropertyChanged
{
    private static readonly CategorieDeReglage[] OrdreDesSections =
    {
        CategorieDeReglage.Performances,
        CategorieDeReglage.Pneus,
        CategorieDeReglage.Alignement,
        CategorieDeReglage.Ressorts,
        CategorieDeReglage.BarresAntiroulis,
        CategorieDeReglage.Amortisseurs,
        CategorieDeReglage.GeometrieDeSuspension,
        CategorieDeReglage.Differentiel,
        CategorieDeReglage.Freins,
        CategorieDeReglage.Aerodynamisme,
        CategorieDeReglage.Volant
    };

    private bool estValide = true;
    private string classeAffichee = string.Empty;

    /// <param name="reglage">Copie à modifier (obtenue par <c>ObtenirPourEditionAsync</c>) : elle est modifiée sur place.</param>
    public EditeurReglage(Reglage reglage, PreferencesUnites unites, CultureInfo cultureDeFormat)
    {
        Reglage = reglage;
        EstModifiable = !reglage.EstOrigine;

        Generaux = new SectionEditable(CategorieDeReglage.Generaux, reglage, unites, cultureDeFormat, Recalculer);
        Sections = OrdreDesSections
            .Select(categorie => new SectionEditable(categorie, reglage, unites, cultureDeFormat, Recalculer))
            .ToList();
        Rapports = new SectionRapports(reglage, unites, cultureDeFormat, Recalculer);

        Recalculer();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Le réglage en cours d'édition, à jour de toutes les saisies valides.</summary>
    public Reglage Reglage { get; }

    public bool EstModifiable { get; }

    public string Nom
    {
        get => Reglage.Nom;
        set
        {
            value ??= string.Empty;
            if (!EstModifiable || value == Reglage.Nom)
            {
                return;
            }

            Reglage.Nom = value;
            Notifier();
            Recalculer();
        }
    }

    public string Code
    {
        get => Reglage.Generaux.Code;
        set
        {
            value ??= string.Empty;
            if (!EstModifiable || value == Reglage.Generaux.Code)
            {
                return;
            }

            Reglage.Generaux.Code = value;
            Notifier();
        }
    }

    public string Moteur
    {
        get => Reglage.Generaux.Moteur;
        set
        {
            value ??= string.Empty;
            if (!EstModifiable || value == Reglage.Generaux.Moteur)
            {
                return;
            }

            Reglage.Generaux.Moteur = value;
            Notifier();
        }
    }

    /// <summary>Transmission, sous forme d'index dans l'énumération <see cref="Transmission"/> (pour une liste déroulante).</summary>
    public int IndexTransmission
    {
        get => (int)Reglage.Generaux.Transmission;
        set
        {
            if (!EstModifiable || !Enum.IsDefined((Transmission)value) || value == (int)Reglage.Generaux.Transmission)
            {
                Notifier(); // redonne au sélecteur la valeur actuelle
                return;
            }

            Reglage.Generaux.Transmission = (Transmission)value;
            Notifier();
        }
    }

    public SectionEditable Generaux { get; }

    /// <summary>Toutes les autres sections, sauf la boîte de vitesses.</summary>
    public IReadOnlyList<SectionEditable> Sections { get; }

    public SectionRapports Rapports { get; }

    /// <summary>Faux tant qu'un champ contient une valeur invalide ou que le nom est vide : l'enregistrement est alors impossible.</summary>
    public bool EstValide
    {
        get => estValide;
        private set
        {
            if (estValide == value)
            {
                return;
            }

            estValide = value;
            Notifier();
        }
    }

    /// <summary>Classe et indice de performance du réglage, par exemple « S 750 ». Vide si l'indice vaut zéro.</summary>
    public string ClasseAffichee
    {
        get => classeAffichee;
        private set
        {
            if (classeAffichee == value)
            {
                return;
            }

            classeAffichee = value;
            Notifier();
        }
    }

    private void Recalculer()
    {
        var champs = Generaux.Champs
            .Concat(Sections.SelectMany(section => section.Champs))
            .Concat(Rapports.Champs);

        EstValide = !string.IsNullOrWhiteSpace(Reglage.Nom)
            && champs.All(champ => champ.EstValide)
            && Rapports.Boite.All(rapport => rapport.EstValide);

        var indice = Reglage.Generaux.IndiceDePerformance;
        ClasseAffichee = indice > 0 ? IndicesDePerformance.Formater(indice) : string.Empty;
    }

    private void Notifier([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
