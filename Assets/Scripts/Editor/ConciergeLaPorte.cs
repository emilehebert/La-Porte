using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu « La Porte → Brancher le concierge (fin) ».
///
/// Le concierge de l'étape « 4 - Porte ouverte » : il entre par la droite, attrape la clé qui pend
/// de la ventilation, la glisse dans la fente à courrier de la porte, puis repart par la gauche.
///
///   Concierge (fin)
///   ├── Concierge              le FBX Mixamo (Animator + ConciergeFin), caché au départ
///   ├── Clé du concierge       le modèle « Key » du couloir (CleSurCorde), cachée au départ
///   ├── Cle_Attache            où la corde de la clé est attachée
///   └── Trajet                 Depart, DevantCle, DevantFente, Sortie
///
/// Ce menu :
///   1. règle l'import des 4 FBX de Assets/Modeles/Concierge (Humanoid, même avatar, marche et attente en boucle);
///   2. crée Assets/Materials/Concierge.mat (PSXLit, texture en Point) et Assets/Animation/Concierge/Concierge.controller;
///   3. crée les objets dans la scène (s'ils n'y sont pas déjà : les repères déplacés à la main ne bougent plus);
///   4. ajoute le concierge et la clé aux « Objets à afficher » de l'étape Fin.
///
/// Ensuite : placer les repères (flèche bleue de DevantCle / DevantFente vers la clé / la porte), puis Ctrl+S.
/// </summary>
public static class ConciergeLaPorte
{
    const string Dossier = "Assets/Modeles/Concierge";
    const string FbxMarche = Dossier + "/Concierge_Walk.fbx";
    const string FbxAttente = Dossier + "/Concierge_Idle.fbx";
    const string FbxPrendre = Dossier + "/Concierge_Prendre.fbx";
    const string FbxDeposer = Dossier + "/Concierge_Deposer.fbx";

    const string CheminTexture = "Assets/Textures/Concierge/Concierge.png";
    const string CheminMateriau = "Assets/Materials/Concierge.mat";
    const string CheminMateriauCle = "Assets/Materials/Key.mat";
    const string DossierAnimation = "Assets/Animation/Concierge";
    const string CheminControleur = DossierAnimation + "/Concierge.controller";
    const string CheminCouloir = "Assets/Model/Couloir.fbx";

    const string NomGroupe = "Concierge (fin)";
    const string NomConcierge = "Concierge";
    const string NomCle = "Clé du concierge";
    const string NomAttache = "Cle_Attache";
    const string NomTrajet = "Trajet";

