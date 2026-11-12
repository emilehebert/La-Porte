using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using HauntedPSX.RenderPipelines.PSX.Editor;

/// <summary>
/// Menu « La Porte → Ajouter les placeholders de test ».
///
/// Ajoute des objets TEMPORAIRES (cubes, textes, bips) pour que ce qui manque encore
/// soit visible pendant les tests :
///   - Partie A ét. 4 : 3 cubes de couleur (les têtes) tombent de la ventilation du corridor,
///     un cube blanc (le journal) tombe de la bibliothèque sur la table. C'est une vraie Timeline
///     (Assets/Placeholders/Indices.playable) branchée sur l'étape « 2 - Indices ».
///   - Partie B ét. X : les pages du journal (texte) s'inversent avec l'interrupteur.
///   - Numpad : « ENTREZ LE CODE » dans la pièce, « CODE FAUX » si TouchDesigner dit que c'est mauvais.
///   - Porte ouverte : « PORTE OUVERTE » en vert sur les deux écrans.
///   - Sons : des bips générés par le code sur toutes les étapes.
///
/// Tout est rangé sous « Placeholders (à remplacer) ». Pour remplacer un placeholder :
/// mettre le vrai objet / son, puis supprimer le placeholder. Annulable avec Ctrl+Z.
/// </summary>
public static class PlaceholdersLaPorte
{
    const string NomRacine = "Placeholders (à remplacer)";
    const string Dossier = "Assets/Placeholders";

    // Hauteur d'une ligne de TextMesh (en mètres) pour fontSize = 1 et characterSize = 1.
    const float TailleLigneParPoint = 0.1f;

