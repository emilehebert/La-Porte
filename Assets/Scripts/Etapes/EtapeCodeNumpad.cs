using System.Globalization;
using UnityEngine;

/// <summary>
/// Partie B du document : le code du numpad.
///
///   Locataire : décrit les deux têtes tombées (dans l'ordre).
///   Visiteur  : retrouve ces personnes dans le journal, et leur numéro;
///               entre le code sur le numpad.
///
/// Le code change à chaque partie (il dépend des têtes tombées, voir ReglagesJeu).
/// Unity l'envoie donc à TouchDesigner, qui le vérifie : Unity reçoit seulement « bon » (1) ou « mauvais » (-1).
/// Bon code → on passe à la suivante. Mauvais code → « CODE FAUX » un moment, et on peut réessayer.
/// </summary>
public class EtapeCodeNumpad : Etape
{
    [Header("Envoi du code à TouchDesigner")]
    [Tooltip("Adresse OSC où Unity envoie le code de la partie (un nombre à 4 chiffres, ex. 9125) :\n" +
             "au début de l'étape, puis chaque seconde tant qu'elle dure. Vide = ne rien envoyer.\n" +
             "Il faut un OSCTransmitter sur Jeu → Capteurs (IP et port de l'ordi de TouchDesigner).")]
    public string adresseOscCode = "/code_attendu";

    [Header("Sons (optionnels)")]
    public AudioSource sonMauvaisCode;
    public AudioSource sonBonCode;

    /// <summary>Time.time du dernier mauvais code (pour afficher « CODE FAUX » un moment).</summary>
    public float HeureDerniereErreur { get; private set; } = -100f;

    int mauvaisEssais;
    float heureDernierEnvoi;


    protected override void Entrer()
    {
        mauvaisEssais = 0;
        EnvoyerCode();
    }

    void Update()   // seulement pendant cette étape (le gestionnaire désactive les autres)
    {
        // Renvoyé chaque seconde : si TouchDesigner redémarre ou rate un message, il l'a quand même.
        if (Capteurs.PeutEnvoyerOSC && Time.time - heureDernierEnvoi >= 1f) EnvoyerCode();
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

    void EnvoyerCode()
    {
        heureDernierEnvoi = Time.time;
        string code = ReglagesJeu.Instance.CodeNumpad;

        if (!float.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out float nombre))
        {
            Debug.LogError($"[{Nom}] Le code « {code} » n'est pas un nombre : vérifier les numéros des têtes (ReglagesJeu → Tetes).");
            return;
        }
        Capteurs.EnvoyerOSC(adresseOscCode, nombre);
    }

    public override string InfoOperateur()
    {
        ReglagesJeu reglages = ReglagesJeu.Instance;
        string envoi = Capteurs.PeutEnvoyerOSC
            ? $"Envoyé à TouchDesigner sur {adresseOscCode}"
            : "<color=#ff7070>Pas d'OSCTransmitter sur Jeu → Capteurs : TouchDesigner ne reçoit pas le code!</color>";

        return $"Code : {reglages.CodeNumpad}  ({reglages.TexteTetesTombees()})\n" +
               $"{envoi}\n" +
               $"Mauvais essais : {mauvaisEssais}";
    }
}
