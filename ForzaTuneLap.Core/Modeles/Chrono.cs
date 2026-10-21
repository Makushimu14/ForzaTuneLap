namespace ForzaTuneLap.Core.Modeles;

/// <summary>
/// Meilleur temps réalisé sur un parcours avec un réglage précis.
/// Il existe au plus un chrono par couple (réglage, parcours).
/// Supprimer ou modifier un réglage supprime ses chronos.
/// </summary>
public class Chrono
{
    public Guid Identifiant { get; set; } = Guid.NewGuid();

    public Guid IdentifiantReglage { get; set; }
    public Reglage? Reglage { get; set; }

    public int IdentifiantParcours { get; set; }
    public Parcours? Parcours { get; set; }

    /// <summary>Durée du tour, en millisecondes.</summary>
    public long DureeMillisecondes { get; set; }

    public DateTime DateRealisation { get; set; }
    public string? Commentaire { get; set; }

    /// <summary>Vue pratique sur DureeMillisecondes, non enregistrée en base.</summary>
    public TimeSpan Duree
    {
        get => TimeSpan.FromMilliseconds(DureeMillisecondes);
        set => DureeMillisecondes = (long)Math.Round(value.TotalMilliseconds);
    }
}
