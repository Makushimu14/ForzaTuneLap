using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Criteres;

/// <summary>
/// Liste de tous les critères numériques comparables. Écrite explicitement (sans réflexion) pour rester
/// compatible avec la compilation optimisée d'iOS et de Mac Catalyst.
/// Les critères textuels (moteur, code, transmission) ne sont pas ici : ils servent de filtres.
/// </summary>
public static class CatalogueCriteres
{
    public const int NombreMaximumDeRapports = 10;

    private static readonly IReadOnlyList<DescriptionCritere> Liste = Construire();
    private static readonly Dictionary<string, DescriptionCritere> ParCle = Liste.ToDictionary(c => c.Cle);

    public static IReadOnlyList<DescriptionCritere> Tous => Liste;

    public static DescriptionCritere? Rechercher(string cle) => ParCle.GetValueOrDefault(cle);

    public static IEnumerable<DescriptionCritere> DeLaCategorie(CategorieDeReglage categorie) =>
        Liste.Where(c => c.Categorie == categorie);

    private static List<DescriptionCritere> Construire()
    {
        var neutre = SensPrefere.Neutre;
        var plusGrand = SensPrefere.PlusGrandEstMeilleur;
        var plusPetit = SensPrefere.PlusPetitEstMeilleur;

        var liste = new List<DescriptionCritere>
        {
            // Généraux
            new("Generaux.IndiceDePerformance", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, neutre, r => r.Generaux.IndiceDePerformance, 100, 999),
            new("Generaux.Puissance", CategorieDeReglage.Generaux, TypeDeGrandeur.Puissance, plusGrand, r => r.Generaux.Puissance),
            new("Generaux.Couple", CategorieDeReglage.Generaux, TypeDeGrandeur.Couple, plusGrand, r => r.Generaux.Couple),
            new("Generaux.Poids", CategorieDeReglage.Generaux, TypeDeGrandeur.Masse, neutre, r => r.Generaux.Poids),
            new("Generaux.RepartitionPoidsAvant", CategorieDeReglage.Generaux, TypeDeGrandeur.Pourcentage, neutre, r => r.Generaux.RepartitionPoidsAvant, 0, 100),
            new("Generaux.NoteAcceleration", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteAcceleration, 0, 10),
            new("Generaux.NoteFreinage", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteFreinage, 0, 10),
            new("Generaux.NoteTenueDeRoute", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteTenueDeRoute, 0, 10),
            new("Generaux.NoteVitesse", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteVitesse, 0, 10),

            // Performances mesurées
            new("Performances.AccelerationDeZeroA97", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.AccelerationDeZeroA97, 0),
            new("Performances.AccelerationDeZeroA161", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.AccelerationDeZeroA161, 0),
            new("Performances.Temps400Metres", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.Temps400Metres, 0),
            new("Performances.DistanceDeFreinageDe97AZero", CategorieDeReglage.Performances, TypeDeGrandeur.Distance, plusPetit, r => r.Performances.DistanceDeFreinageDe97AZero, 0),
            new("Performances.DistanceDeFreinageDe161AZero", CategorieDeReglage.Performances, TypeDeGrandeur.Distance, plusPetit, r => r.Performances.DistanceDeFreinageDe161AZero, 0),
            new("Performances.AccelerationLateraleA97", CategorieDeReglage.Performances, TypeDeGrandeur.AccelerationLaterale, plusGrand, r => r.Performances.AccelerationLateraleA97, 0),
            new("Performances.AccelerationLateraleA193", CategorieDeReglage.Performances, TypeDeGrandeur.AccelerationLaterale, plusGrand, r => r.Performances.AccelerationLateraleA193, 0),
            new("Performances.VitesseMaximale", CategorieDeReglage.Performances, TypeDeGrandeur.Vitesse, plusGrand, r => r.Performances.VitesseMaximale, 0),

            // Pneus (la plage 1 à 3,8 de l'ancien modèle est à rattacher à son unité : non reprise ici)
            new("Pneus.PressionAvant", CategorieDeReglage.Pneus, TypeDeGrandeur.Pression, neutre, r => r.Pneus?.PressionAvant),
            new("Pneus.PressionArriere", CategorieDeReglage.Pneus, TypeDeGrandeur.Pression, neutre, r => r.Pneus?.PressionArriere),

            // Alignement
            new("Alignement.CarrossageAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.CarrossageAvant, -5, 5),
            new("Alignement.CarrossageArriere", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.CarrossageArriere, -5, 5),
            new("Alignement.ParallelismeAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.ParallelismeAvant, -5, 5),
            new("Alignement.ParallelismeArriere", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.ParallelismeArriere, -5, 5),
            new("Alignement.AngleDeChasseAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.AngleDeChasseAvant, 1, 7),

            // Ressorts
            new("Ressorts.RessortsAvant", CategorieDeReglage.Ressorts, TypeDeGrandeur.RaideurDeRessort, neutre, r => r.Ressorts?.RessortsAvant),
            new("Ressorts.RessortsArriere", CategorieDeReglage.Ressorts, TypeDeGrandeur.RaideurDeRessort, neutre, r => r.Ressorts?.RessortsArriere),
            new("Ressorts.GardeAuSolAvant", CategorieDeReglage.Ressorts, TypeDeGrandeur.Hauteur, neutre, r => r.Ressorts?.GardeAuSolAvant),
            new("Ressorts.GardeAuSolArriere", CategorieDeReglage.Ressorts, TypeDeGrandeur.Hauteur, neutre, r => r.Ressorts?.GardeAuSolArriere),

            // Barres anti-roulis
            new("BarresAntiroulis.Avant", CategorieDeReglage.BarresAntiroulis, TypeDeGrandeur.SansUnite, neutre, r => r.BarresAntiroulis?.Avant, 1, 40),
            new("BarresAntiroulis.Arriere", CategorieDeReglage.BarresAntiroulis, TypeDeGrandeur.SansUnite, neutre, r => r.BarresAntiroulis?.Arriere, 1, 40),

            // Amortisseurs
            new("Amortisseurs.RigiditeAmortissementAvant", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeAmortissementAvant, 1, 13),
            new("Amortisseurs.RigiditeAmortissementArriere", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeAmortissementArriere, 1, 13),
            new("Amortisseurs.RigiditeDetenteAvant", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeDetenteAvant, 1, 13),
            new("Amortisseurs.RigiditeDetenteArriere", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeDetenteArriere, 1, 13),

            // Géométrie de suspension
            new("GeometrieDeSuspension.HauteurCentreRoulisAvant", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Hauteur, neutre, r => r.GeometrieDeSuspension?.HauteurCentreRoulisAvant),
            new("GeometrieDeSuspension.HauteurCentreRoulisArriere", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Hauteur, neutre, r => r.GeometrieDeSuspension?.HauteurCentreRoulisArriere),
            new("GeometrieDeSuspension.PourcentageAntigeometrieAvant", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Pourcentage, neutre, r => r.GeometrieDeSuspension?.PourcentageAntigeometrieAvant, -50, 150),
            new("GeometrieDeSuspension.PourcentageAntigeometrieArriere", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Pourcentage, neutre, r => r.GeometrieDeSuspension?.PourcentageAntigeometrieArriere, -50, 150),

            // Différentiel
            new("Differentiel.Acceleration", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.Acceleration, 0, 100),
            new("Differentiel.Deceleration", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.Deceleration, 0, 100),
            new("Differentiel.EquilibreCentral", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.EquilibreCentral, 0, 100),

            // Freins
            new("Freins.PuissanceFreinageEquilibre", CategorieDeReglage.Freins, TypeDeGrandeur.Pourcentage, neutre, r => r.Freins?.PuissanceFreinageEquilibre, 0, 100),
            new("Freins.PuissanceFreinagePression", CategorieDeReglage.Freins, TypeDeGrandeur.Pourcentage, neutre, r => r.Freins?.PuissanceFreinagePression, 0, 200),

            // Aérodynamisme
            new("Aerodynamisme.PortanceNegativeAvant", CategorieDeReglage.Aerodynamisme, TypeDeGrandeur.Force, neutre, r => r.Aerodynamisme?.PortanceNegativeAvant),
            new("Aerodynamisme.PortanceNegativeArriere", CategorieDeReglage.Aerodynamisme, TypeDeGrandeur.Force, neutre, r => r.Aerodynamisme?.PortanceNegativeArriere),

            // Volant
            new("Volant.AngleBraquage", CategorieDeReglage.Volant, TypeDeGrandeur.Angle, neutre, r => r.Volant?.AngleBraquage, 50, 200),
            new("Volant.IntensiteRetourDeForce", CategorieDeReglage.Volant, TypeDeGrandeur.SansUnite, neutre, r => r.Volant?.IntensiteRetourDeForce, 0, 200),

            // Rapports (hors rapports de boîte numérotés, ajoutés ci-dessous)
            new("Rapports.NombreDeRapports", CategorieDeReglage.Rapports, TypeDeGrandeur.SansUnite, neutre, r => r.RapportsDeBoite.Count == 0 ? (double?)null : r.RapportsDeBoite.Count, 1, NombreMaximumDeRapports),
            new("Rapports.RapportFinal", CategorieDeReglage.Rapports, TypeDeGrandeur.SansUnite, neutre, r => r.RapportFinal, 2.2, 6.1),
        };

        for (var n = 1; n <= NombreMaximumDeRapports; n++)
        {
            var numero = n; // copie locale pour la fermeture
            liste.Add(new DescriptionCritere(
                $"Rapports.Rapport{numero}",
                CategorieDeReglage.Rapports,
                TypeDeGrandeur.SansUnite,
                SensPrefere.Neutre,
                r => r.RapportsDeBoite.FirstOrDefault(rapport => rapport.Numero == numero)?.Valeur,
                0.48,
                6));
        }

        return liste;
    }
}
