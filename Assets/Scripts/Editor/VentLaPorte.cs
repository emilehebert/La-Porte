using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Menu « La Porte → Brancher la vent animée (Vent_corridor.fbx) ».
///
/// À l'étape « 2 - Indices », la vent du corridor se fait frapper de l'intérieur (shape keys de Blender
/// = blend shapes dans Unity), puis elle s'ouvre (rotation sur sa charnière), et les têtes tombent ensuite.
///
///   Couloir
///   └── Vent_corridor (animée)    Animator (c'est lui que la Timeline anime)
///       └── Grille                le FBX : coups (blend shapes) + ouverture (rotation)
///
/// La rotation est sur « Grille », un enfant : elle est jouée telle quelle, à la même place que dans Blender.
///
/// Ce menu :
///   1. règle l'import de Assets/Model/Vent_corridor.fbx (blend shapes, animation Generic)
///      et lui donne le même matériau que l'ancienne vent;
///   2. met la nouvelle vent exactement à la place de l'ancienne et cache l'ancienne (décochée, pas supprimée);
///   3. copie l'animation (coups + ouverture) dans Assets/Animation/Indices/VentCoups.anim
///      et l'ajoute à la Timeline de l'étape 2 (piste « Vent (coups) »). La vent reste ouverte à la fin;
///   4. règle « Têtes qui tombent » : la 1re tête tombe quand l'animation de la vent est finie.
///
/// À refaire après chaque nouvel export de Blender (même nom de fichier). Ensuite : Ctrl+S.
/// Export Blender : Bake Animation coché, NLA Strips et All Actions DÉCOCHÉS, Apply Modifiers décoché.
/// </summary>
public static class VentLaPorte
{
    const string CheminFbx = "Assets/Model/Vent_corridor.fbx";
    const string DossierAnimation = "Assets/Animation/Indices";
    const string CheminAnimation = DossierAnimation + "/VentCoups.anim";
    const string NomCouloir = "Couloir";
    const string NomAncienneVent = "Vent_corridor";
    const string NomNouvelleVent = "Vent_corridor (animée)";
    const string NomGrille = "Grille";   // l'enfant animé : c'est aussi le chemin des courbes dans l'animation
    const string NomCinematique = "Cinématique des indices";
    const string NomPiste = "Vent (coups)";

    [MenuItem("La Porte/Brancher la vent animée (Vent_corridor.fbx)")]
    public static void Brancher()
    {
        // --- Vérifications : on ne change rien si quelque chose manque ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CheminFbx) == null)
        {
            Message($"« {CheminFbx} » introuvable : exporter la vent de Blender à cet endroit.");
            return;
        }

        Transform couloir = TetesLaPorte.TrouverDansLaScene(NomCouloir);
        Transform ancienne = couloir != null ? couloir.Find(NomAncienneVent) : null;
        if (ancienne == null)
        {
            Message($"« {NomCouloir} → {NomAncienneVent} » introuvable dans la scène.");
            return;
        }

        Transform cinematique = TetesLaPorte.TrouverDansLaScene(NomCinematique);
        PlayableDirector directeur = cinematique != null ? cinematique.GetComponent<PlayableDirector>() : null;
        TimelineAsset timeline = directeur != null ? directeur.playableAsset as TimelineAsset : null;
        if (timeline == null)
        {
            Message($"« {NomCinematique} » ou sa Timeline introuvable : faire d'abord « La Porte → Finaliser les têtes ».");
            return;
        }

        List<string> rapport = new List<string>();

        // --- 1. L'import du FBX et l'animation ---
        RegleImport(ancienne, rapport);
        GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(CheminFbx);

        AnimationClip source = ClipDuFbx();
        if (source == null)
        {
            Message("Le FBX n'a pas d'animation.\n\nDans Blender, à l'export FBX : Bake Animation coché, NLA Strips et All Actions DÉCOCHÉS.");
            return;
        }
        AnimationClip animation = CopierAnimation(source, rapport);
        if (animation == null)
        {
            Message(string.Join("\n", rapport));
            return;
        }

        Undo.SetCurrentGroupName("Brancher la vent animée");
        int groupeUndo = Undo.GetCurrentGroup();

        // --- 2. La nouvelle vent, à la place de l'ancienne ---
        Transform conteneur = couloir.Find(NomNouvelleVent);
        if (conteneur != null && PrefabUtility.GetCorrespondingObjectFromSource(conteneur.gameObject) == modele)
        {
            // Version d'avant de ce menu : le FBX directement, sans conteneur (la rotation ne pouvait pas être jouée).
            Undo.DestroyObjectImmediate(conteneur.gameObject);
            conteneur = null;
            rapport.Add("✔ Ancienne version de la vent animée remplacée");
        }
        if (conteneur == null)
        {
            GameObject nouveau = new GameObject(NomNouvelleVent);
            Undo.RegisterCreatedObjectUndo(nouveau, "Brancher la vent animée");
            Undo.SetTransformParent(nouveau.transform, couloir, "Brancher la vent animée");
            nouveau.transform.localPosition = Vector3.zero;
            nouveau.transform.localRotation = Quaternion.identity;
            nouveau.transform.localScale = Vector3.one;
            conteneur = nouveau.transform;
            rapport.Add($"✔ « {NomNouvelleVent} » ajoutée sous « {NomCouloir} »");
        }

        Transform grille = conteneur.Find(NomGrille);
        if (grille == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modele, couloir.gameObject.scene);
            instance.name = NomGrille;
            instance.transform.SetParent(conteneur, false);   // même repère que l'ancienne vent : celui du fichier Blender
            Undo.RegisterCreatedObjectUndo(instance, "Brancher la vent animée");
            grille = instance.transform;
        }
        Aligner(conteneur, grille, ancienne, rapport);

