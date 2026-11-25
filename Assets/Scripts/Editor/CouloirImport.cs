using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Protège la scène contre un mauvais export de Couloir.fbx (Blender), AUTOMATIQUEMENT à chaque import :
///
///  - n'importe jamais les lumières ni les caméras de Blender (elles rendaient tout blanc);
///  - retire les objets « … reference … » (ex. « Cegep corridor reference », le plan de l'image de référence);
///  - compare chaque objet du modèle à la référence (Assets/Model/Detaches/Couloir.reference.json) :
///    un objet qui a bougé, changé d'échelle ou disparu = ✘ dans la Console + une fenêtre d'avertissement.
///    C'est ce qui arrive quand l'export n'a pas les bons réglages (« Apply Transform » décoché, autre « Apply Scalings »…)
///    ou quand on applique l'échelle (Ctrl+A) d'un objet dans Blender : ce qui est accroché à l'objet dans Unity
///    (lumière du ventilateur, étincelles de la prise…) se retrouve au mauvais endroit, à la mauvaise taille.
///
/// Export Blender : préréglage « La Porte - Couloir » (Apply Transform coché, Object Types = Mesh seulement).
/// Si un changement est voulu (objet déplacé exprès dans Blender) : La Porte → Accepter Couloir.fbx comme référence.
/// </summary>
public class CouloirImport : AssetPostprocessor
{
    public const string CheminFbx = "Assets/Model/Couloir.fbx";
    public const string CheminReference = "Assets/Model/Detaches/Couloir.reference.json";

    const float ToleranceDistance = 0.001f;   // 1 mm
    const float ToleranceAngle = 0.1f;        // degrés
    const float ToleranceEchelle = 0.001f;    // 0,1 %

    void OnPreprocessModel()
    {
        if (assetPath != CheminFbx) return;
        ModelImporter importeur = (ModelImporter)assetImporter;
        importeur.importLights = false;
        importeur.importCameras = false;
    }

