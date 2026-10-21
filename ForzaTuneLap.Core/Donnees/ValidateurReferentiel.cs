using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Donnees;

/// <summary>
/// Contrôle les fichiers de données et renvoie la liste des problèmes trouvés (liste vide = tout va bien).
/// Utile pour repérer les fautes de frappe quand on saisit des centaines de voitures.
/// Les bornes sont celles, indicatives, du catalogue de critères.
/// </summary>
public static class ValidateurReferentiel
{
    public static IReadOnlyList<string> Valider(FichierCircuits circuits, FichierVoitures voitures)
    {
        var erreurs = new List<string>();

        var tousLesCircuits = circuits.Pays.SelectMany(p => p.Circuits).ToList();
        var tousLesParcours = tousLesCircuits.SelectMany(c => c.Parcours).ToList();
        var toutesLesMarques = voitures.Marques;
        var toutesLesVoitures = toutesLesMarques.SelectMany(m => m.Voitures).ToList();

        VerifierDoublons(erreurs, "pays", circuits.Pays.Select(p => p.Identifiant));
        VerifierDoublons(erreurs, "circuit", tousLesCircuits.Select(c => c.Identifiant));
        VerifierDoublons(erreurs, "parcours", tousLesParcours.Select(p => p.Identifiant));
        VerifierDoublons(erreurs, "marque", toutesLesMarques.Select(m => m.Identifiant));
        VerifierDoublons(erreurs, "voiture", toutesLesVoitures.Select(v => v.Identifiant));

        var identifiantsReglages = toutesLesVoitures
            .Where(v => v.ReglageOrigine is not null)
            .Select(v => v.ReglageOrigine!.Identifiant)
            .ToList();
        VerifierDoublons(erreurs, "réglage d'origine (GUID)", identifiantsReglages);

        var identifiantsPays = circuits.Pays.Select(p => p.Identifiant).ToHashSet();

        foreach (var marque in toutesLesMarques)
        {
            if (string.IsNullOrWhiteSpace(marque.Nom))
            {
                erreurs.Add($"Marque {marque.Identifiant} : nom vide.");
            }

            if (marque.Identifiant >= ConstantesReferentiel.PremierIdentifiantPersonnalise)
            {
                erreurs.Add($"Marque « {marque.Nom} » : l'identifiant {marque.Identifiant} doit être inférieur à {ConstantesReferentiel.PremierIdentifiantPersonnalise}.");
            }

            if (!identifiantsPays.Contains(marque.IdentifiantPays))
            {
                erreurs.Add($"Marque « {marque.Nom} » : le pays {marque.IdentifiantPays} n'existe pas dans circuits.json.");
            }

            foreach (var voiture in marque.Voitures)
            {
                ValiderVoiture(erreurs, marque, voiture);
            }
        }

        return erreurs;
    }

    private static void ValiderVoiture(List<string> erreurs, MarqueFichier marque, VoitureFichier voiture)
    {
        var nom = $"{marque.Nom} {voiture.Nom}";

        if (string.IsNullOrWhiteSpace(voiture.Nom))
        {
            erreurs.Add($"Voiture {voiture.Identifiant} ({marque.Nom}) : nom vide.");
        }

        if (voiture.Identifiant >= ConstantesReferentiel.PremierIdentifiantPersonnalise)
        {
            erreurs.Add($"Voiture « {nom} » : l'identifiant {voiture.Identifiant} doit être inférieur à {ConstantesReferentiel.PremierIdentifiantPersonnalise}.");
        }

        if (voiture.Annee is < 1900 or > 2100)
        {
            erreurs.Add($"Voiture « {nom} » : année {voiture.Annee} invraisemblable.");
        }

        if (voiture.Valeur < 0)
        {
            erreurs.Add($"Voiture « {nom} » : prix négatif.");
        }

        var origine = voiture.ReglageOrigine;
        if (origine is null)
        {
            return;
        }

        if (origine.Identifiant == Guid.Empty)
        {
            erreurs.Add($"Voiture « {nom} » : le réglage d'origine n'a pas d'identifiant (GUID).");
        }

        if (origine.RapportsDeBoite.Count > CatalogueCriteres.NombreMaximumDeRapports)
        {
            erreurs.Add($"Voiture « {nom} » : {origine.RapportsDeBoite.Count} rapports de boîte (maximum {CatalogueCriteres.NombreMaximumDeRapports}).");
        }

        for (var i = 1; i < origine.RapportsDeBoite.Count; i++)
        {
            if (origine.RapportsDeBoite[i] > origine.RapportsDeBoite[i - 1])
            {
                erreurs.Add($"Voiture « {nom} » : le rapport {i + 1} est plus grand que le rapport {i}.");
            }
        }

        // Bornes indicatives du catalogue, appliquées au réglage tel qu'il sera enregistré.
        var reglage = origine.VersReglage(voiture.Identifiant, string.Empty, DateTime.UtcNow);
        foreach (var critere in CatalogueCriteres.Tous)
        {
            if (critere.Lire(reglage) is not { } valeur)
            {
                continue;
            }

            if (critere.Minimum is { } minimum && valeur < minimum)
            {
                erreurs.Add($"Voiture « {nom} » : {critere.Cle} = {valeur} est inférieur à {minimum}.");
            }

            if (critere.Maximum is { } maximum && valeur > maximum)
            {
                erreurs.Add($"Voiture « {nom} » : {critere.Cle} = {valeur} est supérieur à {maximum}.");
            }
        }
    }

    private static void VerifierDoublons<T>(List<string> erreurs, string nature, IEnumerable<T> identifiants)
        where T : notnull
    {
        foreach (var doublon in identifiants.GroupBy(i => i).Where(g => g.Count() > 1))
        {
            erreurs.Add($"Identifiant en double ({nature}) : {doublon.Key}.");
        }
    }
}
