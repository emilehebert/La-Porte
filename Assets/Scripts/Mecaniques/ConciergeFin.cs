using System.Collections;
using UnityEngine;

/// <summary>
/// Le concierge de la fin (étape « 4 - Porte ouverte »).
///
///   Il entre par la droite, marche jusqu'à la clé qui pend à sa corde, l'attrape,
///   va jusqu'à la fente à courrier de la porte, fait semblant d'y glisser la clé, puis repart par la gauche.
///
/// Mise en place :
///   - Le modèle importé en Humanoid (Mixamo), avec un Animator dont le contrôleur a :
///       un paramètre float « Vitesse » (0 = Idle, 1 = Walk), et deux triggers « Prendre » et « Deposer ».
///   - Les clips de marche importés « In Place » : c'est ce script qui avance le personnage (pas de Root Motion).
///   - Quatre objets vides pour le trajet. La flèche bleue (Z) de « Devant la clé » et « Devant la fente »
///     doit pointer vers ce qu'il regarde (la clé, la porte).
///   - Cacher le concierge au départ et l'ajouter à « Objets à afficher » de l'étape Fin :
///     la séquence démarre quand il apparaît. La scène est rechargée au recommencement : rien à remettre en place.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ConciergeFin : MonoBehaviour
{
    [Header("Trajet (objets vides dans la scène)")]
    [Tooltip("À droite, hors du champ de la caméra : il apparaît ici.")]
    public Transform depart;
    [Tooltip("Là où il s'arrête pour attraper la clé. Flèche bleue (Z) vers la clé.")]
    public Transform devantCle;
    [Tooltip("Là où il s'arrête devant la porte. Flèche bleue (Z) vers la fente à courrier.")]
    public Transform devantFente;
    [Tooltip("À gauche, hors du champ : il disparaît ici.")]
    public Transform sortie;

    [Header("La clé")]
    public CleSurCorde cle;
    [Tooltip("Position de la clé dans la main droite (repère de l'os de la main). Ajuster en Play, puis recopier.")]
    public Vector3 positionDansLaMain = new Vector3(0f, 0.08f, 0.03f);
    public Vector3 rotationDansLaMain = Vector3.zero;

    [Header("Marche")]
    [Tooltip("Mètres par seconde. À régler pour que les pieds ne glissent pas (souvent entre 1 et 1.5 pour un Walk Mixamo).")]
    public float vitesseMarche = 1.2f;
    [Tooltip("Degrés par seconde quand il tourne.")]
    public float vitesseRotation = 240f;
    [Tooltip("Secondes après le bon code avant qu'il entre (le temps de voir la clé tomber).")]
    public float attenteAvantDEntrer = 2f;

    [Header("Moments des animations (secondes)")]
    [Tooltip("Dans l'animation « Prendre » : le moment où la main touche la clé.")]
    public float momentMainSurCle = 0.9f;
    [Tooltip("Durée totale de l'animation « Prendre ».")]
    public float dureePrendre = 2f;
    [Tooltip("Dans l'animation « Deposer » : le moment où la main est dans la fente.")]
    public float momentMainDansFente = 1f;
    [Tooltip("Durée totale de l'animation « Deposer ».")]
    public float dureeDeposer = 2f;

    [Header("Sons (optionnels)")]
    public AudioSource sonPas;
    public AudioSource sonCleDansFente;

    static readonly int Vitesse = Animator.StringToHash("Vitesse");
    static readonly int Prendre = Animator.StringToHash("Prendre");
    static readonly int Deposer = Animator.StringToHash("Deposer");

    Animator animateur;


    void Awake()
    {
        animateur = GetComponent<Animator>();
        animateur.applyRootMotion = false;   // c'est le script qui le fait avancer
    }

    void OnEnable()
    {
        // Laissé coché dans la scène par erreur : il attend l'étape Fin (c'est elle qui le montre).
        if (!EtapeFinEnCours())
        {
            gameObject.SetActive(false);
            return;
        }

        if (depart != null) transform.SetPositionAndRotation(depart.position, depart.rotation);
        StartCoroutine(Sequence());
    }

    /// <summary>Vrai seulement pendant l'étape Fin (en Play).</summary>
    public static bool EtapeFinEnCours()
    {
        GestionnaireEtapes jeu = GestionnaireEtapes.Instance;
        return jeu != null && jeu.Etapes != null && jeu.EtapeActuelle is EtapeFin;
    }

    IEnumerator Sequence()
    {
        animateur.SetFloat(Vitesse, 0f);
        yield return new WaitForSeconds(attenteAvantDEntrer);

        // 1. Entre par la droite et va jusqu'à la clé.
        yield return MarcherJusqua(devantCle);
        yield return TournerVers(devantCle.forward);

        // 2. Attrape la clé.
        animateur.SetTrigger(Prendre);
        yield return new WaitForSeconds(momentMainSurCle);
        if (cle != null)
        {
            Transform main = animateur.GetBoneTransform(HumanBodyBones.RightHand);
            cle.Prendre(main != null ? main : transform, positionDansLaMain, rotationDansLaMain);
        }
        yield return new WaitForSeconds(dureePrendre - momentMainSurCle);

        // 3. Va à la porte et glisse la clé dans la fente.
        yield return MarcherJusqua(devantFente);
        yield return TournerVers(devantFente.forward);

        animateur.SetTrigger(Deposer);
        yield return new WaitForSeconds(momentMainDansFente);
        if (cle != null) cle.Deposer();
        if (sonCleDansFente != null) sonCleDansFente.Play();
        yield return new WaitForSeconds(dureeDeposer - momentMainDansFente);

        // 4. Repart par la gauche.
        yield return MarcherJusqua(sortie);
        gameObject.SetActive(false);
    }

    /// <summary>Tourne vers le point, puis marche jusqu'à lui : accélère au départ, ralentit à l'arrivée.</summary>
    IEnumerator MarcherJusqua(Transform cible)
    {
        if (cible == null) yield break;

        Vector3 versCible = cible.position - transform.position;
        versCible.y = 0f;
        if (versCible.sqrMagnitude > 0.0001f) yield return TournerVers(versCible);

        if (sonPas != null) sonPas.Play();
        float vitesse = 0f;

        while (true)
        {
            Vector3 ecart = cible.position - transform.position;
            Vector3 plat = new Vector3(ecart.x, 0f, ecart.z);
            float distance = plat.magnitude;
            if (distance < 0.03f) break;

            // Vitesse voulue : pleine en route, de moins en moins vite dans le dernier demi-mètre.
            float voulue = vitesseMarche * Mathf.Clamp01(distance / 0.5f + 0.25f);
            vitesse = Mathf.MoveTowards(vitesse, voulue, vitesseMarche * 2.5f * Time.deltaTime);

            transform.position = Vector3.MoveTowards(transform.position, cible.position, vitesse * Time.deltaTime);
            Quaternion regard = Quaternion.LookRotation(plat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, regard, vitesseRotation * Time.deltaTime);

            animateur.SetFloat(Vitesse, vitesse / vitesseMarche, 0.1f, Time.deltaTime);
            yield return null;
        }

        if (sonPas != null) sonPas.Stop();

        // Laisse l'Animator revenir doucement à l'Idle.
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            animateur.SetFloat(Vitesse, 0f, 0.1f, Time.deltaTime);
            yield return null;
        }
        animateur.SetFloat(Vitesse, 0f);
    }

    /// <summary>Tourne sur place vers une direction (à l'horizontale).</summary>
    IEnumerator TournerVers(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) yield break;

        Quaternion voulue = Quaternion.LookRotation(direction);
        while (Quaternion.Angle(transform.rotation, voulue) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, voulue, vitesseRotation * Time.deltaTime);
            yield return null;
        }
        transform.rotation = voulue;
    }


#if UNITY_EDITOR
    /// <summary>Éditeur : le trajet en couleur quand le concierge est sélectionné.</summary>
    void OnDrawGizmosSelected()
    {
        Transform[] points = { depart, devantCle, devantFente, sortie };
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawWireSphere(points[i].position, 0.12f);
            Gizmos.DrawRay(points[i].position, points[i].forward * 0.4f);
            if (i + 1 < points.Length && points[i + 1] != null) Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
#endif
}