        if (ancienne.gameObject.activeSelf)
        {
            Undo.RecordObject(ancienne.gameObject, "Brancher la vent animée");
            ancienne.gameObject.SetActive(false);
            rapport.Add($"✔ L'ancienne « {NomAncienneVent} » est cachée (décochée, pas supprimée)");
        }

        // L'Animator sur le conteneur seulement (la Timeline l'anime). Celui que l'import met sur le FBX ne sert pas.
        Animator animateur = conteneur.GetComponent<Animator>();
        if (animateur == null) animateur = Undo.AddComponent<Animator>(conteneur.gameObject);
        Undo.RecordObject(animateur, "Brancher la vent animée");
        animateur.runtimeAnimatorController = null;
        animateur.applyRootMotion = false;
        animateur.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Animator animateurDuFbx = grille.GetComponent<Animator>();
        if (animateurDuFbx != null)
        {
            Undo.RecordObject(animateurDuFbx, "Brancher la vent animée");
            animateurDuFbx.enabled = false;
        }

        foreach (SkinnedMeshRenderer rendu in grille.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Undo.RecordObject(rendu, "Brancher la vent animée");
            rendu.updateWhenOffscreen = true;   // la vent ouverte dépasse sa boîte de départ : ne pas la faire disparaître
        }

        // --- 3. La Timeline de l'étape 2 ---
        AnimationTrack piste = timeline.GetOutputTracks().OfType<AnimationTrack>().FirstOrDefault(t => t.name == NomPiste);
        if (piste == null) piste = timeline.CreateTrack<AnimationTrack>(null, NomPiste);
        foreach (TimelineClip ancienClip in piste.GetClips().ToList()) timeline.DeleteClip(ancienClip);

        double finDesAutres = 0;
        foreach (TrackAsset autre in timeline.GetOutputTracks())
        {
            if (autre == piste) continue;
            foreach (TimelineClip clip in autre.GetClips()) finDesAutres = System.Math.Max(finDesAutres, clip.end);
        }

        TimelineClip clipVent = piste.CreateClip(animation);
        clipVent.displayName = "Coups + ouverture";
        clipVent.start = 0;
        // Plus long que l'animation si le journal finit après : la vent reste ouverte (dernière image) jusqu'à la fin.
        clipVent.duration = System.Math.Max(animation.length, finDesAutres);
        EditorUtility.SetDirty(timeline);

