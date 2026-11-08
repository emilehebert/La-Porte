using UnityEngine;

/// <summary>
/// Partie B du document : le code du numpad.
///
///   Locataire : décrit les trois têtes tombées (dans l'ordre).
///   Visiteur  : retrouve ces personnes dans le journal, et leur numéro;
///               entre le code sur le numpad.
///
/// Le code (ReglagesJeu → codeNumpad) est vérifié dès que le bon nombre de chiffres est entré.
/// Mauvais code → on efface et on recommence.
/// </summary>
public class EtapeCodeNumpad : Etape
{
    [Header("Sons (optionnels)")]
    public AudioSource sonTouche;
    public AudioSource sonMauvaisCode;
    public AudioSource sonBonCode;

    string saisie = "";

    /// <summary>Ce qui est tapé, complété par des tirets (ex. : « 12__ »).</summary>
    public string SaisieAvecTirets =>
        saisie + new string('_', Mathf.Max(0, ReglagesJeu.Instance.codeNumpad.Length - saisie.Length));

    /// <summary>Time.time du dernier mauvais code (pour afficher « CODE FAUX » un moment).</summary>
    public float HeureDerniereErreur { get; private set; } = -100f;


    protected override void Entrer()
    {
        saisie = "";
    }

    public override void QuandToucheNumpad(int chiffre)
    {
        string code = ReglagesJeu.Instance.codeNumpad;

        saisie += chiffre;
        JouerSon(sonTouche);

        if (saisie.Length < code.Length) return;   // pas encore assez de chiffres

        if (saisie == code)
        {
            JouerSon(sonBonCode);
            Terminer();
        }
        else
        {
            JouerSon(sonMauvaisCode);
            saisie = "";
            HeureDerniereErreur = Time.time;
        }
    }

    public override string InfoOperateur()
    {
        return $"Saisi : {SaisieAvecTirets}\nCode : {ReglagesJeu.Instance.codeNumpad}";
    }
}
