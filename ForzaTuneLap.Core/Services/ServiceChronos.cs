using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

/// <summary>Les chronos d'un parcours, du plus rapide au plus lent.</summary>
public sealed record GroupeChronosParcours(Parcours Parcours, IReadOnlyList<Chrono> Chronos);

/// <summary>
/// Saisie et correction des chronos. Il existe au plus un chrono par couple (réglage, parcours) :
/// ajouter un deuxième chrono pour ce couple est refusé (<see cref="CodeErreurMetier.ChronoDejaExistant"/>),
/// mais le chrono existant peut toujours être modifié, quelle que soit la nouvelle valeur.
/// Un chrono peut être rattaché à un réglage d'origine.
/// </summary>
public sealed class ServiceChronos
{
    private readonly ContexteDonnees contexte;

    public ServiceChronos(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    /// <summary>Chrono d'un réglage sur un parcours, ou null s'il n'y en a pas (l'interface propose alors « Ajouter »).</summary>
    public Task<Chrono?> RechercherAsync(Guid identifiantReglage, int identifiantParcours, CancellationToken annulation = default) =>
        contexte.Chronos.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.IdentifiantReglage == identifiantReglage && c.IdentifiantParcours == identifiantParcours,
                annulation);

    /// <summary>Chronos d'un réglage, avec leur parcours et leur circuit, triés par circuit puis par parcours.</summary>
    public async Task<IReadOnlyList<Chrono>> ListerDuReglageAsync(Guid identifiantReglage, CancellationToken annulation = default) =>
        await contexte.Chronos.AsNoTracking()
            .Include(c => c.Parcours).ThenInclude(p => p!.Circuit)
            .Where(c => c.IdentifiantReglage == identifiantReglage)
            .OrderBy(c => c.Parcours!.Circuit!.Nom)
            .ThenBy(c => c.Parcours!.NomFrancais)
            .ToListAsync(annulation);

    /// <summary>
    /// Tous les chronos, regroupés par parcours (triés par circuit puis par parcours), du plus rapide au plus lent dans
    /// chaque groupe. Chaque chrono porte son réglage, sa voiture et la marque. Sert à l'onglet Chronos.
    /// </summary>
    public async Task<IReadOnlyList<GroupeChronosParcours>> ListerParParcoursAsync(CancellationToken annulation = default)
    {
        var chronos = await contexte.Chronos.AsNoTracking()
            .Include(c => c.Parcours).ThenInclude(p => p!.Circuit)
            .Include(c => c.Reglage).ThenInclude(r => r!.Voiture).ThenInclude(v => v!.Marque)
            .ToListAsync(annulation);

        return chronos
            .GroupBy(c => c.IdentifiantParcours)
            .Select(groupe => new GroupeChronosParcours(
                groupe.First().Parcours!,
                groupe.OrderBy(c => c.DureeMillisecondes)
                    .ThenBy(c => c.DateRealisation)
                    .ToList()))
            .OrderBy(groupe => groupe.Parcours.Circuit!.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(groupe => groupe.Parcours.NomFrancais, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Chronos de tous les réglages d'une voiture, avec leur parcours, leur circuit et leur réglage, triés par circuit,
    /// par parcours puis du plus rapide au plus lent. Sert au détail d'une voiture.
    /// </summary>
    public async Task<IReadOnlyList<Chrono>> ListerDeLaVoitureAsync(int identifiantVoiture, CancellationToken annulation = default)
    {
        var chronos = await contexte.Chronos.AsNoTracking()
            .Include(c => c.Parcours).ThenInclude(p => p!.Circuit)
            .Include(c => c.Reglage)
            .Where(c => c.Reglage!.IdentifiantVoiture == identifiantVoiture)
            .ToListAsync(annulation);

        return chronos
            .OrderBy(c => c.Parcours!.Circuit!.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(c => c.Parcours!.NomFrancais, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(c => c.DureeMillisecondes)
            .ToList();
    }

    /// <summary>
    /// Identifiants des réglages d'une voiture qui ont déjà un chrono sur ce parcours : l'écran d'ajout les signale,
    /// puisque le deuxième chrono d'un même couple est refusé.
    /// </summary>
    public async Task<IReadOnlySet<Guid>> ReglagesAvecChronoAsync(
        int identifiantVoiture,
        int identifiantParcours,
        CancellationToken annulation = default)
    {
        var identifiants = await contexte.Chronos.AsNoTracking()
            .Where(c => c.IdentifiantParcours == identifiantParcours && c.Reglage!.IdentifiantVoiture == identifiantVoiture)
            .Select(c => c.IdentifiantReglage)
            .ToListAsync(annulation);

        return identifiants.ToHashSet();
    }

    public async Task<Chrono> AjouterAsync(
        Guid identifiantReglage,
        int identifiantParcours,
        TimeSpan duree,
        DateTime? dateRealisation = null,
        string? commentaire = null,
        CancellationToken annulation = default)
    {
        var millisecondes = ConvertirDuree(duree);

        if (!await contexte.Reglages.AnyAsync(r => r.Identifiant == identifiantReglage, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.ReglageIntrouvable);
        }

        if (!await contexte.Parcours.AnyAsync(p => p.Identifiant == identifiantParcours, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.ParcoursIntrouvable);
        }

        var existe = await contexte.Chronos.AnyAsync(
            c => c.IdentifiantReglage == identifiantReglage && c.IdentifiantParcours == identifiantParcours,
            annulation);
        if (existe)
        {
            throw new ErreurMetierException(CodeErreurMetier.ChronoDejaExistant);
        }

        var chrono = new Chrono
        {
            IdentifiantReglage = identifiantReglage,
            IdentifiantParcours = identifiantParcours,
            DureeMillisecondes = millisecondes,
            DateRealisation = dateRealisation ?? DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Commentaire = NettoyerCommentaire(commentaire)
        };

        contexte.Chronos.Add(chrono);
        await contexte.SaveChangesAsync(annulation);
        return chrono;
    }

    /// <summary>
    /// Corrige un chrono existant. Le réglage et le parcours ne changent pas : pour déplacer un chrono,
    /// on le supprime puis on en ajoute un nouveau.
    /// </summary>
    public async Task<Chrono> ModifierAsync(
        Guid identifiantChrono,
        TimeSpan duree,
        DateTime dateRealisation,
        string? commentaire = null,
        CancellationToken annulation = default)
    {
        var millisecondes = ConvertirDuree(duree);

        var chrono = await contexte.Chronos.FirstOrDefaultAsync(c => c.Identifiant == identifiantChrono, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.ChronoIntrouvable);

        chrono.DureeMillisecondes = millisecondes;
        chrono.DateRealisation = dateRealisation;
        chrono.Commentaire = NettoyerCommentaire(commentaire);
        chrono.DateModification = DateTime.UtcNow;

        await contexte.SaveChangesAsync(annulation);
        return chrono;
    }

    public async Task SupprimerAsync(Guid identifiantChrono, CancellationToken annulation = default)
    {
        var supprimes = await contexte.Chronos
            .Where(c => c.Identifiant == identifiantChrono)
            .ExecuteDeleteAsync(annulation);

        if (supprimes == 0)
        {
            throw new ErreurMetierException(CodeErreurMetier.ChronoIntrouvable);
        }
    }

    private static long ConvertirDuree(TimeSpan duree)
    {
        var millisecondes = (long)Math.Round(duree.TotalMilliseconds);
        if (millisecondes <= 0)
        {
            throw new ErreurMetierException(CodeErreurMetier.DureeInvalide);
        }

        return millisecondes;
    }

    private static string? NettoyerCommentaire(string? commentaire) =>
        string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();
}