    [MenuItem("La Porte/Brancher le concierge (fin)")]
    public static void Brancher()
    {
        string[] fbx = { FbxMarche, FbxAttente, FbxPrendre, FbxDeposer };
        string manquant = fbx.FirstOrDefault(c => AssetDatabase.LoadAssetAtPath<GameObject>(c) == null);
        if (manquant != null)
        {
            Message($"« {manquant} » introuvable.\n\nLes 4 FBX de Mixamo doivent être dans {Dossier} :\n" +
                    "Concierge_Walk (avec skin), Concierge_Idle, Concierge_Prendre, Concierge_Deposer.");
            return;
        }

        List<string> rapport = new List<string>();

        // --- 1. Import ---
        Avatar avatar = RegleImport(rapport);
        if (avatar == null || !avatar.isHuman)
        {
            Message("L'avatar Humanoid n'a pas pu être créé à partir de Concierge_Walk.fbx.\n" +
                    "Sélectionner le FBX → Rig → Configure... pour voir quel os manque.");
            return;
        }
        AnimationClip marche = Clip(FbxMarche), attente = Clip(FbxAttente), prendre = Clip(FbxPrendre), deposer = Clip(FbxDeposer);

        // --- 2. Matériau et Animator Controller ---
        Material materiau = CreerMateriau(rapport);
        AnimatorController controleur = CreerControleur(marche, attente, prendre, deposer, rapport);

        // --- 3. La scène ---
        Undo.SetCurrentGroupName("Brancher le concierge");
        int groupeUndo = Undo.GetCurrentGroup();

        Transform groupe = TetesLaPorte.TrouverDansLaScene(NomGroupe);
        if (groupe == null)
        {
            GameObject nouveau = new GameObject(NomGroupe);
            Undo.RegisterCreatedObjectUndo(nouveau, "Brancher le concierge");
            groupe = nouveau.transform;
            rapport.Add($"✔ « {NomGroupe} » ajouté à la scène");
        }

        Reperes reperes = CalculerReperes(rapport);
        Transform attache = Enfant(groupe, NomAttache, reperes.attache, Quaternion.identity);
        Transform trajet = Enfant(groupe, NomTrajet, Vector3.zero, Quaternion.identity);
        Transform depart = Enfant(trajet, "Depart", reperes.depart, Quaternion.LookRotation(-reperes.droite));
        Transform devantCle = Enfant(trajet, "DevantCle", reperes.devantCle, Quaternion.LookRotation(reperes.versCle));
        Transform devantFente = Enfant(trajet, "DevantFente", reperes.devantFente, Quaternion.LookRotation(reperes.versPorte));
        Transform sortie = Enfant(trajet, "Sortie", reperes.sortie, Quaternion.LookRotation(-reperes.droite));

        CleSurCorde cle = CreerCle(groupe, attache, reperes, rapport);
        ConciergeFin concierge = CreerConcierge(groupe, avatar, controleur, materiau, rapport);
        if (concierge == null)
        {
            Undo.CollapseUndoOperations(groupeUndo);
            Message(string.Join("\n", rapport));
            return;
        }

        Undo.RecordObject(concierge, "Brancher le concierge");
        concierge.depart = depart;
        concierge.devantCle = devantCle;
        concierge.devantFente = devantFente;
        concierge.sortie = sortie;
        concierge.cle = cle;
        if (prendre != null)
        {
            concierge.dureePrendre = prendre.length;
            if (Mathf.Approximately(concierge.momentMainSurCle, 0.9f)) concierge.momentMainSurCle = prendre.length * 0.45f;
        }
        if (deposer != null)
        {
            concierge.dureeDeposer = deposer.length;
            if (Mathf.Approximately(concierge.momentMainDansFente, 1f)) concierge.momentMainDansFente = deposer.length * 0.45f;
        }

        // --- 4. L'étape Fin les montre ---
        BrancherEtapeFin(concierge.gameObject, cle.gameObject, rapport);

        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(groupe.gameObject.scene);
        Selection.activeGameObject = trajet.gameObject;

        rapport.Add("");
        rapport.Add("À faire : placer Depart / DevantCle / DevantFente / Sortie (dans « Trajet ») et Cle_Attache,");
        rapport.Add("flèche bleue de DevantCle vers la clé et de DevantFente vers la porte. Puis Ctrl+S.");
        Message(string.Join("\n", rapport));
    }

    // ---------------------------------------------------------------- Import

    static Avatar RegleImport(List<string> rapport)
    {
        ModelImporter importMarche = (ModelImporter)AssetImporter.GetAtPath(FbxMarche);
        importMarche.animationType = ModelImporterAnimationType.Human;
        importMarche.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importMarche.materialImportMode = ModelImporterMaterialImportMode.None;   // on met Concierge.mat nous-mêmes
        RegleClip(importMarche, "Marche", true);
        importMarche.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(FbxMarche).OfType<Avatar>().FirstOrDefault();
        if (avatar == null) return null;

        foreach ((string chemin, string nom, bool boucle) in new[]
                 { (FbxAttente, "Attente", true), (FbxPrendre, "Prendre", false), (FbxDeposer, "Deposer", false) })
        {
            ModelImporter import = (ModelImporter)AssetImporter.GetAtPath(chemin);
            import.animationType = ModelImporterAnimationType.Human;
            import.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            import.sourceAvatar = avatar;
            import.materialImportMode = ModelImporterMaterialImportMode.None;
            RegleClip(import, nom, boucle);
            import.SaveAndReimport();
        }
        rapport.Add("✔ Import des 4 FBX : Humanoid, même avatar, marche et attente en boucle");
        return avatar;
    }