    [MenuItem("La Porte/Ajouter les placeholders de test")]
    static void Ajouter()
    {
        if (GameObject.Find(NomRacine) != null)
        {
            EditorUtility.DisplayDialog("La Porte", $"« {NomRacine} » existe déjà dans la scène. Rien n'a été changé.", "OK");
            return;
        }

        GestionnaireEtapes jeu = Object.FindFirstObjectByType<GestionnaireEtapes>(FindObjectsInactive.Include);
        Camera camPiece = CameraAfficheeSur(0);      // Display 1 : la pièce (visiteur)
        Camera camCorridor = CameraAfficheeSur(1);   // Display 2 : le corridor (locataire)
        if (jeu == null || camPiece == null || camCorridor == null)
        {
            EditorUtility.DisplayDialog("La Porte",
                "Il faut d'abord « La Porte → Installer dans la scène ouverte », et une caméra active sur Display 1 et sur Display 2.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Ajouter les placeholders");
        int groupeUndo = Undo.GetCurrentGroup();
        List<string> rapport = new List<string>();
        if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets", "Placeholders");

        GameObject racine = new GameObject(NomRacine);
        Undo.RegisterCreatedObjectUndo(racine, "Créer les placeholders");

        AjouterIndices(racine.transform, jeu, camPiece, camCorridor, rapport);
        AjouterAfficheurNumpad(racine.transform, camPiece, rapport);
        AjouterPorteOuverte(racine.transform, jeu, camPiece, camCorridor, rapport);
        AjouterSons(racine.transform, jeu, rapport);

        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(racine.scene);
        Selection.activeGameObject = racine;

        EditorUtility.DisplayDialog("La Porte — placeholders ajoutés",
            string.Join("\n", rapport) + "\n\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- Partie A ét. 4 (têtes + journal) et Partie B ét. X (pages) ----------

    static void AjouterIndices(Transform racine, GestionnaireEtapes jeu, Camera camPiece, Camera camCorridor, List<string> rapport)
    {
        // Un seul objet animé (Animator) : la Timeline déplace ses enfants.
        GameObject groupe = new GameObject("Indices - têtes et journal");
        groupe.transform.SetParent(racine, false);
        Animator animateur = groupe.AddComponent<Animator>();

        // --- Les têtes : tombent de la ventilation du corridor jusqu'au sol ---
        Bounds ventilation;
        if (!BornesDe("Vent_corridor", out ventilation))
        {
            Vector3 devant = camCorridor.transform.position + camCorridor.transform.forward * 5f;
            ventilation = new Bounds(new Vector3(devant.x, 2.4f, devant.z), Vector3.one * 0.5f);
            rapport.Add("⚠ « Vent_corridor » introuvable : les têtes tombent devant la caméra du corridor");
        }

        string[] nomsTetes = { "Tête ORANGE", "Tête CYAN", "Tête ROSE" };
        Color[] couleursTetes = { new Color(1f, 0.5f, 0f), Color.cyan, new Color(1f, 0.4f, 0.75f) };
        List<Chute> chutes = new List<Chute>();

        for (int i = 0; i < 3; i++)
        {
            GameObject tete = Cube(nomsTetes[i], groupe.transform, Vector3.one * 0.35f,
                Materiau("Tete " + (i + 1), couleursTetes[i]));
            Vector3 depart = new Vector3(ventilation.center.x + (i - 1) * 0.45f, ventilation.max.y + 0.4f, ventilation.center.z);
            Vector3 arrivee = new Vector3(depart.x, 0.175f, depart.z);
            tete.transform.position = depart;   // caché dans le plafond au départ
            chutes.Add(new Chute(tete.name, depart, arrivee, 0.2f + i * 0.6f, 0.7f));
        }

        // --- Le journal : tombe de la bibliothèque sur la table ---
        Bounds bibliotheque, table;
        bool trouveBiblio = BornesDe("Library", out bibliotheque);
        bool trouveTable = BornesDe("Table", out table);
        Vector3 devantPiece = camPiece.transform.position + camPiece.transform.forward * 2.5f;

        Vector3 departJournal = trouveBiblio
            ? new Vector3(bibliotheque.center.x, bibliotheque.center.y + bibliotheque.extents.y * 0.5f, bibliotheque.center.z)
            : devantPiece + Vector3.up * 1f;
        Vector3 arriveeJournal = trouveTable
            ? new Vector3(table.center.x, table.max.y + 0.03f, table.center.z)
            : new Vector3(devantPiece.x, 0.8f, devantPiece.z);
        if (!trouveBiblio || !trouveTable) rapport.Add("⚠ « Library » ou « Table » introuvable : le journal tombe devant la caméra de la pièce");

        GameObject journal = new GameObject("Journal");
        journal.transform.SetParent(groupe.transform, false);
        journal.transform.position = departJournal;
        Cube("Livre", journal.transform, new Vector3(0.4f, 0.05f, 0.3f), Materiau("Journal", Color.white));
        chutes.Add(new Chute(journal.name, departJournal, arriveeJournal, 0.4f, 1f));

        // Les pages (texte) : au-dessus du journal (assez haut pour ne pas passer devant la table blanche).
        // JournalPersonnages fait l'inversion avec l'interrupteur (Partie B ét. X).
        Vector3 positionPages = arriveeJournal + Vector3.up * 0.75f;
        GameObject pages = new GameObject("Pages (inversées par l'interrupteur)");
        pages.transform.SetParent(journal.transform, false);
        pages.transform.localPosition = positionPages - arriveeJournal;
        pages.transform.rotation = FaceA(camPiece);

        float lignePage = TailleALEcran(camPiece, positionPages, 0.04f);
        TextMesh pageAllumee = Texte("Version lampe allumée", pages.transform, lignePage, Color.yellow);
        TextMesh pageFermee = Texte("Version lampe fermée", pages.transform, lignePage, Color.yellow);

        JournalPersonnages personnages = pages.AddComponent<JournalPersonnages>();
        personnages.versionLampeAllumee = pageAllumee.gameObject;
        personnages.versionLampeFermee = pageFermee.gameObject;

        JournalPlaceholder placeholder = journal.AddComponent<JournalPlaceholder>();
        placeholder.pages = pages;
        placeholder.texteLampeAllumee = pageAllumee;
        placeholder.texteLampeFermee = pageFermee;
        placeholder.positionSurLaTable = arriveeJournal;
        pages.SetActive(false);   // JournalPlaceholder les montre une fois le journal sur la table

        // --- La Timeline, branchée sur l'étape « 2 - Indices » ---
        PlayableDirector directeur = groupe.AddComponent<PlayableDirector>();
        directeur.playOnAwake = false;
        directeur.extrapolationMode = DirectorWrapMode.Hold;
        directeur.playableAsset = CreerTimeline(chutes, directeur, animateur);

        EtapeTimeline etapeIndices = jeu.GetComponentInChildren<EtapeTimeline>(true);
        if (etapeIndices != null)
        {
            Undo.RecordObject(etapeIndices, "Brancher la Timeline");
            etapeIndices.timeline = directeur;
            rapport.Add($"✔ Têtes + journal : Timeline branchée sur « {etapeIndices.Nom} »");
        }
        else
        {
            rapport.Add("✘ Aucune EtapeTimeline : assigner « Indices - têtes et journal » à la main");
        }
        rapport.Add("✔ Pages du journal : s'inversent avec l'interrupteur (code découpé en ORANGE / CYAN / ROSE)");
    }

    /// <summary>Une chute : quel enfant, d'où à où, quand elle commence et combien de temps elle dure.</summary>
    struct Chute
    {
        public string chemin;
        public Vector3 depart, arrivee;
        public float debut, duree;

        public Chute(string chemin, Vector3 depart, Vector3 arrivee, float debut, float duree)
        {
            this.chemin = chemin;
            this.depart = depart;
            this.arrivee = arrivee;
            this.debut = debut;
            this.duree = duree;
        }
    }

    static TimelineAsset CreerTimeline(List<Chute> chutes, PlayableDirector directeur, Animator animateur)
    {
        // L'animation : chaque enfant reste en place, puis tombe (accélération comme la gravité).
        AnimationClip animation = new AnimationClip { name = "Chute des indices" };
        foreach (Chute chute in chutes)
        {
            float fin = chute.debut + chute.duree;
            string[] axes = { "x", "y", "z" };

            for (int axe = 0; axe < 3; axe++)
            {
                float a = chute.depart[axe], b = chute.arrivee[axe];
                float pente = axe == 1 ? 2f * (b - a) / chute.duree    // y : chute accélérée
                                       : (b - a) / chute.duree;         // x, z : glisse régulièrement
                float penteDepart = axe == 1 ? 0f : pente;

                AnimationCurve courbe = new AnimationCurve(
                    new Keyframe(0f, a, 0f, 0f),
                    new Keyframe(chute.debut, a, 0f, penteDepart),
                    new Keyframe(fin, b, pente, 0f));

                AnimationUtility.SetEditorCurve(animation,
                    EditorCurveBinding.FloatCurve(chute.chemin, typeof(Transform), "m_LocalPosition." + axes[axe]), courbe);
            }
        }
        string cheminAnimation = Dossier + "/Indices.anim";
        AssetDatabase.DeleteAsset(cheminAnimation);
        AssetDatabase.CreateAsset(animation, cheminAnimation);

        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        string cheminTimeline = Dossier + "/Indices.playable";
        AssetDatabase.DeleteAsset(cheminTimeline);
        AssetDatabase.CreateAsset(timeline, cheminTimeline);

        AnimationTrack piste = timeline.CreateTrack<AnimationTrack>(null, "Têtes et journal (placeholders)");
        piste.CreateClip(animation).displayName = "Chute";
        directeur.SetGenericBinding(piste, animateur);

        EditorUtility.SetDirty(timeline);
        return timeline;
    }


    // ---------- Numpad : « ENTREZ LE CODE » / « CODE FAUX » ----------

    static void AjouterAfficheurNumpad(Transform racine, Camera camPiece, List<string> rapport)
    {
        Vector3 position = DevantCamera(camPiece, 1.6f, -0.6f);   // en bas de l'écran
        TextMesh texte = Texte("Afficheur du numpad", racine, TailleALEcran(camPiece, position, 0.08f), Color.cyan);
        texte.transform.SetPositionAndRotation(position, FaceA(camPiece));

        AfficheurNumpadPlaceholder afficheur = texte.gameObject.AddComponent<AfficheurNumpadPlaceholder>();
        afficheur.texte = texte;
        rapport.Add("✔ Numpad : « ENTREZ LE CODE » et « CODE FAUX » affichés dans la pièce");
    }


    // ---------- Porte ouverte ----------

    static void AjouterPorteOuverte(Transform racine, GestionnaireEtapes jeu, Camera camPiece, Camera camCorridor, List<string> rapport)
    {
        GameObject[] messages =
        {
            MessagePorte("Porte ouverte (pièce)", racine, camPiece),
            MessagePorte("Porte ouverte (corridor)", racine, camCorridor),
        };

        EtapeFin fin = jeu.GetComponentInChildren<EtapeFin>(true);
        if (fin != null)
        {
            Undo.RecordObject(fin, "Brancher la porte ouverte");
            fin.objetsAAfficher = messages;
            rapport.Add("✔ Porte ouverte : « PORTE OUVERTE » en vert sur les deux écrans");
        }
        else
        {
            rapport.Add("✘ Aucune EtapeFin : la porte ouverte n'est pas branchée");
        }
    }

    static GameObject MessagePorte(string nom, Transform racine, Camera camera)
    {
        Vector3 position = DevantCamera(camera, 2f, 0f);   // au centre de l'écran
        TextMesh texte = Texte(nom, racine, TailleALEcran(camera, position, 0.15f), Color.green);
        texte.text = "PORTE OUVERTE";
        texte.transform.SetPositionAndRotation(position, FaceA(camera));
        texte.gameObject.SetActive(false);   // apparaît à l'étape « Porte ouverte »
        return texte.gameObject;
    }


    // ---------- Sons ----------

    static void AjouterSons(Transform racine, GestionnaireEtapes jeu, List<string> rapport)
    {
        GameObject sons = new GameObject("Sons (bips)");
        sons.transform.SetParent(racine, false);

        EtapeEnigmeCouleurs couleurs = jeu.GetComponentInChildren<EtapeEnigmeCouleurs>(true);
        EtapeCodeNumpad numpad = jeu.GetComponentInChildren<EtapeCodeNumpad>(true);
        EtapeFin fin = jeu.GetComponentInChildren<EtapeFin>(true);
        int branches = 0;

        // Seulement les sons pas encore assignés : un vrai son déjà en place n'est jamais remplacé.
        // (Pas de « ??= » : il ne voit pas les champs vides des objets Unity comme null.)
        if (couleurs != null)
        {
            Undo.RecordObject(couleurs, "Brancher les sons");
            if (couleurs.sonBonneCouleur == null)    couleurs.sonBonneCouleur    = Bip("Bonne couleur", sons.transform, 880f, 0.12f, 1, 1f, ref branches);
            if (couleurs.sonMauvaiseCouleur == null) couleurs.sonMauvaiseCouleur = Bip("Mauvaise couleur", sons.transform, 200f, 0.4f, 1, 1f, ref branches);
            if (couleurs.sonReussite == null)        couleurs.sonReussite        = Bip("Combinaison réussie", sons.transform, 660f, 0.12f, 3, 1.25f, ref branches);
        }
        if (numpad != null)
        {
            Undo.RecordObject(numpad, "Brancher les sons");
            if (numpad.sonMauvaisCode == null) numpad.sonMauvaisCode = Bip("Mauvais code", sons.transform, 180f, 0.25f, 2, 1f, ref branches);
            if (numpad.sonBonCode == null)     numpad.sonBonCode     = Bip("Bon code", sons.transform, 880f, 0.12f, 3, 1.25f, ref branches);
        }
        if (fin != null)
        {
            Undo.RecordObject(fin, "Brancher les sons");
            if (fin.sonFin == null) fin.sonFin = Bip("Porte ouverte", sons.transform, 523f, 0.25f, 4, 1.12f, ref branches);
        }

        rapport.Add($"✔ Sons : {branches} bips branchés (bonne / mauvaise couleur, numpad, porte)");
    }

    static AudioSource Bip(string nom, Transform parent, float frequence, float duree, int nombre, float montee, ref int compteur)
    {
        GameObject objet = new GameObject(nom);
        objet.transform.SetParent(parent, false);

        AudioSource source = objet.AddComponent<AudioSource>();
        source.playOnAwake = false;

        BipPlaceholder bip = objet.AddComponent<BipPlaceholder>();
        bip.frequence = frequence;
        bip.duree = duree;
        bip.nombreDeBips = nombre;
        bip.montee = montee;

        compteur++;
        return source;
    }


    // ---------- Outils ----------

    /// <summary>
    /// La caméra qu'on VOIT sur cet écran : s'il y en a plusieurs, c'est celle avec la plus grande « Depth »
    /// (elle est dessinée en dernier, par-dessus les autres).
    /// </summary>
    static Camera CameraAfficheeSur(int ecran)
    {
        Camera visible = null;
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!camera.enabled || camera.targetDisplay != ecran) continue;
            if (visible == null || camera.depth > visible.depth) visible = camera;
        }
        return visible;
    }

    /// <summary>
    /// tan(demi-angle vertical) de ce que la caméra montre vraiment.
    /// Lu dans la matrice de projection : marche aussi avec une caméra physique (« Gate Fit »).
    /// </summary>
    static float DemiHauteurParMetre(Camera camera)
    {
        return 1f / camera.projectionMatrix.m11;
    }

    /// <summary>Les bornes (dans le monde) de tous les Renderers de l'objet nommé ainsi.</summary>
    static bool BornesDe(string nom, out Bounds bornes)
    {
        bornes = default;
        bool trouve = false;

        foreach (Transform objet in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (objet.name != nom) continue;
            foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>())
            {
                if (!trouve) { bornes = rendu.bounds; trouve = true; }
                else bornes.Encapsulate(rendu.bounds);
            }
            if (trouve) return true;
        }
        return false;
    }

