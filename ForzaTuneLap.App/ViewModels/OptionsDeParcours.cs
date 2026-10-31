using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Les circuits et leurs parcours, prêts à être choisis dans deux listes en cascade.</summary>
public static class OptionsDeParcours
{
    /// <summary>Circuits triés par nom ; parcours de chaque circuit triés par nom dans la langue de l'interface.</summary>
    public static async Task<IReadOnlyList<OptionCircuit>> ChargerAsync(UniteDeTravail travail)
    {
        var circuits = await travail.Contexte.Circuits.AsNoTracking()
            .Include(c => c.Parcours)
            .ToListAsync();

        return circuits
            .OrderBy(c => c.Nom, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => new OptionCircuit(
                c.Identifiant,
                c.Nom,
                c.Parcours
                    .Select(p => new OptionParcours(p.Identifiant, NomsAffiches.Parcours(p)))
                    .OrderBy(p => p.Nom, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()))
            .ToList();
    }
}
