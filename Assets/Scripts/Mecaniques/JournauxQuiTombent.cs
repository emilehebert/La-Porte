using System.Collections;
using UnityEngine;

/// <summary>
/// Les journaux de la bibliothèque (étape « 2 - Indices ») : un journal par tête, avec l'affiche
/// « HELP WANTED » de cette tête (et son numéro) sur la couverture.
///
/// À chaque partie, ReglagesJeu choisit les têtes qui tombent. Les journaux de CES têtes tombent de la
/// bibliothèque sur la table, couverture vers le haut, dans l'ordre de chute des têtes; les autres restent rangés.
/// Le journal glisse hors de l'étagère, tombe en tournant et rebondit un peu sur la table.
///
///   - « Journaux » : un par tête, DANS LE MÊME ORDRE que ReglagesJeu → Tetes.
///   - « Arrivées » : où les journaux atterrissent (objets vides sur la table), dans l'ordre de chute.
///     La flèche bleue (avant) d'une arrivée = le haut de l'affiche : la tourner vers le fond de la pièce
///     pour que l'affiche se lise à l'endroit depuis la caméra de la pièce.
///   - Sélectionner cet objet (ou une arrivée) pour voir dans la Scene, en jaune, le journal posé à chaque arrivée.
///
/// Branché sur l'étape « 2 - Indices » (EtapeTimeline) : Au Debut → Lacher, Etat Final → PoserSurLaTable.
/// Le menu « La Porte → Brancher les journaux » crée et branche tout.
/// </summary>
public class JournauxQuiTombent : MonoBehaviour
{
    [Tooltip("Un journal par tête, dans le même ordre que ReglagesJeu → Tetes.")]
    public Transform[] journaux = new Transform[0];

    [Tooltip("Où les journaux atterrissent sur la table, dans l'ordre de chute. Flèche bleue = haut de l'affiche.")]
    public Transform[] arrivees = new Transform[0];

    [Header("Rythme")]
    [Tooltip("Coché : chaque journal tombe EN MÊME TEMPS que sa tête dans le corridor (mêmes délais que « Têtes qui tombent »);\n" +
             "les deux délais ci-dessous ne servent pas. Décoché : les journaux suivent leurs propres délais.")]
    public bool enMemeTempsQueLesTetes = true;

    [Tooltip("Les têtes à suivre (si « En Meme Temps Que Les Tetes »). Vide = trouvées toutes seules.")]
    public TetesQuiTombent tetes;

    [Tooltip("Temps (secondes) entre le début de l'étape et la chute du 1er journal.")]
    public float delaiAvantLePremier = 0.5f;

    [Tooltip("Temps (secondes) entre la chute de deux journaux.")]
    public float delaiEntreLesJournaux = 0.7f;

    [Header("Chute")]
    [Tooltip("Temps (secondes) pendant lequel le journal glisse hors de l'étagère avant de tomber.")]
    public float dureeSortie = 0.25f;

    [Tooltip("Distance (mètres) dont le journal glisse vers la pièce avant de tomber.")]
    public float distanceSortie = 0.12f;

    [Tooltip("De combien (degrés) le journal penche vers la pièce en sortant de l'étagère.")]
    public float bascule = 20f;

    [Tooltip("Petit saut vers le haut (m/s) au moment de tomber. 0 = il tombe tout droit.")]
    public float elan = 0.8f;

    [Tooltip("Gravité (m/s²). Plus grand = chute plus rapide.")]
    public float gravite = 9.81f;

    [Tooltip("Hauteur (mètres) du rebond sur la table. 0 = pas de rebond.")]
    public float rebond = 0.025f;

    [Tooltip("Durée (secondes) du rebond.")]
    public float dureeRebond = 0.18f;


    /// <summary>
    /// Ce qu'il faut savoir d'un journal pour le poser à plat, couverture en haut. Exprimé dans le repère
    /// tourné du journal : reste vrai où qu'il soit. Mesuré dans l'éditeur et enregistré dans la scène,
    /// parce que dans un build le maillage (Read/Write désactivé) ne peut pas être lu.
    /// </summary>
    [System.Serializable]
    public struct Mesures
    {
        public Vector3 versLeCentre;   // de la position du journal à son centre
        public Vector3 normale;        // la couverture regarde par là
        public Vector3 hautAffiche;    // le haut de l'affiche
        public float epaisseur, largeur, hauteur;
    }

    [HideInInspector] public Mesures[] mesures = new Mesures[0];
    bool[] tombe;
    bool prepare;


