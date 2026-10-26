using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class UniteDeTravailTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private FabriqueUniteDeTravail CreerFabrique() => new(new FabriqueDeContextesDeTest(baseDeTest));

    [Fact]
    public async Task Chaque_unite_de_travail_a_son_propre_contexte()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        var fabrique = CreerFabrique();

        using var premiere = fabrique.Creer();
        using var seconde = fabrique.Creer();

        Assert.NotSame(premiere.Contexte, seconde.Contexte);
    }

    [Fact]
    public async Task Les_donnees_ecrites_dans_une_unite_sont_visibles_dans_une_autre()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        var fabrique = CreerFabrique();
        Guid identifiant;

        using (var ecriture = fabrique.Creer())
        {
            var brouillon = await ecriture.Reglages.CreerBrouillonAsync(1);
            brouillon.Nom = "Course";
            await ecriture.Reglages.EnregistrerNouveauAsync(brouillon);
            identifiant = brouillon.Identifiant;
            await ecriture.Chronos.AjouterAsync(identifiant, 1, TimeSpan.FromSeconds(90));
        }

        using var lecture = fabrique.Creer();
        var reglage = await lecture.Reglages.ObtenirPourEditionAsync(identifiant);
        var classement = await lecture.ComparaisonChronos.ComparerAsync(1);

        Assert.Equal("Course", reglage.Nom);
        Assert.Single(classement.Lignes);
    }

    [Fact]
    public async Task Liberer_l_unite_de_travail_libere_son_contexte()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        var travail = CreerFabrique().Creer();

        travail.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => travail.Contexte.Pays.CountAsync());
    }
}
