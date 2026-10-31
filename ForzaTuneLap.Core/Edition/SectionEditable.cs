using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Une section d'un réglage (pneus, alignement, ressorts…) avec ses champs. Une section facultative peut être ajoutée
/// ou retirée : une voiture sans aérodynamisme réglable n'a simplement pas cette section.
/// </summary>
public sealed class SectionEditable : INotifyPropertyChanged
{
    private readonly Reglage reglage;
    private readonly Action changementParLUtilisateur;
    private bool estPresente;

    internal SectionEditable(
        CategorieDeReglage categorie,
        Reglage reglage,
        PreferencesUnites unites,
        CultureInfo cultureDeFormat,
        Action changementParLUtilisateur)
    {
        this.reglage = reglage;
        this.changementParLUtilisateur = changementParLUtilisateur;

        Categorie = categorie;
        Titre = ServiceLibelles.Obtenir(categorie);
        EstFacultative = PresenceDesSections.EstFacultative(categorie);
        EstModifiable = !reglage.EstOrigine;

        // Seuls les critères qui s'écrivent un par un sont des champs ; les rapports numérotés ont leur propre liste.
        Champs = CatalogueCriteres.DeLaCategorie(categorie)
            .Where(critere => critere.Ecrire is not null)
            .Select(critere => new ChampReglage(critere, reglage, unites, cultureDeFormat, changementParLUtilisateur))
            .ToList();

        Actualiser();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CategorieDeReglage Categorie { get; }

    public string Titre { get; }

    public IReadOnlyList<ChampReglage> Champs { get; }

    public bool EstFacultative { get; }

    /// <summary>L'inverse de <see cref="EstPresente"/>, pour l'affichage.</summary>
    public bool EstAbsente => !estPresente;

    public bool EstModifiable { get; }

    /// <summary>L'utilisateur peut ajouter ou retirer cette section.</summary>
    public bool PeutBasculer => EstFacultative && EstModifiable;

    /// <summary>Vrai si le réglage contient cette section. L'écrire l'ajoute (valeurs à zéro) ou la retire.</summary>
    public bool EstPresente
    {
        get => estPresente;
        set
        {
            if (!PeutBasculer || value == estPresente)
            {
                return;
            }

            if (value)
            {
                PresenceDesSections.Creer(reglage, Categorie);
            }
            else
            {
                PresenceDesSections.Retirer(reglage, Categorie);
            }

            Actualiser();
            changementParLUtilisateur();
        }
    }

    private void Actualiser()
    {
        estPresente = PresenceDesSections.EstPresente(reglage, Categorie);

        foreach (var champ in Champs)
        {
            champ.Actualiser(estPresente);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstPresente)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstAbsente)));
    }
}