    /// <summary>Un seul clip par FBX, renommé; le déplacement et la rotation restent dans la pose (le script fait avancer).</summary>
    static void RegleClip(ModelImporter import, string nom, bool boucle)
    {
        import.importAnimation = true;
        ModelImporterClipAnimation[] clips = import.clipAnimations.Length > 0 ? import.clipAnimations : import.defaultClipAnimations;
        if (clips.Length == 0) return;

        ModelImporterClipAnimation clip = clips[0];
        clip.name = nom;
        clip.loopTime = boucle;
        clip.loopPose = boucle;
        clip.lockRootRotation = true;
        clip.lockRootHeightY = true;
        clip.lockRootPositionXZ = true;
        clip.keepOriginalOrientation = true;
        clip.keepOriginalPositionY = true;
        clip.keepOriginalPositionXZ = true;
        import.clipAnimations = new[] { clip };
    }

    static AnimationClip Clip(string chemin)
    {
        return AssetDatabase.LoadAllAssetsAtPath(chemin).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    // ---------------------------------------------------------------- Matériau et Animator

    static Material CreerMateriau(List<string> rapport)
    {
        TextureImporter importTexture = AssetImporter.GetAtPath(CheminTexture) as TextureImporter;
        if (importTexture != null)
        {
            importTexture.filterMode = FilterMode.Point;
            importTexture.mipmapEnabled = false;
            importTexture.maxTextureSize = 512;
            importTexture.textureCompression = TextureImporterCompression.Uncompressed;
            importTexture.SaveAndReimport();
        }
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CheminTexture);

        Material materiau = AssetDatabase.LoadAssetAtPath<Material>(CheminMateriau);
        if (materiau == null)
        {
            Material modele = AssetDatabase.LoadAssetAtPath<Material>(CheminMateriauCle);   // même shader PSX que le décor
            materiau = modele != null ? new Material(modele) : new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(materiau, CheminMateriau);
            rapport.Add($"✔ {CheminMateriau} créé");
        }
        if (texture != null)
        {
            materiau.mainTexture = texture;
            if (materiau.HasProperty("_MainTex")) materiau.SetTexture("_MainTex", texture);
            if (materiau.HasProperty("_BaseColorMap")) materiau.SetTexture("_BaseColorMap", texture);
            EditorUtility.SetDirty(materiau);
        }
        else
        {
            rapport.Add($"✘ {CheminTexture} introuvable : le concierge n'aura pas de texture");
        }
        return materiau;
    }

