using System.Globalization;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Une voiture dans la liste.</summary>
public sealed class LigneVoitureViewModel
{
    public LigneVoitureViewModel(LigneVoiture ligne)
    {
        Identifiant = ligne.Identifiant;
        Titre = $"{ligne.NomMarque} {ligne.Voiture.Nom}";

        var elements = new List<string>
        {
            ligne.Voiture.Annee.ToString(CultureInfo.InvariantCulture),
            ServiceLibelles.Obtenir(ligne.Voiture.Division)
        };

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
}
