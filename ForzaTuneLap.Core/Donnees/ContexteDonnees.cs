using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Donnees;

public class ContexteDonnees : DbContext
{
    public ContexteDonnees(DbContextOptions<ContexteDonnees> options) : base(options)
    {
    }

    public DbSet<Pays> Pays => Set<Pays>();
    public DbSet<Circuit> Circuits => Set<Circuit>();
    public DbSet<Parcours> Parcours => Set<Parcours>();
    public DbSet<Marque> Marques => Set<Marque>();
    public DbSet<Voiture> Voitures => Set<Voiture>();
    public DbSet<Reglage> Reglages => Set<Reglage>();
    public DbSet<Chrono> Chronos => Set<Chrono>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Référentiel : identifiants fournis par le fichier de données (ou attribués par le service pour les ajouts utilisateur).
        modelBuilder.Entity<Pays>(entite =>
        {
            entite.HasKey(p => p.Identifiant);
            entite.Property(p => p.Identifiant).ValueGeneratedNever();
            entite.Property(p => p.NomFrancais).IsRequired();
        });

        modelBuilder.Entity<Circuit>(entite =>
        {
            entite.HasKey(c => c.Identifiant);
            entite.Property(c => c.Identifiant).ValueGeneratedNever();
            entite.Property(c => c.Nom).IsRequired();
            entite.HasOne(c => c.Pays).WithMany(p => p.Circuits).HasForeignKey(c => c.IdentifiantPays).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Parcours>(entite =>
        {
            entite.HasKey(p => p.Identifiant);
            entite.Property(p => p.Identifiant).ValueGeneratedNever();
            entite.Property(p => p.NomFrancais).IsRequired();
            entite.HasOne(p => p.Circuit).WithMany(c => c.Parcours).HasForeignKey(p => p.IdentifiantCircuit).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Marque>(entite =>
        {
            entite.HasKey(m => m.Identifiant);
            entite.Property(m => m.Identifiant).ValueGeneratedNever();
            entite.Property(m => m.Nom).IsRequired();
            entite.HasOne(m => m.Pays).WithMany(p => p.Marques).HasForeignKey(m => m.IdentifiantPays).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Voiture>(entite =>
        {
            entite.HasKey(v => v.Identifiant);
            entite.Property(v => v.Identifiant).ValueGeneratedNever();
            entite.Property(v => v.Nom).IsRequired();
            entite.Property(v => v.Division).HasConversion<string>();
            entite.HasOne(v => v.Marque).WithMany(m => m.Voitures).HasForeignKey(v => v.IdentifiantMarque).OnDelete(DeleteBehavior.Restrict);
            entite.Ignore(v => v.NomComplet);
            entite.HasIndex(v => v.Division);
        });

        // Données utilisateur.
        modelBuilder.Entity<Reglage>(entite =>
        {
            entite.HasKey(r => r.Identifiant);
            entite.Property(r => r.Nom).IsRequired();
            entite.HasOne(r => r.Voiture).WithMany(v => v.Reglages).HasForeignKey(r => r.IdentifiantVoiture).OnDelete(DeleteBehavior.Cascade);
            entite.HasIndex(r => r.IdentifiantVoiture);

            // Sections obligatoires.
            entite.OwnsOne(r => r.Generaux, section => section.Property(g => g.Transmission).HasConversion<string>());
            entite.Navigation(r => r.Generaux).IsRequired();
            entite.OwnsOne(r => r.Performances);
            entite.Navigation(r => r.Performances).IsRequired();

            // Sections facultatives : stockées dans la même table, colonnes vides si la section est absente.
            entite.OwnsOne(r => r.Pneus);
            entite.OwnsOne(r => r.Alignement);
            entite.OwnsOne(r => r.Ressorts);
            entite.OwnsOne(r => r.BarresAntiroulis);
            entite.OwnsOne(r => r.Amortisseurs);
            entite.OwnsOne(r => r.GeometrieDeSuspension);
            entite.OwnsOne(r => r.Differentiel);
            entite.OwnsOne(r => r.Freins);
            entite.OwnsOne(r => r.Aerodynamisme);
            entite.OwnsOne(r => r.Volant);

            entite.OwnsMany(r => r.RapportsDeBoite, rapport =>
            {
                rapport.ToTable("RapportsDeBoite");
                rapport.WithOwner().HasForeignKey("IdentifiantReglage");
                rapport.Property<int>("Identifiant").ValueGeneratedOnAdd();
                rapport.HasKey("Identifiant");
            });
        });

        modelBuilder.Entity<Chrono>(entite =>
        {
            entite.HasKey(c => c.Identifiant);
            entite.Ignore(c => c.Duree);

            // Supprimer un réglage supprime ses chronos.
            entite.HasOne(c => c.Reglage).WithMany(r => r.Chronos).HasForeignKey(c => c.IdentifiantReglage).OnDelete(DeleteBehavior.Cascade);
            entite.HasOne(c => c.Parcours).WithMany().HasForeignKey(c => c.IdentifiantParcours).OnDelete(DeleteBehavior.Restrict);

            // Un seul chrono par couple (réglage, parcours).
            entite.HasIndex(c => new { c.IdentifiantReglage, c.IdentifiantParcours }).IsUnique();
            entite.HasIndex(c => c.IdentifiantParcours);
        });
    }
}
