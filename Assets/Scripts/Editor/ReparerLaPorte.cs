using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu « La Porte → Tout rebrancher (après un export du Couloir) ».
///
/// À utiliser quand un nouvel export de Couloir.fbx (Blender) a cassé quelque chose dans la scène :
/// lumières blanches, journaux invisibles, vent ou interactions qui ne marchent plus.
///
/// Ce menu :
///   1. copie la scène ouverte, si elle a des changements pas enregistrés, dans Assets/Scenes/Sauvegardes
///      (rien n'est perdu : on peut la rouvrir pour récupérer quelque chose);
///   2. empêche Couloir.fbx d'importer les lumières et les caméras de Blender
///      (les lumières de Blender arrivent beaucoup trop fortes dans Unity : tout devient blanc);
///   3. rouvre Couloir.unity telle qu'elle est enregistrée sur le disque;
///   4. détache de Couloir.fbx les maillages que la scène lui emprunte (les journaux → Assets/Model/Detaches),
///      pour qu'un export de Blender ne puisse plus les changer;
///      remet ce qui manque : colliders, vieille vent cachée,
///      caméra de la chambre (Display 1), et relance les menus « Brancher … » seulement pour ce qui a disparu;
///   5. vérifie les lumières, les plafonniers, la vent, les étapes et les références cassées,
///      et écrit le rapport complet dans la Console.
///
/// Si quelque chose a été réparé, la scène est enregistrée à la fin.
/// </summary>
public static class ReparerLaPorte
{
    const string CheminScene = "Assets/Scenes/Couloir.unity";
    const string CheminFbx = "Assets/Model/Couloir.fbx";
    const string DossierSauvegardes = "Assets/Scenes/Sauvegardes";

    const string DossierDetaches = "Assets/Model/Detaches";
    const string CheminJournalDetache = DossierDetaches + "/Journal.asset";
    const string NomVentAnimee = "Vent_corridor (animée)";
    const string NomAncienneVent = "Vent_corridor";
    const string NomEtincelles = "Étincelles de la prise";
    const string NomGroupeJournaux = "Journaux qui tombent";
    const string NomRacineSons = "Sons";
    static readonly string[] CamerasAttendues = { "Corridor Camera", "Room Camera" };

    // La chambre était filmée par « Camera Room », la caméra de Blender importée avec Couloir.fbx.
    // Depuis que le FBX n'a plus de caméra, « Room Camera » (dans la scène) prend sa place, avec exactement
    // les mêmes réglages (lus en important l'ancien Couloir.fbx du commit da7a8d8 dans Unity).
    const string NomCameraChambre = "Room Camera";
    const string NomCameraCorridor = "Corridor Camera";
    static readonly Vector3 PositionCameraChambre = new Vector3(0f, 1.6f, -1f);        // dans le repère du modèle Couloir
    static readonly Quaternion RotationCameraChambre = new Quaternion(0f, -1f, 0f, 0f); // tournée de 180° : regarde vers -Z
    const float FocaleCameraChambre = 6.197896f;                                        // mm (caméra physique, comme Blender)
    static readonly Vector2 CapteurCameraChambre = new Vector2(53f, 24f);               // mm

    static List<string> rapport;
    static int reparations;


