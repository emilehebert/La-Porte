using UnityEngine;

/// <summary>
/// Un REFLET qui flashe sur un objet (ex. : la clé), pour attirer l'œil du joueur :
/// une étoile lumineuse qui apparaît d'un coup, tourne un peu, puis s'éteint. Ça recommence SANS FIN.
///
/// Le reflet apparaît là où est CET objet (le déplacer = choisir l'endroit sur la clé).
/// Il est tourné vers la caméra la plus proche et attend pendant l'écran titre.
///
/// Le menu « La Porte → Ajouter le reflet sur la clé » crée l'objet et le matériau.
/// Pour un flash à un moment précis (événement d'une étape, Timeline...) : appeler Flasher().
/// Pour tester : en Play, clic droit sur le composant → « Flasher maintenant ».
/// </summary>
public class RefletIndice : MonoBehaviour
{
    [Tooltip("Décocher pour arrêter les flashs.")]
    public bool actif = true;

    [Tooltip("1 = normal. 2 = deux fois plus rapide. 0,5 = deux fois plus lent.")]
    [Range(0.2f, 5f)] public float vitesse = 1f;

    [Tooltip("Nombre de flashs à la suite. 1 = « tching », 2 = « tching-tching »...")]
    [Range(1, 5)] public int repetitions = 1;

    [Tooltip("Secondes entre deux séries de flashs.")]
    [Min(0.2f)] public float intervalle = 3f;

    [Tooltip("Taille de l'étoile (mètres, d'un bout à l'autre des branches).")]
    [Min(0.01f)] public float tailleEtoile = 0.35f;

    public Color couleur = new Color(1f, 0.97f, 0.85f, 1f);

    [Tooltip("Plus grand = plus éclatant.")]
    [Range(0f, 10f)] public float brillance = 5f;


    // Remplis par le menu : pas besoin d'y toucher.
    [HideInInspector] public Transform suivre;
    [HideInInspector] public Material materiau;


    // Le reste est fixe (changer ici seulement si vraiment nécessaire).
    const float DureeFlash = 0.6f;          // secondes, à vitesse 1
    const float PauseEntreFlashs = 0.12f;   // entre les répétitions, à vitesse 1
    const float DelaiAvantPremier = 1.5f;   // après l'écran titre
    const float Variation = 0.2f;           // ± 20 % de l'intervalle, au hasard, pour que ce soit moins mécanique
    const float MomentDuMaximum = 0.2f;     // s'allume d'un coup, s'éteint doucement
    const float Rotation = 60f;             // degrés pendant un flash
    const float VersLaCamera = 0.06f;       // mètres, pour ne pas être caché dans la clé

    Transform etoile;
    MeshRenderer rendu;
    Camera cameraQuiRegarde;
    Texture2D texture;
    MaterialPropertyBlock proprietes;
    Vector3 decalageSurLObjet;

    // Le rythme, géré dans Update (pas de coroutine : rien ne peut l'arrêter en silence).
    float prochainFlash = -1f;   // Time.time du prochain flash (-1 = pas encore prévu)
    int flashsRestants;          // dans la série en cours
    float progression = -1f;     // 0 → 1 pendant un flash, -1 = pas de flash
    float angleDepart;

    static readonly int IdCouleur = Shader.PropertyToID("_MainColor");
    static readonly int IdTexture = Shader.PropertyToID("_MainTex");


    /// <summary>Un flash tout de suite (pour un événement d'étape ou une Timeline).</summary>
    [ContextMenu("Flasher maintenant")]
    public void Flasher()
    {
        if (progression < 0f) CommencerUnFlash();
    }

    public void Activer(bool oui)
    {
        actif = oui;
    }


    // ---------- Fonctionnement ----------

    void Start()
    {
        if (materiau == null)
        {
            Debug.LogWarning($"[Reflet] « {name} » n'a pas de matériau : refaire le menu « La Porte → Ajouter le reflet sur la clé ».", this);
            enabled = false;
            return;
        }

        if (suivre != null) decalageSurLObjet = suivre.InverseTransformPoint(transform.position);
        Construire();
    }

    void Construire()
    {
        cameraQuiRegarde = CameraLaPlusProche();

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Étoile du reflet";
        Destroy(quad.GetComponent<Collider>());
        etoile = quad.transform;
        etoile.SetParent(transform, false);

        if (texture == null) texture = CreerEtoile(128);
        rendu = quad.GetComponent<MeshRenderer>();
        rendu.sharedMaterial = new Material(materiau) { name = materiau.name + " (reflet)" };
        rendu.sharedMaterial.SetTexture(IdTexture, texture);
        rendu.sharedMaterial.renderQueue = 3100;   // le dernier des objets transparents (PSX : MainTransparentLast)
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;
        rendu.enabled = false;
        proprietes = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (rendu == null) { if (materiau != null) Construire(); return; }   // sécurité

        if (progression >= 0f) AvancerLeFlash();
        else Planifier();
    }

    /// <summary>Décide quand faire le prochain flash. Toujours actif : ça ne s'arrête jamais tout seul.</summary>
    void Planifier()
    {
        if (!actif || Capteurs.EnPause)
        {
            prochainFlash = -1f;   // on repartira avec le petit délai quand ce sera de nouveau possible
            flashsRestants = 0;
            return;
        }

        if (prochainFlash < 0f) prochainFlash = Time.time + DelaiAvantPremier;
        if (Time.time < prochainFlash) return;

        if (flashsRestants <= 0) flashsRestants = Mathf.Max(1, repetitions);
        flashsRestants--;
        CommencerUnFlash();
    }

