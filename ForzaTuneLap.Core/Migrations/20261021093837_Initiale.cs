using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForzaTuneLap.Core.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pays",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false),
                    NomFrancais = table.Column<string>(type: "TEXT", nullable: false),
                    NomAnglais = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pays", x => x.Identifiant);
                });

            migrationBuilder.CreateTable(
                name: "Circuits",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    IdentifiantPays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circuits", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Circuits_Pays_IdentifiantPays",
                        column: x => x.IdentifiantPays,
                        principalTable: "Pays",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Marques",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    IdentifiantPays = table.Column<int>(type: "INTEGER", nullable: false),
                    EstPersonnalisee = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marques", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Marques_Pays_IdentifiantPays",
                        column: x => x.IdentifiantPays,
                        principalTable: "Pays",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parcours",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false),
                    NomFrancais = table.Column<string>(type: "TEXT", nullable: false),
                    NomAnglais = table.Column<string>(type: "TEXT", nullable: true),
                    LongueurKilometres = table.Column<double>(type: "REAL", nullable: false),
                    IdentifiantCircuit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcours", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Parcours_Circuits_IdentifiantCircuit",
                        column: x => x.IdentifiantCircuit,
                        principalTable: "Circuits",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voitures",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Annee = table.Column<int>(type: "INTEGER", nullable: false),
                    Division = table.Column<string>(type: "TEXT", nullable: false),
                    Valeur = table.Column<int>(type: "INTEGER", nullable: false),
                    Achetable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContenuTelechargeable = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstPersonnalisee = table.Column<bool>(type: "INTEGER", nullable: false),
                    IdentifiantMarque = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voitures", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Voitures_Marques_IdentifiantMarque",
                        column: x => x.IdentifiantMarque,
                        principalTable: "Marques",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reglages",
                columns: table => new
                {
                    Identifiant = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdentifiantVoiture = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    EstOrigine = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmpreinteOrigine = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Generaux_Code = table.Column<string>(type: "TEXT", nullable: false),
                    Generaux_Moteur = table.Column<string>(type: "TEXT", nullable: false),
                    Generaux_Transmission = table.Column<string>(type: "TEXT", nullable: false),
                    Generaux_IndiceDePerformance = table.Column<int>(type: "INTEGER", nullable: false),
                    Generaux_Puissance = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_Couple = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_Poids = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_RepartitionPoidsAvant = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_NoteAcceleration = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_NoteFreinage = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_NoteTenueDeRoute = table.Column<double>(type: "REAL", nullable: false),
                    Generaux_NoteVitesse = table.Column<double>(type: "REAL", nullable: false),
                    Performances_AccelerationDeZeroA97 = table.Column<double>(type: "REAL", nullable: false),
                    Performances_AccelerationDeZeroA161 = table.Column<double>(type: "REAL", nullable: false),
                    Performances_Temps400Metres = table.Column<double>(type: "REAL", nullable: false),
                    Performances_DistanceDeFreinageDe97AZero = table.Column<double>(type: "REAL", nullable: false),
                    Performances_DistanceDeFreinageDe161AZero = table.Column<double>(type: "REAL", nullable: false),
                    Performances_AccelerationLateraleA97 = table.Column<double>(type: "REAL", nullable: false),
                    Performances_AccelerationLateraleA193 = table.Column<double>(type: "REAL", nullable: false),
                    Performances_VitesseMaximale = table.Column<double>(type: "REAL", nullable: false),
                    Pneus_PressionAvant = table.Column<double>(type: "REAL", nullable: true),
                    Pneus_PressionArriere = table.Column<double>(type: "REAL", nullable: true),
                    Alignement_CarrossageAvant = table.Column<double>(type: "REAL", nullable: true),
                    Alignement_CarrossageArriere = table.Column<double>(type: "REAL", nullable: true),
                    Alignement_ParallelismeAvant = table.Column<double>(type: "REAL", nullable: true),
                    Alignement_ParallelismeArriere = table.Column<double>(type: "REAL", nullable: true),
                    Alignement_AngleDeChasseAvant = table.Column<double>(type: "REAL", nullable: true),
                    Ressorts_RessortsAvant = table.Column<double>(type: "REAL", nullable: true),
                    Ressorts_RessortsArriere = table.Column<double>(type: "REAL", nullable: true),
                    Ressorts_GardeAuSolAvant = table.Column<double>(type: "REAL", nullable: true),
                    Ressorts_GardeAuSolArriere = table.Column<double>(type: "REAL", nullable: true),
                    BarresAntiroulis_Avant = table.Column<double>(type: "REAL", nullable: true),
                    BarresAntiroulis_Arriere = table.Column<double>(type: "REAL", nullable: true),
                    Amortisseurs_RigiditeAmortissementAvant = table.Column<double>(type: "REAL", nullable: true),
                    Amortisseurs_RigiditeAmortissementArriere = table.Column<double>(type: "REAL", nullable: true),
                    Amortisseurs_RigiditeDetenteAvant = table.Column<double>(type: "REAL", nullable: true),
                    Amortisseurs_RigiditeDetenteArriere = table.Column<double>(type: "REAL", nullable: true),
                    GeometrieDeSuspension_HauteurCentreRoulisAvant = table.Column<double>(type: "REAL", nullable: true),
                    GeometrieDeSuspension_HauteurCentreRoulisArriere = table.Column<double>(type: "REAL", nullable: true),
                    GeometrieDeSuspension_PourcentageAntigeometrieAvant = table.Column<double>(type: "REAL", nullable: true),
                    GeometrieDeSuspension_PourcentageAntigeometrieArriere = table.Column<double>(type: "REAL", nullable: true),
                    Differentiel_Acceleration = table.Column<double>(type: "REAL", nullable: true),
                    Differentiel_Deceleration = table.Column<double>(type: "REAL", nullable: true),
                    Differentiel_EquilibreCentral = table.Column<double>(type: "REAL", nullable: true),
                    Freins_PuissanceFreinageEquilibre = table.Column<double>(type: "REAL", nullable: true),
                    Freins_PuissanceFreinagePression = table.Column<double>(type: "REAL", nullable: true),
                    Aerodynamisme_PortanceNegativeAvant = table.Column<double>(type: "REAL", nullable: true),
                    Aerodynamisme_PortanceNegativeArriere = table.Column<double>(type: "REAL", nullable: true),
                    Volant_AngleBraquage = table.Column<double>(type: "REAL", nullable: true),
                    Volant_IntensiteRetourDeForce = table.Column<double>(type: "REAL", nullable: true),
                    RapportFinal = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reglages", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Reglages_Voitures_IdentifiantVoiture",
                        column: x => x.IdentifiantVoiture,
                        principalTable: "Voitures",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chronos",
                columns: table => new
                {
                    Identifiant = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdentifiantReglage = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdentifiantParcours = table.Column<int>(type: "INTEGER", nullable: false),
                    DureeMillisecondes = table.Column<long>(type: "INTEGER", nullable: false),
                    DateRealisation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Commentaire = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chronos", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_Chronos_Parcours_IdentifiantParcours",
                        column: x => x.IdentifiantParcours,
                        principalTable: "Parcours",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chronos_Reglages_IdentifiantReglage",
                        column: x => x.IdentifiantReglage,
                        principalTable: "Reglages",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RapportsDeBoite",
                columns: table => new
                {
                    Identifiant = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Valeur = table.Column<double>(type: "REAL", nullable: false),
                    IdentifiantReglage = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportsDeBoite", x => x.Identifiant);
                    table.ForeignKey(
                        name: "FK_RapportsDeBoite_Reglages_IdentifiantReglage",
                        column: x => x.IdentifiantReglage,
                        principalTable: "Reglages",
                        principalColumn: "Identifiant",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chronos_IdentifiantParcours",
                table: "Chronos",
                column: "IdentifiantParcours");

            migrationBuilder.CreateIndex(
                name: "IX_Chronos_IdentifiantReglage_IdentifiantParcours",
                table: "Chronos",
                columns: new[] { "IdentifiantReglage", "IdentifiantParcours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Circuits_IdentifiantPays",
                table: "Circuits",
                column: "IdentifiantPays");

            migrationBuilder.CreateIndex(
                name: "IX_Marques_IdentifiantPays",
                table: "Marques",
                column: "IdentifiantPays");

            migrationBuilder.CreateIndex(
                name: "IX_Parcours_IdentifiantCircuit",
                table: "Parcours",
                column: "IdentifiantCircuit");

            migrationBuilder.CreateIndex(
                name: "IX_RapportsDeBoite_IdentifiantReglage",
                table: "RapportsDeBoite",
                column: "IdentifiantReglage");

            migrationBuilder.CreateIndex(
                name: "IX_Reglages_IdentifiantVoiture",
                table: "Reglages",
                column: "IdentifiantVoiture");

            migrationBuilder.CreateIndex(
                name: "IX_Voitures_Division",
                table: "Voitures",
                column: "Division");

            migrationBuilder.CreateIndex(
                name: "IX_Voitures_IdentifiantMarque",
                table: "Voitures",
                column: "IdentifiantMarque");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chronos");

            migrationBuilder.DropTable(
                name: "RapportsDeBoite");

            migrationBuilder.DropTable(
                name: "Parcours");

            migrationBuilder.DropTable(
                name: "Reglages");

            migrationBuilder.DropTable(
                name: "Circuits");

            migrationBuilder.DropTable(
                name: "Voitures");

            migrationBuilder.DropTable(
                name: "Marques");

            migrationBuilder.DropTable(
                name: "Pays");
        }
    }
}