    /// <summary>
    /// Un point devant la caméra. « hauteurEcran » : 0 = centre de l'écran, -1 = bord du bas, 1 = bord du haut.
    /// (Les caméras du projet voient très large, ~135° : il faut en tenir compte.)
    /// </summary>
    static Vector3 DevantCamera(Camera camera, float distance, float hauteurEcran)
    {
        Transform t = camera.transform;
        float demiHauteurVisible = distance * DemiHauteurParMetre(camera);
        return t.position + t.forward * distance + t.up * (hauteurEcran * demiHauteurVisible);
    }

    /// <summary>La hauteur (en mètres) qui occupe cette fraction de l'écran, à cette position.</summary>
    static float TailleALEcran(Camera camera, Vector3 position, float fractionEcran)
    {
        float profondeur = Vector3.Dot(position - camera.transform.position, camera.transform.forward);
        return 2f * profondeur * DemiHauteurParMetre(camera) * fractionEcran;
    }

    /// <summary>
    /// La rotation qui rend un texte lisible depuis cette caméra : parallèle à l'écran.
    /// (Tourné vers la caméra, il serait très étiré sur les bords avec un objectif aussi large.)
    /// </summary>
    static Quaternion FaceA(Camera camera)
    {
        return camera.transform.rotation;
    }

