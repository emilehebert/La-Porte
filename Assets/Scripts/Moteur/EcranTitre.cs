using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// L'écran titre, sur LES DEUX écrans en même temps (Display 1 = la pièce, Display 2 = le corridor) :
/// fond noir, le logo qui bat lentement comme un cœur (boum-boum... boum-boum...) et un petit texte.
///
/// Créé par l'étape « 0 - Attente des joueurs » (EtapeAttente) : on n'a rien à mettre dans la scène.
/// Quand l'expérience démarre, Disparaitre() fait un fondu au noir → scène, puis l'objet se détruit tout seul.
///
/// Tant qu'il existe (fondu compris), les capteurs sont EN PAUSE (Capteurs.EnPause) : rien ne change dans le décor
/// et aucun son d'objet ne joue. Les joueurs découvrent donc la scène dans son état de départ.
/// </summary>
public class EcranTitre : MonoBehaviour
{
    /// <summary>Les réglages, remplis par EtapeAttente (c'est là qu'on les change, dans l'Inspector).</summary>
    [System.Serializable]
    public class Reglages
    {
        [Tooltip("Le logo (PNG avec transparence de préférence).\n" +
                 "Vide = Assets/Resources/Logo.png s'il existe, sinon le texte « LA PORTE ».")]
        public Texture2D logo;

        [Tooltip("Hauteur du logo, en pourcentage de la hauteur de l'écran.")]
        [Range(5f, 90f)] public float tailleLogo = 45f;

        [TextArea(2, 4)]
        public string texte = "Appuyez sur l'interrupteur et interagissez avec la sonnette en même temps pour commencer l'expérience";

        [Tooltip("Taille du texte (pour un écran 1080p).")]
        public int tailleTexte = 34;

        public Color couleurTexte = new Color(0.75f, 0.75f, 0.75f, 1f);

        [Header("Battement de cœur")]
        [Tooltip("Battements par minute. 40 = lent et inquiétant, 60 = cœur au repos.")]
        [Range(20f, 120f)] public float battementsParMinute = 40f;

        [Tooltip("De combien le logo grossit à chaque battement (0,08 = 8 %).")]
        [Range(0f, 0.4f)] public float amplitude = 0.08f;

        [Tooltip("Coché : double battement « boum-boum » comme un vrai cœur. Décoché : un seul gonflement doux.")]
        public bool doubleBattement = true;

        [Header("Fondu au démarrage")]
        [Tooltip("Durée du fondu (secondes) entre l'écran titre et la scène, sur les deux écrans.")]
        public float dureeFondu = 2.5f;
    }

    /// <summary>true tant que l'écran titre est visible (même pendant le fondu).</summary>
    public static bool EstAffiche { get; private set; }

    Reglages reglages;
    readonly CanvasGroup[] groupes = new CanvasGroup[2];
    readonly RectTransform[] logos = new RectTransform[2];

    float coup;             // petit battement en plus quand un des deux joueurs fait sa part (0 à 1)
    float debutFondu = -1f;


    // ---------- Utilisé par EtapeAttente ----------

    public static EcranTitre Creer(Reglages reglages)
    {
        var objet = new GameObject("Écran titre (créé par 0 - Attente des joueurs)");
        var ecran = objet.AddComponent<EcranTitre>();
        ecran.reglages = reglages;
        ecran.Construire();
        return ecran;
    }

    /// <summary>Un battement plus fort, tout de suite : « ça a compté, il manque l'autre joueur ».</summary>
    public void Coup()
    {
        coup = 1f;
    }

    /// <summary>Fondu vers la scène sur les deux écrans, puis l'écran titre se détruit.</summary>
    public void Disparaitre()
    {
        if (debutFondu < 0f) debutFondu = Time.unscaledTime;
    }


    // ---------- Fonctionnement interne ----------

