using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Un rapport de la boîte de vitesses (sans unité). Même principe que <see cref="ChampReglage"/> : la valeur du réglage
/// n'est réécrite que quand l'utilisateur modifie le texte. Les rapports d'une boîte vont en décroissant : la mise en
/// cohérence avec les autres rapports est faite par <see cref="SectionRapports"/>.
/// </summary>
public sealed class RapportEditable : INotifyPropertyChanged
{
    private const double MinimumIndicatif = 0.48;
    private const double MaximumIndicatif = 6;

    private readonly RapportDeBoite rapport;
    private readonly CultureInfo cultureDeFormat;
    private readonly Action<RapportEditable, bool> apresSaisie;

    private string texte;
    private bool estValide = true;
    private bool estInhabituel;
    private bool estSuperieurAuPrecedent;
    private bool estAjusteAutomatiquement;

    /// <param name="apresSaisie">Appelée après chaque saisie de l'utilisateur ; le booléen indique si le texte est un nombre valide.</param>
    internal RapportEditable(
        RapportDeBoite rapport,
        bool estModifiable,
        CultureInfo cultureDeFormat,
        Action<RapportEditable, bool> apresSaisie)
    {
        this.rapport = rapport;
        this.cultureDeFormat = cultureDeFormat;
        this.apresSaisie = apresSaisie;

        EstModifiable = estModifiable;
        var critere = CatalogueCriteres.Rechercher($"Rapports.Rapport{rapport.Numero}");
        Libelle = critere is null ? $"#{rapport.Numero}" : ServiceLibelles.Obtenir(critere);
        texte = Nombres.Formater(rapport.Valeur, cultureDeFormat);
        estInhabituel = HorsBornes(rapport.Valeur);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal RapportDeBoite Rapport => rapport;

    /// <summary>Dernière valeur valide du rapport (celle du réglage).</summary>
    internal double Valeur => rapport.Valeur;

    public int Numero => rapport.Numero;

    public string Libelle { get; }

    public bool EstModifiable { get; }

    public string Texte
    {
        get => texte;
        set
        {
            value ??= string.Empty;
            if (value == texte)
            {
                return;
            }

            texte = value;
            Notifier();

            if (!EstModifiable)
            {
                return;
            }

            if (estAjusteAutomatiquement)
            {
                estAjusteAutomatiquement = false;
                Notifier(nameof(EstAjusteAutomatiquement));
            }

            var valide = Nombres.TryParse(texte, out var saisie);
            if (valide)
            {
                rapport.Valeur = saisie;
                estValide = true;
                estInhabituel = HorsBornes(saisie);
            }
            else
            {
                estValide = false;
                estInhabituel = false;
            }

            NotifierEtat();
            apresSaisie(this, valide);
        }
    }

    public bool EstValide => estValide;

    public bool EstInvalide => !estValide;

    /// <summary>Valeur hors des bornes indicatives. Un avertissement, jamais un blocage.</summary>
    public bool EstInhabituel => estInhabituel;

    /// <summary>
    /// Ce rapport est plus grand que le précédent, alors que les rapports vont en décroissant. Un avertissement :
    /// la valeur n'est pas modifiée, c'est à l'utilisateur de décider.
    /// </summary>
    public bool EstSuperieurAuPrecedent => estSuperieurAuPrecedent;

    /// <summary>
    /// La valeur a été ramenée automatiquement par la saisie d'un rapport précédent. Un message d'information, qui
    /// disparaît dès que l'utilisateur modifie ce rapport lui-même.
    /// </summary>
    public bool EstAjusteAutomatiquement => estAjusteAutomatiquement;

    /// <summary>Impose une valeur (mise en cohérence avec un autre rapport) et met à jour le texte affiché.</summary>
    internal void DefinirValeur(double valeur, bool ajuste)
    {
        estAjusteAutomatiquement = ajuste;
        Notifier(nameof(EstAjusteAutomatiquement));

        rapport.Valeur = valeur;
        texte = Nombres.Formater(valeur, cultureDeFormat);
        estValide = true;
        estInhabituel = HorsBornes(valeur);

        Notifier(nameof(Texte));
        NotifierEtat();
    }

    internal void DefinirSuperieurAuPrecedent(bool valeur)
    {
        if (estSuperieurAuPrecedent == valeur)
        {
            return;
        }

        estSuperieurAuPrecedent = valeur;
        Notifier(nameof(EstSuperieurAuPrecedent));
    }

    private static bool HorsBornes(double valeur) => valeur < MinimumIndicatif || valeur > MaximumIndicatif;

    private void NotifierEtat()
    {
        Notifier(nameof(EstValide));
        Notifier(nameof(EstInvalide));
        Notifier(nameof(EstInhabituel));
    }

    private void Notifier([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
