using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using extOSC;

/// <summary>
/// Menu « La Porte → Installer dans la scène ouverte ».
///
/// Crée l'objet « Jeu » (moteur + étapes) et branche les objets qui existent déjà,
/// en reprenant les réglages de l'ancien OSCObjectController :
///   récepteur OSC, lumières du corridor, lampe sur pied, couleurs.
/// Puis désactive l'ancien script (il ferait double emploi).
///
/// Tout est annulable avec Ctrl+Z. N'oubliez pas de sauvegarder la scène (Ctrl+S).
/// </summary>
public static class InstallerLaPorte
{
    [MenuItem("La Porte/Installer dans la scène ouverte")]
    static void Installer()
    {
        if (Object.FindFirstObjectByType<GestionnaireEtapes>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("La Porte", "La scène contient déjà un GestionnaireEtapes. Rien n'a été changé.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Installer La Porte");
        int groupeUndo = Undo.GetCurrentGroup();
        List<string> rapport = new List<string>();

        // L'ancien script est lu par son nom (pas par son type), pour que cet outil
        // continue de compiler même si OSCObjectController.cs est supprimé un jour.
        MonoBehaviour ancienScript = TrouverScriptParNom("OSCObjectController");
        SerializedObject ancien = ancienScript != null ? new SerializedObject(ancienScript) : null;

        // ---------- 1. L'objet « Jeu » : le moteur ----------
        GameObject jeu = new GameObject("Jeu");
        Undo.RegisterCreatedObjectUndo(jeu, "Créer Jeu");

        Capteurs capteurs = jeu.AddComponent<Capteurs>();
        capteurs.recepteur = ancien != null
            ? ancien.FindProperty("receiver").objectReferenceValue as OSCReceiver
            : Object.FindFirstObjectByType<OSCReceiver>();
        rapport.Add(capteurs.recepteur != null ? "✔ Récepteur OSC branché" : "✘ Aucun OSCReceiver trouvé : à assigner sur Jeu → Capteurs");

        ReglagesJeu reglages = jeu.AddComponent<ReglagesJeu>();
        if (ancien != null) RecupererCouleurs(ancien, reglages, rapport);

        jeu.AddComponent<GestionnaireEtapes>();
        jeu.AddComponent<PanneauOperateur>();
        jeu.AddComponent<ActiverEcrans>();

        // ---------- 2. Les mécaniques (toujours actives) ----------
        Transform parentLumieres = ancien != null ? ancien.FindProperty("lightsParent").objectReferenceValue as Transform : null;
        if (parentLumieres != null)
        {
            LumieresCorridor corridor = Undo.AddComponent<LumieresCorridor>(parentLumieres.gameObject);
            corridor.lumieres = LumieresTrieesParNumero(parentLumieres);
            corridor.lumieresDuCode = LumieresDuCode(ancien);
            corridor.intensiteMax = ancien.FindProperty("maxIntensity").floatValue;
            corridor.largeurDegrade = ancien.FindProperty("gradientWidth").floatValue;
            corridor.vitesse = ancien.FindProperty("movementSpeed").floatValue;
            rapport.Add($"✔ LumieresCorridor ajouté sur « {parentLumieres.name} » ({corridor.lumieres.Length} lumières, {corridor.lumieresDuCode.Length} pour le code)");
        }
        else
        {
            rapport.Add("✘ Lumières du corridor non trouvées : ajouter LumieresCorridor sur « Lights » à la main");
        }

        LampeSurPied lampe = null;
        GameObject objetLampe = ancien != null ? ancien.FindProperty("lampe").objectReferenceValue as GameObject : null;
        if (objetLampe != null && objetLampe.GetComponent<Light>() != null)
        {
            lampe = Undo.AddComponent<LampeSurPied>(objetLampe);
            lampe.lumiere = objetLampe.GetComponent<Light>();
            rapport.Add($"✔ LampeSurPied ajouté sur « {objetLampe.name} »");
        }
        else
        {
            rapport.Add("✘ Lampe sur pied non trouvée : ajouter LampeSurPied sur la lampe à la main");
        }

        // ---------- 3. Les étapes (enfants de Jeu, dans l'ordre) ----------
        CreerEtape<EtapeAttente>(jeu, "0 - Attente des joueurs",
            "Tout est remis à neuf. Démarre au premier contact.");

        EtapeEnigmeCouleurs enigmeCouleurs = CreerEtape<EtapeEnigmeCouleurs>(jeu, "1 - Énigme des couleurs",
            "Locataire : poignée + interrupteur. Visiteur : heurtoir (couleur) + sonnette (sélection).");
        enigmeCouleurs.lampe = lampe;

        CreerEtape<EtapeTimeline>(jeu, "2 - Indices (têtes + journal)",
            "Les têtes tombent de la ventilation, le journal tombe sur la table.");

        CreerEtape<EtapeCodeNumpad>(jeu, "3 - Code du numpad",
            "Locataire décrit les têtes. Visiteur trouve les numéros dans le journal.");

        CreerEtape<EtapeFin>(jeu, "4 - Porte ouverte",
            "Fin. Retour automatique au début.");

        rapport.Add("✔ Objet « Jeu » créé avec 5 étapes");

        // ---------- 4. L'ancien script ferait double emploi ----------
        if (ancienScript != null)
        {
            Undo.RecordObject(ancienScript, "Désactiver OSCObjectController");
            ancienScript.enabled = false;
            rapport.Add("✔ Ancien OSCObjectController désactivé (pas supprimé)");
        }

        // ---------- 5. La scène doit être dans la liste du build pour pouvoir être rechargée ----------
        AjouterSceneAuBuild(jeu.scene.path, rapport);

        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(jeu.scene);
        Selection.activeGameObject = jeu;

        EditorUtility.DisplayDialog("La Porte — installation terminée",
            string.Join("\n", rapport) + "\n\nSauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- Outils ----------

    static T CreerEtape<T>(GameObject parent, string nom, string note) where T : Etape
    {
        GameObject objet = new GameObject(nom);
        objet.transform.SetParent(parent.transform, false);
        T etape = objet.AddComponent<T>();
        etape.note = note;
        return etape;
    }

    static MonoBehaviour TrouverScriptParNom(string nomDuScript)
    {
        foreach (GameObject racine in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (MonoBehaviour script in racine.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script != null && script.GetType().Name == nomDuScript) return script;
            }
        }
        return null;
    }

    /// <summary>Les lumières enfants, triées selon le numéro dans leur nom (light 1, light 2...).</summary>
    static Light[] LumieresTrieesParNumero(Transform parent)
    {
        Light[] lumieres = parent.GetComponentsInChildren<Light>(true);
        System.Array.Sort(lumieres, (a, b) => NumeroDansLeNom(a.name).CompareTo(NumeroDansLeNom(b.name)));
        return lumieres;
    }

    static int NumeroDansLeNom(string nom)
    {
        Match nombre = Regex.Match(nom, @"\d+");
        return nombre.Success ? int.Parse(nombre.Value) : int.MaxValue;
    }

    /// <summary>Reprend Light1, Light2, Light3 de l'ancien script (les lumières de la séquence de couleurs).</summary>
    static Light[] LumieresDuCode(SerializedObject ancien)
    {
        List<Light> lumieres = new List<Light>();
        foreach (string champ in new[] { "Light1", "Light2", "Light3" })
        {
            GameObject objet = ancien.FindProperty(champ).objectReferenceValue as GameObject;
            if (objet != null && objet.GetComponent<Light>() != null) lumieres.Add(objet.GetComponent<Light>());
        }
        return lumieres.ToArray();
    }

    /// <summary>
    /// Reprend les couleurs de la lampe (lampColors) et devine l'ordre de l'énigme
    /// à partir de HeaderColorPattern (les couleurs que l'ancien code mettait sur Light1-2-3).
    /// </summary>
    static void RecupererCouleurs(SerializedObject ancien, ReglagesJeu reglages, List<string> rapport)
    {
        string[] nomsParDefaut = { "Bleu", "Rouge", "Vert", "Jaune", "Mauve" };

        SerializedProperty couleursLampe = ancien.FindProperty("lampColors");
        if (couleursLampe == null || couleursLampe.arraySize == 0) return;

        reglages.couleursLampe = new CouleurNommee[couleursLampe.arraySize];
        for (int i = 0; i < couleursLampe.arraySize; i++)
        {
            string nom = i < nomsParDefaut.Length ? nomsParDefaut[i] : $"Couleur {i}";
            reglages.couleursLampe[i] = new CouleurNommee(nom, couleursLampe.GetArrayElementAtIndex(i).colorValue);
        }

        SerializedProperty motif = ancien.FindProperty("HeaderColorPattern");
        if (motif == null || motif.arraySize == 0) return;

        List<int> ordre = new List<int>();
        for (int i = 0; i < motif.arraySize; i++)
        {
            int index = IndexDeLaCouleur(reglages.couleursLampe, motif.GetArrayElementAtIndex(i).colorValue);
            if (index >= 0) ordre.Add(index);
        }

        if (ordre.Count > 0)
        {
            reglages.ordreCouleurs = ordre.ToArray();
            rapport.Add($"✔ Couleurs reprises de l'ancien script — ordre : {reglages.TexteOrdreCouleurs()}");
        }
    }

    static int IndexDeLaCouleur(CouleurNommee[] couleurs, Color cherchee)
    {
        for (int i = 0; i < couleurs.Length; i++)
        {
            Color c = couleurs[i].couleur;
            bool pareille = Mathf.Abs(c.r - cherchee.r) < 0.01f
                         && Mathf.Abs(c.g - cherchee.g) < 0.01f
                         && Mathf.Abs(c.b - cherchee.b) < 0.01f;
            if (pareille) return i;
        }
        return -1;
    }

    static void AjouterSceneAuBuild(string cheminScene, List<string> rapport)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (scene.path == cheminScene) return;   // déjà là
        }

        scenes.Insert(0, new EditorBuildSettingsScene(cheminScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        rapport.Add("✔ Scène ajoutée à la liste du build");
    }
}
