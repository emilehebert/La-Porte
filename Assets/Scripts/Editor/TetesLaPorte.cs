using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Menu « La Porte → Finaliser les têtes ».
///
/// Met les têtes dans leur version finale, avec la physique. On peut le refaire sans danger : il fait seulement ce qui manque.
///   1. Head_01 à Head_05 deviennent des prefabs (Assets/Prefabs/Tetes) et sont retirées de la scène.
///      Chaque prefab reçoit un Rigidbody et des MeshCollider (convexes).
///   2. « Cinématique des indices » porte la Timeline de l'étape 2 (le journal, encore un placeholder).
///      Son enfant « Têtes qui tombent » a 3 emplacements vides (les anciens cubes de couleur sont supprimés).
///      Les emplacements ne sont plus animés : c'est leur position dans la scène qui compte.
///   3. L'étape « 2 - Indices » lâche les têtes au début (Au Debut → Lacher)
///      et les met au bout de leur corde si on la saute (Etat Final → PoserAuSol).
///   4. Le Corridor reçoit des MeshCollider (sol et murs) : sinon les têtes tombent à travers.
///
/// Ensuite : Ctrl+S. Ne pas faire Ctrl+Z : les fichiers créés ou modifiés (prefabs, Timeline)
/// ne seraient pas annulés. En cas de problème, revenir en arrière avec git.
/// </summary>
public static class TetesLaPorte
{
    const string NomCinematique = "Cinématique des indices";
    const string NomTetes = "Têtes qui tombent";
    const string NomGroupePlaceholder = "Indices - têtes et journal";
    const string NomDecor = "Corridor";
    static readonly string[] NomsCubes = { "Tête ORANGE", "Tête CYAN", "Tête ROSE" };   // dans l'ordre de chute
    static readonly string[] Axes = { "x", "y", "z" };

    const string DossierPrefabs = "Assets/Prefabs/Tetes";
    const string DossierAnimation = "Assets/Animation/Indices";
    const float MasseTete = 4f;   // kg, à peu près une vraie tête

