using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Tests;

public class ValidateurReferentielTests
{
    private static FichierVoitures LireExemple()
    {
        using var flux = typeof(ValidateurReferentielTests).Assembly.GetManifestResourceStream("voitures.exemple.json")
            ?? throw new InvalidOperationException("voitures.exemple.json introuvable.");
        return ChargeurReferentiel.LireVoitures(flux);
    }

    [Fact]
    public void Les_fichiers_embarques_sont_valides()
    {
        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), ChargeurReferentiel.LireVoitures());

        Assert.Empty(erreurs);
    }

    [Fact]
    public void Le_fichier_d_exemple_est_valide()
    {
        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), LireExemple());

        Assert.Empty(erreurs);
    }

    [Fact]
    public void Le_fichier_d_exemple_est_lu_correctement()
    {
        var voiture = LireExemple().Marques.Single().Voitures.Single();

        Assert.Equal(Division.ForzaGT3, voiture.Division);
        Assert.NotNull(voiture.ReglageOrigine);
        Assert.Equal(Transmission.Propulsion, voiture.ReglageOrigine!.Generaux.Transmission);
        Assert.Equal(6, voiture.ReglageOrigine.RapportsDeBoite.Count);
        Assert.Null(voiture.ReglageOrigine.Aerodynamisme);
    }

    [Fact]
    public void Un_pays_inconnu_est_signale()
    {
        var voitures = LireExemple();
        voitures.Marques[0].IdentifiantPays = 9999;

        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), voitures);

        Assert.Contains(erreurs, e => e.Contains("9999"));
    }

    [Fact]
    public void Un_identifiant_en_double_est_signale()
    {
        var voitures = LireExemple();
        voitures.Marques.Add(new MarqueFichier { Identifiant = 1, Nom = "Autre", IdentifiantPays = 11 });

        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), voitures);

        Assert.Contains(erreurs, e => e.Contains("double") && e.Contains("marque"));
    }

    [Fact]
    public void Une_valeur_hors_plage_est_signalee()
    {
        var voitures = LireExemple();
        voitures.Marques[0].Voitures[0].ReglageOrigine!.Amortisseurs!.RigiditeDetenteAvant = 25;

        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), voitures);

        Assert.Contains(erreurs, e => e.Contains("Amortisseurs.RigiditeDetenteAvant"));
    }

    [Fact]
    public void Des_rapports_de_boite_croissants_sont_signales()
    {
        var voitures = LireExemple();
        voitures.Marques[0].Voitures[0].ReglageOrigine!.RapportsDeBoite = new List<double> { 2.0, 3.0 };

        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), voitures);

        Assert.Contains(erreurs, e => e.Contains("rapport 2"));
    }

    [Fact]
    public void Un_identifiant_de_voiture_dans_la_plage_personnalisee_est_signale()
    {
        var voitures = LireExemple();
        voitures.Marques[0].Voitures[0].Identifiant = ConstantesReferentiel.PremierIdentifiantPersonnalise;

        var erreurs = ValidateurReferentiel.Valider(ChargeurReferentiel.LireCircuits(), voitures);

        Assert.Contains(erreurs, e => e.Contains("inférieur"));
    }
}
