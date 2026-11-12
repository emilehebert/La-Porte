using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using extOSC;

/// <summary>
/// LE point d'entrée de tout ce qui vient du monde physique.
///
///   Arduino → TouchDesigner → OSC → Capteurs → événements que le reste du jeu écoute.
///
/// Le clavier de debug et le panneau opérateur appellent les MÊMES méthodes
/// (DeclencherSonnette, ChangerInterrupteur...). Le jeu ne fait donc jamais la différence
/// entre la vraie sonnette et la touche S du clavier.
///
/// Pour écouter un capteur dans un autre script :
///     void OnEnable()  { Capteurs.Sonnette += QuandSonnette; }
///     void OnDisable() { Capteurs.Sonnette -= QuandSonnette; }   // toujours se désabonner!
/// </summary>
public class Capteurs : MonoBehaviour
{
    [Header("Réception OSC (TouchDesigner → Unity)")]
    public OSCReceiver recepteur;

    [Header("Envoi OSC (Unity → TouchDesigner) — optionnel")]
    [Tooltip("Seulement si Unity doit commander quelque chose de physique (ex. : la serrure de la porte).")]
    public OSCTransmitter transmetteur;

    [Header("Adresses OSC — à valider avec l'équipe TouchDesigner")]
    [Tooltip("Angle de la poignée, de -1 à 1.")]
    public string adressePoignee = "/rotation";
    [Tooltip("1 = appuyée, 0 = relâchée.")]
    public string adresseSonnette = "/sonette";
    [Tooltip("1 = un coup a été détecté.")]
    public string adresseHeurtoir = "/vibration";
    [Tooltip("1 = allumé, 0 = fermé.")]
    public string adresseInterrupteur = "/switch";
    [Tooltip("TouchDesigner vérifie le code et envoie EN CONTINU : 0 = rien, 1 = bon code, -1 = mauvais code.\n" +
             "Un essai compte quand la valeur CHANGE (0 → 1 ou 0 → -1). Revenir à 0 entre deux essais.")]
    public string adresseNumpad = "/code";

    [Header("Anti-rebond (secondes)")]
    [Tooltip("Temps minimum entre deux coups de heurtoir. Augmenter si un seul coup compte plusieurs fois.")]
    public float delaiHeurtoir = 0.25f;
    [Tooltip("Temps minimum entre deux coups de sonnette.")]
    public float delaiSonnette = 0.3f;

    [Header("Poignée")]
    [Tooltip("Plus petit mouvement de la poignée qui compte comme un contact (pour le retour automatique au début).\n" +
             "Augmenter si le capteur « tremble » : sinon le jeu croit que quelqu'un touche toujours la poignée.")]
    public float seuilBruitPoignee = 0.02f;


    // ---------- Les événements que le jeu écoute ----------

    public static event Action Sonnette;
    public static event Action Heurtoir;
    public static event Action<bool> Interrupteur;   // true = allumé
    public static event Action<bool> CodeNumpad;     // true = bon code, false = mauvais code


    // ---------- L'état actuel du matériel ----------
    // « static » : ces valeurs survivent au rechargement de la scène quand on saute à une étape
    // ou qu'on rejoue une étape. Quand l'expérience RECOMMENCE AU DÉBUT, RemettreAZero() les remet au repos.

    public static float AnglePoignee { get; private set; }        // de -1 à 1
    public static bool InterrupteurAllume { get; private set; }
    public static float HeureDernierContact { get; private set; } // Time.time de la dernière interaction


    // ---------- Pour le moniteur OSC du panneau opérateur ----------

    public class MessageRecu
    {
        public string valeur;
        public float heure;
    }

    /// <summary>Le dernier message reçu sur chaque adresse OSC (même les adresses inconnues).</summary>
    public static readonly Dictionary<string, MessageRecu> DerniersMessages = new Dictionary<string, MessageRecu>();

    /// <summary>Texte du genre « port 7000 — IP de cet ordi : 10.10.35.10 ».</summary>
    public static string TexteConnexion { get; private set; } = "Aucun récepteur OSC";


    static Capteurs instance;
    DetecteurAppui detecteurSonnette;
    DetecteurAppui detecteurHeurtoir;
    bool codeDejaRecu;          // false tant qu'aucune valeur /code n'est arrivée depuis le chargement
    int etatCodePrecedent;      // 0 = rien, 1 = bon, -1 = mauvais


    void Awake()
    {
        instance = this;
        detecteurSonnette = new DetecteurAppui(delaiSonnette);
        detecteurHeurtoir = new DetecteurAppui(delaiHeurtoir);
    }

    void Start()
    {
        if (recepteur == null)
        {
            Debug.LogError("[Capteurs] Aucun OSCReceiver assigné : seul le clavier de debug va fonctionner.");
            return;
        }

        TexteConnexion = $"port {recepteur.LocalPort} — IP de cet ordi : {OSCUtilities.GetLocalHost()}";

        recepteur.Bind("*", NoterPourLeMoniteur);   // "*" = toutes les adresses
        recepteur.Bind(adressePoignee, message => ChangerPoignee(LireNombre(message)));
        recepteur.Bind(adresseInterrupteur, message => ChangerInterrupteur(LireNombre(message) >= 0.5f));
        recepteur.Bind(adresseNumpad, message => RecevoirCodeNumpad(LireNombre(message)));

        recepteur.Bind(adresseSonnette, message =>
        {
            if (detecteurSonnette.EstNouvelAppui(LireNombre(message))) DeclencherSonnette();
        });

        recepteur.Bind(adresseHeurtoir, message =>
        {
            if (detecteurHeurtoir.EstNouvelAppui(LireNombre(message))) DeclencherHeurtoir();
        });
    }


