using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.Core.Tests;

public class ServiceChronosListesTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private async Task<ContexteDonnees> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        return baseDeTest.CreerContexte();
    }

    private static async Task<Guid> CreerReglageAsync(ContexteDonnees contexte, string nom)
    {
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(1);
        brouillon.Nom = nom;
        await service.EnregistrerNouveauAsync(brouillon);
        return brouillon.Identifiant;
    }

    [Fact]
    public async Task Les_chronos_sont_regroupes_par_parcours_du_plus_rapide_au_plus_lent()
    {
        await using var contexte = await PreparerAsync();
        var course = await CreerReglageAsync(contexte, "Course");
        var qualification = await CreerReglageAsync(contexte, "Qualification");
        var service = new ServiceChronos(contexte);

        await service.AjouterAsync(course, 1, TimeSpan.FromSeconds(95));
        await service.AjouterAsync(qualification, 1, TimeSpan.FromSeconds(91));
        await service.AjouterAsync(course, 2, TimeSpan.FromSeconds(120));

        await using var lecture = baseDeTest.CreerContexte();
        var groupes = await new ServiceChronos(lecture).ListerParParcoursAsync();

        Assert.Equal(2, groupes.Count);
        var premierParcours = groupes.Single(g => g.Parcours.Identifiant == 1);
        Assert.Equal(new long[] { 91_000, 95_000 }, premierParcours.Chronos.Select(c => c.DureeMillisecondes));
        Assert.Equal("Qualification", premierParcours.Chronos[0].Reglage!.Nom);
        Assert.NotNull(premierParcours.Chronos[0].Reglage!.Voiture!.Marque);
        Assert.NotNull(premierParcours.Parcours.Circuit);
        Assert.Single(groupes.Single(g => g.Parcours.Identifiant == 2).Chronos);
    }

    [Fact]
    public async Task Sans_chrono_la_liste_par_parcours_est_vide()
    {
        await using var contexte = await PreparerAsync();

        Assert.Empty(await new ServiceChronos(contexte).ListerParParcoursAsync());
    }

    [Fact]
    public async Task Les_chronos_d_une_voiture_regroupent_tous_ses_reglages()
    {
        await using var contexte = await PreparerAsync();
        var course = await CreerReglageAsync(contexte, "Course");
        var qualification = await CreerReglageAsync(contexte, "Qualification");
        var service = new ServiceChronos(contexte);
        await service.AjouterAsync(course, 1, TimeSpan.FromSeconds(95));
        await service.AjouterAsync(qualification, 1, TimeSpan.FromSeconds(91));
        await service.AjouterAsync(course, 2, TimeSpan.FromSeconds(120));

        // Un chrono sur le réglage d'une autre voiture ne doit pas apparaître.
        var autreVoiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
        var reglageAutre = BaseDeDonneesDeTest.CreerReglage(autreVoiture.Identifiant, "Autre");
        contexte.Reglages.Add(reglageAutre);
        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglageAutre.Identifiant));
        await contexte.SaveChangesAsync();

        await using var lecture = baseDeTest.CreerContexte();
        var chronos = await new ServiceChronos(lecture).ListerDeLaVoitureAsync(1);

        Assert.Equal(3, chronos.Count);
        Assert.All(chronos, c => Assert.NotNull(c.Parcours!.Circuit));
        Assert.All(chronos, c => Assert.NotNull(c.Reglage));
        Assert.Empty(await new ServiceChronos(lecture).ListerDeLaVoitureAsync(99));
    }

    [Fact]
    public async Task Les_reglages_ayant_deja_un_chrono_sur_le_parcours_sont_signales()
    {
        await using var contexte = await PreparerAsync();
        var course = await CreerReglageAsync(contexte, "Course");
        var qualification = await CreerReglageAsync(contexte, "Qualification");
        var service = new ServiceChronos(contexte);
        await service.AjouterAsync(course, 1, TimeSpan.FromSeconds(95));
        await service.AjouterAsync(qualification, 2, TimeSpan.FromSeconds(91));

        var surPremierParcours = await service.ReglagesAvecChronoAsync(1, 1);
        var surSecondParcours = await service.ReglagesAvecChronoAsync(1, 2);
        var autreVoiture = await service.ReglagesAvecChronoAsync(2, 1);

        Assert.Equal(new[] { course }, surPremierParcours);
        Assert.Equal(new[] { qualification }, surSecondParcours);
        Assert.Empty(autreVoiture);
    }
}
