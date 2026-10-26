using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class ServiceDemarrageTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private ServiceDemarrage CreerService() => new(new FabriqueUniteDeTravail(new FabriqueDeContextesDeTest(baseDeTest)));

    [Fact]
    public async Task Le_demarrage_prepare_la_base_a_partir_des_fichiers_embarques()
    {
        var service = CreerService();
        Assert.Null(service.Resultat);

        var resultat = await service.DemarrerAsync();

        Assert.True(resultat.Fusions.EstVide);
        Assert.Same(resultat, service.Resultat);

        await using var contexte = baseDeTest.CreerContexte();
        Assert.Equal(19, await contexte.Pays.CountAsync());
        Assert.Equal(29, await contexte.Circuits.CountAsync());
        Assert.Equal(73, await contexte.Parcours.CountAsync());
    }

    [Fact]
    public async Task Le_demarrage_peut_etre_relance_sans_doublon()
    {
        var service = CreerService();

        await service.DemarrerAsync();
        await service.DemarrerAsync();

        await using var contexte = baseDeTest.CreerContexte();
        Assert.Equal(73, await contexte.Parcours.CountAsync());
    }
}