    [MenuItem("La Porte/Finaliser les têtes")]
    public static void Finaliser()
    {
        ReglagesJeu reglages = Object.FindFirstObjectByType<ReglagesJeu>(FindObjectsInactive.Include);
        if (reglages == null)
        {
            EditorUtility.DisplayDialog("La Porte", "Aucun ReglagesJeu dans la scène : faire d'abord « La Porte → Installer dans la scène ouverte ».", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Finaliser les têtes");
        int groupeUndo = Undo.GetCurrentGroup();
        List<string> rapport = new List<string>();

        // 1. Les prefabs, avec la physique
        BrancherPrefabs(reglages, rapport);
        foreach (Tete tete in reglages.tetes) AjouterPhysique(tete.modele, rapport);

        // 2. Les emplacements (la première fois : à partir des cubes de couleur)
        Transform tetes = TrouverDansLaScene(NomTetes);
        if (tetes == null) tetes = CreerCinematique(rapport);
        else RetirerAnimationDesTetes(tetes, rapport);

        if (tetes != null)
        {
            TetesQuiTombent tetesQuiTombent = tetes.GetComponent<TetesQuiTombent>();
            if (tetesQuiTombent == null) tetesQuiTombent = Undo.AddComponent<TetesQuiTombent>(tetes.gameObject);

            // 3. L'étape lâche les têtes
            BrancherEtape(tetes.GetComponentInParent<PlayableDirector>(), tetesQuiTombent, rapport);
            Selection.activeGameObject = tetes.gameObject;
        }

        // 4. Le sol
        AjouterCollidersDuDecor(rapport);

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(reglages.gameObject.scene);

        rapport.Add("");
        rapport.Add($"Pour choisir d'où tombent les têtes : déplacer les « Emplacement tête » (juste sous la ventilation).");
        rapport.Add("Sélectionner « Têtes qui tombent » pour les voir : en jaune, la corde et la tête au repos; en rouge, la tête toucherait le sol.");
        EditorUtility.DisplayDialog("La Porte — têtes finalisées", string.Join("\n", rapport) + "\n\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- 1. Les prefabs ----------

    /// <summary>Les têtes qui ne sont pas encore des prefabs le deviennent (et sont retirées de la scène).</summary>
    static void BrancherPrefabs(ReglagesJeu reglages, List<string> rapport)
    {
        CreerDossier(DossierPrefabs);
        Undo.RecordObject(reglages, "Finaliser les têtes");

        for (int i = 0; i < reglages.tetes.Length; i++)
        {
            Tete tete = reglages.tetes[i];
            if (tete.modele != null && EditorUtility.IsPersistent(tete.modele)) continue;   // déjà un prefab

            string nom = $"Head_{i + 1:00}";
            tete.modele = CreerPrefab(nom, rapport);
            if (tete.modele != null) rapport.Add($"✔ {nom} = {tete.description} ({tete.numero}) : prefab créé");
        }
        EditorUtility.SetDirty(reglages);
    }

    /// <summary>
    /// Crée le prefab de la tête à partir de celle de la scène (rotation, taille, matériaux), puis la retire de la scène.
    /// Si le prefab existe déjà, il est gardé tel quel.
    /// </summary>
    static GameObject CreerPrefab(string nom, List<string> rapport)
    {
        string chemin = $"{DossierPrefabs}/{nom}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
        Transform dansLaScene = TrouverDansLaScene(nom);

        if (dansLaScene != null)
        {
            if (prefab == null)
            {
                // Position 0 dans le prefab : c'est TetesQuiTombent qui place la tête.
                Undo.RecordObject(dansLaScene, "Finaliser les têtes");
                Vector3 position = dansLaScene.position;
                dansLaScene.position = Vector3.zero;

                prefab = PrefabUtility.SaveAsPrefabAsset(dansLaScene.gameObject, chemin, out bool reussi);
                if (!reussi)
                {
                    dansLaScene.position = position;
                    rapport.Add($"✘ {nom} : le prefab n'a pas pu être créé (la tête reste dans la scène)");
                    return null;
                }
            }
            Undo.DestroyObjectImmediate(dansLaScene.gameObject);
        }

        if (prefab == null) rapport.Add($"✘ « {nom} » introuvable, ni dans la scène, ni dans {DossierPrefabs}");
        return prefab;
    }

    /// <summary>Un Rigidbody sur le prefab, et un MeshCollider convexe sur chaque morceau (visage, cheveux, lunettes...).</summary>
    static void AjouterPhysique(GameObject prefab, List<string> rapport)
    {
        if (prefab == null) return;

        string chemin = AssetDatabase.GetAssetPath(prefab);
        GameObject contenu = PrefabUtility.LoadPrefabContents(chemin);
        bool change = false;

        if (contenu.GetComponent<Rigidbody>() == null)
        {
            Rigidbody corps = contenu.AddComponent<Rigidbody>();
            corps.mass = MasseTete;
            corps.interpolation = RigidbodyInterpolation.Interpolate;                    // mouvement fluide à l'écran
            corps.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;     // ne traverse pas le sol en tombant vite
            change = true;
        }

        int colliders = 0;
        foreach (MeshFilter maillage in contenu.GetComponentsInChildren<MeshFilter>(true))
        {
            if (AjouterCollider(maillage.gameObject, maillage.sharedMesh)) colliders++;
        }
        foreach (SkinnedMeshRenderer maillage in contenu.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (AjouterCollider(maillage.gameObject, maillage.sharedMesh)) colliders++;
        }
        change |= colliders > 0;

        if (change)
        {
            PrefabUtility.SaveAsPrefabAsset(contenu, chemin);
            rapport.Add($"✔ {prefab.name} : Rigidbody ({MasseTete} kg) + {colliders} collider(s)");
        }
        PrefabUtility.UnloadPrefabContents(contenu);
    }

    static bool AjouterCollider(GameObject objet, Mesh maillage)
    {
        if (maillage == null || objet.GetComponent<Collider>() != null) return false;

        MeshCollider collider = objet.AddComponent<MeshCollider>();
        collider.sharedMesh = maillage;
        collider.convex = true;   // obligatoire avec un Rigidbody
        return true;
    }


    // ---------- 2. Les emplacements ----------

    /// <summary>
    /// La première fois : « Cinématique des indices » (la Timeline) et « Têtes qui tombent » (3 emplacements vides
    /// à la place des cubes de couleur). Les cubes et l'ancien PlayableDirector sont supprimés.
    /// </summary>
    static Transform CreerCinematique(List<string> rapport)
    {
        Transform groupe = TrouverDansLaScene(NomGroupePlaceholder);
        PlayableDirector ancienDirecteur = groupe != null ? groupe.GetComponent<PlayableDirector>() : null;
        TimelineAsset timeline = ancienDirecteur != null ? ancienDirecteur.playableAsset as TimelineAsset : null;
        if (timeline == null)
        {
            rapport.Add($"✘ Pas de Timeline sur « {NomGroupePlaceholder} » : les emplacements n'ont pas été créés");
            return null;
        }
        AnimationTrack pisteJournal = TrouverPisteDesCubes(timeline, out AnimationClip animationCubes);

        GameObject cinematique = new GameObject(NomCinematique);
        GameObject tetes = new GameObject(NomTetes);
        tetes.transform.SetParent(cinematique.transform, false);

        // Les emplacements : au-dessus des cubes, juste sous la ventilation.
        bool ventilationTrouvee = BornesDe("Vent_corridor", out Bounds ventilation);
        Transform[] emplacements = new Transform[NomsCubes.Length];
        for (int i = 0; i < NomsCubes.Length; i++)
        {
            Transform emplacement = new GameObject($"Emplacement tête {i + 1}").transform;
            emplacement.SetParent(tetes.transform, false);

            Transform cube = groupe.Find(NomsCubes[i]);
            Vector3 position = cube != null ? cube.position : groupe.position;
            if (ventilationTrouvee) position.y = ventilation.min.y - 0.3f;
            emplacement.position = position;
            emplacements[i] = emplacement;

            if (animationCubes != null) RetirerCourbesDePosition(animationCubes, NomsCubes[i]);
        }

        TetesQuiTombent tetesQuiTombent = tetes.AddComponent<TetesQuiTombent>();
        tetesQuiTombent.emplacements = emplacements;

        if (pisteJournal != null)
        {
            pisteJournal.name = "Journal (placeholder)";
            EditorUtility.SetDirty(animationCubes);   // n'anime plus que le journal
        }

        // La Timeline sort du dossier des placeholders.
        string cheminTimeline = AssetDatabase.GetAssetPath(timeline);
        if (cheminTimeline.StartsWith("Assets/Placeholders/"))
        {
            CreerDossier(DossierAnimation);
            string erreur = AssetDatabase.MoveAsset(cheminTimeline, AssetDatabase.GenerateUniqueAssetPath($"{DossierAnimation}/Indices.playable"));
            if (!string.IsNullOrEmpty(erreur)) rapport.Add($"⚠ La Timeline n'a pas pu être déplacée : {erreur}");
        }
        EditorUtility.SetDirty(timeline);

        PlayableDirector directeur = cinematique.AddComponent<PlayableDirector>();
        directeur.playOnAwake = false;
        directeur.extrapolationMode = DirectorWrapMode.Hold;
        directeur.playableAsset = timeline;
        foreach (TrackAsset piste in timeline.GetOutputTracks())
        {
            directeur.SetGenericBinding(piste, ancienDirecteur.GetGenericBinding(piste));
        }

        Undo.RegisterCreatedObjectUndo(cinematique, "Finaliser les têtes");
        rapport.Add($"✔ « {NomCinematique} » : la Timeline, et 3 emplacements vides sous la ventilation");

        foreach (EtapeTimeline etape in Object.FindObjectsByType<EtapeTimeline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (etape.timeline != ancienDirecteur) continue;
            Undo.RecordObject(etape, "Finaliser les têtes");
            etape.timeline = directeur;
        }

        // Ménage : les cubes et l'ancien PlayableDirector.
        foreach (string nom in NomsCubes)
        {
            Transform cube = groupe.Find(nom);
            if (cube != null) Undo.DestroyObjectImmediate(cube.gameObject);
        }
        TetesQuiTombent ancienScript = groupe.GetComponent<TetesQuiTombent>();
        if (ancienScript != null) Undo.DestroyObjectImmediate(ancienScript);
        Undo.DestroyObjectImmediate(ancienDirecteur);
        rapport.Add("✔ Les 3 cubes de couleur sont supprimés");

        return tetes.transform;
    }

    /// <summary>
    /// Version d'avant : la Timeline animait les emplacements (ils revenaient toujours à la même place).
    /// On retire cette piste, son animation et l'Animator : c'est maintenant la physique qui fait tomber les têtes.
    /// </summary>
    static void RetirerAnimationDesTetes(Transform tetes, List<string> rapport)
    {
        Animator animateur = tetes.GetComponent<Animator>();
        if (animateur == null) return;   // déjà fait

        foreach (PlayableDirector directeur in Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(directeur.playableAsset is TimelineAsset timeline)) continue;

            List<TrackAsset> aRetirer = new List<TrackAsset>();
            foreach (TrackAsset piste in timeline.GetOutputTracks())
            {
                if (directeur.GetGenericBinding(piste) == animateur) aRetirer.Add(piste);
            }

            foreach (TrackAsset piste in aRetirer)
            {
                string nomPiste = piste.name;   // la piste n'existe plus après DeleteTrack
                List<string> animations = new List<string>();
                foreach (TimelineClip clip in piste.GetClips())
                {
                    if (clip.animationClip != null) animations.Add(AssetDatabase.GetAssetPath(clip.animationClip));
                }

                Undo.RecordObject(directeur, "Finaliser les têtes");
                directeur.ClearGenericBinding(piste);
                timeline.DeleteTrack(piste);
                EditorUtility.SetDirty(timeline);

                foreach (string chemin in animations)
                {
                    if (chemin.StartsWith(DossierAnimation + "/")) AssetDatabase.MoveAssetToTrash(chemin);
                }
                rapport.Add($"✔ Piste « {nomPiste} » retirée de la Timeline : la physique remplace l'animation");
            }
        }

        Undo.DestroyObjectImmediate(animateur);
        rapport.Add("✔ Les emplacements gardent la place où tu les as mis dans la scène");
    }

    /// <summary>La piste de la Timeline dont l'animation fait tomber les cubes de couleur (elle anime aussi le journal).</summary>
    static AnimationTrack TrouverPisteDesCubes(TimelineAsset timeline, out AnimationClip animation)
    {
        foreach (TrackAsset piste in timeline.GetOutputTracks())
        {
            if (!(piste is AnimationTrack pisteAnimation)) continue;

            foreach (TimelineClip clip in piste.GetClips())
            {
                if (clip.animationClip == null) continue;
                foreach (EditorCurveBinding courbe in AnimationUtility.GetCurveBindings(clip.animationClip))
                {
                    if (courbe.path != NomsCubes[0]) continue;
                    animation = clip.animationClip;
                    return pisteAnimation;
                }
            }
        }
        animation = null;
        return null;
    }

    static void RetirerCourbesDePosition(AnimationClip animation, string chemin)
    {
        foreach (string axe in Axes)
        {
            AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.FloatCurve(chemin, typeof(Transform), "m_LocalPosition." + axe), null);
        }
    }


    // ---------- 3. L'étape ----------

    /// <summary>L'étape qui joue la Timeline des indices appelle Lacher au début, et PoserAuSol si on la saute.</summary>
    static void BrancherEtape(PlayableDirector directeur, TetesQuiTombent tetesQuiTombent, List<string> rapport)
    {
        EtapeTimeline etape = null;
        foreach (EtapeTimeline candidate in Object.FindObjectsByType<EtapeTimeline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (directeur != null && candidate.timeline == directeur) etape = candidate;
        }
        if (etape == null)
        {
            rapport.Add("✘ Aucune étape ne joue la Timeline des indices : sur l'étape, brancher Au Debut → Lacher et Etat Final → PoserAuSol");
            return;
        }

        Undo.RecordObject(etape, "Finaliser les têtes");
        if (!APourAppel(etape.auDebut, tetesQuiTombent, nameof(TetesQuiTombent.Lacher)))
        {
            UnityEventTools.AddPersistentListener(etape.auDebut, tetesQuiTombent.Lacher);
        }
        if (!APourAppel(etape.etatFinal, tetesQuiTombent, nameof(TetesQuiTombent.PoserAuSol)))
        {
            UnityEventTools.AddPersistentListener(etape.etatFinal, tetesQuiTombent.PoserAuSol);
        }
        EditorUtility.SetDirty(etape);
        rapport.Add($"✔ « {etape.Nom} » lâche les têtes au début, et les met au bout de leur corde si on saute l'étape");
    }

    static bool APourAppel(UnityEventBase evenement, Object cible, string methode)
    {
        for (int i = 0; i < evenement.GetPersistentEventCount(); i++)
        {
            if (evenement.GetPersistentTarget(i) == cible && evenement.GetPersistentMethodName(i) == methode) return true;
        }
        return false;
    }


    // ---------- 4. Le sol ----------

    /// <summary>Un MeshCollider (immobile) sur chaque morceau du Corridor : le sol et les murs arrêtent les têtes.</summary>
    static void AjouterCollidersDuDecor(List<string> rapport)
    {
        Transform decor = TrouverDansLaScene(NomDecor);
        if (decor == null)
        {
            rapport.Add($"✘ « {NomDecor} » introuvable : mettre un collider sur le sol, sinon les têtes tombent à travers");
            return;
        }

        int ajoutes = 0;
        foreach (MeshFilter maillage in decor.GetComponentsInChildren<MeshFilter>(true))
        {
            if (maillage.sharedMesh == null || maillage.GetComponent<Collider>() != null) continue;

            MeshCollider collider = Undo.AddComponent<MeshCollider>(maillage.gameObject);
            collider.sharedMesh = maillage.sharedMesh;
            ajoutes++;
        }
        rapport.Add(ajoutes > 0 ? $"✔ {NomDecor} : {ajoutes} MeshCollider ajouté(s) (sol et murs)" : $"✔ {NomDecor} : a déjà ses colliders");
    }


    // ---------- Outils ----------

    /// <summary>
    /// L'objet de ce nom le plus haut dans la Hierarchy (même désactivé).
    /// Ex. : « Head_04 » contient aussi un enfant « Head_04 » (le maillage) : on veut le parent.
    /// </summary>
    internal static Transform TrouverDansLaScene(string nom)
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

    /// <summary>Les bornes (dans le monde) de tous les Renderers de l'objet nommé ainsi.</summary>
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

    internal static void CreerDossier(string chemin)
    {
        if (AssetDatabase.IsValidFolder(chemin)) return;

        string parent = Path.GetDirectoryName(chemin).Replace('\\', '/');
        CreerDossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(chemin));
    }
}
