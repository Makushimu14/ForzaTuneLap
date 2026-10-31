using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Criteres;

/// <summary>
/// Liste de tous les critères numériques comparables et éditables. Écrite explicitement (sans réflexion) pour rester
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
            // Generaux
            new("Generaux.IndiceDePerformance", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, neutre, r => r.Generaux.IndiceDePerformance, 100, 999, (r, v) => r.Generaux.IndiceDePerformance = (int)Math.Round(v ?? 0)),
            new("Generaux.Puissance", CategorieDeReglage.Generaux, TypeDeGrandeur.Puissance, plusGrand, r => r.Generaux.Puissance, null, null, (r, v) => r.Generaux.Puissance = v ?? 0),
            new("Generaux.Couple", CategorieDeReglage.Generaux, TypeDeGrandeur.Couple, plusGrand, r => r.Generaux.Couple, null, null, (r, v) => r.Generaux.Couple = v ?? 0),
            new("Generaux.Poids", CategorieDeReglage.Generaux, TypeDeGrandeur.Masse, neutre, r => r.Generaux.Poids, null, null, (r, v) => r.Generaux.Poids = v ?? 0),
            new("Generaux.RepartitionPoidsAvant", CategorieDeReglage.Generaux, TypeDeGrandeur.Pourcentage, neutre, r => r.Generaux.RepartitionPoidsAvant, 0, 100, (r, v) => r.Generaux.RepartitionPoidsAvant = v ?? 0),
            new("Generaux.NoteAcceleration", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteAcceleration, 0, 10, (r, v) => r.Generaux.NoteAcceleration = v ?? 0),
            new("Generaux.NoteFreinage", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteFreinage, 0, 10, (r, v) => r.Generaux.NoteFreinage = v ?? 0),
            new("Generaux.NoteTenueDeRoute", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteTenueDeRoute, 0, 10, (r, v) => r.Generaux.NoteTenueDeRoute = v ?? 0),
            new("Generaux.NoteVitesse", CategorieDeReglage.Generaux, TypeDeGrandeur.SansUnite, plusGrand, r => r.Generaux.NoteVitesse, 0, 10, (r, v) => r.Generaux.NoteVitesse = v ?? 0),

            // Performances
            new("Performances.AccelerationDeZeroA97", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.AccelerationDeZeroA97, 0, null, (r, v) => r.Performances.AccelerationDeZeroA97 = v ?? 0),
            new("Performances.AccelerationDeZeroA161", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.AccelerationDeZeroA161, 0, null, (r, v) => r.Performances.AccelerationDeZeroA161 = v ?? 0),
            new("Performances.Temps400Metres", CategorieDeReglage.Performances, TypeDeGrandeur.Duree, plusPetit, r => r.Performances.Temps400Metres, 0, null, (r, v) => r.Performances.Temps400Metres = v ?? 0),
            new("Performances.DistanceDeFreinageDe97AZero", CategorieDeReglage.Performances, TypeDeGrandeur.Distance, plusPetit, r => r.Performances.DistanceDeFreinageDe97AZero, 0, null, (r, v) => r.Performances.DistanceDeFreinageDe97AZero = v ?? 0),
            new("Performances.DistanceDeFreinageDe161AZero", CategorieDeReglage.Performances, TypeDeGrandeur.Distance, plusPetit, r => r.Performances.DistanceDeFreinageDe161AZero, 0, null, (r, v) => r.Performances.DistanceDeFreinageDe161AZero = v ?? 0),
            new("Performances.AccelerationLateraleA97", CategorieDeReglage.Performances, TypeDeGrandeur.AccelerationLaterale, plusGrand, r => r.Performances.AccelerationLateraleA97, 0, null, (r, v) => r.Performances.AccelerationLateraleA97 = v ?? 0),
            new("Performances.AccelerationLateraleA193", CategorieDeReglage.Performances, TypeDeGrandeur.AccelerationLaterale, plusGrand, r => r.Performances.AccelerationLateraleA193, 0, null, (r, v) => r.Performances.AccelerationLateraleA193 = v ?? 0),
            new("Performances.VitesseMaximale", CategorieDeReglage.Performances, TypeDeGrandeur.Vitesse, plusGrand, r => r.Performances.VitesseMaximale, 0, null, (r, v) => r.Performances.VitesseMaximale = v ?? 0),

            // Pneus
            new("Pneus.PressionAvant", CategorieDeReglage.Pneus, TypeDeGrandeur.Pression, neutre, r => r.Pneus?.PressionAvant, null, null, (r, v) => r.Pneus!.PressionAvant = v ?? 0),
            new("Pneus.PressionArriere", CategorieDeReglage.Pneus, TypeDeGrandeur.Pression, neutre, r => r.Pneus?.PressionArriere, null, null, (r, v) => r.Pneus!.PressionArriere = v ?? 0),

            // Alignement
            new("Alignement.CarrossageAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.CarrossageAvant, -5, 5, (r, v) => r.Alignement!.CarrossageAvant = v ?? 0),
            new("Alignement.CarrossageArriere", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.CarrossageArriere, -5, 5, (r, v) => r.Alignement!.CarrossageArriere = v ?? 0),
            new("Alignement.ParallelismeAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.ParallelismeAvant, -5, 5, (r, v) => r.Alignement!.ParallelismeAvant = v ?? 0),
            new("Alignement.ParallelismeArriere", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.ParallelismeArriere, -5, 5, (r, v) => r.Alignement!.ParallelismeArriere = v ?? 0),
            new("Alignement.AngleDeChasseAvant", CategorieDeReglage.Alignement, TypeDeGrandeur.Angle, neutre, r => r.Alignement?.AngleDeChasseAvant, 1, 7, (r, v) => r.Alignement!.AngleDeChasseAvant = v ?? 0),

            // Ressorts
            new("Ressorts.RessortsAvant", CategorieDeReglage.Ressorts, TypeDeGrandeur.RaideurDeRessort, neutre, r => r.Ressorts?.RessortsAvant, null, null, (r, v) => r.Ressorts!.RessortsAvant = v ?? 0),
            new("Ressorts.RessortsArriere", CategorieDeReglage.Ressorts, TypeDeGrandeur.RaideurDeRessort, neutre, r => r.Ressorts?.RessortsArriere, null, null, (r, v) => r.Ressorts!.RessortsArriere = v ?? 0),
            new("Ressorts.GardeAuSolAvant", CategorieDeReglage.Ressorts, TypeDeGrandeur.Hauteur, neutre, r => r.Ressorts?.GardeAuSolAvant, null, null, (r, v) => r.Ressorts!.GardeAuSolAvant = v ?? 0),
            new("Ressorts.GardeAuSolArriere", CategorieDeReglage.Ressorts, TypeDeGrandeur.Hauteur, neutre, r => r.Ressorts?.GardeAuSolArriere, null, null, (r, v) => r.Ressorts!.GardeAuSolArriere = v ?? 0),

            // BarresAntiroulis
            new("BarresAntiroulis.Avant", CategorieDeReglage.BarresAntiroulis, TypeDeGrandeur.SansUnite, neutre, r => r.BarresAntiroulis?.Avant, 1, 40, (r, v) => r.BarresAntiroulis!.Avant = v ?? 0),
            new("BarresAntiroulis.Arriere", CategorieDeReglage.BarresAntiroulis, TypeDeGrandeur.SansUnite, neutre, r => r.BarresAntiroulis?.Arriere, 1, 40, (r, v) => r.BarresAntiroulis!.Arriere = v ?? 0),

            // Amortisseurs
            new("Amortisseurs.RigiditeAmortissementAvant", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeAmortissementAvant, 1, 13, (r, v) => r.Amortisseurs!.RigiditeAmortissementAvant = v ?? 0),
            new("Amortisseurs.RigiditeAmortissementArriere", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeAmortissementArriere, 1, 13, (r, v) => r.Amortisseurs!.RigiditeAmortissementArriere = v ?? 0),
            new("Amortisseurs.RigiditeDetenteAvant", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeDetenteAvant, 1, 13, (r, v) => r.Amortisseurs!.RigiditeDetenteAvant = v ?? 0),
            new("Amortisseurs.RigiditeDetenteArriere", CategorieDeReglage.Amortisseurs, TypeDeGrandeur.SansUnite, neutre, r => r.Amortisseurs?.RigiditeDetenteArriere, 1, 13, (r, v) => r.Amortisseurs!.RigiditeDetenteArriere = v ?? 0),

            // GeometrieDeSuspension
            new("GeometrieDeSuspension.HauteurCentreRoulisAvant", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Hauteur, neutre, r => r.GeometrieDeSuspension?.HauteurCentreRoulisAvant, null, null, (r, v) => r.GeometrieDeSuspension!.HauteurCentreRoulisAvant = v ?? 0),
            new("GeometrieDeSuspension.HauteurCentreRoulisArriere", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Hauteur, neutre, r => r.GeometrieDeSuspension?.HauteurCentreRoulisArriere, null, null, (r, v) => r.GeometrieDeSuspension!.HauteurCentreRoulisArriere = v ?? 0),
            new("GeometrieDeSuspension.PourcentageAntigeometrieAvant", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Pourcentage, neutre, r => r.GeometrieDeSuspension?.PourcentageAntigeometrieAvant, -50, 150, (r, v) => r.GeometrieDeSuspension!.PourcentageAntigeometrieAvant = v ?? 0),
            new("GeometrieDeSuspension.PourcentageAntigeometrieArriere", CategorieDeReglage.GeometrieDeSuspension, TypeDeGrandeur.Pourcentage, neutre, r => r.GeometrieDeSuspension?.PourcentageAntigeometrieArriere, -50, 150, (r, v) => r.GeometrieDeSuspension!.PourcentageAntigeometrieArriere = v ?? 0),

            // Differentiel
            new("Differentiel.Acceleration", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.Acceleration, 0, 100, (r, v) => r.Differentiel!.Acceleration = v ?? 0),
            new("Differentiel.Deceleration", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.Deceleration, 0, 100, (r, v) => r.Differentiel!.Deceleration = v ?? 0),
            new("Differentiel.EquilibreCentral", CategorieDeReglage.Differentiel, TypeDeGrandeur.Pourcentage, neutre, r => r.Differentiel?.EquilibreCentral, 0, 100, (r, v) => r.Differentiel!.EquilibreCentral = v, true),

            // Freins
            new("Freins.PuissanceFreinageEquilibre", CategorieDeReglage.Freins, TypeDeGrandeur.Pourcentage, neutre, r => r.Freins?.PuissanceFreinageEquilibre, 0, 100, (r, v) => r.Freins!.PuissanceFreinageEquilibre = v ?? 0),
            new("Freins.PuissanceFreinagePression", CategorieDeReglage.Freins, TypeDeGrandeur.Pourcentage, neutre, r => r.Freins?.PuissanceFreinagePression, 0, 200, (r, v) => r.Freins!.PuissanceFreinagePression = v ?? 0),

            // Aerodynamisme
            new("Aerodynamisme.PortanceNegativeAvant", CategorieDeReglage.Aerodynamisme, TypeDeGrandeur.Force, neutre, r => r.Aerodynamisme?.PortanceNegativeAvant, null, null, (r, v) => r.Aerodynamisme!.PortanceNegativeAvant = v ?? 0),
            new("Aerodynamisme.PortanceNegativeArriere", CategorieDeReglage.Aerodynamisme, TypeDeGrandeur.Force, neutre, r => r.Aerodynamisme?.PortanceNegativeArriere, null, null, (r, v) => r.Aerodynamisme!.PortanceNegativeArriere = v ?? 0),

            // Volant
            new("Volant.AngleBraquage", CategorieDeReglage.Volant, TypeDeGrandeur.Angle, neutre, r => r.Volant?.AngleBraquage, 50, 200, (r, v) => r.Volant!.AngleBraquage = v ?? 0),
            new("Volant.IntensiteRetourDeForce", CategorieDeReglage.Volant, TypeDeGrandeur.SansUnite, neutre, r => r.Volant?.IntensiteRetourDeForce, 0, 200, (r, v) => r.Volant!.IntensiteRetourDeForce = v ?? 0),

            // Rapports : le nombre et les rapports numérotés se lisent mais ne s'écrivent pas un par un
            // (la liste de rapports a une longueur variable) ; le rapport final est un champ ordinaire, facultatif.
            new("Rapports.NombreDeRapports", CategorieDeReglage.Rapports, TypeDeGrandeur.SansUnite, neutre, r => r.RapportsDeBoite.Count == 0 ? (double?)null : r.RapportsDeBoite.Count, 1, NombreMaximumDeRapports),
            new("Rapports.RapportFinal", CategorieDeReglage.Rapports, TypeDeGrandeur.SansUnite, neutre, r => r.RapportFinal, 2.2, 6.1, (r, v) => r.RapportFinal = v, true),
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
