using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Les têtes qui tombent de la ventilation (étape « 2 - Indices »), PENDUES à une corde élastique.
///
/// À chaque partie, ReglagesJeu choisit 2 têtes au hasard (et le code du numpad à 4 chiffres qui va avec).
/// Ce script crée ces têtes à partir de leur prefab (Assets/Prefabs/Tetes), cachées.
/// Au début de l'étape, il les lâche une après l'autre, chacune à son emplacement
/// (les premiers de la liste : s'il y a plus d'emplacements que de têtes, les derniers ne servent pas) :
/// la tête tombe, la corde la retient, elle rebondit un peu et se balance (physique : Rigidbody + SpringJoint).
///
/// Heurtoir : chaque coup pousse les têtes et les fait tourner, pour voir leurs détails.
///
///   - Les emplacements sont des objets vides : c'est là que la corde est attachée (au-dessus du plafond,
///     sous la ventilation : une tête est cachée jusqu'à ce qu'elle tombe).
///   - Sélectionner cet objet (ou un emplacement) pour voir dans la Scene (éditeur seulement), en jaune,
///     la corde et la tête au repos. En ROUGE : la tête toucherait le sol (corde trop longue).
///   - Chaque tête part avec la rotation de son prefab.
///
/// Branché sur l'étape « 2 - Indices » (EtapeTimeline) : Au Debut → Lacher, Etat Final → PoserAuSol.
/// </summary>
public class TetesQuiTombent : MonoBehaviour
{
    [Tooltip("Les objets vides d'où tombent les têtes (et où leur corde est attachée), dans l'ordre de chute.")]
    public Transform[] emplacements = new Transform[0];

    [Tooltip("Temps (secondes) entre le début de l'étape et la chute de la 1re tête.")]
    public float delaiAvantLaPremiere = 0.2f;

    [Tooltip("Temps (secondes) entre la chute de deux têtes.")]
    public float delaiEntreLesTetes = 0.6f;

    [Tooltip("Petite rotation au hasard quand une tête est lâchée, pour qu'elles ne tombent pas toutes pareil (0 = aucune).")]
    public float rotationAuHasard = 2f;

    [Header("Corde")]
    [Tooltip("Longueur de la corde au repos (mètres), de l'emplacement jusqu'au dessus de la tête.")]
    public float longueurCorde = 1.6f;

    [Tooltip("Raideur de la corde. Plus petit = plus élastique (la tête descend plus bas et rebondit plus).")]
    public float raideur = 800f;

    [Tooltip("Freine les rebonds. Plus grand = la tête se calme plus vite.")]
    public float amortissement = 10f;

    [Tooltip("Freine le balancement (résistance de l'air). 0 = la tête se balance très longtemps.")]
    public float freinageBalancement = 0.3f;

    [Tooltip("Freine la rotation de la tête sur elle-même. 0 = elle tourne très longtemps.")]
    public float freinageRotation = 0.5f;

    [Tooltip("Épaisseur de la corde (mètres). Trop fine, elle disparaît avec la basse résolution PS1.")]
    public float epaisseurCorde = 0.03f;

    [Tooltip("Matériau de la corde. Vide = Resources/CordeTete.")]
    public Material materiauCorde;

    [Header("Heurtoir")]
    [Tooltip("Vitesse (m/s) donnée à chaque tête par un coup de heurtoir, dans une direction au hasard. 0 = rien.")]
    public float pousseeHeurtoir = 0.6f;

    [Tooltip("Vitesse de rotation (tours/s) donnée autour de la corde par un coup de heurtoir, pour voir les côtés.")]
    public float rotationHeurtoir = 0.4f;

    [Header("Son")]
    [Tooltip("Joué à la chute de CHAQUE tête (ex. : vent-tete-tombe), dans le corridor. Vide = aucun son.")]
    public AudioSource sonChute;

    const float TailleTete = 0.45f;   // seulement pour le dessin dans l'éditeur


    /// <summary>Une tête créée pour cette partie.</summary>
    class TeteCreee
    {
        public GameObject objet;
        public Rigidbody corps;
        public Transform emplacement;
        public Vector3 versLeCentre;   // de la position de la tête au centre de ce qu'on voit
        public float demiHauteur;
        public Vector3 pointDAttache;  // le dessus de la tête, dans le repère de la tête
        public LineRenderer corde;
        public bool lachee;
    }

    readonly List<TeteCreee> tetes = new List<TeteCreee>();
    bool creees;


    void OnEnable()  { Capteurs.Heurtoir += Pousser; }
    void OnDisable() { Capteurs.Heurtoir -= Pousser; }

    void Start()
    {
        Creer();
    }

    void LateUpdate()
    {
        // La corde suit la tête (après la physique et l'interpolation).
        foreach (TeteCreee tete in tetes)
        {
            if (tete.corde == null || !tete.lachee) continue;
            tete.corde.SetPosition(0, tete.emplacement.position);
            tete.corde.SetPosition(1, tete.objet.transform.TransformPoint(tete.pointDAttache));
        }
    }

