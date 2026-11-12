using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HauntedPSX.RenderPipelines.PSX.Editor;

/// <summary>
/// Menu « La Porte → Ajouter les étincelles à la prise ».
///
/// Crée l'objet « Étincelles de la prise » devant la prise (« Outlet »), tourné vers la caméra qui la voit :
///   - un Particle System réglé en petite gerbe d'étincelles (qui tombent avec la gravité);
///   - le script EtincellesPrise (la gerbe part à chaque coup de SONNETTE, seulement lampe éteinte);
///   - le matériau Assets/Materials/Etincelles.mat (PSX, additif : les particules par défaut ne s'affichent pas avec ce pipeline).
///
/// Annulable avec Ctrl+Z. Ensuite : ajuster la position / rotation si besoin, puis Ctrl+S.
/// </summary>
public static class EtincellesLaPorte
{
    const string NomObjet = "Étincelles de la prise";
    const string NomPrise = "Outlet";
    const string CheminMateriau = "Assets/Materials/Etincelles.mat";

    [MenuItem("La Porte/Ajouter les étincelles à la prise")]
    static void Ajouter()
    {
        if (Object.FindFirstObjectByType<EtincellesPrise>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("La Porte", "La scène contient déjà des étincelles (EtincellesPrise). Rien n'a été changé.", "OK");
            return;
        }

        string rapport;
        Vector3 position;
        Vector3 direction;

        if (BornesDe(NomPrise, out Bounds prise))
        {
            direction = DirectionVersLaPiece(prise);
            float epaisseur = Vector3.Scale(prise.extents, Abs(direction)).magnitude;
            position = prise.center + direction * (epaisseur + 0.01f);
            rapport = $"✔ Placées devant « {NomPrise} », tournées vers « {CameraQuiVoit(prise.center)?.name ?? "?"} ».";
        }
        else
        {
            Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("La Porte", $"Ni « {NomPrise} » ni caméra trouvée dans la scène. Rien n'a été changé.", "OK");
                return;
            }
            position = camera.transform.position + camera.transform.forward * 2f;
            direction = -camera.transform.forward;
            rapport = $"⚠ « {NomPrise} » introuvable : les étincelles sont devant « {camera.name} ». Déplacez-les sur la prise.";
        }

        Undo.SetCurrentGroupName("Ajouter les étincelles");
        int groupeUndo = Undo.GetCurrentGroup();

        GameObject objet = new GameObject(NomObjet);
        Undo.RegisterCreatedObjectUndo(objet, "Créer les étincelles");
        objet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

        ParticleSystem particules = objet.AddComponent<ParticleSystem>();
        ReglerParticules(particules);

        ParticleSystemRenderer rendu = objet.GetComponent<ParticleSystemRenderer>();
        rendu.renderMode = ParticleSystemRenderMode.Stretch;   // des traits, comme de vraies étincelles
        rendu.lengthScale = 4f;
        rendu.velocityScale = 0f;
        rendu.sharedMaterial = Materiau();
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;

        objet.AddComponent<EtincellesPrise>();