    static GameObject Cube(string nom, Transform parent, Vector3 taille, Material materiau)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = nom;
        cube.transform.SetParent(parent, false);
        cube.transform.localScale = taille;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.GetComponent<MeshRenderer>().sharedMaterial = materiau;
        return cube;
    }

    /// <summary>
    /// Matériau PSX « Unlit » : couleur pleine, visible même dans le noir,
    /// et caché correctement derrière les murs (contrairement à Unlit/Color avec ce pipeline).
    /// </summary>
    static Material Materiau(string nom, Color couleur)
    {
        string chemin = $"{Dossier}/{nom}.mat";
        Material materiau = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (materiau == null)
        {
            materiau = new Material(Shader.Find("PSX/PSXLit"));
            AssetDatabase.CreateAsset(materiau, chemin);
        }

        materiau.SetColor("_MainColor", couleur);
        materiau.SetFloat("_LightingMode", (float)PSXMaterialUtils.LightingMode.Unlit);
        PSXMaterialUtils.SetMaterialKeywords(materiau);
        EditorUtility.SetDirty(materiau);
        return materiau;
    }

    /// <summary>
    /// Un texte 3D. Avec ce pipeline, il est dessiné PAR-DESSUS le décor :
    /// parfait pour des placeholders bien visibles.
    /// </summary>
    static TextMesh Texte(string nom, Transform parent, float hauteurLigne, Color couleur)
    {
        GameObject objet = new GameObject(nom);
        objet.transform.SetParent(parent, false);

        TextMesh texte = objet.AddComponent<TextMesh>();
        texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texte.fontSize = 64;
        texte.characterSize = hauteurLigne / (texte.fontSize * TailleLigneParPoint);
        texte.anchor = TextAnchor.MiddleCenter;
        texte.alignment = TextAlignment.Center;
        texte.color = couleur;

        MeshRenderer rendu = objet.GetComponent<MeshRenderer>();
        if (rendu == null) rendu = objet.AddComponent<MeshRenderer>();
        rendu.sharedMaterial = texte.font.material;
        return texte;
    }
}