    /// <summary>Au début de l'étape : les têtes tombent une après l'autre, retenues par leur corde.</summary>
    public void Lacher()
    {
        Creer();
        StopAllCoroutines();
        StartCoroutine(LacherUneAUne());
    }

    /// <summary>
    /// On saute l'étape : les têtes pas encore tombées sont mises tout de suite au bout de leur corde, immobiles.
    /// (Le nom date d'avant les cordes; il est gardé parce qu'il est branché dans la scène.)
    /// </summary>
    public void PoserAuSol()
    {
        Creer();
        StopAllCoroutines();

        foreach (TeteCreee tete in tetes)
        {
            if (tete.lachee) continue;

            Vector3 centre = tete.emplacement.position + Vector3.down * (DescenteAuRepos(tete) + tete.demiHauteur);
            Apparaitre(tete, centre);
            if (tete.corps != null)
            {
                tete.corps.linearVelocity = Vector3.zero;
                tete.corps.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>Un coup de heurtoir : chaque tête tombée est poussée et tourne sur elle-même.</summary>
    void Pousser()
    {
        foreach (TeteCreee tete in tetes)
        {
            if (!tete.lachee || tete.corps == null) continue;

            Vector2 direction = Random.insideUnitCircle.normalized;
            tete.corps.AddForce(new Vector3(direction.x, 0f, direction.y) * pousseeHeurtoir, ForceMode.VelocityChange);

            float sens = Random.value < 0.5f ? -1f : 1f;
            tete.corps.AddTorque(Vector3.up * (sens * rotationHeurtoir * 2f * Mathf.PI), ForceMode.VelocityChange);
        }
    }

    IEnumerator LacherUneAUne()
    {
        yield return new WaitForSeconds(delaiAvantLaPremiere);

        foreach (TeteCreee tete in tetes)
        {
            if (tete.lachee) continue;

            Apparaitre(tete, tete.emplacement.position);
            if (tete.corps != null) tete.corps.angularVelocity = Random.insideUnitSphere * rotationAuHasard;
            SortieSon.Superposer(sonChute);

            yield return new WaitForSeconds(delaiEntreLesTetes);
        }
    }

    /// <summary>Montre la tête, centrée sur ce point, et l'attache à sa corde. La physique fait le reste.</summary>
    void Apparaitre(TeteCreee tete, Vector3 centre)
    {
        tete.objet.transform.position = centre - tete.versLeCentre;
        tete.objet.SetActive(true);
        tete.lachee = true;

        Attacher(tete);
        if (tete.corde != null)
        {
            tete.corde.enabled = true;
            LateUpdate();   // la corde est déjà au bon endroit dès la première image
        }
    }

    /// <summary>
    /// La corde : un SpringJoint entre le dessus de la tête et l'emplacement.
    /// Plus courte que la longueur de la corde, elle est molle; au-delà, elle tire comme un élastique.
    /// </summary>
    void Attacher(TeteCreee tete)
    {
        if (tete.corps == null || tete.objet.GetComponent<SpringJoint>() != null) return;

        SpringJoint joint = tete.objet.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = null;   // attachée au décor (au plafond)
        joint.anchor = tete.pointDAttache;
        joint.connectedAnchor = tete.emplacement.position;
        joint.minDistance = 0f;
        joint.maxDistance = longueurCorde;
        joint.spring = raideur;
        joint.damper = amortissement;
        joint.tolerance = 0.005f;
        joint.enableCollision = false;

        tete.corps.linearDamping = freinageBalancement;
        tete.corps.angularDamping = freinageRotation;
    }

    /// <summary>De combien la tête descend sous l'emplacement, une fois immobile : la corde + son étirement par le poids.</summary>
    float DescenteAuRepos(TeteCreee tete)
    {
        float masse = tete.corps != null ? tete.corps.mass : 0f;
        float etirement = raideur > 0f ? masse * -Physics.gravity.y / raideur : 0f;
        return longueurCorde + etirement;
    }

    /// <summary>
    /// Crée les têtes tirées au hasard, cachées. Une seule fois, au plus tard au premier Lacher / PoserAuSol
    /// (le GestionnaireEtapes peut appeler PoserAuSol avant notre Start, quand on saute directement à une étape).
    /// </summary>
    void Creer()
    {
        if (creees) return;
        creees = true;

        ReglagesJeu reglages = ReglagesJeu.Instance;
        if (reglages == null)
        {
            Debug.LogError("[Têtes] Aucun ReglagesJeu dans la scène : les têtes ne peuvent pas être choisies.");
            return;
        }

        if (materiauCorde == null) materiauCorde = Resources.Load<Material>("CordeTete");

        int[] tombees = reglages.TetesTombees;
        if (emplacements.Length < tombees.Length)
        {
            Debug.LogError($"[Têtes] {tombees.Length} têtes tombent, mais il n'y a que {emplacements.Length} emplacements sur « {name} ».");
        }

        for (int i = 0; i < tombees.Length && i < emplacements.Length; i++)
        {
            Tete tete = reglages.tetes[tombees[i]];

            if (tete.modele == null)
            {
                Debug.LogError($"[Têtes] Pas de prefab pour « {tete.description} » : menu « La Porte → Finaliser les têtes ».");
                continue;
            }
            if (emplacements[i] == null)
            {
                Debug.LogError($"[Têtes] L'emplacement {i + 1} est vide sur « {name} ».");
                continue;
            }

            GameObject objet = Instantiate(tete.modele, emplacements[i].position, tete.modele.transform.rotation, transform);
            objet.name = tete.modele.name;

            Rigidbody corps = objet.GetComponent<Rigidbody>();
            if (corps == null)
            {
                Debug.LogWarning($"[Têtes] « {objet.name} » n'a pas de Rigidbody : elle ne tombera pas. Menu « La Porte → Finaliser les têtes ».");
            }

            Bounds bornes = Bornes(objet.transform);
            Vector3 dessus = bornes.center + Vector3.up * bornes.extents.y;

            tetes.Add(new TeteCreee
            {
                objet = objet,
                corps = corps,
                emplacement = emplacements[i],
                versLeCentre = bornes.center - objet.transform.position,
                demiHauteur = bornes.extents.y,
                pointDAttache = objet.transform.InverseTransformPoint(dessus),
                corde = corps != null ? CreerCorde(objet.name) : null,
            });

            objet.SetActive(false);   // cachée jusqu'à sa chute
        }
    }

    /// <summary>La corde visible : une ligne entre l'emplacement et le dessus de la tête.</summary>
    LineRenderer CreerCorde(string nomTete)
    {
        GameObject objet = new GameObject("Corde - " + nomTete);
        objet.transform.SetParent(transform, false);

        LineRenderer ligne = objet.AddComponent<LineRenderer>();
        ligne.useWorldSpace = true;
        ligne.positionCount = 2;
        ligne.widthMultiplier = epaisseurCorde;
        ligne.sharedMaterial = materiauCorde;
        ligne.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ligne.receiveShadows = false;
        ligne.enabled = false;   // apparaît avec sa tête
        return ligne;
    }

    /// <summary>La boîte (dans le monde) qui contient tous les morceaux visibles de la tête (visage, cheveux, lunettes...).</summary>
    static Bounds Bornes(Transform objet)
    {
        Bounds bornes = new Bounds(objet.position, Vector3.zero);
        bool trouve = false;

        foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>())
        {
            if (!trouve)
            {
                bornes = rendu.bounds;
                trouve = true;
            }
            else
            {
                bornes.Encapsulate(rendu.bounds);
            }
        }
        return bornes;
    }


#if UNITY_EDITOR
    /// <summary>Éditeur seulement : la corde et la tête au repos (si cet objet ou un emplacement est sélectionné).</summary>
    void OnDrawGizmos()
    {
        if (!EstSelectionne()) return;

        // Étirement approximatif (une tête d'environ 4 kg) : seulement pour le dessin.
        float descente = longueurCorde + (raideur > 0f ? 4f * 9.81f / raideur : 0f);

        foreach (Transform emplacement in emplacements)
        {
            if (emplacement == null) continue;

            Vector3 attache = emplacement.position;
            Vector3 centreAuRepos = attache + Vector3.down * (descente + TailleTete / 2f);
            bool toucheLeSol = HauteurDuSol(attache, out float sol) && sol > centreAuRepos.y - TailleTete / 2f;

            Gizmos.color = toucheLeSol ? Color.red : Color.yellow;
            Gizmos.DrawLine(attache, centreAuRepos + Vector3.up * (TailleTete / 2f));
            Gizmos.DrawWireCube(centreAuRepos, Vector3.one * TailleTete);
        }
    }

    /// <summary>
    /// Le sol sous ce point : le collider le PLUS BAS (la corde passe par la ventilation,
    /// qu'il ne faut pas prendre pour le sol).
    /// </summary>
    static bool HauteurDuSol(Vector3 depart, out float hauteur)
    {
        hauteur = float.MaxValue;
        foreach (RaycastHit touche in Physics.RaycastAll(depart, Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            hauteur = Mathf.Min(hauteur, touche.point.y);
        }
        return hauteur < float.MaxValue;
    }

    bool EstSelectionne()
    {
        foreach (Transform choisi in UnityEditor.Selection.transforms)
        {
            if (choisi.IsChildOf(transform)) return true;   // cet objet ou un de ses enfants (les emplacements)
        }
        return false;
    }
#endif
}
