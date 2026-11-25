using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu « La Porte → Brancher les sons ».
///
/// Trouve les sons de Gabriel PAR LEUR NOM DE FICHIER, n'importe où dans Assets/Audio
/// (ses sous-dossiers peuvent rester comme ils sont), et les branche :
///
///   CHAMBRE (gauche)                              CORRIDOR (droite)
///   bonne-couleur, mauvaise-couleur,              vent-tete-tombe (avec l'animation de la vent)
///   bonne-combinaison, mauvaise-combinaison       lum-corridor-eteint, lum-corridor-allume
///   spark, lamp-moving, lamp-on, lamp-off         ambiance-corridor (en boucle)
///   journal-tombe, mauvais-code
///   ambiance-appart (en boucle)                   LES DEUX : bon-code, porte-ouverte
///
/// Crée l'objet « Sons » (ReglagesSons + SonsInteractions) avec un enfant par pièce.
/// Un son sans fichier garde son bip placeholder (s'il en a un). Un son branché à la main ailleurs n'est pas touché.
/// Règle aussi l'import des fichiers : mono (une enceinte par pièce), et les longs sons lus en continu (Streaming).
///
/// On peut le refaire sans danger, par exemple quand Gabriel ajoute un son : il complète ce qui manque.
/// Annulable avec Ctrl+Z (sauf les réglages d'import). Ensuite : Ctrl+S.
/// </summary>
public static class SonsLaPorte
{
    const string DossierSons = "Assets/Audio";
    const string NomRacine = "Sons";
    const float SecondesPourStreaming = 10f;   // plus long = lu en continu depuis le disque (ambiances)

    static Dictionary<string, AudioClip> clips;      // nom du fichier en minuscules (sans extension) → son
    static HashSet<string> utilises;
    static List<string> rapport;
    static Transform racine;


