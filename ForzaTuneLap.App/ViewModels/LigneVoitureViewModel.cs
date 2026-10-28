using System.Globalization;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une voiture dans la liste.</summary>
public sealed class LigneVoitureViewModel
{
    /// <param name="critereDeTri">
    /// Critère de la liste : quand il s'agit de la puissance, de la vitesse maximale ou de l'accélération,
    /// la valeur correspondante est ajoutée à la ligne pour qu'on voie ce qui est comparé.
    /// </param>
    public LigneVoitureViewModel(LigneVoiture ligne, CritereDeTriVoitures critereDeTri, ServicePreferences preferences)
    {
        Identifiant = ligne.Identifiant;
        Titre = $"{ligne.NomMarque} {ligne.Voiture.Nom}";

        // Classe et indice du réglage d'origine, par exemple « S 750 ». Vide si la voiture n'en a pas.
        Indice = ligne.IndiceDePerformanceOrigine is { } indice ? IndicesDePerformance.Formater(indice) : string.Empty;

        var elements = new List<string>
        {
            ligne.Voiture.Annee.ToString(CultureInfo.InvariantCulture),
            ServiceLibelles.Obtenir(ligne.Voiture.Division)
        };

        var valeurDeTri = DecrireValeurDeTri(ligne, critereDeTri, preferences);
        if (valeurDeTri is not null)
        {
            elements.Add(valeurDeTri);
        }

        if (ligne.Voiture.EstPersonnalisee)
        {
            elements.Add(Texte.Obtenir("Voiture_Perso"));
        }

        if (ligne.NombreDeReglagesCrees > 0)
        {
            elements.Add(string.Format(Texte.Obtenir("Voitures_NbReglages"), ligne.NombreDeReglagesCrees));
        }

        if (ligne.NombreDeChronos > 0)
        {
            elements.Add(string.Format(Texte.Obtenir("Voitures_NbChronos"), ligne.NombreDeChronos));
        }

        SousTitre = string.Join(" · ", elements);
    }

    public int Identifiant { get; }

    public string Titre { get; }

    public string SousTitre { get; }

    public string Indice { get; }

    public bool AUnIndice => Indice.Length > 0;

    private static string? DecrireValeurDeTri(LigneVoiture ligne, CritereDeTriVoitures critere, ServicePreferences preferences)
    {
        return critere switch
        {
            CritereDeTriVoitures.Puissance =>
                Formater(ligne.PuissanceOrigine, TypeDeGrandeur.Puissance, "N0", preferences),
            CritereDeTriVoitures.VitesseMaximale =>
                Formater(ligne.VitesseMaximaleOrigine, TypeDeGrandeur.Vitesse, "N0", preferences),
            CritereDeTriVoitures.Acceleration =>
                Formater(ligne.AccelerationOrigine, TypeDeGrandeur.Duree, "N1", preferences),
            _ => null
        };
    }

    private static string? Formater(double? valeurCanonique, TypeDeGrandeur grandeur, string format, ServicePreferences preferences)
    {
        if (valeurCanonique is not { } valeur)
        {
            return null;
        }

        var unite = preferences.Unites.ObtenirUnite(grandeur);
        return $"{unite.DepuisCanonique(valeur).ToString(format, CultureInfo.CurrentCulture)} {unite.Symbole}";
    }
}