    /// <summary>Au début de l'étape : les journaux des têtes tombées tombent sur la table, un après l'autre.</summary>
    public void Lacher()
    {
        if (!Preparer()) return;
        StopAllCoroutines();
        StartCoroutine(LacherUnAUn());
    }

    /// <summary>On saute l'étape : les journaux des têtes tombées sont mis tout de suite sur la table.</summary>
    public void PoserSurLaTable()
    {
        if (!Preparer()) return;
        StopAllCoroutines();

        int[] tombees = ReglagesJeu.Instance.TetesTombees;
        for (int i = 0; i < tombees.Length; i++)
        {
            if (!Valide(tombees[i], i)) continue;
            PoseFinale(tombees[i], arrivees[i], out Vector3 position, out Quaternion rotation);
            journaux[tombees[i]].SetPositionAndRotation(position, rotation);
            tombe[tombees[i]] = true;
        }
    }

    IEnumerator LacherUnAUn()
    {
        float avant = delaiAvantLePremier;
        float entre = delaiEntreLesJournaux;
        if (enMemeTempsQueLesTetes)
        {
            if (tetes == null) tetes = FindAnyObjectByType<TetesQuiTombent>();
            if (tetes != null)
            {
                // Le journal commence à glisser un peu avant : il quitte l'étagère quand la tête tombe.
                avant = Mathf.Max(0f, tetes.delaiAvantLaPremiere - dureeSortie);
                entre = tetes.delaiEntreLesTetes;
            }
        }
        yield return new WaitForSeconds(avant);

        int[] tombees = ReglagesJeu.Instance.TetesTombees;
        for (int i = 0; i < tombees.Length; i++)
        {
            if (!Valide(tombees[i], i) || tombe[tombees[i]]) continue;

            tombe[tombees[i]] = true;
            StartCoroutine(Tomber(journaux[tombees[i]], tombees[i], arrivees[i]));
            yield return new WaitForSeconds(entre);
        }
    }

    IEnumerator Tomber(Transform journal, int index, Transform arrivee)
    {
        Vector3 depart = journal.position;
        Quaternion rotationDepart = journal.rotation;
        PoseFinale(index, arrivee, out Vector3 positionFinale, out Quaternion rotationFinale);

        Vector3 versLaPiece = positionFinale - depart;
        versLaPiece.y = 0f;
        versLaPiece = versLaPiece.sqrMagnitude > 1e-6f ? versLaPiece.normalized : Vector3.forward;
        Vector3 axeBascule = Vector3.Cross(Vector3.up, versLaPiece);

        // 1. Il glisse hors de l'étagère en penchant vers la pièce (de plus en plus vite).
        Vector3 sortie = depart + versLaPiece * distanceSortie;
        Quaternion rotationSortie = Quaternion.AngleAxis(bascule, axeBascule) * rotationDepart;
        for (float t = 0f; t < dureeSortie; t += Time.deltaTime)
        {
            float k = t / dureeSortie;
            k *= k;
            journal.SetPositionAndRotation(Vector3.Lerp(depart, sortie, k), Quaternion.Slerp(rotationDepart, rotationSortie, k));
            yield return null;
        }

        // 2. Il tombe (parabole) en se mettant à plat, couverture en haut.
        float hauteurChute = Mathf.Max(sortie.y - positionFinale.y, 0.01f);
        float g = Mathf.Max(gravite, 0.1f);
        float duree = (elan + Mathf.Sqrt(elan * elan + 2f * g * hauteurChute)) / g;
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            float k = t / duree;
            Vector3 p = Vector3.Lerp(sortie, positionFinale, k);
            p.y = sortie.y + elan * t - 0.5f * g * t * t;
            journal.SetPositionAndRotation(p, Quaternion.Slerp(rotationSortie, rotationFinale, 1f - (1f - k) * (1f - k)));
            yield return null;
        }

