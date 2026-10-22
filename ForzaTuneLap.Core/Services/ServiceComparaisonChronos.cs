using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

/// <summary>Une ligne du classement : le chrono, le réglage et la voiture qui l'ont réalisé.</summary>
/// <param name="Rang">1 pour le plus rapide. Deux temps identiques partagent le même rang.</param>
/// <param name="EcartMillisecondes">Retard sur le meilleur temps affiché (0 pour le premier).</param>
/// <param name="EcartPourcentage">Même retard, en pourcentage du meilleur temps.</param>
public sealed record LigneComparaisonChrono(
    int Rang,
    Chrono Chrono,
    Reglage Reglage,
    Voiture Voiture,
    long EcartMillisecondes,
    double EcartPourcentage);

public sealed record ResultatComparaisonChronos(Parcours Parcours, IReadOnlyList<LigneComparaisonChrono> Lignes);

/// <summary>
/// Compare les chronos d'un parcours précis, du plus rapide au plus lent, avec des filtres sur la voiture
/// et son réglage. Les filtres sont appliqués en mémoire : le nombre de chronos d'un même parcours reste petit
/// et cela permet de filtrer sur n'importe quel critère du catalogue.
/// </summary>
public sealed class ServiceComparaisonChronos
{
    private readonly ContexteDonnees contexte;

    public ServiceComparaisonChronos(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    public async Task<ResultatComparaisonChronos> ComparerAsync(
        int identifiantParcours,
        FiltreChronos? filtre = null,
        CancellationToken annulation = default)
    {
        var parcours = await contexte.Parcours.AsNoTracking()
            .Include(p => p.Circuit)
            .FirstOrDefaultAsync(p => p.Identifiant == identifiantParcours, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.ParcoursIntrouvable);

        var criteres = ApplicationFiltre.ResoudreCriteres(filtre);

        var chronos = await contexte.Chronos.AsNoTracking()
            .Include(c => c.Reglage).ThenInclude(r => r!.Voiture).ThenInclude(v => v!.Marque)
            .Where(c => c.IdentifiantParcours == identifiantParcours)
            .ToListAsync(annulation);

        var retenus = chronos
            .Where(c => filtre is null || ApplicationFiltre.Correspond(c.Reglage!, filtre, criteres))
            .OrderBy(c => c.DureeMillisecondes)
            .ThenBy(c => c.DateRealisation)
            .ThenBy(c => c.Reglage!.Voiture!.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var lignes = new List<LigneComparaisonChrono>(retenus.Count);
        var meilleur = retenus.Count > 0 ? retenus[0].DureeMillisecondes : 0;

        foreach (var chrono in retenus)
        {
            // Rang « à la compétition » : 1, 1, 3 en cas d'égalité pour la première place.
            var rang = 1 + retenus.Count(autre => autre.DureeMillisecondes < chrono.DureeMillisecondes);
            var ecart = chrono.DureeMillisecondes - meilleur;
            var pourcentage = meilleur > 0 ? ecart * 100.0 / meilleur : 0;

            lignes.Add(new LigneComparaisonChrono(rang, chrono, chrono.Reglage!, chrono.Reglage!.Voiture!, ecart, pourcentage));
        }

        return new ResultatComparaisonChronos(parcours, lignes);
    }
}
