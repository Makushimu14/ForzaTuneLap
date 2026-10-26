using ForzaTuneLap.Core.Donnees;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Un contexte de base de données et tous les services qui s'en servent, pour UNE opération (un écran, une action).
/// À créer par <see cref="FabriqueUniteDeTravail"/> dans un « using », puis à libérer.
/// Un contexte neuf à chaque fois évite de garder en mémoire des données périmées : le suivi d'EF Core ne dure
/// que le temps de l'opération.
/// </summary>
public sealed class UniteDeTravail : IDisposable
{
    public UniteDeTravail(ContexteDonnees contexte)
    {
        Contexte = contexte;
        Catalogue = new ServiceCatalogue(contexte);
        Reglages = new ServiceReglages(contexte);
        Chronos = new ServiceChronos(contexte);
        ComparaisonChronos = new ServiceComparaisonChronos(contexte);
        ComparaisonReglages = new ServiceComparaisonReglages(contexte);
        Sauvegarde = new ServiceSauvegarde(contexte);
    }

    /// <summary>Accès direct aux données pour les lectures simples (listes de voitures, de circuits…).</summary>
    public ContexteDonnees Contexte { get; }

    public ServiceCatalogue Catalogue { get; }
    public ServiceReglages Reglages { get; }
    public ServiceChronos Chronos { get; }
    public ServiceComparaisonChronos ComparaisonChronos { get; }
    public ServiceComparaisonReglages ComparaisonReglages { get; }
    public ServiceSauvegarde Sauvegarde { get; }

    public void Dispose() => Contexte.Dispose();
}

/// <summary>Crée des unités de travail. Se déclare une seule fois (singleton) dans l'application.</summary>
public sealed class FabriqueUniteDeTravail
{
    private readonly IDbContextFactory<ContexteDonnees> fabrique;

    public FabriqueUniteDeTravail(IDbContextFactory<ContexteDonnees> fabrique)
    {
        this.fabrique = fabrique;
    }

    public UniteDeTravail Creer() => new(fabrique.CreateDbContext());
}