    [MenuItem("La Porte/Brancher les sons")]
    static void Brancher()
    {
        GestionnaireEtapes jeu = Object.FindFirstObjectByType<GestionnaireEtapes>(FindObjectsInactive.Include);
        if (jeu == null)
        {
            EditorUtility.DisplayDialog("La Porte", "Il faut d'abord « La Porte → Installer dans la scène ouverte ».", "OK");
            return;
        }

        rapport = new List<string>();
        utilises = new HashSet<string>();
        TrouverEtReglerLesFichiers();

        Undo.SetCurrentGroupName("Brancher les sons");
        int groupeUndo = Undo.GetCurrentGroup();

        racine = RacineSons();
        ReglagesSons reglages = Composant<ReglagesSons>(racine.gameObject);
        SonsInteractions interactions = Composant<SonsInteractions>(racine.gameObject);

        // --- Étape 1 : l'énigme des couleurs ---
        EtapeEnigmeCouleurs couleurs = jeu.GetComponentInChildren<EtapeEnigmeCouleurs>(true);
        if (couleurs != null)
        {
            Undo.RecordObject(couleurs, "Brancher les sons");
            couleurs.sonBonneCouleur        = Son("Bonne couleur", Espace.Chambre, "bonne-couleur", couleurs.sonBonneCouleur);
            couleurs.sonMauvaiseCouleur     = Son("Mauvaise couleur", Espace.Chambre, "mauvaise-couleur", couleurs.sonMauvaiseCouleur);
            couleurs.sonMauvaiseCombinaison = Son("Mauvaise combinaison", Espace.Chambre, "mauvaise-combinaison", couleurs.sonMauvaiseCombinaison);
            couleurs.sonReussite            = Son("Combinaison réussie", Espace.Chambre, "bonne-combinaison", couleurs.sonReussite);
            EditorUtility.SetDirty(couleurs);
        }
        else rapport.Add("⚠ Pas d'étape EtapeEnigmeCouleurs sous « Jeu »");

        // --- Étape 3 : le numpad ---
        EtapeCodeNumpad numpad = jeu.GetComponentInChildren<EtapeCodeNumpad>(true);
        if (numpad != null)
        {
            Undo.RecordObject(numpad, "Brancher les sons");
            numpad.sonBonCode     = Son("Bon code", Espace.LesDeux, "bon-code", numpad.sonBonCode);
            numpad.sonMauvaisCode = Son("Mauvais code", Espace.Chambre, "mauvais-code", numpad.sonMauvaisCode);
            EditorUtility.SetDirty(numpad);
        }

        // --- Étape 4 : la porte ---
        EtapeFin fin = jeu.GetComponentInChildren<EtapeFin>(true);
        if (fin != null)
        {
            Undo.RecordObject(fin, "Brancher les sons");
            fin.sonFin = Son("Porte ouverte", Espace.LesDeux, "porte-ouverte", fin.sonFin);
            EditorUtility.SetDirty(fin);
        }

        // --- Les mécaniques ---
        EtincellesPrise etincelles = Object.FindFirstObjectByType<EtincellesPrise>(FindObjectsInactive.Include);
        if (etincelles != null)
        {
            Undo.RecordObject(etincelles, "Brancher les sons");
            etincelles.son = Son("Étincelles", Espace.Chambre, "spark", etincelles.son);
            EditorUtility.SetDirty(etincelles);
        }
        else rapport.Add("⚠ Pas d'étincelles dans la scène (menu « Ajouter les étincelles à la prise ») : « spark » pas branché");

        TetesQuiTombent tetes = Object.FindFirstObjectByType<TetesQuiTombent>(FindObjectsInactive.Include);
        if (tetes != null)
        {
            Undo.RecordObject(tetes, "Brancher les sons");
            tetes.sonChute = Son("Tête qui tombe", Espace.Corridor, "vent-tete-tombe", tetes.sonChute);
            EditorUtility.SetDirty(tetes);
        }

        JournauxQuiTombent journaux = Object.FindFirstObjectByType<JournauxQuiTombent>(FindObjectsInactive.Include);
        if (journaux != null)
        {
            Undo.RecordObject(journaux, "Brancher les sons");
            journaux.sonChute = Son("Journal qui tombe", Espace.Chambre, "journal-tombe", journaux.sonChute);
            EditorUtility.SetDirty(journaux);
        }

        Undo.RecordObject(interactions, "Brancher les sons");
        interactions.sonHeurtoir       = Son("Heurtoir (lampe qui tremble)", Espace.Chambre, "lamp-moving", interactions.sonHeurtoir);
        interactions.sonLampeAllumee   = Son("Lampe allumée", Espace.Chambre, "lamp-on", interactions.sonLampeAllumee);
        interactions.sonLampeEteinte   = Son("Lampe éteinte", Espace.Chambre, "lamp-off", interactions.sonLampeEteinte);
        interactions.sonCorridorSombre = Son("Corridor qui s'assombrit", Espace.Corridor, "lum-corridor-eteint", interactions.sonCorridorSombre);
        interactions.sonCorridorAllume = Son("Corridor qui se rallume", Espace.Corridor, "lum-corridor-allume", interactions.sonCorridorAllume);
        if (interactions.corridor == null) interactions.corridor = Object.FindFirstObjectByType<LumieresCorridor>(FindObjectsInactive.Include);
        EditorUtility.SetDirty(interactions);

        // --- Les ambiances (en boucle, du début à la fin) ---
        Ambiance("Ambiance chambre", Espace.Chambre, "ambiance-appart");
        Ambiance("Ambiance corridor", Espace.Corridor, "ambiance-corridor");

        VerifierAudioListener();
        SignalerLesFichiersInutilises();

        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(racine.gameObject.scene);
        Selection.activeGameObject = racine.gameObject;

        // Le rapport complet va dans la Console (la fenêtre de Unity coupe les longs textes).
        Debug.Log("[Sons] Rapport de « Brancher les sons » :\n" + string.Join("\n", rapport));

        // La fenêtre : seulement le résumé et ce qui demande de l'attention.
        int branches = 0;
        List<string> attention = new List<string>();
        foreach (string ligne in rapport)
        {
            if (ligne.StartsWith("✔ ") && ligne.Contains(" ← ")) branches++;
            else if (ligne.StartsWith("⚠") || ligne.StartsWith("—")) attention.Add(ligne);
        }

        EditorUtility.DisplayDialog("La Porte — sons",
            $"✔ {branches} sons branchés.\n\n" +
            (attention.Count > 0 ? string.Join("\n", attention) + "\n\n" : "") +
            "Rapport complet : dans la Console.\n" +
            "Chambre = sortie GAUCHE, corridor = sortie DROITE.\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- Les fichiers ----------

    /// <summary>Liste les sons de Assets/Audio et règle leur import (mono, streaming pour les longs).</summary>
    static void TrouverEtReglerLesFichiers()
    {
        clips = new Dictionary<string, AudioClip>();

        if (!AssetDatabase.IsValidFolder(DossierSons))
        {
            rapport.Add($"⚠ Le dossier « {DossierSons} » n'existe pas : mettre les sons de Gabriel dedans, puis refaire ce menu.");
            return;
        }

        int regles = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { DossierSons }))
        {
            string chemin = AssetDatabase.GUIDToAssetPath(guid);
            string nom = Path.GetFileNameWithoutExtension(chemin).ToLowerInvariant();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(chemin);
            if (clip == null) continue;

            if (clips.ContainsKey(nom)) rapport.Add($"⚠ Deux fichiers s'appellent « {nom} » : seul « {AssetDatabase.GetAssetPath(clips[nom])} » est utilisé");
            else clips[nom] = clip;

            if (ReglerImport(chemin, clip.length)) regles++;
        }

        rapport.Add($"✔ {clips.Count} sons trouvés dans {DossierSons}" + (regles > 0 ? $" ({regles} réimportés en mono)" : ""));
    }

