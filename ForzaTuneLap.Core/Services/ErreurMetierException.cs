namespace ForzaTuneLap.Core.Services;

public enum CodeErreurMetier
{
    NomVide,
    NomDejaUtilise,
    ReglageIntrouvable,
    VoitureIntrouvable,
    ReglageDejaExistant,
    ReglageOrigineNonModifiable,
    ChangementDeVoitureInterdit,
    ParcoursIntrouvable,
    ChronoIntrouvable,
    ChronoDejaExistant,
    DureeInvalide,
    CritereInconnu,
    PaysIntrouvable,
    MarqueIntrouvable,
    MarqueDejaExistante,
    MarqueOfficielleNonModifiable,
    MarqueNonVide,
    VoitureDejaExistante,
    VoitureOfficielleNonModifiable,
    AnneeInvalide,
    FusionInvalide
}

/// <summary>
/// Erreur attendue, à présenter à l'utilisateur. L'interface traduit le code en message
/// dans la langue choisie (le service ne renvoie jamais de texte).
/// </summary>
public sealed class ErreurMetierException : Exception
{
    public ErreurMetierException(CodeErreurMetier code) : base(code.ToString())
    {
        Code = code;
    }

    public CodeErreurMetier Code { get; }
}