        // 3. Petit rebond sur la table.
        for (float t = 0f; t < dureeRebond && rebond > 0f; t += Time.deltaTime)
        {
            float k = t / dureeRebond;
            float bosse = Mathf.Sin(Mathf.PI * k);
            journal.SetPositionAndRotation(positionFinale + Vector3.up * (rebond * bosse),
                                           Quaternion.AngleAxis(4f * bosse * (1f - k), axeBascule) * rotationFinale);
            yield return null;
        }
        journal.SetPositionAndRotation(positionFinale, rotationFinale);
    }

    /// <summary>La position et la rotation du journal posé à plat sur cette arrivée, couverture en haut.</summary>
    void PoseFinale(int index, Transform arrivee, out Vector3 position, out Quaternion rotation)
    {
        Mesures m = mesures[index];
        rotation = RotationAPlat(m, arrivee);
        position = arrivee.position + arrivee.up * (m.epaisseur / 2f) - rotation * m.versLeCentre;
    }

    static Quaternion RotationAPlat(Mesures m, Transform arrivee)
    {
        Vector3 haut = arrivee.up;
        Vector3 avant = Vector3.ProjectOnPlane(arrivee.forward, haut);
        if (avant.sqrMagnitude < 1e-6f) avant = Vector3.ProjectOnPlane(Vector3.forward, haut);

        Vector3 hautAffiche = Vector3.ProjectOnPlane(m.hautAffiche, m.normale);
        Quaternion repereJournal = Quaternion.LookRotation(hautAffiche, m.normale);
        return Quaternion.LookRotation(avant, haut) * Quaternion.Inverse(repereJournal);
    }

    /// <summary>
    /// Mesure les journaux sur l'étagère (une seule fois, au plus tard au premier Lacher / PoserSurLaTable :
    /// le GestionnaireEtapes peut appeler PoserSurLaTable avant notre Start, quand on saute directement à une étape).
    /// </summary>
    bool Preparer()
    {
        if (prepare) return true;

        if (ReglagesJeu.Instance == null)
        {
            Debug.LogError("[Journaux] Aucun ReglagesJeu dans la scène : on ne sait pas quels journaux tombent.");
            return false;
        }
        prepare = true;
        tombe = new bool[journaux.Length];

        // Normalement déjà mesurés dans l'éditeur; sinon (maillage lisible) on mesure maintenant.
        if (mesures == null || mesures.Length != journaux.Length) MesurerLesJournaux();
        for (int i = 0; i < journaux.Length; i++)
        {
            if (journaux[i] != null && mesures[i].epaisseur <= 0f)
            {
                Debug.LogError($"[Journaux] « {journaux[i].name} » n'est pas mesuré : il ne pourra pas tomber. Menu « La Porte → Brancher les journaux ».");
            }
        }

        int nombre = ReglagesJeu.Instance.TetesTombees.Length;
        if (journaux.Length != ReglagesJeu.Instance.tetes.Length)
        {
            Debug.LogError($"[Journaux] Il y a {ReglagesJeu.Instance.tetes.Length} têtes mais {journaux.Length} journaux : menu « La Porte → Brancher les journaux ».");
        }
        if (arrivees.Length < nombre)
        {
            Debug.LogError($"[Journaux] {nombre} journaux tombent, mais il n'y a que {arrivees.Length} arrivées sur « {name} ».");
        }
        return true;
    }

    bool Valide(int index, int ordre)
    {
        return index < journaux.Length && journaux[index] != null && index < mesures.Length && mesures[index].epaisseur > 0f
               && ordre < arrivees.Length && arrivees[ordre] != null;
    }

    /// <summary>Mesure chaque journal (couverture, haut de l'affiche, dimensions). Appelé par le menu, et dans l'éditeur.</summary>
    public void MesurerLesJournaux()
    {
        mesures = new Mesures[journaux.Length];
        for (int i = 0; i < journaux.Length; i++)
        {
            if (journaux[i] != null) Mesurer(journaux[i], out mesures[i]);
        }
    }

#if UNITY_EDITOR
    /// <summary>Éditeur : les mesures suivent si on change la liste des journaux.</summary>
    void OnValidate()
    {
        if (!Application.isPlaying) MesurerLesJournaux();
    }