    /// <summary>
    /// Mono : chaque pièce n'a qu'une enceinte (sinon, en stéréo, la moitié du son serait perdue d'un côté).
    /// Les sons longs (ambiances) sont lus en continu depuis le disque, les courts sont prêts en mémoire (pas de délai).
    /// Retourne true si quelque chose a changé.
    /// </summary>
    static bool ReglerImport(string chemin, float duree)
    {
        AudioImporter import = AssetImporter.GetAtPath(chemin) as AudioImporter;
        if (import == null) return false;

        AudioImporterSampleSettings reglage = import.defaultSampleSettings;
        AudioClipLoadType chargement = duree > SecondesPourStreaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;

        bool aChanger = !import.forceToMono
                        || reglage.loadType != chargement
                        || reglage.compressionFormat != AudioCompressionFormat.Vorbis;
        if (!aChanger) return false;

        import.forceToMono = true;
        reglage.loadType = chargement;
        reglage.compressionFormat = AudioCompressionFormat.Vorbis;
        reglage.quality = 0.8f;
        import.defaultSampleSettings = reglage;
        import.SaveAndReimport();
        return true;
    }

    static AudioClip Clip(string fichier)
    {
        if (clips.TryGetValue(fichier.ToLowerInvariant(), out AudioClip clip))
        {
            utilises.Add(fichier.ToLowerInvariant());
            return clip;
        }
        return null;
    }


    // ---------- Les AudioSources ----------

    /// <summary>
    /// Le son « nom », dans sa pièce, avec le fichier « fichier ».
    ///   - Fichier trouvé : une AudioSource sous « Sons / pièce » (créée ou réutilisée) remplace le bip.
    ///   - Pas de fichier : on garde ce qui est branché (le bip est rangé sous « Sons / pièce »).
    ///   - Un vrai son branché à la main ailleurs n'est jamais remplacé.
    /// </summary>
    static AudioSource Son(string nom, Espace espace, string fichier, AudioSource actuel)
    {
        bool estAMoi = actuel != null && actuel.transform.IsChildOf(racine);
        bool estUnBip = actuel != null && actuel.GetComponent<BipPlaceholder>() != null;

        if (actuel != null && !estAMoi && !estUnBip)
        {
            Sortie(actuel.gameObject, espace);
            rapport.Add($"• {nom} : gardé tel quel (« {actuel.name} », branché à la main)");
            return actuel;
        }

        AudioClip clip = Clip(fichier);
        if (clip == null)
        {
            if (actuel == null)
            {
                rapport.Add($"— {nom} : pas de fichier « {fichier} », rien de branché");
                return null;
            }

            if (!estAMoi)
            {
                Undo.SetTransformParent(actuel.transform, Piece(espace), "Ranger le bip");
                actuel.name = $"(bip) {nom}";
            }
            Sortie(actuel.gameObject, espace);
            rapport.Add($"⚠ {nom} : pas de fichier « {fichier} », le bip reste");
            return actuel;
        }

        // Le vrai son arrive : le bip qu'on avait rangé sous « Sons » ne sert plus.
        if (estAMoi && estUnBip) Undo.DestroyObjectImmediate(actuel.gameObject);

        Transform parent = Piece(espace);
        Transform existant = parent.Find(nom);
        GameObject objet;
        if (existant != null) objet = existant.gameObject;
        else
        {
            objet = new GameObject(nom);
            Undo.RegisterCreatedObjectUndo(objet, "Créer un son");
            Undo.SetTransformParent(objet.transform, parent, "Créer un son");
        }

        AudioSource source = Composant<AudioSource>(objet);
        Undo.RecordObject(source, "Brancher les sons");
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        Sortie(objet, espace);

        rapport.Add($"✔ {nom} ← {clip.name}");
        return source;
    }

