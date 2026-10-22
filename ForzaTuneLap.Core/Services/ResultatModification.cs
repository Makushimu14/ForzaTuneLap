namespace ForzaTuneLap.Core.Services;

public enum StatutModification
{
    /// <summary>La modification a été enregistrée.</summary>
    Modifie,

    /// <summary>Rien n'a été modifié : des chronos seraient supprimés et l'utilisateur doit confirmer.</summary>
    ConfirmationRequise
}

/// <param name="NombreDeChronos">
/// Avec <see cref="StatutModification.ConfirmationRequise"/> : nombre de chronos qui seraient supprimés.
/// Avec <see cref="StatutModification.Modifie"/> : nombre de chronos qui ont été supprimés.
/// </param>
public sealed record ResultatModification(StatutModification Statut, int NombreDeChronos);
