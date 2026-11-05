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
        }
    }

    public override string InfoOperateur()
    {
        string code = ReglagesJeu.Instance.codeNumpad;
        string tirets = new string('_', Mathf.Max(0, code.Length - saisie.Length));

        return $"Saisi : {saisie}{tirets}\nCode : {code}";
    }
}