    static void Ambiance(string nom, Espace espace, string fichier)
    {
        AudioClip clip = Clip(fichier);
        Transform existant = Piece(espace).Find(nom);
        if (clip == null)
        {
            rapport.Add($"— {nom} : pas de fichier « {fichier} », pas d'ambiance");
            return;
        }

        AudioSource source = Son(nom, espace, fichier, existant != null ? existant.GetComponent<AudioSource>() : null);
        Undo.RecordObject(source, "Brancher les sons");
        source.loop = true;
        source.playOnAwake = false;   // c'est AmbianceContinue qui la lance (sinon : doublée un instant au rechargement)
        Composant<AmbianceContinue>(source.gameObject);
        rapport[rapport.Count - 1] += " (en boucle)";
    }

    static void Sortie(GameObject objet, Espace espace)
    {
        SortieSon sortie = Composant<SortieSon>(objet);
        Undo.RecordObject(sortie, "Brancher les sons");
        sortie.espace = espace;
        EditorUtility.SetDirty(sortie);
    }


    // ---------- La scène ----------

    static Transform RacineSons()
    {
        GameObject objet = null;
        ReglagesSons dejaLa = Object.FindFirstObjectByType<ReglagesSons>(FindObjectsInactive.Include);
        if (dejaLa != null) objet = dejaLa.gameObject;
        else
        {
            objet = new GameObject(NomRacine);
            Undo.RegisterCreatedObjectUndo(objet, "Créer les sons");
        }
        return objet.transform;
    }

    static Transform Piece(Espace espace)
    {
        string nom = espace == Espace.Chambre ? "Chambre (gauche)" : espace == Espace.Corridor ? "Corridor (droite)" : "Les deux";
        Transform piece = racine.Find(nom);
        if (piece != null) return piece;

        GameObject objet = new GameObject(nom);
        Undo.RegisterCreatedObjectUndo(objet, "Créer les sons");
        Undo.SetTransformParent(objet.transform, racine, "Créer les sons");
        return objet.transform;
    }

    /// <summary>Unity n'accepte qu'UN AudioListener : on vérifie qu'il y en a exactement un d'actif.</summary>
    static void VerifierAudioListener()
    {
        List<AudioListener> actifs = new List<AudioListener>();
        foreach (AudioListener oreille in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (oreille.enabled && oreille.gameObject.activeInHierarchy) actifs.Add(oreille);
        }

        if (actifs.Count == 1)
        {
            rapport.Add($"✔ Un seul AudioListener (sur « {actifs[0].name} »)");
            return;
        }

        if (actifs.Count == 0)
        {
            Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null) { rapport.Add("⚠ Aucun AudioListener et aucune caméra : on n'entendra rien"); return; }
            Undo.AddComponent<AudioListener>(camera.gameObject);
            rapport.Add($"✔ AudioListener ajouté sur « {camera.name} »");
            return;
        }

        // Plusieurs : on garde le premier, on désactive les autres (deux ne marchent pas ensemble).
        for (int i = 1; i < actifs.Count; i++)
        {
            Undo.RecordObject(actifs[i], "Un seul AudioListener");
            actifs[i].enabled = false;
        }
        rapport.Add($"✔ {actifs.Count} AudioListeners : seul celui de « {actifs[0].name} » est gardé (Unity n'en accepte qu'un)");
    }

    static void SignalerLesFichiersInutilises()
    {
        List<string> restes = new List<string>();
        foreach (string nom in clips.Keys)
        {
            if (!utilises.Contains(nom)) restes.Add(nom);
        }
        if (restes.Count > 0) rapport.Add($"• Pas utilisés (noms inconnus) : {string.Join(", ", restes)}");
    }

    static T Composant<T>(GameObject objet) where T : Component
    {
        T composant = objet.GetComponent<T>();
        return composant != null ? composant : Undo.AddComponent<T>(objet);
    }
}
