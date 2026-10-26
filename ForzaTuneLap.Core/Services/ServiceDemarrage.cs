namespace ForzaTuneLap.Core.Services;

public sealed record ResultatDemarrage(FusionsPossibles Fusions);

/// <summary>
/// Préparation de l'application au démarrage : migrations, synchronisation du catalogue officiel avec les fichiers de données,
/// puis recherche des fusions à proposer (voitures ou marques personnalisées désormais présentes dans le catalogue).
/// </summary>
public sealed class ServiceDemarrage
{
    private readonly FabriqueUniteDeTravail fabrique;

    public ServiceDemarrage(FabriqueUniteDeTravail fabrique)
    {
        this.fabrique = fabrique;
    }

    /// <summary>Résultat du dernier démarrage réussi, vide tant que celui-ci n'a pas eu lieu.</summary>
    public ResultatDemarrage? Resultat { get; private set; }

    public async Task<ResultatDemarrage> DemarrerAsync(CancellationToken annulation = default)
    {
        using var travail = fabrique.Creer();

        await Donnees.InitialiseurBaseDeDonnees.InitialiserAsync(travail.Contexte, annulation);
        var fusions = await travail.Catalogue.DetecterFusionsAsync(annulation);

        Resultat = new ResultatDemarrage(fusions);
        return Resultat;
    }
}
