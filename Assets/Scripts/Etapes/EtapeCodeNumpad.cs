using UnityEngine;

/// <summary>
/// Partie B du document : le code du numpad.
///
///   Locataire : décrit les trois têtes tombées (dans l'ordre).
///   Visiteur  : retrouve ces personnes dans le journal, et leur numéro;
///               entre le code sur le numpad.
///
/// C'est TouchDesigner qui vérifie le code : Unity reçoit seulement « bon » (1) ou « mauvais » (-1).
/// Bon code → on passe à la suivante. Mauvais code → « CODE FAUX » un moment, et on peut réessayer.
/// </summary>
public class EtapeCodeNumpad : Etape
{
    [Header("Sons (optionnels)")]
    public AudioSource sonMauvaisCode;
    public AudioSource sonBonCode;

    /// <summary>Time.time du dernier mauvais code (pour afficher « CODE FAUX » un moment).</summary>
    public float HeureDerniereErreur { get; private set; } = -100f;

    int mauvaisEssais;


    protected override void Entrer()
    {
        mauvaisEssais = 0;
    }

    public override void QuandCodeNumpad(bool bonCode)
    {
        if (bonCode)
        {
            JouerSon(sonBonCode);
            Terminer();
        }
        else
        {
            JouerSon(sonMauvaisCode);
            mauvaisEssais++;
            HeureDerniereErreur = Time.time;
        }
    }

    public override string InfoOperateur()
    {
        return $"Code vérifié par TouchDesigner (doit être : {ReglagesJeu.Instance.codeNumpad})\n" +
               $"Mauvais essais : {mauvaisEssais}";
    }
}
