using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Menu « La Porte → Brancher les journaux ».
///
/// Met un journal par tête dans la bibliothèque, avec l'affiche HELP WANTED de la tête sur la couverture,
/// et fait tomber sur la table les journaux des têtes tombées. On peut le refaire sans danger.
///   1. Matériaux (Assets/Materials/Journal) : « Journal_47_Moustachu »... (copie du matériau de la couverture
///      du journal du décor, avec l'affiche Assets/Textures/Journal/Affiche_47_Moustachu.png) et « Journal_tranche »
///      (les côtés, couleur papier). Refaire le menu après avoir changé un numéro ou une affiche.
///   2. « Journaux qui tombent » : 5 copies du journal du décor, rangées sur l'étagère à la place qu'il a
///      dans le modèle Couloir (même s'il a été déplacé dans la scène; il est caché), et 2 « Arrivée journal »
///      sur le plateau de la table, côté bibliothèque, tournées pour que l'affiche se lise depuis la Room Camera.
///   3. L'étape « 2 - Indices » fait tomber les journaux au début (Au Debut → Lacher)
///      et les pose sur la table si on la saute (Etat Final → PoserSurLaTable).
///   4. Le journal placeholder (cube + textes) est désactivé.
///
/// Les journaux et les arrivées déjà créés ne sont pas déplacés (on peut les ajuster à la main),
/// sauf si les journaux ne sont plus dans la bibliothèque : ils y sont remis et les arrivées recalculées.
/// Ensuite : Ctrl+S.
/// </summary>
public static class JournauxLaPorte
{
    const string NomGroupe = "Journaux qui tombent";
    const string NomCinematique = "Cinématique des indices";
    const string NomJournalDuDecor = "Journal";
    const string NomTable = "Table";
    const string NomBibliotheque = "Library";
    const string NomCameraPiece = "Room Camera";
    const string DossierMateriaux = "Assets/Materials/Journal";
    const string DossierAffiches = "Assets/Textures/Journal";

    // Taille voulue des journaux par rapport au journal du modèle Couloir (1 = pareil). Plus grand = affiche plus lisible.
    // Limité tout seul pour que le journal debout ne touche pas la planche du dessus. Refaire le menu après l'avoir changé.
    const float Agrandissement = 1.35f;
    const float JeuSousLaPlanche = 0.01f;    // espace minimum entre le haut du journal et la planche du dessus (m)
    const float EcartSurLEtagere = 0.004f;   // entre deux journaux rangés (m)
    const float BordTable = 0.06f;           // espace entre un journal posé et le bord de la table (m)
    const float EcartSurLaTable = 0.05f;     // espace entre deux journaux posés sur la table (m)
    static readonly Color CouleurPapier = new Color(0.92f, 0.9f, 0.84f);
    static readonly string[] ProprietesTexture = { "_MainTex", "_BaseMap" };
    static readonly string[] ProprietesCouleur = { "_MainColor", "_Color", "_BaseColor" };