#endif

    /// <summary>
    /// Trouve la couverture (le plus petit morceau du maillage : une seule face), le sens où elle regarde
    /// et le haut de l'affiche (le sens où la coordonnée V de la texture augmente). Tout est exprimé dans
    /// le repère tourné du journal, pour rester vrai quand on le tourne.
    /// </summary>
    static bool Mesurer(Transform journal, out Mesures m)
    {
        m = default;
        MeshFilter filtre = journal.GetComponentInChildren<MeshFilter>(true);
        if (filtre == null || filtre.sharedMesh == null) return false;

        Mesh maillage = filtre.sharedMesh;
        if (!maillage.isReadable && !Application.isEditor) return false;   // build : utiliser les mesures enregistrées

        int couverture = 0;
        int moinsDeTriangles = int.MaxValue;
        for (int s = 0; s < maillage.subMeshCount; s++)
        {
            int n = (int)maillage.GetIndexCount(s);
            if (n >= 3 && n < moinsDeTriangles) { moinsDeTriangles = n; couverture = s; }
        }
        if (moinsDeTriangles == int.MaxValue) return false;

        int[] triangles = maillage.GetTriangles(couverture);
        Vector3[] sommets = maillage.vertices;
        Vector2[] uv = maillage.uv;
        if (uv.Length != sommets.Length) return false;

        Vector3 p0 = sommets[triangles[0]], p1 = sommets[triangles[1]], p2 = sommets[triangles[2]];
        Vector2 t0 = uv[triangles[0]], t1 = uv[triangles[1]], t2 = uv[triangles[2]];
        Vector3 e1 = p1 - p0, e2 = p2 - p0;
        Vector2 d1 = t1 - t0, d2 = t2 - t0;
        float det = d1.x * d2.y - d2.x * d1.y;
        if (Mathf.Abs(det) < 1e-8f) return false;
        Vector3 sensV = (e2 * d1.x - e1 * d2.x) / det;   // le sens où V augmente (le haut de l'image)

        // La normale exacte de la face; les normales des sommets disent seulement de quel côté elle regarde.
        Vector3 normaleLocale = Vector3.Cross(e1, e2);
        Vector3[] normales = maillage.normals;
        if (normales.Length == sommets.Length)
        {
            Vector3 moyenne = normales[triangles[0]] + normales[triangles[1]] + normales[triangles[2]];
            if (Vector3.Dot(normaleLocale, moyenne) < 0f) normaleLocale = -normaleLocale;
        }

        Matrix4x4 versMonde = filtre.transform.localToWorldMatrix;
        Quaternion inverse = Quaternion.Inverse(journal.rotation);
        Vector3 normale = (inverse * versMonde.inverse.transpose.MultiplyVector(normaleLocale)).normalized;
        Vector3 haut = (inverse * versMonde.MultiplyVector(sensV)).normalized;
        Vector3 centre = filtre.transform.TransformPoint(maillage.bounds.center);

        // Les dimensions : les coins du maillage projetés sur les axes de la couverture.
        Vector3 lateral = Vector3.Cross(normale, haut).normalized;
        Vector3 min = Vector3.one * float.MaxValue, max = -min;
        Bounds b = maillage.bounds;
        for (int c = 0; c < 8; c++)
        {
            Vector3 coin = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
            Vector3 r = inverse * (filtre.transform.TransformPoint(coin) - journal.position);
            Vector3 p = new Vector3(Vector3.Dot(r, lateral), Vector3.Dot(r, normale), Vector3.Dot(r, haut));
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        m.versLeCentre = inverse * (centre - journal.position);
        m.normale = normale;
        m.hautAffiche = haut;
        m.largeur = max.x - min.x;
        m.epaisseur = max.y - min.y;
        m.hauteur = max.z - min.z;
        return m.epaisseur > 0f;
    }


#if UNITY_EDITOR
    /// <summary>Éditeur seulement : le journal posé à chaque arrivée (si cet objet ou une arrivée est sélectionné).</summary>
    void OnDrawGizmos()
    {
        if (!EstSelectionne() || Application.isPlaying) return;

        Mesures m = default;
        foreach (Mesures candidate in mesures)
        {
            if (candidate.epaisseur > 0f) { m = candidate; break; }
        }
        if (m.epaisseur <= 0f) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < arrivees.Length; i++)
        {
            if (arrivees[i] == null) continue;

            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(arrivees[i].forward, arrivees[i].up), arrivees[i].up);
            Gizmos.matrix = Matrix4x4.TRS(arrivees[i].position, rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, m.epaisseur / 2f, 0f), new Vector3(m.largeur, m.epaisseur, m.hauteur));
            // flèche : le haut de l'affiche
            Gizmos.DrawLine(new Vector3(0f, m.epaisseur, 0f), new Vector3(0f, m.epaisseur, m.hauteur * 0.45f));
            Gizmos.DrawLine(new Vector3(0f, m.epaisseur, m.hauteur * 0.45f), new Vector3(-m.largeur * 0.15f, m.epaisseur, m.hauteur * 0.3f));
            Gizmos.DrawLine(new Vector3(0f, m.epaisseur, m.hauteur * 0.45f), new Vector3(m.largeur * 0.15f, m.epaisseur, m.hauteur * 0.3f));
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    bool EstSelectionne()
    {
        foreach (Transform choisi in UnityEditor.Selection.transforms)
        {
            if (choisi.IsChildOf(transform)) return true;
        }
        return false;
    }
#endif
}
