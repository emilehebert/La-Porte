using System.Collections;
using UnityEngine;

/// <summary>
/// La clé de la fin, pendue à une corde comme les têtes (étape « 4 - Porte ouverte »).
///
/// Mettre ce script sur l'objet de la clé. Le mouvement est calculé (pas de physique) :
/// la clé tombe de son point d'attache, rebondit au bout de la corde, puis se balance doucement.
/// Le concierge (ConciergeFin) l'attrape avec Prendre(), puis la fait disparaître dans la fente avec Deposer().
///
/// Cacher l'objet de la clé au départ et l'ajouter à « Objets à afficher » de l'étape Fin :
/// elle tombe dès qu'elle apparaît.
/// </summary>
public class CleSurCorde : MonoBehaviour
{
    [Tooltip("Objet vide où la corde est attachée (dans la ventilation, à droite des têtes).")]
    public Transform attache;

    [Tooltip("Longueur de la corde (mètres), du point d'attache jusqu'à la clé.")]
    public float longueurCorde = 1.4f;

    [Tooltip("Coché : la clé tombe de la ventilation quand elle apparaît. Décoché : elle pend déjà.")]
    public bool tomberEnApparaissant = true;

    [Tooltip("Durée de la chute et du rebond (secondes).")]
    public float dureeChute = 1.2f;

    [Tooltip("Balancement de la clé au bout de la corde (degrés).")]
    public float balancement = 6f;

    [Tooltip("Épaisseur de la corde (mètres).")]
    public float epaisseurCorde = 0.02f;

    [Tooltip("Combien de fois la texture de la corde se répète par mètre. 0 = automatique (une répétition toutes les 6 épaisseurs). Plus petit = motif plus grand.")]
    public float repetitionsCordeParMetre = 0f;

    [Tooltip("Matériau de la corde. Vide = le même que les têtes.")]
    public Material materiauCorde;

    LineRenderer corde;
    bool tenue;
    float descente;      // 0 = collée au point d'attache, 1 = au bout de la corde
    float heureDebut;


    void Awake()
    {
        CreerCorde();
    }

    /// <summary>La corde visible. Créée une seule fois (aussi appelée par Prendre, si la clé n'a jamais été montrée).</summary>
    void CreerCorde()
    {
        if (corde != null) return;
        if (materiauCorde == null)
        {
            // La même corde que les têtes.
            TetesQuiTombent tetes = FindFirstObjectByType<TetesQuiTombent>(FindObjectsInactive.Include);
            materiauCorde = tetes != null && tetes.materiauCorde != null ? tetes.materiauCorde : Resources.Load<Material>("CordeTete");
        }

        GameObject objet = new GameObject("Corde - " + name);
        objet.transform.SetParent(transform.parent, false);
        corde = objet.AddComponent<LineRenderer>();
        corde.useWorldSpace = true;
        corde.positionCount = 2;
        corde.widthMultiplier = epaisseurCorde;
        corde.sharedMaterial = materiauCorde;
        TetesQuiTombent.RepeterTexture(corde, epaisseurCorde, repetitionsCordeParMetre);
        corde.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        corde.receiveShadows = false;
    }

    void OnEnable()
    {
        // Laissée cochée dans la scène par erreur : elle attend l'étape Fin (c'est elle qui la montre).
        if (!tenue && !ConciergeFin.EtapeFinEnCours())
        {
            gameObject.SetActive(false);
            return;
        }

        heureDebut = Time.time;
        if (corde != null) corde.enabled = !tenue;
        if (tomberEnApparaissant && !tenue) StartCoroutine(Tomber());
        else descente = 1f;
    }

    void OnDisable()
    {
        if (corde != null) corde.enabled = false;
    }

    IEnumerator Tomber()
    {
        // Chute, puis petit rebond amorti autour du bout de la corde.
        for (float t = 0f; t < dureeChute; t += Time.deltaTime)
        {
            float x = t / dureeChute;
            descente = x < 0.35f
                ? (x / 0.35f) * (x / 0.35f)                                         // chute qui accélère
                : 1f + 0.08f * Mathf.Exp(-6f * (x - 0.35f)) * Mathf.Sin(25f * (x - 0.35f));   // rebond
            yield return null;
        }
        descente = 1f;
    }

    void LateUpdate()
    {
        if (tenue || attache == null) return;

        float angle = balancement * Mathf.Sin((Time.time - heureDebut) * 1.8f) * Mathf.Min(1f, descente);
        Vector3 bas = Quaternion.AngleAxis(angle, attache.right) * Vector3.down;
        transform.position = attache.position + bas * (longueurCorde * descente);

        corde.SetPosition(0, attache.position);
        corde.SetPosition(1, transform.position);
    }

    /// <summary>Le concierge attrape la clé : la corde disparaît, la clé suit sa main.</summary>
    public void Prendre(Transform main, Vector3 position, Vector3 rotation)
    {
        StopAllCoroutines();
        tenue = true;
        CreerCorde();
        corde.enabled = false;
        if (!gameObject.activeSelf) gameObject.SetActive(true);   // la clé n'avait pas été montrée : elle apparaît dans sa main
        Vector3 tailleMonde = transform.lossyScale;
        transform.SetParent(main, false);
        transform.localPosition = position;
        transform.localEulerAngles = rotation;

        // Même taille qu'avant, même si l'os de la main a une échelle (fréquent avec les FBX Mixamo).
        Vector3 echelleMain = main.lossyScale;
        transform.localScale = new Vector3(tailleMonde.x / echelleMain.x, tailleMonde.y / echelleMain.y, tailleMonde.z / echelleMain.z);
    }

    /// <summary>La clé passe dans la fente à courrier : elle disparaît.</summary>
    public void Deposer()
    {
        gameObject.SetActive(false);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (attache == null) return;
        Gizmos.color = Color.yellow;
        Vector3 bout = attache.position + Vector3.down * longueurCorde;
        Gizmos.DrawLine(attache.position, bout);
        Gizmos.DrawWireSphere(bout, 0.05f);
    }
#endif
}
