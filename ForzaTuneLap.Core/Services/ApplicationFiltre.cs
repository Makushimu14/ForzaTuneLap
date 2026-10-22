using System.Globalization;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Services;

/// <summary>Application d'un <see cref="FiltreReglages"/> à un réglage dont la voiture et la marque sont chargées.</summary>
internal static class ApplicationFiltre
{
    /// <summary>Associe chaque filtre numérique à son critère, et refuse une clé inconnue.</summary>
    public static List<(DescriptionCritere Critere, FiltreNumerique Filtre)> ResoudreCriteres(FiltreReglages? filtre)
    {
        var resolus = new List<(DescriptionCritere Critere, FiltreNumerique Filtre)>();
        if (filtre is null)
        {
            return resolus;
        }

        foreach (var filtreNumerique in filtre.Criteres)
        {
            var critere = CatalogueCriteres.Rechercher(filtreNumerique.CleCritere)
                ?? throw new ErreurMetierException(CodeErreurMetier.CritereInconnu);
            resolus.Add((critere, filtreNumerique));
        }

        return resolus;
    }

    public static bool Correspond(
        Reglage reglage,
        FiltreReglages filtre,
        List<(DescriptionCritere Critere, FiltreNumerique Filtre)> criteres)
    {
        var voiture = reglage.Voiture!;
        var marque = voiture.Marque!;

        if (filtre.IdentifiantsMarques.Count > 0 && !filtre.IdentifiantsMarques.Contains(marque.Identifiant))
        {
            return false;
        }

        if (filtre.IdentifiantsPays.Count > 0 && !filtre.IdentifiantsPays.Contains(marque.IdentifiantPays))
        {
            return false;
        }

        if (filtre.Divisions.Count > 0 && !filtre.Divisions.Contains(voiture.Division))
        {
            return false;
        }

        if (filtre.Transmissions.Count > 0 && !filtre.Transmissions.Contains(reglage.Generaux.Transmission))
        {
            return false;
        }

        if (filtre.AnneeMinimum is { } anneeMinimum && voiture.Annee < anneeMinimum)
        {
            return false;
        }

        if (filtre.AnneeMaximum is { } anneeMaximum && voiture.Annee > anneeMaximum)
        {
            return false;
        }

        var indice = reglage.Generaux.IndiceDePerformance;
        if (filtre.IndiceDePerformanceMinimum is { } indiceMinimum && indice < indiceMinimum)
        {
            return false;
        }

        if (filtre.IndiceDePerformanceMaximum is { } indiceMaximum && indice > indiceMaximum)
        {
            return false;
        }

        if (filtre.Origine == OrigineDuReglage.Origine && !reglage.EstOrigine)
        {
            return false;
        }

        if (filtre.Origine == OrigineDuReglage.Creation && reglage.EstOrigine)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filtre.Texte))
        {
            var texte = $"{marque.Nom} {voiture.Nom} {reglage.Nom}";
            var comparaison = CultureInfo.CurrentCulture.CompareInfo;
            var options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
            if (comparaison.IndexOf(texte, filtre.Texte.Trim(), options) < 0)
            {
                return false;
            }
        }

        foreach (var (critere, filtreNumerique) in criteres)
        {
            if (filtreNumerique.Minimum is null && filtreNumerique.Maximum is null)
            {
                continue;
            }

            if (critere.Lire(reglage) is not { } valeur)
            {
                return false; // la section n'existe pas pour ce réglage
            }

            if (filtreNumerique.Minimum is { } minimum && valeur < minimum)
            {
                return false;
            }

            if (filtreNumerique.Maximum is { } maximum && valeur > maximum)
            {
                return false;
            }
        }

        return true;
    }
}
