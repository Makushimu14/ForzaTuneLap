using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Edition;

/// <summary>Présence, création et suppression des sections facultatives d'un réglage.</summary>
internal static class PresenceDesSections
{
    public static bool EstFacultative(CategorieDeReglage categorie) =>
        categorie is not (CategorieDeReglage.Generaux or CategorieDeReglage.Performances or CategorieDeReglage.Rapports);

    public static bool EstPresente(Reglage reglage, CategorieDeReglage categorie) => categorie switch
    {
        CategorieDeReglage.Pneus => reglage.Pneus is not null,
        CategorieDeReglage.Alignement => reglage.Alignement is not null,
        CategorieDeReglage.Ressorts => reglage.Ressorts is not null,
        CategorieDeReglage.BarresAntiroulis => reglage.BarresAntiroulis is not null,
        CategorieDeReglage.Amortisseurs => reglage.Amortisseurs is not null,
        CategorieDeReglage.GeometrieDeSuspension => reglage.GeometrieDeSuspension is not null,
        CategorieDeReglage.Differentiel => reglage.Differentiel is not null,
        CategorieDeReglage.Freins => reglage.Freins is not null,
        CategorieDeReglage.Aerodynamisme => reglage.Aerodynamisme is not null,
        CategorieDeReglage.Volant => reglage.Volant is not null,
        _ => true // généraux, performances et rapports existent toujours
    };

    /// <summary>Crée la section avec des valeurs à zéro (les champs facultatifs restent vides).</summary>
    public static void Creer(Reglage reglage, CategorieDeReglage categorie)
    {
        switch (categorie)
        {
            case CategorieDeReglage.Pneus: reglage.Pneus ??= new ReglagePneus(); break;
            case CategorieDeReglage.Alignement: reglage.Alignement ??= new ReglageAlignement(); break;
            case CategorieDeReglage.Ressorts: reglage.Ressorts ??= new ReglageRessorts(); break;
            case CategorieDeReglage.BarresAntiroulis: reglage.BarresAntiroulis ??= new ReglageBarresAntiroulis(); break;
            case CategorieDeReglage.Amortisseurs: reglage.Amortisseurs ??= new ReglageAmortisseurs(); break;
            case CategorieDeReglage.GeometrieDeSuspension: reglage.GeometrieDeSuspension ??= new ReglageGeometrieDeSuspension(); break;
            case CategorieDeReglage.Differentiel: reglage.Differentiel ??= new ReglageDifferentiel(); break;
            case CategorieDeReglage.Freins: reglage.Freins ??= new ReglageFreins(); break;
            case CategorieDeReglage.Aerodynamisme: reglage.Aerodynamisme ??= new ReglageAerodynamisme(); break;
            case CategorieDeReglage.Volant: reglage.Volant ??= new ReglageVolant(); break;
        }
    }

    public static void Retirer(Reglage reglage, CategorieDeReglage categorie)
    {
        switch (categorie)
        {
            case CategorieDeReglage.Pneus: reglage.Pneus = null; break;
            case CategorieDeReglage.Alignement: reglage.Alignement = null; break;
            case CategorieDeReglage.Ressorts: reglage.Ressorts = null; break;
            case CategorieDeReglage.BarresAntiroulis: reglage.BarresAntiroulis = null; break;
            case CategorieDeReglage.Amortisseurs: reglage.Amortisseurs = null; break;
            case CategorieDeReglage.GeometrieDeSuspension: reglage.GeometrieDeSuspension = null; break;
            case CategorieDeReglage.Differentiel: reglage.Differentiel = null; break;
            case CategorieDeReglage.Freins: reglage.Freins = null; break;
            case CategorieDeReglage.Aerodynamisme: reglage.Aerodynamisme = null; break;
            case CategorieDeReglage.Volant: reglage.Volant = null; break;
        }
    }
}