    // ---------- Les méthodes qui déclenchent les événements ----------
    // Appelées par l'OSC ci-dessus ET par le clavier / panneau opérateur.

    public static void DeclencherSonnette()
    {
        NoterContact();
        Sonnette?.Invoke();
    }

    public static void DeclencherHeurtoir()
    {
        NoterContact();
        Heurtoir?.Invoke();
    }

    public static void ChangerInterrupteur(bool allume)
    {
        if (allume == InterrupteurAllume) return;   // rien n'a changé
        InterrupteurAllume = allume;
        NoterContact();
        Interrupteur?.Invoke(allume);
    }

    /// <summary>Un code a été entré sur le numpad. TouchDesigner a déjà vérifié s'il est bon.</summary>
    public static void EntrerCodeNumpad(bool bonCode)
    {
        NoterContact();
        CodeNumpad?.Invoke(bonCode);
    }

    public static void ChangerPoignee(float angle)
    {
        angle = Mathf.Clamp(angle, -1f, 1f);

        float seuil = instance != null ? instance.seuilBruitPoignee : 0.02f;
        if (Mathf.Abs(angle - AnglePoignee) > seuil) NoterContact();   // ignore le petit bruit du capteur
        AnglePoignee = angle;
    }

    /// <summary>
    /// Remet le matériel « au repos » : interrupteur fermé (lampe éteinte), poignée droite.
    /// Appelé juste avant de recharger la scène pour recommencer l'expérience au début :
    /// pas besoin d'événement, tous les objets relisent cet état en se rechargeant.
    /// (Avec le vrai matériel, TouchDesigner renverra l'état réel au prochain message.)
    /// </summary>
    public static void RemettreAZero()
    {
        InterrupteurAllume = false;
        AnglePoignee = 0f;
    }

    /// <summary>Envoie une valeur à TouchDesigner (ex. : ouvrir la serrure). Ne fait rien si l'adresse est vide.</summary>
    public static void EnvoyerOSC(string adresse, float valeur)
    {
        if (string.IsNullOrEmpty(adresse)) return;

        if (instance == null || instance.transmetteur == null)
        {
            Debug.Log($"[Capteurs] (aucun transmetteur OSC) {adresse} {valeur}");
            return;
        }

        instance.transmetteur.Send(new OSCMessage(adresse, OSCValue.Float(valeur)));
    }


    // ---------- Outils internes ----------

    /// <summary>
    /// /code arrive EN CONTINU depuis TouchDesigner (0 0 0 ... 1 1 1 ... 0 0).
    ///   0 = personne n'a entré de code · 1 = bon code · -1 = mauvais code.
    /// Un essai compte seulement quand la valeur CHANGE (0 → 1 ou 0 → -1).
    ///
    /// La toute première valeur reçue (au chargement de la scène) ne compte pas :
    /// c'est l'état actuel de TouchDesigner, pas un nouvel essai.
    /// (Avant, le « 0 » reçu au chargement passait pour un mauvais code et sautait l'étape d'attente.)
    /// </summary>
    void RecevoirCodeNumpad(float valeur)
    {
        int etat = valeur >= 0.5f ? 1 : valeur <= -0.5f ? -1 : 0;

        bool premiereValeur = !codeDejaRecu;
        bool aChange = etat != etatCodePrecedent;

        codeDejaRecu = true;
        etatCodePrecedent = etat;

        if (premiereValeur || !aChange) return;

        if (etat == 1) EntrerCodeNumpad(true);
        else if (etat == -1) EntrerCodeNumpad(false);
        // retour à 0 : rien à faire, le numpad est prêt pour le prochain essai
    }

    static void NoterContact()
    {
        HeureDernierContact = Time.time;
    }

    static void NoterPourLeMoniteur(OSCMessage message)
    {
        if (!DerniersMessages.TryGetValue(message.Address, out MessageRecu recu))
        {
            recu = new MessageRecu();
            DerniersMessages[message.Address] = recu;
        }

        recu.valeur = message.Values.Count > 0 ? LireNombre(message).ToString("0.##") : "(vide)";
        recu.heure = Time.time;
    }

    /// <summary>Lit la première valeur du message, peu importe son type (float, int, texte...).</summary>
    static float LireNombre(OSCMessage message)
    {
        if (message.Values.Count == 0) return 0f;

        OSCValue valeur = message.Values[0];
        switch (valeur.Type)
        {
            case OSCValueType.Float:   return valeur.FloatValue;
            case OSCValueType.Int:     return valeur.IntValue;
            case OSCValueType.Double:  return (float)valeur.DoubleValue;
            case OSCValueType.True:    return 1f;
            case OSCValueType.False:   return 0f;
            case OSCValueType.Impulse: return 1f;
            case OSCValueType.String:
                float.TryParse(valeur.StringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float nombre);
                return nombre;
            default:
                return 0f;
        }
    }
}
