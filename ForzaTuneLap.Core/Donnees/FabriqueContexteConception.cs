using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForzaTuneLap.Core.Donnees;

/// <summary>
/// Utilisée uniquement par les outils EF Core (création des migrations) : le projet Core étant une bibliothèque,
/// les outils ont besoin de savoir comment construire le contexte.
/// </summary>
public sealed class FabriqueContexteConception : IDesignTimeDbContextFactory<ContexteDonnees>
{
    public ContexteDonnees CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ContexteDonnees>()
            .UseSqlite("Data Source=conception.db")
            .Options;

        return new ContexteDonnees(options);
    }
}