        Undo.RecordObject(directeur, "Brancher la vent animée");
        directeur.SetGenericBinding(piste, animateur);
        rapport.Add($"✔ Timeline de l'étape 2 : piste « {NomPiste} » ({animation.length:0.00} s)");

        // --- 4. Les têtes tombent quand la vent est ouverte ---
        TetesQuiTombent tetes = Object.FindFirstObjectByType<TetesQuiTombent>(FindObjectsInactive.Include);
        if (tetes != null)
        {
            Undo.RecordObject(tetes, "Brancher la vent animée");
            tetes.delaiAvantLaPremiere = animation.length;
            rapport.Add($"✔ La 1re tête tombe à {animation.length:0.00} s, quand la vent est ouverte (Têtes qui tombent → Delai Avant La Premiere)");
        }
        else
        {
            rapport.Add("⚠ « Têtes qui tombent » introuvable : faire « La Porte → Finaliser les têtes »");
        }

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(couloir.gameObject.scene);
        Selection.activeGameObject = conteneur.gameObject;

        EditorUtility.DisplayDialog("La Porte — vent animée", string.Join("\n", rapport) + "\n\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- 1. L'import et l'animation ----------

    /// <summary>Blend shapes et animation importées (Generic, pour la Timeline), et le matériau de l'ancienne vent.</summary>
    static void RegleImport(Transform ancienne, List<string> rapport)
    {
        ModelImporter importeur = (ModelImporter)AssetImporter.GetAtPath(CheminFbx);
        bool change = false;

        if (!importeur.importBlendShapes) { importeur.importBlendShapes = true; change = true; }
        if (!importeur.importAnimation) { importeur.importAnimation = true; change = true; }
        if (importeur.animationType != ModelImporterAnimationType.Generic)
        {
            importeur.animationType = ModelImporterAnimationType.Generic;
            change = true;
        }

        // Le même matériau que l'ancienne vent (même texture, même shader PSX).
        Renderer ancienRendu = ancienne.GetComponentInChildren<Renderer>(true);
        GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(CheminFbx);
        Renderer nouveauRendu = modele.GetComponentInChildren<Renderer>(true);
        if (ancienRendu != null && nouveauRendu != null)
        {
            Material[] anciens = ancienRendu.sharedMaterials;
            Material[] nouveaux = nouveauRendu.sharedMaterials;
            for (int i = 0; i < nouveaux.Length && i < anciens.Length; i++)
            {
                if (nouveaux[i] == null || anciens[i] == null || nouveaux[i] == anciens[i]) continue;
                importeur.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nouveaux[i].name), anciens[i]);
                change = true;
            }
        }

