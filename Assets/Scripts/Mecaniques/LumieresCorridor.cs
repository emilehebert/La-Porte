using UnityEngine;

/// <summary>
/// Les lumières au plafond du corridor (l'écran du LOCATAIRE).
///
///  - Interrupteur fermé : TOUT le corridor est allumé, en blanc.
///    Tourner la poignée fait SCINTILLER les lumières qui correspondent à sa position
///    (même scintillement que la lampe sur pied) : ça montre au locataire que la poignée fait quelque chose.
///  - Interrupteur allumé : un filtre de couleur révèle l'ordre de l'énigme (réglé dans ReglagesJeu),
///    et seule une zone est éclairée, qui suit la poignée :
///      poignée au repos : la lumière la plus proche de la caméra du corridor;
///      poignée tournée (vers le haut OU vers le bas) : la zone avance vers le fond du corridor.
///    Aperçu : au moment où on allume l'interrupteur, les couleurs restent allumées un moment au fond,
///    scintillent, puis s'éteignent. Le locataire voit qu'il y a des couleurs à découvrir avec la poignée.
///  - À partir de l'étape des têtes (ToutAllumer) : l'énigme des couleurs est finie,
///    TOUT le corridor revient à 1, en blanc, sans scintillement, peu importe l'interrupteur et la poignée
///    (il faut bien voir les têtes). Branché sur « 2 - Indices » : Au Debut et Etat Final → ToutAllumer.
///
/// À mettre sur l'objet « Lights ».
/// </summary>
public class LumieresCorridor : MonoBehaviour
{
    [Tooltip("Toutes les lumières du plafond. L'ordre n'a pas d'importance si la caméra du corridor est assignée :\n" +
             "elles sont triées de la plus proche à la plus loin de la caméra.")]
    public Light[] lumieres;

    [Tooltip("La caméra du corridor. Au repos, c'est la lumière la plus proche d'elle qui est allumée.\n" +
             "Vide = la première lumière de la liste est le point de départ.")]
    public Transform cameraCorridor;

    [Tooltip("Les lumières qui montrent l'ordre des couleurs : 1re couleur, 2e couleur, etc.")]
    public Light[] lumieresDuCode;

    [Tooltip("Le « blanc » des lumières du corridor (interrupteur fermé, poignée au repos, étape des têtes) : un blanc chaud.\n" +
             "Les plafonniers, eux, s'affichent en blanc pur avec cette couleur (voir PlafonnierLumiere).")]
    public Color couleurBlanche = new Color(1f, 0.97f, 0.89f);

    [Header("Zone éclairée qui suit la poignée")]
    public float intensiteMax = 1f;

    [Tooltip("L'intensité de TOUTES les lumières à partir de l'étape des têtes (ToutAllumer).")]
    public float intensiteToutAllume = 1f;

    [Tooltip("Combien de lumières sont éclairées autour de la position de la poignée.")]
    public float largeurDegrade = 1.5f;

    [Tooltip("Vitesse à laquelle la zone éclairée glisse vers la position de la poignée.")]
    public float vitesse = 4f;

    [Tooltip("Durée (secondes) du passage « tout allumé » ↔ « zone qui suit la poignée » quand on bascule l'interrupteur.\n" +
             "Petit = franc, comme un vrai interrupteur. 0 = instantané.")]
    public float dureeBasculeInterrupteur = 0.12f;

    [Tooltip("Durée (secondes) du dégradé du blanc aux couleurs du code (et des couleurs au blanc)\n" +
             "quand on bascule l'interrupteur. 0 = instantané.")]
    public float dureeTransitionCouleurs = 0.6f;

    [Header("Scintillement (interrupteur fermé)")]
    [Tooltip("Interrupteur fermé : tourner la poignée fait scintiller les lumières à sa position.")]
    public bool scintillerSiFerme = true;

    [Tooltip("De combien la poignée doit tourner (de -1 à 1) pour faire scintiller. Augmenter si le capteur tremble.")]
    public float seuilMouvementPoignee = 0.02f;