    void OnPostprocessModel(GameObject racine)
    {
        if (assetPath != CheminFbx) return;
        foreach (Transform objet in racine.GetComponentsInChildren<Transform>(true))
        {
            if (objet == null || objet == racine.transform) continue;
            if (objet.name.IndexOf("reference", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Debug.Log($"[Couloir.fbx] « {objet.name} » retiré à l'import (objet de référence de Blender).");
            Object.DestroyImmediate(objet.gameObject);
        }
    }

    static void OnPostprocessAllAssets(string[] importes, string[] supprimes, string[] deplaces, string[] anciensChemins)
    {
        if (!importes.Contains(CheminFbx)) return;
        EditorApplication.delayCall += () =>
        {
            List<string> rapport = Verifier(out int problemes);
            if (rapport == null) return;
            string texte = "[Couloir.fbx] Vérification après import\n" + string.Join("\n", rapport);
            if (problemes == 0) { Debug.Log(texte); return; }

            Debug.LogError(texte);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("La Porte — Couloir.fbx",
                    $"{problemes} objet(s) du nouveau Couloir.fbx ont bougé, changé d'échelle ou disparu.\n\n" +
                    "Ce qui est accroché à ces objets dans Unity (lumière, étincelles, animation…) sera au mauvais endroit.\n\n" +
                    "Cause habituelle : export sans le préréglage « La Porte - Couloir », ou échelle appliquée (Ctrl+A) dans Blender.\n" +
                    "→ Réexporter avec le préréglage.\n\n" +
                    "Si c'est voulu : La Porte → Accepter Couloir.fbx comme référence.\n\nLa liste est dans la Console.", "OK");
        };
    }

    [MenuItem("La Porte/Accepter Couloir.fbx comme référence", false, 1)]
    static void Accepter()
    {
        if (!EditorUtility.DisplayDialog("La Porte",
                "Le Couloir.fbx actuel devient la référence : ses objets (positions, échelles, formes) seront considérés comme corrects.\n\n" +
                "À faire seulement si les changements dans Blender sont voulus.", "Accepter", "Annuler"))
            return;
        if (EcrireReference()) Debug.Log($"[Couloir.fbx] Référence mise à jour : {CheminReference}");
    }


    // ---------------------------------------------------------------- Référence

    [System.Serializable]
    public class Objet
    {
        public string chemin;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 echelle;
        public int sommets = -1;
        public Vector3 centre;
        public Vector3 taille;
    }

    [System.Serializable]
    public class Reference
    {
        public List<Objet> objets = new List<Objet>();
    }

    static string Chemin(Transform objet, Transform racine)
    {
        string chemin = objet.name;
        for (Transform parent = objet.parent; parent != null && parent != racine; parent = parent.parent)
            chemin = parent.name + "/" + chemin;
        return chemin;
    }

    public static Reference Lire(GameObject modele)
    {
        Reference reference = new Reference();
        foreach (Transform objet in modele.GetComponentsInChildren<Transform>(true))
        {
            if (objet == modele.transform) continue;
            Objet o = new Objet
            {
                chemin = Chemin(objet, modele.transform),
                position = objet.localPosition,
                rotation = objet.localRotation,
                echelle = objet.localScale,
            };
            MeshFilter filtre = objet.GetComponent<MeshFilter>();
            if (filtre != null && filtre.sharedMesh != null)
            {
                o.sommets = filtre.sharedMesh.vertexCount;
                o.centre = filtre.sharedMesh.bounds.center;
                o.taille = filtre.sharedMesh.bounds.size;
            }
            reference.objets.Add(o);
        }
        return reference;
    }

    /// <summary>Enregistre le Couloir.fbx actuel comme référence.</summary>
    public static bool EcrireReference()
    {
        GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(CheminFbx);
        if (modele == null) return false;
        Directory.CreateDirectory(Path.GetDirectoryName(CheminReference));
        File.WriteAllText(CheminReference, JsonUtility.ToJson(Lire(modele), true));
        AssetDatabase.ImportAsset(CheminReference);
        return true;
    }

    /// <summary>
    /// Compare Couloir.fbx à la référence. Retourne les lignes du rapport (✔ ✘ ⚠ ℹ) et le nombre de ✘.
    /// Sans référence : la crée à partir du FBX actuel.
    /// </summary>
    public static List<string> Verifier(out int problemes)
    {
        problemes = 0;
        List<string> rapport = new List<string>();
        GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(CheminFbx);
        if (modele == null) { rapport.Add($"✘ « {CheminFbx} » introuvable."); problemes++; return rapport; }

        if (!File.Exists(CheminReference))
        {
            EcrireReference();
            rapport.Add($"ℹ Pas encore de référence pour Couloir.fbx : créée à partir du fichier actuel ({CheminReference}).");
            return rapport;
        }

        Reference attendue = JsonUtility.FromJson<Reference>(File.ReadAllText(CheminReference));
        Dictionary<string, Objet> actuels = Lire(modele).objets.ToDictionary(o => o.chemin);
        HashSet<string> connus = new HashSet<string>();

        foreach (Objet avant in attendue.objets)
        {
            connus.Add(avant.chemin);
            if (!actuels.TryGetValue(avant.chemin, out Objet apres))
            {
                problemes++;
                rapport.Add($"✘ « {avant.chemin} » a disparu du FBX (renommé ou supprimé dans Blender ?).");
                continue;
            }

            List<string> changements = new List<string>();
            if (Vector3.Distance(avant.position, apres.position) > ToleranceDistance)
                changements.Add($"position {avant.position:F3} → {apres.position:F3}");
            if (Quaternion.Angle(avant.rotation, apres.rotation) > ToleranceAngle)
                changements.Add($"rotation {avant.rotation.eulerAngles:F1} → {apres.rotation.eulerAngles:F1}");
            if (EchelleDifferente(avant.echelle, apres.echelle))
                changements.Add($"échelle {avant.echelle:F4} → {apres.echelle:F4}");
            if (changements.Count > 0)
            {
                problemes++;
                rapport.Add($"✘ « {avant.chemin} » : " + string.Join(", ", changements));
                continue;
            }

            if (avant.sommets >= 0 && (Vector3.Distance(avant.centre, apres.centre) > ToleranceDistance ||
                                       Vector3.Distance(avant.taille, apres.taille) > ToleranceDistance))
                rapport.Add($"⚠ « {avant.chemin} » : forme modifiée (taille {avant.taille:F3} → {apres.taille:F3}). Normal si c'est voulu.");
        }

        List<string> nouveaux = actuels.Keys.Where(c => !connus.Contains(c)).ToList();
        if (nouveaux.Count > 0) rapport.Add($"ℹ {nouveaux.Count} nouvel(s) objet(s) : " + string.Join(", ", nouveaux));

        if (problemes == 0) rapport.Insert(0, $"✔ Couloir.fbx conforme à la référence ({attendue.objets.Count} objets vérifiés).");
        return rapport;
    }

    static bool EchelleDifferente(Vector3 a, Vector3 b)
    {
        for (int i = 0; i < 3; i++)
        {
            float reference = Mathf.Max(Mathf.Abs(a[i]), 1e-6f);
            if (Mathf.Abs(a[i] - b[i]) / reference > ToleranceEchelle) return true;
        }
        return false;
    }
}
