using ForzaTuneLap.Core.Donnees;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

/// <summary>Fabrique de contextes pour les tests : chaque appel renvoie un contexte neuf sur la même base en mémoire.</summary>
public sealed class FabriqueDeContextesDeTest : IDbContextFactory<ContexteDonnees>
{
    private readonly BaseDeDonneesDeTest baseDeTest;

    public FabriqueDeContextesDeTest(BaseDeDonneesDeTest baseDeTest)
    {
        this.baseDeTest = baseDeTest;
    }

    public ContexteDonnees CreateDbContext() => baseDeTest.CreerContexte();
}