    [Tooltip("L'intensité pendant un scintillement (1 = normale), dans le temps.\n" +
             "Par défaut : la même courbe que la lampe sur pied (animation Lamp_interact, 0,5 s).")]
    public AnimationCurve courbeScintillement = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.033f, 0.51f, -13.4f, -13.4f),
        new Keyframe(0.05f, 0.33f, -1.2f, -1.2f),
        new Keyframe(0.1f, 0.31f),
        new Keyframe(0.117f, 0.54f),
        new Keyframe(0.133f, 0f),
        new Keyframe(0.183f, 0.54f),
        new Keyframe(0.217f, 0.24f),
        new Keyframe(0.233f, 0.59f),
        new Keyframe(0.333f, 0.05f),
        new Keyframe(0.35f, 0.46f, 12.4f, 12.4f),
        new Keyframe(0.383f, 0.67f, 3.6f, 3.6f),
        new Keyframe(0.5f, 1f));

    [Header("Aperçu des couleurs (quand on allume l'interrupteur)")]
    [Tooltip("En allumant l'interrupteur, les lumières de couleur restent allumées ce temps (secondes),\n" +
             "puis scintillent et s'éteignent : on voit qu'il y a des couleurs à découvrir avec la poignée. 0 = pas d'aperçu.")]
    public float dureeApercu = 1.2f;

    [Tooltip("Durée du scintillement à la fin de l'aperçu (secondes) : de plus en plus faible, jusqu'à s'éteindre.")]
    public float dureeScintillementApercu = 1f;

    float position;        // position actuelle de la zone éclairée, en « numéro de lumière » (0 = la plus proche de la caméra)
    float debutApercu = -100f;   // Time.time où l'interrupteur a été allumé (début de l'aperçu des couleurs)
    bool[] estCouleur;           // pour chaque lumière (après le tri) : est-ce une lumière du code?
    float suitLaPoignee;   // 0 = tout le corridor allumé (interrupteur fermé), 1 = seulement la zone de la poignée
    float[] debutScintillement;   // Time.time du début du scintillement de chaque lumière
    float dernierAnglePoignee;
    bool toutAllume;       // vrai à partir de l'étape des têtes (ToutAllumer)
    float intensiteActuelle;   // glisse de intensiteMax vers intensiteToutAllume à l'étape des têtes
    Color[] couleurDepart;     // la couleur de chaque lumière au début du dégradé
    Color[] couleurCible;      // la couleur vers laquelle elle va (blanc, ou sa couleur du code)
    float debutDegrade = -100f;   // Time.time du début du dégradé de couleurs

    /// <summary>Vrai à partir de l'étape des têtes : tout le corridor est allumé en blanc, quoi qu'il arrive.</summary>
    public bool ToutEstAllume => toutAllume;


    /// <summary>
    /// L'énigme des couleurs est finie : tout le corridor revient à 1, en blanc, et y reste
    /// (jusqu'au rechargement de la scène). Appelé par l'étape des têtes (Au Debut et Etat Final).
    /// </summary>
    public void ToutAllumer()
    {
        toutAllume = true;
        MettreAJourCouleurs();
    }


    void OnEnable()  { Capteurs.Interrupteur += QuandInterrupteur; }
    void OnDisable() { Capteurs.Interrupteur -= QuandInterrupteur; }

    void Start()
    {
        TrierDeLaCameraVersLeFond();
        position = PositionVisee();   // pas de glissement au démarrage
        suitLaPoignee = Capteurs.InterrupteurAllume && !toutAllume ? 1f : 0f;
        intensiteActuelle = toutAllume ? intensiteToutAllume : intensiteMax;
        dernierAnglePoignee = Capteurs.AnglePoignee;

        debutScintillement = new float[lumieres.Length];
        for (int i = 0; i < debutScintillement.Length; i++) debutScintillement[i] = -100f;

        estCouleur = new bool[lumieres.Length];
        for (int i = 0; i < lumieres.Length; i++) estCouleur[i] = System.Array.IndexOf(lumieresDuCode, lumieres[i]) >= 0;

        MettreAJourCouleurs(instantane: true);   // au départ : directement les bonnes couleurs
    }

    /// <summary>Remet la liste dans l'ordre : la lumière la plus proche de la caméra en premier.</summary>
    void TrierDeLaCameraVersLeFond()
    {
        if (cameraCorridor == null) return;

        Vector3 camera = cameraCorridor.position;
        System.Array.Sort(lumieres, (a, b) =>
            Vector3.Distance(a.transform.position, camera).CompareTo(Vector3.Distance(b.transform.position, camera)));
    }

    void Update()
    {
        if (lumieres.Length == 0) return;

        // Glisse doucement vers la position de la poignée, et vers le mode choisi par l'interrupteur
        // (même vitesse peu importe les FPS).
        float lissage = 1f - Mathf.Exp(-vitesse * Time.deltaTime);
        position = Mathf.Lerp(position, PositionVisee(), lissage);
        bool modeEnigme = Capteurs.InterrupteurAllume && !toutAllume;
        float cibleMode = modeEnigme ? 1f : 0f;
        suitLaPoignee = dureeBasculeInterrupteur > 0f
            ? Mathf.MoveTowards(suitLaPoignee, cibleMode, Time.deltaTime / dureeBasculeInterrupteur)
            : cibleMode;
        intensiteActuelle = Mathf.Lerp(intensiteActuelle, toutAllume ? intensiteToutAllume : intensiteMax, lissage);

        VerifierPoignee();
        AppliquerDegradeCouleurs();
        float apercu = Apercu();

        for (int i = 0; i < lumieres.Length; i++)
        {
            float distance = Mathf.Abs(i - position);
            float force = Mathf.Clamp01(1f - distance / largeurDegrade);
            force = Mathf.SmoothStep(0f, 1f, force);   // bords plus doux

            // Interrupteur fermé : toutes à fond (sauf celles qui scintillent). Allumé : seulement la zone de la poignée.
            float intensite = Mathf.Lerp(Scintillement(i), force, suitLaPoignee);

            // Juste après avoir allumé l'interrupteur : les couleurs restent un moment (aperçu).
            if (estCouleur != null && i < estCouleur.Length && estCouleur[i]) intensite = Mathf.Max(intensite, apercu);

            lumieres[i].intensity = intensite * intensiteActuelle;
        }
    }

    /// <summary>
    /// L'intensité (0 à 1) des lumières de couleur pendant l'aperçu :
    /// allumées pendant « dureeApercu », puis elles scintillent de plus en plus faiblement et s'éteignent.
    /// </summary>
    float Apercu()
    {
        float temps = Time.time - debutApercu;
        if (toutAllume || temps < 0f) return 0f;
        if (temps < dureeApercu) return 1f;

        float t = temps - dureeApercu;
        if (t >= dureeScintillementApercu || DureeScintillement <= 0f) return 0f;

        float scintillement = courbeScintillement.Evaluate(t % DureeScintillement);   // le même scintillement que la lampe, en boucle
        float fondu = 1f - t / dureeScintillementApercu;
        return scintillement * fondu;
    }

    /// <summary>Interrupteur fermé + la poignée a tourné : les lumières à sa position scintillent.</summary>
    void VerifierPoignee()
    {
        float angle = Capteurs.AnglePoignee;
        if (Mathf.Abs(angle - dernierAnglePoignee) <= seuilMouvementPoignee) return;   // ignore le petit bruit du capteur
        dernierAnglePoignee = angle;

        if (!scintillerSiFerme || Capteurs.InterrupteurAllume || toutAllume || debutScintillement == null) return;

        // La lumière la plus proche de la position de la poignée (les deux, si elle est entre deux).
        float cible = PositionVisee();
        for (int i = 0; i < lumieres.Length; i++)
        {
            bool correspond = Mathf.Abs(i - cible) < 0.75f;
            bool dejaEnTrain = Time.time - debutScintillement[i] < DureeScintillement;
            if (correspond && !dejaEnTrain) debutScintillement[i] = Time.time;   // pas de redémarrage en boucle : il va jusqu'au bout
        }
    }

    float DureeScintillement => courbeScintillement.length > 0 ? courbeScintillement[courbeScintillement.length - 1].time : 0f;

    /// <summary>L'intensité de cette lumière à cause du scintillement (1 = normale).</summary>
    float Scintillement(int i)
    {
        if (toutAllume) return 1f;   // étape des têtes : pile à 1, plus de scintillement
        if (debutScintillement == null || i >= debutScintillement.Length) return 1f;   // ex. : script recompilé pendant le Play

        float temps = Time.time - debutScintillement[i];
        return temps < DureeScintillement ? courbeScintillement.Evaluate(temps) : 1f;
    }

    void QuandInterrupteur(bool allume)
    {
        // On allume l'interrupteur : aperçu des couleurs avant qu'elles s'éteignent. On le ferme : plus d'aperçu.
        debutApercu = allume && !toutAllume ? Time.time : -100f;
        MettreAJourCouleurs();
    }

    /// <summary>
    /// Poignée → numéro de lumière.
    /// Au repos (0) : la lumière 0, la plus proche de la caméra.
    /// Tournée au maximum, vers le haut (1) OU vers le bas (-1) : la dernière, au fond du corridor.
    /// </summary>
    float PositionVisee()
    {
        float tour = Mathf.Abs(Capteurs.AnglePoignee);   // 0 = repos, 1 = tournée au maximum
        return tour * (lumieres.Length - 1);
    }

    /// <summary>
    /// Choisit la couleur de chaque lumière (blanc, ou sa couleur du code pendant l'énigme)
    /// et lance le dégradé vers ces couleurs. « instantane » : directement, sans dégradé (au démarrage).
    /// </summary>
    void MettreAJourCouleurs(bool instantane = false)
    {
        if (couleurCible == null || couleurCible.Length != lumieres.Length)
        {
            couleurCible = new Color[lumieres.Length];
            couleurDepart = new Color[lumieres.Length];
            instantane = true;
        }

        for (int i = 0; i < lumieres.Length; i++)
        {
            couleurDepart[i] = lumieres[i].color;   // on part de la couleur actuelle (même au milieu d'un dégradé)
            couleurCible[i] = couleurBlanche;
        }

        bool couleursDuCode = Capteurs.InterrupteurAllume && !toutAllume;   // seulement pendant l'énigme
        ReglagesJeu reglages = ReglagesJeu.Instance;
        if (couleursDuCode && reglages == null)
        {
            Debug.LogError("[LumieresCorridor] Il manque un ReglagesJeu dans la scène.");
            couleursDuCode = false;
        }

        if (couleursDuCode)
        {
            for (int k = 0; k < lumieresDuCode.Length && k < reglages.ordreCouleurs.Length; k++)
            {
                Color couleur = reglages.Couleur(reglages.ordreCouleurs[k]);
                int i = System.Array.IndexOf(lumieres, lumieresDuCode[k]);

                if (i >= 0) couleurCible[i] = couleur;
                else if (lumieresDuCode[k] != null) lumieresDuCode[k].color = couleur;   // pas dans « Lumieres » : sans dégradé
            }
        }

        debutDegrade = instantane ? -100f : Time.time;
        AppliquerDegradeCouleurs();
    }

    /// <summary>Le dégradé de couleurs : de la couleur de départ à la couleur cible, en « dureeTransitionCouleurs ».</summary>
    void AppliquerDegradeCouleurs()
    {
        if (couleurCible == null || couleurCible.Length != lumieres.Length) return;

        float t = dureeTransitionCouleurs > 0f ? Mathf.Clamp01((Time.time - debutDegrade) / dureeTransitionCouleurs) : 1f;
        float doux = Mathf.SmoothStep(0f, 1f, t);

        for (int i = 0; i < lumieres.Length; i++)
        {
            lumieres[i].color = Color.Lerp(couleurDepart[i], couleurCible[i], doux);
        }
    }
}