        if (change)
        {
            importeur.SaveAndReimport();
            rapport.Add("✔ Import du FBX réglé : blend shapes, animation, matériau de l'ancienne vent");
        }
    }

    /// <summary>L'animation du FBX (la plus longue s'il y en a plusieurs).</summary>
    static AnimationClip ClipDuFbx()
    {
        AnimationClip meilleur = null;
        foreach (Object objet in AssetDatabase.LoadAllAssetsAtPath(CheminFbx))
        {
            if (!(objet is AnimationClip clip) || clip.name.StartsWith("__preview__")) continue;
            if (meilleur == null || clip.length > meilleur.length) meilleur = clip;
        }
        return meilleur;
    }

    /// <summary>
    /// Copie toute l'animation du FBX (coups + ouverture) dans VentCoups.anim, en la mettant sur l'enfant « Grille ».
    /// </summary>
    static AnimationClip CopierAnimation(AnimationClip source, List<string> rapport)
    {
        AnimationClip animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(CheminAnimation);
        bool nouveau = animation == null;
        if (nouveau) animation = new AnimationClip();
        else animation.ClearCurves();

        animation.frameRate = source.frameRate;

        int coups = 0;
        bool bouge = false;
        foreach (EditorCurveBinding lien in AnimationUtility.GetCurveBindings(source))
        {
            AnimationCurve courbe = AnimationUtility.GetEditorCurve(source, lien);
            if (courbe == null) continue;

            EditorCurveBinding surLaGrille = lien;
            surLaGrille.path = string.IsNullOrEmpty(lien.path) ? NomGrille : NomGrille + "/" + lien.path;
            AnimationUtility.SetEditorCurve(animation, surLaGrille, courbe);

            if (lien.propertyName.StartsWith("blendShape.")) coups++;
            else if (lien.type == typeof(Transform) && !EstConstante(courbe)) bouge = true;
        }

        if (coups == 0 && !bouge)
        {
            rapport.Add("✘ L'animation du FBX ne fait rien : dans Blender, vérifier les clés, puis réexporter.");
            return null;
        }

        if (nouveau)
        {
            TetesLaPorte.CreerDossier(DossierAnimation);
            AssetDatabase.CreateAsset(animation, CheminAnimation);
        }
        else
        {
            EditorUtility.SetDirty(animation);
        }

        rapport.Add($"✔ Animation ({source.length:0.00} s) : {coups} coups (blend shapes)" +
                    (bouge ? " + l'ouverture (rotation)" : ", sans mouvement de la grille") + $" → {CheminAnimation}");
        return animation;
    }

    static bool EstConstante(AnimationCurve courbe)
    {
        for (int i = 1; i < courbe.length; i++)
        {
            if (Mathf.Abs(courbe.keys[i].value - courbe.keys[0].value) > 0.0001f) return false;
        }
        return true;
    }


    // ---------- 2. La place dans la scène ----------

    /// <summary>
    /// La grille (au repos) doit couvrir exactement l'ancienne vent. Si elle a la même taille mais est décalée,
    /// on déplace le conteneur (pas la grille, que l'animation contrôle). Si la taille ne correspond pas,
    /// c'est un réglage d'export différent (axes, échelle) : on le signale.
    /// </summary>
    static void Aligner(Transform conteneur, Transform grille, Transform ancienne, List<string> rapport)
    {
        Bounds a = BornesDuMaillage(ancienne);
        Bounds n = BornesDuMaillage(grille);
        Vector3 ecartTaille = a.size - n.size;

        if (Mathf.Max(Mathf.Abs(ecartTaille.x), Mathf.Abs(ecartTaille.y), Mathf.Abs(ecartTaille.z)) > 0.05f)
        {
            rapport.Add($"⚠ La nouvelle vent n'a pas la même taille que l'ancienne ({Taille(n.size)} au lieu de {Taille(a.size)}).\n" +
                        "   Vérifier les réglages Transform de l'export Blender (les mêmes que pour Couloir.fbx).");
            return;
        }

        Vector3 decalage = a.center - n.center;
        if (decalage.magnitude > 0.005f)
        {
            Undo.RecordObject(conteneur, "Brancher la vent animée");
            conteneur.position += decalage;
            rapport.Add($"✔ Nouvelle vent recentrée sur l'ancienne (décalage de {decalage.magnitude * 100f:0.0} cm)");
        }
        else
        {
            rapport.Add("✔ Nouvelle vent exactement à la place de l'ancienne");
        }
    }

    /// <summary>La boîte (dans le monde) du maillage au repos, même si l'objet est caché.</summary>
    static Bounds BornesDuMaillage(Transform objet)
    {
        Mesh maillage = null;
        Transform repere = objet;

        MeshFilter filtre = objet.GetComponentInChildren<MeshFilter>(true);
        SkinnedMeshRenderer peau = objet.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (filtre != null && filtre.sharedMesh != null) { maillage = filtre.sharedMesh; repere = filtre.transform; }
        else if (peau != null && peau.sharedMesh != null) { maillage = peau.sharedMesh; repere = peau.transform; }

        if (maillage == null) return new Bounds(objet.position, Vector3.zero);

        Bounds local = maillage.bounds;
        Bounds monde = new Bounds(repere.TransformPoint(local.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 coin = local.center + Vector3.Scale(local.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            monde.Encapsulate(repere.TransformPoint(coin));
        }
        return monde;
    }

    static string Taille(Vector3 taille)
    {
        return $"{taille.x:0.00} × {taille.y:0.00} × {taille.z:0.00} m";
    }

    static void Message(string texte)
    {
        EditorUtility.DisplayDialog("La Porte", texte, "OK");
    }
}