        Undo.CollapseUndoOperations(groupeUndo);
        EditorSceneManager.MarkSceneDirty(objet.scene);
        Selection.activeGameObject = objet;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        EditorUtility.DisplayDialog("La Porte — étincelles ajoutées",
            rapport + "\n\nElles partent à chaque coup de SONNETTE quand la lampe est éteinte (touche S en Play).\n" +
            "Pour les voir sans Play : sélectionner l'objet, puis « Play » dans la petite fenêtre Particle Effect de la scène.\n\n" +
            "Sauvegardez la scène (Ctrl+S).", "OK");
    }


    // ---------- Réglages ----------

    static void ReglerParticules(ParticleSystem particules)
    {
        particules.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule principal = particules.main;
        principal.duration = 0.3f;
        principal.loop = false;
        principal.playOnAwake = false;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        principal.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
        principal.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.85f, 0.4f));
        principal.gravityModifier = 1f;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = 60;

        ParticleSystem.EmissionModule emission = particules.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 20) });   // une seule gerbe de 12 à 20 étincelles

        ParticleSystem.ShapeModule forme = particules.shape;
        forme.shapeType = ParticleSystemShapeType.Cone;   // sort vers l'avant de l'objet (son axe bleu)
        forme.angle = 35f;
        forme.radius = 0.01f;

        Gradient couleurs = new Gradient();
        couleurs.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.4f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f),
            });
        ParticleSystem.ColorOverLifetimeModule couleurDansLeTemps = particules.colorOverLifetime;
        couleurDansLeTemps.enabled = true;
        couleurDansLeTemps.color = new ParticleSystem.MinMaxGradient(couleurs);

        ParticleSystem.SizeOverLifetimeModule tailleDansLeTemps = particules.sizeOverLifetime;
        tailleDansLeTemps.enabled = true;
        tailleDansLeTemps.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
    }

    /// <summary>
    /// Matériau PSX « Unlit », transparent ADDITIF, qui prend la couleur des particules.
    /// S'il existe déjà, il est réutilisé tel quel (vos réglages sont gardés).
    /// </summary>
    static Material Materiau()
    {
        Material materiau = AssetDatabase.LoadAssetAtPath<Material>(CheminMateriau);
        if (materiau != null) return materiau;

        materiau = new Material(Shader.Find("PSX/PSXLit"));
        materiau.SetColor("_MainColor", Color.white);
        materiau.SetFloat("_LightingMode", (float)PSXMaterialUtils.LightingMode.Unlit);
        materiau.SetFloat("_Surface", (float)PSXMaterialUtils.SurfaceType.Transparent);
        materiau.SetFloat("_Blend", (float)PSXMaterialUtils.BlendMode.Additive);
        materiau.SetFloat("_VertexColorMode", (float)PSXMaterialUtils.VertexColorMode.Color);
        PSXMaterialUtils.SetMaterialKeywords(materiau);

        AssetDatabase.CreateAsset(materiau, CheminMateriau);
        AssetDatabase.SaveAssets();
        return materiau;
    }


    // ---------- Outils ----------

    /// <summary>
    /// La direction « hors du mur » : l'axe le plus mince de la prise,
    /// du côté de la caméra qui la voit.
    /// </summary>
    static Vector3 DirectionVersLaPiece(Bounds prise)
    {
        Vector3 taille = prise.size;
        Vector3 axe = Vector3.right;
        if (taille.y < taille.x && taille.y <= taille.z) axe = Vector3.up;
        else if (taille.z < taille.x && taille.z < taille.y) axe = Vector3.forward;

        Camera camera = CameraQuiVoit(prise.center);
        if (camera != null && Vector3.Dot(camera.transform.position - prise.center, axe) < 0f) axe = -axe;
        return axe;
    }

    /// <summary>La caméra active qui a ce point dans son champ, la plus proche (sinon la plus proche tout court).</summary>
    static Camera CameraQuiVoit(Vector3 point)
    {
        Camera meilleure = null;
        float meilleureDistance = float.MaxValue;
        bool meilleureVoit = false;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!camera.enabled || !camera.gameObject.activeInHierarchy) continue;

            Vector3 vue = camera.WorldToViewportPoint(point);
            bool voit = vue.z > 0f && vue.x >= 0f && vue.x <= 1f && vue.y >= 0f && vue.y <= 1f;
            float distance = Vector3.Distance(camera.transform.position, point);

            bool mieux = (voit && !meilleureVoit) || (voit == meilleureVoit && distance < meilleureDistance);
            if (mieux)
            {
                meilleure = camera;
                meilleureDistance = distance;
                meilleureVoit = voit;
            }
        }
        return meilleure;
    }

    /// <summary>Les bornes (dans le monde) de tous les Renderers de l'objet nommé ainsi.</summary>
    static bool BornesDe(string nom, out Bounds bornes)
    {
        bornes = default;

        foreach (Transform objet in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (objet.name != nom) continue;

            bool trouve = false;
            foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>())
            {
                if (!trouve) { bornes = rendu.bounds; trouve = true; }
                else bornes.Encapsulate(rendu.bounds);
            }
            if (trouve) return true;
        }
        return false;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