    void CommencerUnFlash()
    {
        progression = 0f;
        angleDepart = Random.Range(0f, 90f);
        if (cameraQuiRegarde == null || !cameraQuiRegarde.isActiveAndEnabled) cameraQuiRegarde = CameraLaPlusProche();
        rendu.enabled = true;
        AvancerLeFlash();
    }

    void AvancerLeFlash()
    {
        progression += Time.deltaTime * vitesse / DureeFlash;

        if (progression >= 1f)
        {
            progression = -1f;
            rendu.enabled = false;

            // Encore des flashs dans la série : petite pause. Sinon : l'intervalle (un peu au hasard).
            float attente = flashsRestants > 0
                ? PauseEntreFlashs / vitesse
                : intervalle * (1f + Random.Range(-Variation, Variation));
            prochainFlash = Time.time + attente;
            return;
        }

        float t = progression;
        float force = t < MomentDuMaximum
            ? Mathf.SmoothStep(0f, 1f, t / MomentDuMaximum)
            : 1f - Mathf.SmoothStep(0f, 1f, (t - MomentDuMaximum) / (1f - MomentDuMaximum));

        Placer(Mathf.Lerp(0.4f, 1f, force) * tailleEtoile, angleDepart + Rotation * t);
        proprietes.SetColor(IdCouleur, couleur * (brillance * force));
        rendu.SetPropertyBlock(proprietes);
    }

    void LateUpdate()
    {
        if (suivre != null) transform.position = suivre.TransformPoint(decalageSurLObjet);
    }

    /// <summary>Met l'étoile à sa place, tournée vers la caméra, à la bonne taille.</summary>
    void Placer(float tailleActuelle, float angle)
    {
        Vector3 position = transform.position;
        Quaternion orientation = Quaternion.identity;

        if (cameraQuiRegarde != null)
        {
            Vector3 versCamera = (cameraQuiRegarde.transform.position - position).normalized;
            position += versCamera * VersLaCamera;
            orientation = Quaternion.LookRotation(-versCamera, cameraQuiRegarde.transform.up);
        }

        etoile.SetPositionAndRotation(position, orientation * Quaternion.Euler(0f, 0f, angle));
        Vector3 echelleParent = transform.lossyScale;
        etoile.localScale = new Vector3(tailleActuelle / Mathf.Max(0.0001f, echelleParent.x),
                                        tailleActuelle / Mathf.Max(0.0001f, echelleParent.y), 1f);
    }

    Camera CameraLaPlusProche()
    {
        Camera meilleure = null;
        float distance = float.MaxValue;
        foreach (Camera camera in Camera.allCameras)
        {
            float d = (camera.transform.position - transform.position).sqrMagnitude;
            if (d < distance) { distance = d; meilleure = camera; }
        }
        return meilleure != null ? meilleure : Camera.main;
    }

    /// <summary>L'image de l'étoile (4 grandes branches, 4 petites en X, un gros cœur et un halo), dessinée par le code.</summary>
    static Texture2D CreerEtoile(int resolution)
    {
        var image = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Étoile du reflet",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        const float finesse = 0.06f;
        var pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + 0.5f) / resolution * 2f - 1f;
                float v = (y + 0.5f) / resolution * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = (u + v) * 0.7071f, b = (u - v) * 0.7071f;

                float valeur = Branche(u, v, finesse, 1f) + Branche(v, u, finesse, 1f)
                             + 0.6f * (Branche(a, b, finesse, 0.6f) + Branche(b, a, finesse, 0.6f))
                             + 1.5f * Mathf.Exp(-(r / 0.16f) * (r / 0.16f))     // gros cœur blanc
                             + 0.35f * Mathf.Exp(-(r / 0.4f) * (r / 0.4f));     // halo
                valeur = Mathf.Clamp01(valeur) * (1f - Mathf.SmoothStep(0.9f, 1f, r));

                pixels[y * resolution + x] = new Color(valeur, valeur, valeur, valeur);
            }
        }

        image.SetPixels(pixels);
        image.Apply(false, true);
        return image;
    }

    static float Branche(float le_long, float travers, float finesse, float longueur)
    {
        float bout = Mathf.Max(0f, 1f - Mathf.Abs(le_long) / longueur);
        return Mathf.Exp(-Mathf.Abs(travers) / finesse) * Mathf.Sqrt(bout);
    }

    void OnDestroy()
    {
        if (texture != null) Destroy(texture);
        if (rendu != null && rendu.sharedMaterial != null) Destroy(rendu.sharedMaterial);
    }

    // Dans la vue Scene : une étoile jaune montre où le reflet apparaît, à sa taille.
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.9f);
        Vector3 p = transform.position;
        float d = tailleEtoile * 0.5f;
        Gizmos.DrawLine(p - Vector3.right * d, p + Vector3.right * d);
        Gizmos.DrawLine(p - Vector3.up * d, p + Vector3.up * d);
        Gizmos.DrawLine(p - Vector3.forward * d, p + Vector3.forward * d);
    }
}