    [MenuItem("La Porte/Brancher les journaux")]
    public static void Brancher()
    {
        ReglagesJeu reglages = Object.FindFirstObjectByType<ReglagesJeu>(FindObjectsInactive.Include);
        if (reglages == null)
        {
            EditorUtility.DisplayDialog("La Porte", "Aucun ReglagesJeu dans la scène : faire d'abord « La Porte → Installer dans la scène ouverte ».", "OK");
            return;
        }

        MeshRenderer decor = TrouverJournalDuDecor();
        if (decor == null)
        {
            EditorUtility.DisplayDialog("La Porte", $"Journal du décor introuvable : il faut un objet « {NomJournalDuDecor} » avec un maillage " +
                                                    "en 2 morceaux (côtés + couverture), dans la bibliothèque (modèle Couloir).", "OK");
            return;
        }

        int couverture = IndexCouverture(decor);
        Material modeleMateriau = decor.sharedMaterials.Length > couverture ? decor.sharedMaterials[couverture] : null;
        if (modeleMateriau == null)
        {
            EditorUtility.DisplayDialog("La Porte", $"Le journal du décor n'a pas de matériau sur sa couverture (morceau {couverture}).", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Brancher les journaux");
        int groupeUndo = Undo.GetCurrentGroup();
        List<string> rapport = new List<string>();

        // 1. Les matériaux
        CreerDossier(DossierMateriaux);
        Material tranche = Materiau($"{DossierMateriaux}/Journal_tranche.mat", modeleMateriau, null);
        Material[] couvertures = new Material[reglages.tetes.Length];
        for (int i = 0; i < reglages.tetes.Length; i++)
        {
            Tete tete = reglages.tetes[i];
            Texture2D affiche = TrouverAffiche(tete.numero);
            if (affiche == null)
            {
                rapport.Add($"✘ Pas d'affiche « Affiche_{tete.numero}_... » dans {DossierAffiches} pour « {tete.description} » : couverture blanche");
            }
            couvertures[i] = Materiau($"{DossierMateriaux}/Journal_{tete.numero}_{NomDeFichier(tete.description)}.mat", modeleMateriau, affiche);
        }
        rapport.Add($"✔ {couvertures.Length} matériaux de couverture + Journal_tranche dans {DossierMateriaux}");

        // 2. Le groupe, les journaux sur l'étagère, les arrivées sur la table
        Transform groupe = TrouverDansLaScene(NomGroupe);
        if (groupe == null)
        {
            GameObject nouveau = new GameObject(NomGroupe);
            Undo.RegisterCreatedObjectUndo(nouveau, "Brancher les journaux");
            Transform cinematique = TrouverDansLaScene(NomCinematique);
            if (cinematique != null && SansRotationNiEchelle(cinematique)) Undo.SetTransformParent(nouveau.transform, cinematique, "Brancher les journaux");
            nouveau.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            groupe = nouveau.transform;
        }

        JournauxQuiTombent composant = groupe.GetComponent<JournauxQuiTombent>();
        if (composant == null) composant = Undo.AddComponent<JournauxQuiTombent>(groupe.gameObject);
        Undo.RecordObject(composant, "Brancher les journaux");

        Etagere etagere = PoseSurLEtagere(decor, rapport);
        Transform[] journaux = RangerLesJournaux(composant.journaux, decor, groupe, reglages.tetes.Length, etagere, rapport, out bool replaces);
        for (int i = 0; i < journaux.Length; i++)
        {
            Tete tete = reglages.tetes[i];
            journaux[i].name = $"Journal {tete.numero} - {tete.description}";
            MeshRenderer rendu = journaux[i].GetComponent<MeshRenderer>();
            Undo.RecordObject(rendu, "Brancher les journaux");
            Material[] materiaux = rendu.sharedMaterials;
            for (int m = 0; m < materiaux.Length; m++) materiaux[m] = m == couverture ? couvertures[i] : tranche;
            rendu.sharedMaterials = materiaux;
        }

        if (decor.gameObject.activeSelf)
        {
            Undo.RecordObject(decor.gameObject, "Brancher les journaux");
            decor.gameObject.SetActive(false);
            rapport.Add($"✔ Le journal du décor est caché (remplacé par les {journaux.Length} journaux)");
        }

        composant.journaux = journaux;
        composant.MesurerLesJournaux();   // avant les arrivées : elles ont besoin de la taille des journaux
        composant.arrivees = Arrivees(composant.arrivees, groupe, etagere, composant.mesures[0], replaces, rapport);
        EditorUtility.SetDirty(composant);

        // 3. L'étape
        BrancherEtape(composant, rapport);

        // 4. Le placeholder
        foreach (JournalPlaceholder placeholder in Object.FindObjectsByType<JournalPlaceholder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!placeholder.gameObject.activeSelf) continue;
            Undo.RecordObject(placeholder.gameObject, "Brancher les journaux");
            placeholder.gameObject.SetActive(false);
            rapport.Add($"✔ Journal placeholder « {placeholder.name} » désactivé (cube + textes)");
        }

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(reglages.gameObject.scene);
        Selection.activeGameObject = groupe.gameObject;

        rapport.Add("");
        rapport.Add("Sélectionner « Journaux qui tombent » pour voir, en jaune, les journaux posés sur la table.");
        rapport.Add("Déplacer / tourner les « Arrivée journal » pour changer où ils atterrissent (flèche bleue = haut de l'affiche).");
        EditorUtility.DisplayDialog("La Porte — journaux branchés", string.Join("\n", rapport) + "\n\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- 1. Matériaux et affiches ----------

    /// <summary>Crée ou met à jour un matériau : copie du modèle (même shader PS1), avec cette affiche (ou couleur papier).</summary>
    static Material Materiau(string chemin, Material modele, Texture2D affiche)
    {
        Material materiau = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (materiau == null)
        {
            materiau = new Material(modele);
            AssetDatabase.CreateAsset(materiau, chemin);
        }
        else
        {
            materiau.shader = modele.shader;
            materiau.CopyPropertiesFromMaterial(modele);
        }

        foreach (string propriete in ProprietesTexture)
        {
            if (materiau.HasProperty(propriete)) materiau.SetTexture(propriete, affiche);
        }
        foreach (string propriete in ProprietesCouleur)
        {
            if (materiau.HasProperty(propriete)) materiau.SetColor(propriete, affiche != null ? Color.white : CouleurPapier);
        }
        EditorUtility.SetDirty(materiau);
        return materiau;
    }

    /// <summary>L'affiche « Affiche_47_... » (de preference dans Assets/Textures/Journal), réglée pour ne pas baver sur les bords.</summary>
    static Texture2D TrouverAffiche(string numero)
    {
        string prefixe = $"Affiche_{numero}_";
        string trouve = null;
        foreach (string guid in AssetDatabase.FindAssets($"{prefixe} t:Texture2D"))
        {
            string chemin = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(chemin).StartsWith(prefixe)) continue;
            if (trouve == null || chemin.StartsWith(DossierAffiches)) trouve = chemin;
        }
        if (trouve == null) return null;

        if (AssetImporter.GetAtPath(trouve) is TextureImporter importeur && importeur.wrapMode != TextureWrapMode.Clamp)
        {
            importeur.wrapMode = TextureWrapMode.Clamp;
            importeur.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(trouve);
    }

    static string NomDeFichier(string texte)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) texte = texte.Replace(c, '_');
        return texte.Replace(' ', '_');
    }


    // ---------- 2. Les journaux et les arrivées ----------

    /// <summary>
    /// Le journal du modèle Couloir : un « Journal » avec un maillage en 2 morceaux ou plus, qui n'est ni
    /// une de nos copies, ni le placeholder. De préférence celui qui vient du modèle (prefab).
    /// </summary>
    static MeshRenderer TrouverJournalDuDecor()
    {
        MeshRenderer meilleur = null;
        foreach (MeshRenderer rendu in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rendu.name != NomJournalDuDecor || EditorUtility.IsPersistent(rendu)) continue;
            if (rendu.GetComponentInParent<JournauxQuiTombent>(true) != null) continue;
            if (rendu.GetComponentInParent<JournalPlaceholder>(true) != null) continue;

            MeshFilter filtre = rendu.GetComponent<MeshFilter>();
            if (filtre == null || filtre.sharedMesh == null || filtre.sharedMesh.subMeshCount < 2) continue;

            if (meilleur == null || PrefabUtility.IsPartOfPrefabInstance(rendu)) meilleur = rendu;
        }
        return meilleur;
    }