    [MenuItem("La Porte/Tout rebrancher (après un export du Couloir)", false, 0)]
    static void ToutRebrancher()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("La Porte", "Arrêter le mode Play d'abord.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("La Porte — Tout rebrancher",
                "Ce bouton :\n\n" +
                "• copie les changements pas enregistrés dans Assets/Scenes/Sauvegardes;\n" +
                "• empêche Couloir.fbx d'importer les lumières et caméras de Blender;\n" +
                "• rouvre Couloir.unity telle qu'enregistrée;\n" +
                "• remet ce qui manque (journaux, vent, étincelles, sons…) et vérifie tout.\n\n" +
                "Le rapport complet sera dans la Console.",
                "Tout rebrancher", "Annuler"))
            return;

        rapport = new List<string>();
        reparations = 0;

        AssetDatabase.Refresh();

        SauvegarderLesChangementsPasEnregistres();
        ReglerImportDuFbx();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CheminScene) == null)
        {
            EditorUtility.DisplayDialog("La Porte", $"« {CheminScene} » introuvable.", "OK");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(CheminScene, OpenSceneMode.Single);
        rapport.Add($"✔ Scène rouverte telle qu'enregistrée : {CheminScene}");

        VerifierLaVent();
        RelancerCeQuiManque();
        DetacherLesMaillages();   // après les menus relancés : « Brancher les journaux » reprend le maillage du FBX
        RemesurerLesJournaux();   // après le détachement : les mesures doivent venir du bon maillage
        VerifierLesLumieres();
        RemettreLaCameraDeLaChambre();
        VerifierLesCameras();
        VerifierLesReferences();
        ListerCeQuiTientAuxNomsDeBlender();

        if (reparations > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            rapport.Add($"✔ {reparations} réparation(s) faite(s), scène enregistrée.");
        }
        else rapport.Add("✔ Rien à réparer dans la scène enregistrée.");

        int erreurs = rapport.Count(l => l.StartsWith("✘"));
        int alertes = rapport.Count(l => l.StartsWith("⚠"));
        string texte = string.Join("\n", rapport);

        if (erreurs > 0) Debug.LogError("[Tout rebrancher]\n" + texte);
        else if (alertes > 0) Debug.LogWarning("[Tout rebrancher]\n" + texte);
        else Debug.Log("[Tout rebrancher]\n" + texte);

        EditorUtility.DisplayDialog("La Porte — Tout rebrancher",
            $"Terminé : {reparations} réparation(s), {erreurs} problème(s), {alertes} avertissement(s).\n\n" +
            "Le rapport complet est dans la Console.", "OK");
    }


    // ---------------------------------------------------------------- 1. Sauvegarde

    static void SauvegarderLesChangementsPasEnregistres()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isDirty || string.IsNullOrEmpty(s.path)) continue;

            Directory.CreateDirectory(DossierSauvegardes);
            string copie = $"{DossierSauvegardes}/{s.name} (pas enregistrée) {System.DateTime.Now:yyyy-MM-dd HH'h'mm}.unity";
            if (EditorSceneManager.SaveScene(s, copie, true))
                rapport.Add($"✔ Changements pas enregistrés de « {s.name} » copiés dans {copie}");
            else
                rapport.Add($"⚠ Impossible de copier les changements pas enregistrés de « {s.name} »");
        }
    }


    // ---------------------------------------------------------------- 2. Import du FBX

    static void ReglerImportDuFbx()
    {
        ModelImporter importeur = AssetImporter.GetAtPath(CheminFbx) as ModelImporter;
        if (importeur == null)
        {
            rapport.Add($"✘ « {CheminFbx} » introuvable.");
            return;
        }

        if (importeur.importLights || importeur.importCameras)
        {
            importeur.importLights = false;
            importeur.importCameras = false;
            importeur.SaveAndReimport();
            rapport.Add("✔ Couloir.fbx : import des lumières et caméras de Blender désactivé (c'est ce qui rendait tout blanc).");
        }
        else rapport.Add("✔ Couloir.fbx n'importe pas les lumières ni les caméras de Blender.");

        // Chaque objet du modèle comparé à la référence (voir CouloirImport) : position, rotation, échelle, forme.
        rapport.AddRange(CouloirImport.Verifier(out int _));
    }

    /// <summary>
    /// « Journaux qui tombent » garde la taille et le sens de chaque journal (mesurés dans l'éditeur, OnValidate).
    /// Si elles ont été prises pendant que les journaux avaient un mauvais maillage, ils tombent minuscules :
    /// on les remesure avec le bon maillage.
    /// </summary>
    static void RemesurerLesJournaux()
    {
        foreach (JournauxQuiTombent groupe in DansLaScene<JournauxQuiTombent>())
        {
            Undo.RecordObject(groupe, "Tout rebrancher");
            string avant = EditorJsonUtility.ToJson(groupe);
            groupe.MesurerLesJournaux();
            if (EditorJsonUtility.ToJson(groupe) != avant)
            {
                EditorUtility.SetDirty(groupe);
                reparations++;
                rapport.Add("✔ Mesures des journaux recalculées (taille, épaisseur, sens de l'affiche).");
            }
            else rapport.Add("✔ Mesures des journaux à jour.");

            if (groupe.mesures != null)
                for (int i = 0; i < groupe.mesures.Length; i++)
                    if (groupe.mesures[i].hauteur < 0.05f || groupe.mesures[i].largeur < 0.05f)
                        rapport.Add($"✘ Journal n° {i + 1} : {groupe.mesures[i].largeur:F3} × {groupe.mesures[i].hauteur:F3} m, beaucoup trop petit (maillage cassé ?).");
        }
    }


    // ---------------------------------------------------------------- 4. Réparations

    /// <summary>
    /// Les objets de la scène qui ne font PAS partie du modèle Couloir mais qui lui empruntent un maillage
    /// (les 5 journaux) reçoivent une copie à eux dans Assets/Model/Detaches :
    /// un nouvel export de Blender ne peut plus les changer.
    /// Les journaux reprennent le maillage d'origine (Detaches/Journal.asset, tiré du Couloir.fbx du commit da7a8d8),
    /// même si le Journal de Blender est cassé.
    /// Le décor lui-même (le modèle Couloir et ce qui est accroché à ses objets) continue de suivre le FBX : c'est voulu.
    /// </summary>
    static void DetacherLesMaillages()
    {
        Mesh journal = AssetDatabase.LoadAssetAtPath<Mesh>(CheminJournalDetache);
        if (journal == null)
            rapport.Add($"✘ « {CheminJournalDetache} » introuvable : c'est le maillage d'origine des journaux.");

        int journaux = 0;
        foreach (MeshFilter filtre in DansLaScene<MeshFilter>())
        {
            if (FaitPartieDuModeleCouloir(filtre)) continue;
            Mesh actuel = filtre.sharedMesh;
            bool vientDuFbx = actuel != null && AssetDatabase.GetAssetPath(actuel) == CheminFbx;

            if (filtre.name.StartsWith("Journal") && journal != null && (actuel == null || vientDuFbx))
            {
                Undo.RecordObject(filtre, "Tout rebrancher");
                filtre.sharedMesh = journal;
                EditorUtility.SetDirty(filtre);
                journaux++;
                reparations++;
                continue;
            }

            if (actuel == null)
            {
                rapport.Add($"✘ « {Chemin(filtre.transform)} » n'a plus de maillage.");
                continue;
            }
            if (!vientDuFbx) continue;

            Mesh copie = CopieDetachee(actuel);
            Undo.RecordObject(filtre, "Tout rebrancher");
            filtre.sharedMesh = copie;
            EditorUtility.SetDirty(filtre);
            reparations++;
            rapport.Add($"✔ « {Chemin(filtre.transform)} » : maillage « {actuel.name} » détaché de Couloir.fbx ({AssetDatabase.GetAssetPath(copie)}).");
        }

        if (journaux > 0)
            rapport.Add($"✔ {journaux} journal(aux) détaché(s) de Couloir.fbx : maillage d'origine ({CheminJournalDetache}). " +
                        "Un export de Blender ne peut plus les changer.");
        else if (journal != null)
            rapport.Add("✔ Les journaux ont leur propre maillage (Couloir.fbx ne peut plus les changer).");

        foreach (MeshCollider collider in DansLaScene<MeshCollider>())
        {
            if (collider.sharedMesh != null) continue;
            MeshFilter filtre = collider.GetComponent<MeshFilter>();
            if (filtre != null && filtre.sharedMesh != null)
            {
                Undo.RecordObject(collider, "Tout rebrancher");
                collider.sharedMesh = filtre.sharedMesh;
                EditorUtility.SetDirty(collider);
                reparations++;
                rapport.Add($"✔ Collider de « {Chemin(collider.transform)} » rebranché sur son maillage.");
            }
            else rapport.Add($"⚠ Le MeshCollider de « {Chemin(collider.transform)} » n'a pas de maillage.");
        }
    }

    static void VerifierLaVent()
    {
        Transform nouvelle = TetesLaPorte.TrouverDansLaScene(NomVentAnimee);
        if (nouvelle == null)
        {
            rapport.Add($"⚠ « {NomVentAnimee} » manquante : je relance « Brancher la vent animée ».");
            VentLaPorte.Brancher();
            reparations++;
            nouvelle = TetesLaPorte.TrouverDansLaScene(NomVentAnimee);
            if (nouvelle == null) { rapport.Add("✘ La vent animée n'a pas pu être remise."); return; }
        }

        if (!nouvelle.gameObject.activeSelf)
        {
            Undo.RecordObject(nouvelle.gameObject, "Tout rebrancher");
            nouvelle.gameObject.SetActive(true);
            reparations++;
            rapport.Add($"✔ « {NomVentAnimee} » réactivée.");
        }

        Transform ancienne = nouvelle.parent != null ? nouvelle.parent.Find(NomAncienneVent) : null;
        if (ancienne != null && ancienne.gameObject.activeSelf)
        {
            Undo.RecordObject(ancienne.gameObject, "Tout rebrancher");
            ancienne.gameObject.SetActive(false);
            reparations++;
            rapport.Add($"✔ Vieille « {NomAncienneVent} » (celle du FBX) cachée de nouveau.");
        }

        Animator animateur = nouvelle.GetComponent<Animator>();
        if (animateur == null) rapport.Add($"✘ « {NomVentAnimee} » n'a plus d'Animator : relancer « La Porte → Brancher la vent animée ».");
        else rapport.Add($"✔ Vent animée en place ({Chemin(nouvelle)}).");
    }

    static void RelancerCeQuiManque()
    {
        if (Object.FindFirstObjectByType<GestionnaireEtapes>(FindObjectsInactive.Include) == null)
        {
            rapport.Add("⚠ Pas de « GestionnaireEtapes » : je relance « Installer dans la scène ouverte ».");
            Relancer("La Porte/Installer dans la scène ouverte");
        }
        else rapport.Add("✔ Étapes du jeu (GestionnaireEtapes) en place.");

        Relancer(NomEtincelles, "La Porte/Ajouter les étincelles à la prise");
        Relancer(NomGroupeJournaux, "La Porte/Brancher les journaux");

        if (Object.FindFirstObjectByType<SonsInteractions>(FindObjectsInactive.Include) == null)
        {
            rapport.Add("⚠ Interactions sonores (SonsInteractions) manquantes : je relance « Brancher les sons ».");
            Relancer("La Porte/Brancher les sons");
        }
        else rapport.Add("✔ Sons et interactions (SonsInteractions) en place.");
    }

    static void Relancer(string nomObjet, string menu)
    {
        if (TetesLaPorte.TrouverDansLaScene(nomObjet) != null)
        {
            rapport.Add($"✔ « {nomObjet} » en place.");
            return;
        }
        rapport.Add($"⚠ « {nomObjet} » manquant : je relance « {menu.Substring("La Porte/".Length)} ».");
        Relancer(menu);
    }

    static void Relancer(string menu)
    {
        if (EditorApplication.ExecuteMenuItem(menu)) reparations++;
        else rapport.Add($"✘ Le menu « {menu} » n'a pas pu être lancé.");
    }


    // ---------------------------------------------------------------- 5. Vérifications

    static void VerifierLesLumieres()
    {
        Light[] lumieres = DansLaScene<Light>().ToArray();

        // Des lumières venues de Blender ne devraient plus exister (import désactivé).
        foreach (Light lumiere in lumieres)
        {
            Object source = PrefabUtility.GetCorrespondingObjectFromSource(lumiere);
            if (source != null && AssetDatabase.GetAssetPath(source) == CheminFbx)
                rapport.Add($"✘ « {Chemin(lumiere.transform)} » est une lumière de Blender (Couloir.fbx) : elle rend tout blanc.");
        }

        int eteintes = 0;
        foreach (Light lumiere in lumieres)
        {
            if (lumiere.enabled && lumiere.gameObject.activeInHierarchy) continue;
            eteintes++;
            rapport.Add($"⚠ Lumière désactivée : « {Chemin(lumiere.transform)} » (normal seulement si c'est voulu).");
        }
        rapport.Add($"✔ {lumieres.Length} lumière(s) dans la scène, {lumieres.Length - eteintes} active(s).");

        foreach (LumieresCorridor corridor in DansLaScene<LumieresCorridor>())
        {
            int vides = (corridor.lumieres ?? new Light[0]).Count(l => l == null);
            if (corridor.lumieres == null || corridor.lumieres.Length == 0)
                rapport.Add($"✘ « {Chemin(corridor.transform)} » → Lumieres Corridor : la liste des lumières est vide.");
            else if (vides > 0)
                rapport.Add($"✘ « {Chemin(corridor.transform)} » → Lumieres Corridor : {vides} lumière(s) manquante(s) dans la liste.");
            else
                rapport.Add($"✔ Lumières du corridor : {corridor.lumieres.Length} branchées.");

            if (corridor.lumieresDuCode != null && corridor.lumieresDuCode.Any(l => l == null))
                rapport.Add($"✘ « {Chemin(corridor.transform)} » → Lumieres Du Code : une lumière manque.");
            if (corridor.cameraCorridor == null)
                rapport.Add($"⚠ « {Chemin(corridor.transform)} » → Camera Corridor est vide.");
        }

        foreach (CopyLightIntensity copie in DansLaScene<CopyLightIntensity>())
            if (copie.MainLight == null)
                rapport.Add($"✘ « {Chemin(copie.transform)} » → Copy Light Intensity : Main Light est vide.");

        Renderer[] rendus = DansLaScene<Renderer>().ToArray();
        foreach (PlafonnierLumiere plafonnier in DansLaScene<PlafonnierLumiere>())
        {
            if (plafonnier.materiauPlafonnier == null)
            {
                rapport.Add($"✘ « {Chemin(plafonnier.transform)} » → Plafonnier : le matériau est vide.");
                continue;
            }
            bool utilise = rendus.Any(r => r.sharedMaterials.Contains(plafonnier.materiauPlafonnier));
            if (!utilise)
                rapport.Add($"✘ Plafonnier de « {plafonnier.name} » : plus aucun objet n'utilise « {plafonnier.materiauPlafonnier.name} » " +
                            "(le matériau du plafonnier a changé de nom dans Blender ?).");
        }
    }

    /// <summary>
    /// Le Display 1 (l'écran du visiteur, la chambre) doit être filmé par une caméra active.
    /// Sinon, « Room Camera » est activée et réglée comme l'ancienne « Camera Room » de Blender.
    /// </summary>
    static void RemettreLaCameraDeLaChambre()
    {
        Camera ecran1 = DansLaScene<Camera>().FirstOrDefault(c =>
            c.enabled && c.gameObject.activeInHierarchy && c.targetTexture == null &&
            c.targetDisplay == 0 && c.name != NomCameraCorridor);
        if (ecran1 != null)
        {
            rapport.Add($"✔ La chambre (Display 1) est filmée par « {Chemin(ecran1.transform)} ».");
            return;
        }

        Transform couloir = RacineDuModeleCouloir();
        if (couloir == null)
        {
            rapport.Add("✘ Personne ne filme la chambre (Display 1), et le modèle Couloir est introuvable dans la scène pour placer la caméra.");
            return;
        }

        Transform objet = TetesLaPorte.TrouverDansLaScene(NomCameraChambre);
        if (objet == null)
        {
            GameObject nouveau = new GameObject(NomCameraChambre);
            Undo.RegisterCreatedObjectUndo(nouveau, "Tout rebrancher");
            nouveau.tag = "MainCamera";
            objet = nouveau.transform;
        }

        Camera camera = objet.GetComponent<Camera>();
        if (camera == null) camera = Undo.AddComponent<Camera>(objet.gameObject);

        Undo.RecordObject(objet, "Tout rebrancher");
        objet.SetPositionAndRotation(couloir.TransformPoint(PositionCameraChambre), couloir.rotation * RotationCameraChambre);
        objet.localScale = Vector3.one;

        Undo.RecordObject(camera, "Tout rebrancher");
        camera.enabled = true;
        camera.targetDisplay = 0;
        camera.targetTexture = null;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000f;
        camera.usePhysicalProperties = true;
        camera.sensorSize = CapteurCameraChambre;
        camera.focalLength = FocaleCameraChambre;
        camera.gateFit = Camera.GateFitMode.Horizontal;
        camera.lensShift = Vector2.zero;
        EditorUtility.SetDirty(camera);

        Undo.RecordObject(objet.gameObject, "Tout rebrancher");
        objet.gameObject.SetActive(true);

        reparations++;
        rapport.Add($"✔ Caméra de la chambre remise : « {NomCameraChambre} » activée sur le Display 1, à la place et avec les réglages " +
                    "de l'ancienne « Camera Room » de Blender.");
    }

    /// <summary>Une copie du maillage dans Assets/Model/Detaches (réutilisée si elle existe déjà).</summary>
    static Mesh CopieDetachee(Mesh source)
    {
        string chemin = $"{DossierDetaches}/{source.name}.asset";
        Mesh existante = AssetDatabase.LoadAssetAtPath<Mesh>(chemin);
        if (existante != null) return existante;

        Directory.CreateDirectory(DossierDetaches);
        Mesh copie = Object.Instantiate(source);
        copie.name = source.name;
        AssetDatabase.CreateAsset(copie, chemin);
        return copie;
    }

    /// <summary>Vrai si l'objet vient du modèle Couloir.fbx lui-même (le décor, qui doit suivre les exports).</summary>
    static bool FaitPartieDuModeleCouloir(Component composant)
    {
        Object source = PrefabUtility.GetCorrespondingObjectFromSource(composant.gameObject);
        return source != null && AssetDatabase.GetAssetPath(source) == CheminFbx;
    }

    /// <summary>
    /// Ce qui est accroché aux objets du modèle (lumière de la lampe, étincelles de la prise, vent animée…)
    /// tient par le NOM de l'objet dans Blender : renommer ou supprimer cet objet dans Blender le perd.
    /// </summary>
    static void ListerCeQuiTientAuxNomsDeBlender()
    {
        Transform couloir = RacineDuModeleCouloir();
        if (couloir == null) return;

        List<string> accroches = new List<string>();
        foreach (AddedGameObject ajout in PrefabUtility.GetAddedGameObjects(couloir.gameObject))
            if (ajout.instanceGameObject != null && ajout.instanceGameObject.transform.parent != null)
                accroches.Add($"{ajout.instanceGameObject.transform.parent.name} → {ajout.instanceGameObject.name}");
        foreach (AddedComponent ajout in PrefabUtility.GetAddedComponents(couloir.gameObject))
            if (ajout.instanceComponent != null)
                accroches.Add($"{ajout.instanceComponent.gameObject.name} → {ajout.instanceComponent.GetType().Name}");

        if (accroches.Count > 0)
            rapport.Add("ℹ Accroché aux objets de Couloir.fbx (ne pas renommer ces objets dans Blender) : " + string.Join(", ", accroches));
    }

    /// <summary>L'objet de la scène qui est le modèle Couloir.fbx (la racine de l'instance).</summary>
    static Transform RacineDuModeleCouloir()
    {
        foreach (Transform objet in DansLaScene<Transform>())
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(objet.gameObject)) continue;
            Object source = PrefabUtility.GetCorrespondingObjectFromSource(objet.gameObject);
            if (source != null && AssetDatabase.GetAssetPath(source) == CheminFbx) return objet;
        }
        return null;
    }

    static void VerifierLesCameras()
    {
        foreach (string nom in CamerasAttendues)
        {
            Transform objet = TetesLaPorte.TrouverDansLaScene(nom);
            Camera camera = objet != null ? objet.GetComponent<Camera>() : null;
            if (camera == null)
                rapport.Add($"✘ Caméra « {nom} » introuvable.");
            else if (!camera.enabled || !objet.gameObject.activeInHierarchy)
                rapport.Add($"⚠ Caméra « {nom} » désactivée.");
            else
                rapport.Add($"✔ Caméra « {nom} » active (Display {camera.targetDisplay + 1}).");
        }

        foreach (Camera camera in DansLaScene<Camera>())
        {
            Object source = PrefabUtility.GetCorrespondingObjectFromSource(camera);
            if (source != null && AssetDatabase.GetAssetPath(source) == CheminFbx)
                rapport.Add($"✘ « {Chemin(camera.transform)} » est une caméra de Blender (Couloir.fbx).");
        }
    }

    /// <summary>Scripts manquants et références vers des objets qui n'existent plus.</summary>
    static void VerifierLesReferences()
    {
        int cassees = 0;
        foreach (GameObject objet in DansLaScene<Transform>().Select(t => t.gameObject))
        {
            int scriptsManquants = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objet);
            if (scriptsManquants > 0)
            {
                cassees++;
                rapport.Add($"✘ « {Chemin(objet.transform)} » : {scriptsManquants} script(s) manquant(s).");
            }

            foreach (Component composant in objet.GetComponents<Component>())
            {
                if (composant == null || composant is Transform) continue;

                SerializedProperty propriete = new SerializedObject(composant).GetIterator();
                while (propriete.Next(true))
                {
                    if (propriete.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (propriete.objectReferenceValue != null || propriete.objectReferenceInstanceIDValue == 0) continue;
                    if (propriete.propertyPath.StartsWith("m_PrefabInstance") ||
                        propriete.propertyPath.StartsWith("m_CorrespondingSourceObject") ||
                        propriete.propertyPath.StartsWith("m_PrefabAsset")) continue;

                    cassees++;
                    rapport.Add($"✘ Référence cassée : « {Chemin(objet.transform)} » → {composant.GetType().Name} → {propriete.displayName}");
                }
            }
        }
        if (cassees == 0) rapport.Add("✔ Aucun script manquant ni référence cassée.");
    }


    // ---------------------------------------------------------------- Outils

    static IEnumerable<T> DansLaScene<T>() where T : Component =>
        Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .Where(c => c != null && !EditorUtility.IsPersistent(c));

    static string Chemin(Transform objet)
    {
        string chemin = objet.name;
        for (Transform parent = objet.parent; parent != null; parent = parent.parent) chemin = parent.name + "/" + chemin;
        return chemin;
    }
}