    void Construire()
    {
        EstAffiche = true;
        Capteurs.MettreEnPause(true);   // les objets ne réagissent plus jusqu'à la fin du fondu

        Texture2D logo = reglages.logo != null ? reglages.logo : Resources.Load<Texture2D>("Logo");
        Font police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Display 1 (la pièce, visiteur) et Display 2 (le corridor, locataire).
        for (int ecran = 0; ecran < 2; ecran++)
        {
            var canvasObjet = new GameObject(ecran == 0 ? "Display 1 (pièce)" : "Display 2 (corridor)");
            canvasObjet.transform.SetParent(transform, false);

            var canvas = canvasObjet.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = ecran;
            canvas.sortingOrder = 1000;   // par-dessus tout le reste

            var echelle = canvasObjet.AddComponent<CanvasScaler>();
            echelle.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            echelle.referenceResolution = new Vector2(1920f, 1080f);
            echelle.matchWidthOrHeight = 1f;   // tout suit la hauteur de l'écran

            groupes[ecran] = canvasObjet.AddComponent<CanvasGroup>();
            groupes[ecran].blocksRaycasts = false;
            groupes[ecran].interactable = false;

            // Fond noir sur tout l'écran.
            var fond = CreerEnfant<Image>("Fond noir", canvasObjet.transform);
            fond.color = Color.black;
            fond.raycastTarget = false;
            Etirer(fond.rectTransform);

            // Le logo, au centre (un peu au-dessus).
            float hauteur = 1080f * reglages.tailleLogo / 100f;
            if (logo != null)
            {
                var image = CreerEnfant<RawImage>("Logo", canvasObjet.transform);
                image.texture = logo;
                image.raycastTarget = false;
                float ratio = logo.height > 0 ? (float)logo.width / logo.height : 1f;
                image.rectTransform.sizeDelta = new Vector2(hauteur * ratio, hauteur);
                logos[ecran] = image.rectTransform;
            }
            else
            {
                var titre = CreerTexte("Logo (texte)", canvasObjet.transform, police, "LA PORTE",
                                       Mathf.RoundToInt(hauteur * 0.45f), Color.white);
                titre.rectTransform.sizeDelta = new Vector2(1800f, hauteur);
                logos[ecran] = titre.rectTransform;
            }
            logos[ecran].anchoredPosition = new Vector2(0f, 60f);

            // Le petit texte, en bas.
            var texte = CreerTexte("Texte", canvasObjet.transform, police, reglages.texte,
                                   reglages.tailleTexte, reglages.couleurTexte);
            RectTransform zone = texte.rectTransform;
            zone.anchorMin = new Vector2(0.5f, 0f);
            zone.anchorMax = new Vector2(0.5f, 0f);
            zone.pivot = new Vector2(0.5f, 0f);
            zone.sizeDelta = new Vector2(1400f, 200f);
            zone.anchoredPosition = new Vector2(0f, 90f);
        }
    }

    void Update()
    {
        // Le battement : la taille du logo suit le rythme d'un cœur.
        float periode = 60f / Mathf.Max(1f, reglages.battementsParMinute);
        float phase = Mathf.Repeat(Time.unscaledTime, periode) / periode;   // de 0 à 1, une fois par battement

        coup = Mathf.MoveTowards(coup, 0f, Time.unscaledDeltaTime / 0.5f);
        float taille = 1f + reglages.amplitude * (Battement(phase) + 0.8f * Bosse(1f - coup, 0.3f, 0.25f));

        foreach (RectTransform logo in logos)
        {
            if (logo != null) logo.localScale = new Vector3(taille, taille, 1f);
        }

        // Le fondu vers la scène.
        if (debutFondu >= 0f)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - debutFondu) / Mathf.Max(0.01f, reglages.dureeFondu));
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            foreach (CanvasGroup groupe in groupes) groupe.alpha = alpha;

            if (t >= 1f) Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Fin du fondu (la scène est entièrement visible) ou scène rechargée : les objets réagissent de nouveau.
        EstAffiche = false;
        Capteurs.MettreEnPause(false);
    }

    /// <summary>De 0 à 1 selon la phase du battement (0 à 1).</summary>
    float Battement(float phase)
    {
        if (!reglages.doubleBattement)
        {
            // Un seul gonflement doux, comme une respiration.
            return 0.5f - 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
        }

        // « Boum-boum » : un gros battement, un petit juste après, puis une longue pause.
        return Bosse(phase, 0.10f, 0.055f) + 0.6f * Bosse(phase, 0.27f, 0.06f);
    }

    /// <summary>Une bosse en cloche centrée sur « centre », de largeur « largeur ».</summary>
    static float Bosse(float x, float centre, float largeur)
    {
        float d = (x - centre) / largeur;
        return Mathf.Exp(-d * d);
    }

    static T CreerEnfant<T>(string nom, Transform parent) where T : Component
    {
        var objet = new GameObject(nom, typeof(RectTransform));
        objet.transform.SetParent(parent, false);
        return objet.AddComponent<T>();
    }

    static Text CreerTexte(string nom, Transform parent, Font police, string contenu, int taille, Color couleur)
    {
        var texte = CreerEnfant<Text>(nom, parent);
        texte.font = police;
        texte.text = contenu;
        texte.fontSize = taille;
        texte.color = couleur;
        texte.alignment = TextAnchor.MiddleCenter;
        texte.horizontalOverflow = HorizontalWrapMode.Wrap;
        texte.verticalOverflow = VerticalWrapMode.Overflow;
        texte.raycastTarget = false;
        return texte;
    }

    static void Etirer(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
