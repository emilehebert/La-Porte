using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HauntedPSX.RenderPipelines.PSX.Editor;

/// <summary>
/// Menu « La Porte → Ajouter le reflet sur la clé ».
///
/// Crée l'objet « Reflet (Key) » sur le dessus de la clé, avec le script RefletIndice (qui suit la clé),
/// et le matériau Assets/Materials/Reflet.mat (PSX non éclairé, additif : le noir est invisible).
///
/// Marche aussi pour un autre objet : le SÉLECTIONNER dans la Hierarchy avant de lancer le menu.
/// Annulable avec Ctrl+Z. Ensuite : déplacer l'objet du reflet à l'endroit voulu sur la clé, puis Ctrl+S.
/// </summary>
public static class RefletLaPorte
{
    const string NomCle = "Key";
    const string CheminMateriau = "Assets/Materials/Reflet.mat";

    [MenuItem("La Porte/Ajouter le reflet sur la clé")]
    static void Ajouter()
    {
        GameObject cible = Selection.activeGameObject;
        if (cible == null || cible.GetComponentInChildren<Renderer>() == null || cible.GetComponent<RefletIndice>() != null)
        {
            cible = TrouverDansLaScene(NomCle);
        }

        if (cible == null)
        {
            EditorUtility.DisplayDialog("La Porte",
                $"Aucun objet « {NomCle} » dans la scène, et rien de sélectionné.\n" +
                "Sélectionnez l'objet qui doit briller dans la Hierarchy, puis refaites le menu.", "OK");
            return;
        }

        foreach (RefletIndice existant in Object.FindObjectsByType<RefletIndice>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existant.suivre == cible.transform)
            {
                Selection.activeGameObject = existant.gameObject;
                EditorUtility.DisplayDialog("La Porte",
                    $"« {cible.name} » a déjà un reflet : « {existant.name} » (sélectionné). Rien n'a été changé.", "OK");
                return;
            }
        }

        Bounds bornes = Bornes(cible);

        Undo.SetCurrentGroupName("Ajouter le reflet");
        int groupeUndo = Undo.GetCurrentGroup();

        var objet = new GameObject($"Reflet ({cible.name})");
        Undo.RegisterCreatedObjectUndo(objet, "Créer le reflet");
        objet.transform.position = new Vector3(bornes.center.x, bornes.max.y, bornes.center.z);   // sur le dessus

        RefletIndice reflet = objet.AddComponent<RefletIndice>();
        reflet.suivre = cible.transform;
        reflet.materiau = Materiau();

        Undo.CollapseUndoOperations(groupeUndo);
        Selection.activeGameObject = objet;
        EditorSceneManager.MarkSceneDirty(objet.scene);

        Debug.Log($"[Reflet] ✔ « {objet.name} » créé sur « {cible.name} ». " +
                  "Déplacez-le à l'endroit voulu sur l'objet (l'étoile jaune), réglez-le dans l'Inspector, puis Ctrl+S.", objet);
    }

    static GameObject TrouverDansLaScene(string nom)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == nom && t.gameObject.scene.IsValid()) return t.gameObject;
        }
        return null;
    }

    static Bounds Bornes(GameObject objet)
    {
        Renderer[] rendus = objet.GetComponentsInChildren<Renderer>();
        if (rendus.Length == 0) return new Bounds(objet.transform.position, Vector3.one * 0.1f);

        Bounds bornes = rendus[0].bounds;
        foreach (Renderer rendu in rendus) bornes.Encapsulate(rendu.bounds);
        return bornes;
    }

    /// <summary>Matériau PSX non éclairé, transparent ADDITIF. S'il existe déjà, il est réutilisé tel quel.</summary>
    static Material Materiau()
    {
        Material materiau = AssetDatabase.LoadAssetAtPath<Material>(CheminMateriau);
        if (materiau != null) return materiau;

        materiau = new Material(Shader.Find("PSX/PSXLit"));
        materiau.SetColor("_MainColor", Color.white);
        materiau.SetFloat("_LightingMode", (float)PSXMaterialUtils.LightingMode.Unlit);
        materiau.SetFloat("_Surface", (float)PSXMaterialUtils.SurfaceType.Transparent);
        materiau.SetFloat("_Blend", (float)PSXMaterialUtils.BlendMode.Additive);
        materiau.SetFloat("_VertexColorMode", (float)PSXMaterialUtils.VertexColorMode.Disabled);
        materiau.SetFloat("_FogWeight", 0f);
        materiau.SetFloat("_AffineTextureWarpingWeight", 0f);
        PSXMaterialUtils.SetMaterialKeywords(materiau);

        AssetDatabase.CreateAsset(materiau, CheminMateriau);
        AssetDatabase.SaveAssets();
        return materiau;
    }
}