    /// <summary>La couverture = le plus petit morceau du maillage (une seule face).</summary>
    static int IndexCouverture(MeshRenderer rendu)
    {
        Mesh maillage = rendu.GetComponent<MeshFilter>().sharedMesh;
        int index = maillage.subMeshCount - 1;
        uint moins = uint.MaxValue;
        for (int s = 0; s < maillage.subMeshCount; s++)
        {
            uint n = maillage.GetIndexCount(s);
            if (n >= 3 && n < moins) { moins = n; index = s; }
        }
        return index;
    }

    /// <summary>Où ranger les journaux : la pose du journal du modèle sur l'étagère (agrandi), et dans quel sens les aligner.</summary>
    struct Etagere
    {
        public Vector3 position;     // position du 1er journal
        public Quaternion rotation;
        public Vector3 echelle;      // échelle dans le monde (agrandissement compris)
        public Vector3 sens;         // vers le journal suivant (vers le milieu de l'étagère)
        public float pas;            // épaisseur d'un journal + l'écart
        public Vector3 centre;       // centre du 1er journal
        public float agrandissement; // celui qui est vraiment appliqué (limité par la planche du dessus)
        public bool aBibliotheque;
        public Bounds bibliotheque;
    }

    /// <summary>
    /// La pose du journal sur l'étagère = celle qu'il a dans le modèle Couloir (le FBX), même si le journal
    /// du décor a été déplacé dans la scène (ex. posé sur la table pour voir l'affiche). Agrandi de
    /// « Agrandissement » en gardant le bas posé sur l'étagère, au même endroit.
    /// </summary>
    static Etagere PoseSurLEtagere(MeshRenderer decor, List<string> rapport)
    {
        Matrix4x4 pose = decor.transform.localToWorldMatrix;
        Transform source = PrefabUtility.GetCorrespondingObjectFromSource(decor.transform);
        GameObject racine = PrefabUtility.GetOutermostPrefabInstanceRoot(decor.gameObject);
        if (source != null && racine != null)
        {
            pose = racine.transform.localToWorldMatrix * source.root.worldToLocalMatrix * source.localToWorldMatrix;
        }

        Mesh maillage = decor.GetComponent<MeshFilter>().sharedMesh;
        Vector3 pivot = pose.GetColumn(3);
        Vector3 centre = pose.MultiplyPoint3x4(maillage.bounds.center);

        // demi-hauteur (dans le monde) du journal tel qu'il est rangé
        float bas = float.MaxValue, haut = float.MinValue;
        Bounds b = maillage.bounds;
        for (int c = 0; c < 8; c++)
        {
            Vector3 coin = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
            float y = pose.MultiplyPoint3x4(coin).y;
            bas = Mathf.Min(bas, y);
            haut = Mathf.Max(haut, y);
        }
        float demiHauteur = (haut - bas) / 2f;

        // agrandi autour du milieu du dessous : il reste posé sur l'étagère, centré au même endroit;
        // pas plus haut que la planche du dessus
        float k = Agrandissement;
        float planche = PlancheAuDessus(centre, bas);
        if (planche < float.MaxValue && haut > bas)
        {
            float kMax = (planche - bas - JeuSousLaPlanche) / (haut - bas);
            if (kMax < k)
            {
                k = Mathf.Max(1f, kMax);
                rapport.Add($"✔ Agrandissement limité à x{k:0.00} (au lieu de x{Agrandissement:0.00}) : debout, le journal toucherait la planche du dessus");
            }
        }
        Etagere e = new Etagere();
        e.agrandissement = k;
        e.rotation = pose.rotation;
        e.echelle = pose.lossyScale * k;
        e.position = pivot + (1f - k) * (centre - pivot) + Vector3.up * ((k - 1f) * demiHauteur);
        e.centre = centre + Vector3.up * ((k - 1f) * demiHauteur);

        Vector3 taille = Vector3.Scale(maillage.bounds.size, Abs(e.echelle));
        int axe = taille.x <= taille.y && taille.x <= taille.z ? 0 : (taille.y <= taille.z ? 1 : 2);
        e.sens = (e.rotation * (axe == 0 ? Vector3.right : axe == 1 ? Vector3.up : Vector3.forward)).normalized;
        e.pas = taille[axe] + EcartSurLEtagere;

        e.aBibliotheque = BornesDe(NomBibliotheque, out e.bibliotheque);
        if (e.aBibliotheque)
        {
            if (Vector3.Dot(e.bibliotheque.center - e.centre, e.sens) < 0f) e.sens = -e.sens;   // vers le milieu de l'étagère
            if (!DansLaBibliotheque(e, e.centre))
            {
                rapport.Add($"✘ Le journal du modèle Couloir n'est pas dans la bibliothèque « {NomBibliotheque} » : ranger les journaux à la main");
            }
        }
        return e;
    }

