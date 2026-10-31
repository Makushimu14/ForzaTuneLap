using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Edition;

/// <summary>La boîte de vitesses : le rapport final (facultatif) et une liste de rapports de longueur variable.</summary>
public sealed class SectionRapports : INotifyPropertyChanged
{
    private readonly Reglage reglage;
    private readonly CultureInfo cultureDeFormat;
    private readonly Action changementParLUtilisateur;
    private readonly Dictionary<int, double> valeursAvantSaisie = new();
    private RapportEditable? rapportEnSaisie;

    internal SectionRapports(
        Reglage reglage,
        PreferencesUnites unites,
        CultureInfo cultureDeFormat,
        Action changementParLUtilisateur)
    {
        this.reglage = reglage;
        this.cultureDeFormat = cultureDeFormat;
        this.changementParLUtilisateur = changementParLUtilisateur;

        EstModifiable = !reglage.EstOrigine;
        Titre = ServiceLibelles.Obtenir(CategorieDeReglage.Rapports);

        Champs = CatalogueCriteres.DeLaCategorie(CategorieDeReglage.Rapports)
            .Where(critere => critere.Ecrire is not null)
            .Select(critere => new ChampReglage(critere, reglage, unites, cultureDeFormat, changementParLUtilisateur))
            .ToList();
        foreach (var champ in Champs)
        {
            champ.Actualiser(sectionPresente: true);
        }

        Boite = new ObservableCollection<RapportEditable>(
            reglage.RapportsDeBoite
                .OrderBy(rapport => rapport.Numero)
                .Select(rapport => new RapportEditable(rapport, EstModifiable, cultureDeFormat, ApresSaisie)));

        // Signale les rapports incohérents déjà présents dans le réglage, sans les modifier.
        ActualiserAvertissements();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Titre { get; }

    public bool EstModifiable { get; }

    /// <summary>Le rapport final.</summary>
    public IReadOnlyList<ChampReglage> Champs { get; }

    /// <summary>Les rapports de la boîte, dans l'ordre.</summary>
    public ObservableCollection<RapportEditable> Boite { get; }

    public bool PeutAjouter => EstModifiable && Boite.Count < CatalogueCriteres.NombreMaximumDeRapports;

    public bool PeutRetirer => EstModifiable && Boite.Count > 0;

    /// <summary>Ajoute un rapport à la fin, à zéro (valeur inhabituelle, à remplacer).</summary>
    public void Ajouter()
    {
        if (!PeutAjouter)
        {
            return;
        }

        var rapport = new RapportDeBoite { Numero = Boite.Count + 1, Valeur = 0 };
        reglage.RapportsDeBoite.Add(rapport);
        rapportEnSaisie = null;
        Boite.Add(new RapportEditable(rapport, EstModifiable, cultureDeFormat, ApresSaisie));

        ActualiserAvertissements();
        Notifier();
        changementParLUtilisateur();
    }

    public void RetirerDernier()
    {
        if (!PeutRetirer)
        {
            return;
        }

        var dernier = Boite[^1];
        reglage.RapportsDeBoite.Remove(dernier.Rapport);
        rapportEnSaisie = null;
        Boite.RemoveAt(Boite.Count - 1);

        ActualiserAvertissements();
        Notifier();
        changementParLUtilisateur();
    }

    /// <summary>
    /// Les rapports vont en décroissant (le premier est le plus grand) : quand l'utilisateur saisit une valeur valide,
    /// les rapports suivants qui la dépassent sont ramenés à cette valeur. Les rapports précédents ne sont jamais modifiés.
    /// </summary>
    private void ApresSaisie(RapportEditable saisi, bool valide)
    {
        if (valide)
        {
            // La saisie est évaluée à chaque caractère : en tapant « 0,8 », le texte passe par « 0 ». Les valeurs des
            // rapports suivants sont donc mémorisées au début de la saisie, et recalculées à partir d'elles à chaque
            // caractère (minimum entre la valeur d'origine et la valeur saisie), pour qu'elles remontent à « 0,8 ».
            if (!ReferenceEquals(saisi, rapportEnSaisie))
            {
                rapportEnSaisie = saisi;
                valeursAvantSaisie.Clear();
                foreach (var suivant in Boite.Where(rapport => rapport.Numero > saisi.Numero))
                {
                    valeursAvantSaisie[suivant.Numero] = suivant.Valeur;
                }
            }

            var plafond = saisi.Valeur;
            foreach (var suivant in Boite.Where(rapport => rapport.Numero > saisi.Numero))
            {
                var cible = valeursAvantSaisie.TryGetValue(suivant.Numero, out var avant)
                    ? Math.Min(avant, plafond)
                    : suivant.Valeur;
                if (suivant.Valeur != cible)
                {
                    // Ajusté tant que la valeur diffère de celle d'avant la saisie ; sinon elle est rétablie.
                    var avantSaisie = valeursAvantSaisie.TryGetValue(suivant.Numero, out var origine) && origine == cible;
                    suivant.DefinirValeur(cible, ajuste: !avantSaisie);
                }
            }
        }

        ActualiserAvertissements();
        changementParLUtilisateur();
    }

    /// <summary>Signale chaque rapport plus grand que le précédent.</summary>
    private void ActualiserAvertissements()
    {
        for (var i = 0; i < Boite.Count; i++)
        {
            Boite[i].DefinirSuperieurAuPrecedent(i > 0 && Boite[i].Valeur > Boite[i - 1].Valeur);
        }
    }

    private void Notifier()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutAjouter)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutRetirer)));
    }
}
