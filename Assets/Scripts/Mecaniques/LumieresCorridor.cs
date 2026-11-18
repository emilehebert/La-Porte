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
///
/// Fonctionne tout le temps, peu importe l'étape (aucune condition dans le document).
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

    public Color couleurBlanche = new Color(1f, 0.97f, 0.89f);

    [Header("Zone éclairée qui suit la poignée")]
    public float intensiteMax = 1f;

    [Tooltip("Combien de lumières sont éclairées autour de la position de la poignée.")]
    public float largeurDegrade = 1.5f;

    [Tooltip("Vitesse à laquelle la zone éclairée glisse vers la position de la poignée,\n" +
             "et à laquelle le corridor passe de « tout allumé » à « zone qui suit la poignée » (interrupteur).")]
    public float vitesse = 4f;

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

    float position;        // position actuelle de la zone éclairée, en « numéro de lumière » (0 = la plus proche de la caméra)
    float suitLaPoignee;   // 0 = tout le corridor allumé (interrupteur fermé), 1 = seulement la zone de la poignée
    float[] debutScintillement;   // Time.time du début du scintillement de chaque lumière
    float dernierAnglePoignee;


    void OnEnable()  { Capteurs.Interrupteur += QuandInterrupteur; }
    void OnDisable() { Capteurs.Interrupteur -= QuandInterrupteur; }

    void Start()
    {
        TrierDeLaCameraVersLeFond();
        position = PositionVisee();   // pas de glissement au démarrage
        suitLaPoignee = Capteurs.InterrupteurAllume ? 1f : 0f;
        dernierAnglePoignee = Capteurs.AnglePoignee;

        debutScintillement = new float[lumieres.Length];
        for (int i = 0; i < debutScintillement.Length; i++) debutScintillement[i] = -100f;

        MettreAJourCouleurs();
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
        suitLaPoignee = Mathf.Lerp(suitLaPoignee, Capteurs.InterrupteurAllume ? 1f : 0f, lissage);

        VerifierPoignee();

        for (int i = 0; i < lumieres.Length; i++)
        {
            float distance = Mathf.Abs(i - position);
            float force = Mathf.Clamp01(1f - distance / largeurDegrade);
            force = Mathf.SmoothStep(0f, 1f, force);   // bords plus doux

            // Interrupteur fermé : toutes à fond (sauf celles qui scintillent). Allumé : seulement la zone de la poignée.
            lumieres[i].intensity = Mathf.Lerp(Scintillement(i), force, suitLaPoignee) * intensiteMax;
        }
    }

    /// <summary>Interrupteur fermé + la poignée a tourné : les lumières à sa position scintillent.</summary>
    void VerifierPoignee()
    {
        float angle = Capteurs.AnglePoignee;
        if (Mathf.Abs(angle - dernierAnglePoignee) <= seuilMouvementPoignee) return;   // ignore le petit bruit du capteur
        dernierAnglePoignee = angle;

        if (!scintillerSiFerme || Capteurs.InterrupteurAllume || debutScintillement == null) return;

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
        if (debutScintillement == null || i >= debutScintillement.Length) return 1f;   // ex. : script recompilé pendant le Play

        float temps = Time.time - debutScintillement[i];
        return temps < DureeScintillement ? courbeScintillement.Evaluate(temps) : 1f;
    }

    void QuandInterrupteur(bool allume)
    {
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

    void MettreAJourCouleurs()
    {
        foreach (Light lumiere in lumieres)
        {
            lumiere.color = couleurBlanche;
        }

        if (!Capteurs.InterrupteurAllume) return;

        ReglagesJeu reglages = ReglagesJeu.Instance;
        if (reglages == null)
        {
            Debug.LogError("[LumieresCorridor] Il manque un ReglagesJeu dans la scène.");
            return;
        }

        for (int i = 0; i < lumieresDuCode.Length && i < reglages.ordreCouleurs.Length; i++)
        {
            lumieresDuCode[i].color = reglages.Couleur(reglages.ordreCouleurs[i]);
        }
    }
}