    /// <summary>
    /// La hauteur du dessous de la planche juste au-dessus de ce point de la bibliothèque (la face horizontale
    /// la plus basse au-dessus du journal). float.MaxValue si rien au-dessus.
    /// </summary>
    static float PlancheAuDessus(Vector3 point, float basDuJournal)
    {
        MeshFilter bibliotheque = TrouverMaillage(NomBibliotheque);
        if (bibliotheque == null) return float.MaxValue;

        Mesh maillage = bibliotheque.sharedMesh;
        Matrix4x4 versMonde = bibliotheque.transform.localToWorldMatrix;
        Vector3[] sommets = maillage.vertices;
        int[] triangles = maillage.triangles;
        float plus_bas = float.MaxValue;
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Vector3 a = versMonde.MultiplyPoint3x4(sommets[triangles[i]]);
            Vector3 b = versMonde.MultiplyPoint3x4(sommets[triangles[i + 1]]);
            Vector3 c = versMonde.MultiplyPoint3x4(sommets[triangles[i + 2]]);
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f || Mathf.Abs(n.normalized.y) < 0.9f) continue;   // seulement les faces horizontales

            float y = (a.y + b.y + c.y) / 3f;
            if (y <= basDuJournal + 0.05f || y >= plus_bas) continue;
            float minX = Mathf.Min(a.x, b.x, c.x), maxX = Mathf.Max(a.x, b.x, c.x);
            float minZ = Mathf.Min(a.z, b.z, c.z), maxZ = Mathf.Max(a.z, b.z, c.z);
            if (point.x < minX || point.x > maxX || point.z < minZ || point.z > maxZ) continue;   // pas au-dessus du journal
            plus_bas = y;
        }
        return plus_bas;
    }

    static bool DansLaBibliotheque(Etagere e, Vector3 point)
    {
        Bounds b = e.bibliotheque;
        b.Expand(0.1f);
        return b.Contains(point);
    }

    static void PoserSurLEtagere(Transform journal, Etagere e, int i, Transform groupe)
    {
        journal.SetPositionAndRotation(e.position + e.sens * (e.pas * i), e.rotation);
        journal.localScale = Diviser(e.echelle, groupe.lossyScale);
    }

    /// <summary>
    /// Les journaux rangés comme des livres dans la bibliothèque, à la place du journal du modèle Couloir,
    /// vers le milieu de l'étagère. Ceux qui y sont déjà, à la bonne taille, ne bougent pas (on peut les ajuster
    /// à la main); s'ils sont ailleurs (ex. sur la table) ou pas à la bonne taille, ils sont rangés à nouveau.
    /// </summary>
    static Transform[] RangerLesJournaux(Transform[] existants, MeshRenderer decor, Transform groupe, int nombre, Etagere etagere,
                                         List<string> rapport, out bool replaces)
    {
        bool complets = existants != null && existants.Length == nombre;
        for (int i = 0; complets && i < nombre; i++) complets = existants[i] != null;

        if (complets)
        {
            bool tousRanges = true, bonneTaille = true;
            for (int i = 0; i < nombre; i++)
            {
                tousRanges &= !etagere.aBibliotheque || DansLaBibliotheque(etagere, CentreDe(existants[i]));
                bonneTaille &= Vector3.Distance(existants[i].lossyScale, etagere.echelle) <= 0.001f * etagere.echelle.magnitude;
            }
            replaces = !tousRanges || !bonneTaille;
            if (!replaces)
            {
                rapport.Add($"✔ Les {nombre} journaux étaient déjà dans la bibliothèque, à la bonne taille (pas déplacés; matériaux mis à jour)");
                return existants;
            }

            for (int i = 0; i < nombre; i++)
            {
                Undo.RecordObject(existants[i], "Brancher les journaux");
                PoserSurLEtagere(existants[i], etagere, i, groupe);
            }
            rapport.Add(!tousRanges
                ? $"✔ Les {nombre} journaux n'étaient pas dans la bibliothèque : ils y sont remis (agrandis x{etagere.agrandissement:0.00})"
                : $"✔ Les {nombre} journaux sont agrandis (x{etagere.agrandissement:0.00}) et rangés à nouveau dans la bibliothèque");
            return existants;
        }

        replaces = true;
        if (existants != null)
        {
            foreach (Transform ancien in existants)
            {
                if (ancien != null) Undo.DestroyObjectImmediate(ancien.gameObject);
            }
        }

        Transform[] journaux = new Transform[nombre];
        for (int i = 0; i < nombre; i++)
        {
            GameObject copie = Object.Instantiate(decor.gameObject, groupe);
            Undo.RegisterCreatedObjectUndo(copie, "Brancher les journaux");
            copie.SetActive(true);
            PoserSurLEtagere(copie.transform, etagere, i, groupe);
            journaux[i] = copie.transform;
        }
        rapport.Add($"✔ {nombre} journaux (x{etagere.agrandissement:0.00}) rangés dans la bibliothèque, à la place du journal du modèle (ordre de ReglagesJeu → Tetes)");
        return journaux;
    }

    static Vector3 CentreDe(Transform journal)
    {
        MeshFilter filtre = journal.GetComponentInChildren<MeshFilter>(true);
        if (filtre == null || filtre.sharedMesh == null) return journal.position;
        return filtre.transform.TransformPoint(filtre.sharedMesh.bounds.center);
    }

    /// <summary>Le plateau de la table : son centre, ses 2 axes horizontaux (dans le monde) et sa demi-taille sur chacun.</summary>
    struct Plateau
    {
        public Vector3 centre;
        public float dessus;
        public Vector3 axeA, axeB;
        public float demiA, demiB;
    }

    static bool TrouverPlateau(out Plateau p)
    {
        p = default;
        MeshFilter table = TrouverMaillage(NomTable);
        if (table == null) return false;

        Transform t = table.transform;
        Bounds b = table.sharedMesh.bounds;
        p.centre = t.TransformPoint(b.center);
        p.dessus = table.GetComponent<Renderer>().bounds.max.y;

        int trouves = 0;
        for (int a = 0; a < 3; a++)
        {
            Vector3 axe = Vector3.zero;
            axe[a] = 1f;
            Vector3 dansLeMonde = t.TransformVector(axe);
            if (dansLeMonde.sqrMagnitude < 1e-12f || Mathf.Abs(dansLeMonde.normalized.y) > 0.7f) continue;   // l'axe vertical

            Vector3 horizontal = Vector3.ProjectOnPlane(dansLeMonde, Vector3.up).normalized;
            float demi = b.extents[a] * dansLeMonde.magnitude;
            if (trouves == 0) { p.axeA = horizontal; p.demiA = demi; }
            else { p.axeB = horizontal; p.demiB = demi; }
            trouves++;
        }
        return trouves == 2;
    }

    /// <summary>
    /// Une arrivée par journal qui tombe, sur le plateau de la table, du côté de la bibliothèque, côte à côte
    /// et alignées sur les bords de la table (ils ne se chevauchent pas). Le haut de l'affiche est tourné à
    /// l'opposé de la caméra de la pièce : elle s'y lit à l'endroit. Les arrivées existantes ne bougent pas,
    /// sauf si les journaux viennent d'être (re)rangés : elles sont alors recalculées.
    /// </summary>
    static Transform[] Arrivees(Transform[] existantes, Transform groupe, Etagere etagere, JournauxQuiTombent.Mesures journal,
                                bool recalculer, List<string> rapport)
    {
        int nombre = ReglagesJeu.NombreDeTetesQuiTombent;
        bool completes = existantes != null && existantes.Length >= nombre;
        for (int i = 0; completes && i < nombre; i++) completes = existantes[i] != null;
        if (completes && !recalculer)
        {
            rapport.Add("✔ Les arrivées sur la table existaient déjà (pas déplacées)");
            return existantes;
        }

        Camera camera = TrouverCameraPiece();
        Vector3[] positions = new Vector3[nombre];
        Vector3 hautAffiche;

        if (TrouverPlateau(out Plateau p) && journal.largeur > 0f)
        {
            Vector3 versEtagere = Vector3.ProjectOnPlane(etagere.centre - p.centre, Vector3.up);

            // L'axe qui va vers la bibliothèque (« approche ») et l'autre (« côté ») : les journaux sont côte à côte sur le côté.
            bool aVersEtagere = Mathf.Abs(Vector3.Dot(versEtagere, p.axeA)) >= Mathf.Abs(Vector3.Dot(versEtagere, p.axeB));
            Vector3 approche = aVersEtagere ? p.axeA : p.axeB, cote = aVersEtagere ? p.axeB : p.axeA;
            float demiApproche = aVersEtagere ? p.demiA : p.demiB, demiCote = aVersEtagere ? p.demiB : p.demiA;
            if (Vector3.Dot(versEtagere, approche) < 0f) approche = -approche;   // + = vers la bibliothèque

            // Le haut de l'affiche suit l'axe d'approche (journaux côte à côte par leur largeur);
            // si ça ne rentre pas sur la table, on les tourne d'un quart de tour.
            float largeur = journal.largeur, hauteur = journal.hauteur;
            bool hautSurApproche = nombre * largeur + (nombre - 1) * EcartSurLaTable <= 2f * (demiCote - BordTable);
            float surApproche = hautSurApproche ? hauteur : largeur;
            float surCote = hautSurApproche ? largeur : hauteur;
            if (nombre * surCote + (nombre - 1) * EcartSurLaTable > 2f * (demiCote - BordTable))
            {
                rapport.Add("✘ Les journaux sont trop grands pour tenir côte à côte sur la table : ils se chevauchent (réduire l'agrandissement)");
            }

            // Collés du côté de la bibliothèque, côte à côte, centrés en face de l'étagère (sans dépasser du plateau).
            float a = demiApproche - BordTable - surApproche / 2f;
            float pas = surCote + EcartSurLaTable;
            float etendue = (nombre - 1) * pas / 2f;
            float limite = Mathf.Max(0f, demiCote - BordTable - surCote / 2f - etendue);
            float milieu = Mathf.Clamp(Vector3.Dot(versEtagere, cote), -limite, limite);
            for (int i = 0; i < nombre; i++)
            {
                positions[i] = p.centre + approche * a + cote * (milieu + (i - (nombre - 1) / 2f) * pas);
                positions[i].y = p.dessus;
            }

            // le haut de l'affiche : sur l'axe choisi, dans le sens qui s'éloigne de la caméra
            hautAffiche = hautSurApproche ? approche : cote;
            if (camera != null && Vector3.Dot(positions[0] - camera.transform.position, hautAffiche) < 0f) hautAffiche = -hautAffiche;
            rapport.Add($"✔ {nombre} arrivées sur le plateau de la table « {NomTable} », côté bibliothèque, côte à côte");
        }
        else
        {
            hautAffiche = Vector3.right;
            for (int i = 0; i < nombre; i++) positions[i] = etagere.centre + Vector3.right * 0.6f + Vector3.down + Vector3.forward * (0.5f * i);
            rapport.Add($"✘ Table « {NomTable} » introuvable : placer les « Arrivée journal » à la main sur la table");
        }

        Transform[] arrivees = new Transform[nombre];
        for (int i = 0; i < nombre; i++)
        {
            Quaternion rotation = Quaternion.LookRotation(hautAffiche, Vector3.up);

            Transform arrivee = completes ? existantes[i] : null;
            if (arrivee == null)
            {
                GameObject nouvelle = new GameObject($"Arrivée journal {i + 1}");
                Undo.RegisterCreatedObjectUndo(nouvelle, "Brancher les journaux");
                nouvelle.transform.SetParent(groupe, false);
                arrivee = nouvelle.transform;
            }
            else
            {
                Undo.RecordObject(arrivee, "Brancher les journaux");
            }
            arrivee.SetPositionAndRotation(positions[i], rotation);
            arrivees[i] = arrivee;
        }
        if (camera == null) rapport.Add($"✘ Caméra « {NomCameraPiece} » introuvable : vérifier le sens des arrivées (flèche bleue = haut de l'affiche)");
        return arrivees;
    }

    static MeshFilter TrouverMaillage(string nom)
    {
        Transform objet = TrouverDansLaScene(nom);
        return objet != null ? objet.GetComponentInChildren<MeshFilter>(true) : null;
    }

    static Camera TrouverCameraPiece()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras) if (camera.name == NomCameraPiece) return camera;
        foreach (Camera camera in cameras) if (camera.targetDisplay == 0) return camera;
        return Camera.main;
    }


    // ---------- 3. L'étape ----------

    /// <summary>L'étape des indices (celle qui lâche les têtes) fait aussi tomber les journaux.</summary>
    static void BrancherEtape(JournauxQuiTombent journaux, List<string> rapport)
    {
        EtapeTimeline etape = null;
        foreach (EtapeTimeline candidate in Object.FindObjectsByType<EtapeTimeline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < candidate.auDebut.GetPersistentEventCount(); i++)
            {
                if (candidate.auDebut.GetPersistentTarget(i) is TetesQuiTombent) etape = candidate;
            }
            if (etape == null && candidate.name.Contains("Indices")) etape = candidate;
        }
        if (etape == null)
        {
            rapport.Add("✘ Étape des indices introuvable : sur l'étape, brancher Au Debut → Lacher et Etat Final → PoserSurLaTable");
            return;
        }

        Undo.RecordObject(etape, "Brancher les journaux");
        if (!APourAppel(etape.auDebut, journaux, nameof(JournauxQuiTombent.Lacher)))
        {
            UnityEventTools.AddPersistentListener(etape.auDebut, journaux.Lacher);
        }
        if (!APourAppel(etape.etatFinal, journaux, nameof(JournauxQuiTombent.PoserSurLaTable)))
        {
            UnityEventTools.AddPersistentListener(etape.etatFinal, journaux.PoserSurLaTable);
        }
        rapport.Add($"✔ « {etape.Nom} » fait tomber les journaux au début, et les pose sur la table si on saute l'étape");

        // La lampe validée, l'interrupteur est levé : la lumière de la pièce (inversée) est éteinte.
        // À l'étape 2 elle se rallume (et le corridor aussi) pour qu'on voie les têtes ET les journaux tomber.
        int lumieres = 0;
        foreach (LumiereInterrupteur lumiere in Object.FindObjectsByType<LumiereInterrupteur>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!APourAppel(etape.auDebut, lumiere, nameof(LumiereInterrupteur.ToutAllumer)))
            {
                UnityEventTools.AddPersistentListener(etape.auDebut, lumiere.ToutAllumer);
            }
            if (!APourAppel(etape.etatFinal, lumiere, nameof(LumiereInterrupteur.ToutAllumer)))
            {
                UnityEventTools.AddPersistentListener(etape.etatFinal, lumiere.ToutAllumer);
            }
            lumieres++;
        }
        foreach (LumieresCorridor corridor in Object.FindObjectsByType<LumieresCorridor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!APourAppel(etape.auDebut, corridor, nameof(LumieresCorridor.ToutAllumer)))
            {
                UnityEventTools.AddPersistentListener(etape.auDebut, corridor.ToutAllumer);
            }
            if (!APourAppel(etape.etatFinal, corridor, nameof(LumieresCorridor.ToutAllumer)))
            {
                UnityEventTools.AddPersistentListener(etape.etatFinal, corridor.ToutAllumer);
            }
        }
        EditorUtility.SetDirty(etape);
        rapport.Add($"✔ « {etape.Nom} » rallume la pièce ({lumieres} lumière(s) de l'interrupteur) et le corridor : on voit les deux chutes en même temps");
    }

    static bool APourAppel(UnityEventBase evenement, Object cible, string methode)
    {
        for (int i = 0; i < evenement.GetPersistentEventCount(); i++)
        {
            if (evenement.GetPersistentTarget(i) == cible && evenement.GetPersistentMethodName(i) == methode) return true;
        }
        return false;
    }


    // ---------- Outils ----------

    /// <summary>L'objet de ce nom le plus haut dans la Hierarchy (même désactivé).</summary>
    static Transform TrouverDansLaScene(string nom)
    {
        Transform meilleur = null;
        int meilleureProfondeur = int.MaxValue;
        foreach (Transform objet in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (objet.name != nom || EditorUtility.IsPersistent(objet)) continue;

            int profondeur = 0;
            for (Transform parent = objet.parent; parent != null; parent = parent.parent) profondeur++;
            if (profondeur < meilleureProfondeur)
            {
                meilleur = objet;
                meilleureProfondeur = profondeur;
            }
        }
        return meilleur;
    }

    /// <summary>Les bornes (dans le monde) de tous les Renderers actifs de l'objet nommé ainsi.</summary>
    static bool BornesDe(string nom, out Bounds bornes)
    {
        bornes = default;
        Transform objet = TrouverDansLaScene(nom);
        if (objet == null) return false;

        bool trouve = false;
        foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>())
        {
            if (!trouve) { bornes = rendu.bounds; trouve = true; }
            else bornes.Encapsulate(rendu.bounds);
        }
        return trouve;
    }

    static bool SansRotationNiEchelle(Transform objet)
    {
        return Quaternion.Angle(objet.rotation, Quaternion.identity) < 0.01f && (objet.lossyScale - Vector3.one).sqrMagnitude < 1e-6f;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static Vector3 Diviser(Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

    static void CreerDossier(string chemin)
    {
        if (AssetDatabase.IsValidFolder(chemin)) return;

        string parent = Path.GetDirectoryName(chemin).Replace('\\', '/');
        CreerDossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(chemin));
    }
}
