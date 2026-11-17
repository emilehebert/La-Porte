using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Les têtes qui tombent de la ventilation (étape « 2 - Indices »), avec la PHYSIQUE (Rigidbody).
///
/// À chaque partie, ReglagesJeu choisit 3 têtes au hasard (et le code du numpad qui va avec).
/// Ce script crée ces 3 têtes à partir de leur prefab (Assets/Prefabs/Tetes), cachées.
/// Au début de l'étape, il les lâche une après l'autre, chacune à son emplacement :
/// c'est la physique qui les fait tomber, rebondir et rouler jusqu'à ce qu'elles s'arrêtent.
///
///   - Les emplacements sont des objets vides. Les déplacer dans la scène = choisir d'où tombent les têtes
///     (juste sous la ventilation : une tête est cachée jusqu'à ce qu'elle tombe).
///   - Sélectionner cet objet (ou un emplacement) pour les voir dans la Scene, en jaune (éditeur seulement) :
///     la ligne descend jusqu'au sol. Une ligne ROUGE = aucun collider en dessous : la tête tomberait dans le vide.
///   - Chaque tête part avec la rotation de son prefab.
///
/// Branché sur l'étape « 2 - Indices » (EtapeTimeline) : Au Debut → Lacher, Etat Final → PoserAuSol.
/// </summary>
public class TetesQuiTombent : MonoBehaviour
{
    [Tooltip("Les objets vides d'où tombent les têtes, dans l'ordre de chute (le 1er tombe en premier).")]
    public Transform[] emplacements = new Transform[0];

    [Tooltip("Temps (secondes) entre le début de l'étape et la chute de la 1re tête.")]
    public float delaiAvantLaPremiere = 0.2f;

    [Tooltip("Temps (secondes) entre la chute de deux têtes.")]
    public float delaiEntreLesTetes = 0.6f;

    [Tooltip("Petite rotation au hasard quand une tête est lâchée, pour qu'elles ne tombent pas toutes pareil (0 = aucune).")]
    public float rotationAuHasard = 2f;

    const float TailleTete = 0.45f;   // seulement pour le dessin dans l'éditeur


    /// <summary>Une tête créée pour cette partie.</summary>
    class TeteCreee
    {
        public GameObject objet;
        public Rigidbody corps;
        public Transform emplacement;
        public Vector3 versLeCentre;   // de la position de la tête au centre de ce qu'on voit
        public float demiHauteur;
        public bool lachee;
    }

    readonly List<TeteCreee> tetes = new List<TeteCreee>();
    bool creees;


    void Start()
    {
        Creer();
    }

    /// <summary>Au début de l'étape : les têtes tombent une après l'autre.</summary>
    public void Lacher()
    {
        Creer();
        StopAllCoroutines();
        StartCoroutine(LacherUneAUne());
    }

    /// <summary>On saute l'étape : les têtes pas encore tombées sont posées au sol tout de suite.</summary>
    public void PoserAuSol()
    {
        Creer();
        StopAllCoroutines();

        foreach (TeteCreee tete in tetes)
        {
            if (tete.lachee) continue;

            Vector3 centre = tete.emplacement.position;
            if (Physics.Raycast(centre, Vector3.down, out RaycastHit sol, 50f, ~0, QueryTriggerInteraction.Ignore))
            {
                centre.y = sol.point.y + tete.demiHauteur + 0.01f;
            }
            Apparaitre(tete, centre);
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

            yield return new WaitForSeconds(delaiEntreLesTetes);
        }
    }

    /// <summary>Montre la tête, centrée sur ce point. La physique fait le reste.</summary>
    static void Apparaitre(TeteCreee tete, Vector3 centre)
    {
        tete.objet.transform.position = centre - tete.versLeCentre;
        tete.objet.SetActive(true);
        tete.lachee = true;
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
            tetes.Add(new TeteCreee
            {
                objet = objet,
                corps = corps,
                emplacement = emplacements[i],
                versLeCentre = bornes.center - objet.transform.position,
                demiHauteur = bornes.extents.y,
            });

            objet.SetActive(false);   // cachée jusqu'à sa chute
        }
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
    /// <summary>Éditeur seulement : d'où tombent les têtes, et où elles arrivent (si cet objet ou un emplacement est sélectionné).</summary>
    void OnDrawGizmos()
    {
        if (!EstSelectionne()) return;

        foreach (Transform emplacement in emplacements)
        {
            if (emplacement == null) continue;

            Vector3 depart = emplacement.position;
            bool solTrouve = Physics.Raycast(depart, Vector3.down, out RaycastHit sol, 50f, ~0, QueryTriggerInteraction.Ignore);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(depart, Vector3.one * TailleTete);

            Gizmos.color = solTrouve ? Color.yellow : Color.red;
            Gizmos.DrawLine(depart, solTrouve ? sol.point : depart + Vector3.down * 50f);
            if (solTrouve) Gizmos.DrawWireCube(sol.point + Vector3.up * (TailleTete / 2f), Vector3.one * TailleTete);
        }
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
