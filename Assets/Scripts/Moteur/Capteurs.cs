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
    [Tooltip("UN message par touche appuyée, avec le chiffre (0 à 9).")]
    public string adresseNumpad = "/code";

    [Header("Anti-rebond (secondes)")]
    [Tooltip("Temps minimum entre deux coups de heurtoir. Augmenter si un seul coup compte plusieurs fois.")]
    public float delaiHeurtoir = 0.25f;
    [Tooltip("Temps minimum entre deux coups de sonnette.")]
    public float delaiSonnette = 0.3f;


    // ---------- Les événements que le jeu écoute ----------

    public static event Action Sonnette;
    public static event Action Heurtoir;
    public static event Action<bool> Interrupteur;   // true = allumé
    public static event Action<int> ToucheNumpad;    // chiffre de 0 à 9


    // ---------- L'état actuel du matériel ----------
    // « static » : ces valeurs survivent au rechargement de la scène,
    // comme le vrai matériel qui ne bouge pas quand on recommence.

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
        recepteur.Bind(adresseNumpad, message => AppuyerNumpad(Mathf.RoundToInt(LireNombre(message))));

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

    public static void AppuyerNumpad(int chiffre)
    {
        if (chiffre < 0 || chiffre > 9) return;     // ex. : -1 = « aucune touche »
        NoterContact();
        ToucheNumpad?.Invoke(chiffre);
    }

    public static void ChangerPoignee(float angle)
    {
        angle = Mathf.Clamp(angle, -1f, 1f);
        if (Mathf.Abs(angle - AnglePoignee) > 0.02f) NoterContact();   // ignore le petit bruit du capteur
        AnglePoignee = angle;
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