    static AnimatorController CreerControleur(AnimationClip marche, AnimationClip attente, AnimationClip prendre,
                                              AnimationClip deposer, List<string> rapport)
    {
        TetesLaPorte.CreerDossier(DossierAnimation);
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CheminControleur) != null) AssetDatabase.DeleteAsset(CheminControleur);

        AnimatorController controleur = AnimatorController.CreateAnimatorControllerAtPath(CheminControleur);
        controleur.AddParameter("Vitesse", AnimatorControllerParameterType.Float);
        controleur.AddParameter("Prendre", AnimatorControllerParameterType.Trigger);
        controleur.AddParameter("Deposer", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controleur.layers[0].stateMachine;

        AnimatorState etatMarche = controleur.CreateBlendTreeInController("Attente / Marche", out BlendTree arbre, 0);
        arbre.blendParameter = "Vitesse";
        arbre.useAutomaticThresholds = false;
        arbre.AddChild(attente, 0f);
        arbre.AddChild(marche, 1f);
        machine.defaultState = etatMarche;

        AnimatorState etatPrendre = machine.AddState("Prendre");
        etatPrendre.motion = prendre;
        AnimatorState etatDeposer = machine.AddState("Deposer");
        etatDeposer.motion = deposer;

        foreach ((AnimatorState etat, string declencheur) in new[] { (etatPrendre, "Prendre"), (etatDeposer, "Deposer") })
        {
            AnimatorStateTransition aller = etatMarche.AddTransition(etat);
            aller.AddCondition(AnimatorConditionMode.If, 0f, declencheur);
            aller.hasExitTime = false;
            aller.duration = 0.2f;

            AnimatorStateTransition retour = etat.AddTransition(etatMarche);
            retour.hasExitTime = true;
            retour.exitTime = 0.9f;
            retour.duration = 0.2f;
        }

        AssetDatabase.SaveAssets();
        rapport.Add($"✔ {CheminControleur} : Vitesse (0 attente → 1 marche), triggers Prendre et Deposer");
        return controleur;
    }

    // ---------------------------------------------------------------- Scène

    struct Reperes
    {
        public Vector3 attache, depart, devantCle, devantFente, sortie;
        public Vector3 droite, versCle, versPorte;
        public float longueurCorde;
    }

    /// <summary>Des positions de départ raisonnables, à partir de la caméra du corridor, des têtes et de la porte.</summary>
    static Reperes CalculerReperes(List<string> rapport)
    {
        Reperes r = new Reperes();

        Transform camera = TetesLaPorte.TrouverDansLaScene("Corridor Camera");
        Vector3 versCamera = camera != null ? Vector3.ProjectOnPlane(-camera.forward, Vector3.up).normalized : Vector3.back;
        if (versCamera.sqrMagnitude < 0.01f) versCamera = Vector3.back;
        r.droite = camera != null ? Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized : Vector3.right;

        // La porte (et la hauteur du sol)
        bool porteTrouvee = BornesDe("Door", out Bounds porte);
        float sol = porteTrouvee ? porte.min.y : 0f;
        if (!porteTrouvee) rapport.Add("✘ Objet « Door » introuvable : DevantFente est à l'origine, à placer à la main");

        // La clé : à droite de la tête la plus à droite (vue de la caméra du corridor)
        TetesQuiTombent tetes = Object.FindFirstObjectByType<TetesQuiTombent>(FindObjectsInactive.Include);
        Transform plusADroite = tetes != null
            ? tetes.emplacements.Where(e => e != null).OrderByDescending(e => Vector3.Dot(e.position, r.droite)).FirstOrDefault()
            : null;
        r.attache = plusADroite != null ? plusADroite.position + r.droite * 0.45f : porte.center + r.droite * 2f + Vector3.up * 1.5f;
        if (plusADroite == null) rapport.Add("✘ Pas de têtes dans la scène : Cle_Attache est à placer à la main");

        float hauteurMain = sol + 1.5f;   // main tendue d'un homme de 1,75 m
        r.longueurCorde = Mathf.Max(0.3f, r.attache.y - hauteurMain);

        Vector3 cleAuSol = new Vector3(r.attache.x, sol, r.attache.z);
        r.devantCle = cleAuSol + versCamera * 0.45f;
        r.versCle = -versCamera;

        Vector3 porteAuSol = new Vector3(porte.center.x, sol, porte.center.z);
        r.devantFente = porteAuSol + versCamera * 0.6f;
        r.versPorte = -versCamera;

        r.depart = r.devantCle + r.droite * 3.5f;
        r.sortie = r.devantFente - r.droite * 3.5f;
        return r;
    }

    /// <summary>Trouve l'enfant par son nom, ou le crée à cette position (un repère déjà là n'est pas déplacé).</summary>
    static Transform Enfant(Transform parent, string nom, Vector3 position, Quaternion rotation)
    {
        Transform enfant = parent.Find(nom);
        if (enfant != null) return enfant;

        GameObject objet = new GameObject(nom);
        Undo.RegisterCreatedObjectUndo(objet, "Brancher le concierge");
        objet.transform.SetParent(parent, false);
        objet.transform.SetPositionAndRotation(position, rotation);
        return objet.transform;
    }

    static CleSurCorde CreerCle(Transform groupe, Transform attache, Reperes reperes, List<string> rapport)
    {
        Transform existante = groupe.Find(NomCle);
        if (existante != null && existante.TryGetComponent(out CleSurCorde dejaLa)) return dejaLa;

        GameObject objet = new GameObject(NomCle);
        Undo.RegisterCreatedObjectUndo(objet, "Brancher le concierge");
        objet.transform.SetParent(groupe, false);
        objet.transform.position = attache.position + Vector3.down * reperes.longueurCorde;

        // Le modèle « Key » du couloir, avec son matériau
        Mesh maillage = AssetDatabase.LoadAllAssetsAtPath(CheminCouloir).OfType<Mesh>().FirstOrDefault(m => m.name == "Key");
        Transform cleDuDecor = TetesLaPorte.TrouverDansLaScene("Key");
        if (maillage != null)
        {
            objet.AddComponent<MeshFilter>().sharedMesh = maillage;
            objet.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(CheminMateriauCle);
            if (cleDuDecor != null) objet.transform.localScale = cleDuDecor.lossyScale;
        }
        else
        {
            rapport.Add("✘ Modèle « Key » introuvable dans Couloir.fbx : la clé est un objet vide, y mettre un modèle");
        }

        CleSurCorde cle = objet.AddComponent<CleSurCorde>();
        cle.attache = attache;
        cle.longueurCorde = reperes.longueurCorde;
        objet.SetActive(false);   // l'étape Fin la montre : elle tombe à ce moment-là
        rapport.Add($"✔ « {NomCle} » créée (cachée), corde de {reperes.longueurCorde:0.00} m");
        return cle;
    }

    static ConciergeFin CreerConcierge(Transform groupe, Avatar avatar, AnimatorController controleur, Material materiau,
                                       List<string> rapport)
    {
        Transform existant = groupe.Find(NomConcierge);
        GameObject objet;
        if (existant != null)
        {
            objet = existant.gameObject;
        }
        else
        {
            GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(FbxMarche);
            objet = (GameObject)PrefabUtility.InstantiatePrefab(modele, groupe.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(objet, "Brancher le concierge");
            objet.name = NomConcierge;
            objet.transform.SetParent(groupe, false);
            rapport.Add($"✔ « {NomConcierge} » ajouté (caché jusqu'à l'étape Fin)");
        }

        Animator animateur = objet.GetComponent<Animator>();
        if (animateur == null) animateur = Undo.AddComponent<Animator>(objet);
        Undo.RecordObject(animateur, "Brancher le concierge");
        animateur.avatar = avatar;
        animateur.runtimeAnimatorController = controleur;
        animateur.applyRootMotion = false;
        animateur.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(rendu, "Brancher le concierge");
            rendu.sharedMaterials = Enumerable.Repeat(materiau, rendu.sharedMaterials.Length).ToArray();
        }

        ConciergeFin concierge = objet.GetComponent<ConciergeFin>();
        if (concierge == null) concierge = Undo.AddComponent<ConciergeFin>(objet);

        Undo.RecordObject(objet, "Brancher le concierge");
        objet.SetActive(false);
        return concierge;
    }

    static void BrancherEtapeFin(GameObject concierge, GameObject cle, List<string> rapport)
    {
        EtapeFin fin = Object.FindFirstObjectByType<EtapeFin>(FindObjectsInactive.Include);
        if (fin == null)
        {
            rapport.Add("✘ Pas d'étape Fin dans la scène : ajouter le concierge et la clé à ses « Objets à afficher » à la main");
            return;
        }

        List<GameObject> objets = new List<GameObject>(fin.objetsAAfficher ?? new GameObject[0]);
        int avant = objets.Count;
        if (!objets.Contains(cle)) objets.Add(cle);
        if (!objets.Contains(concierge)) objets.Add(concierge);
        if (objets.Count == avant) return;

        Undo.RecordObject(fin, "Brancher le concierge");
        fin.objetsAAfficher = objets.ToArray();
        EditorUtility.SetDirty(fin);
        rapport.Add($"✔ L'étape « {fin.name} » montre la clé puis le concierge");
    }

    static bool BornesDe(string nom, out Bounds bornes)
    {
        bornes = new Bounds(Vector3.zero, Vector3.one);
        Transform objet = TetesLaPorte.TrouverDansLaScene(nom);
        if (objet == null) return false;

        bool trouve = false;
        foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>(true))
        {
            if (!trouve) { bornes = rendu.bounds; trouve = true; }
            else bornes.Encapsulate(rendu.bounds);
        }
        return trouve;
    }

    static void Message(string texte)
    {
        EditorUtility.DisplayDialog("La Porte", texte, "OK");
    }
}
