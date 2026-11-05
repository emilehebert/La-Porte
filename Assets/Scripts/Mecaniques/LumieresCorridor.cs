using UnityEngine;

/// <summary>
/// Les lumières au plafond du corridor (l'écran du LOCATAIRE).
///
///  - Poignée : déplace la zone éclairée, de la porte jusqu'au fond du corridor.
///  - Interrupteur fermé : lumières blanches.
///    Interrupteur allumé : un filtre de couleur révèle l'ordre de l'énigme (réglé dans ReglagesJeu).
///
/// Fonctionne tout le temps, peu importe l'étape (aucune condition dans le document).
/// À mettre sur l'objet « Lights ».
/// </summary>
public class LumieresCorridor : MonoBehaviour
{
    [Tooltip("Toutes les lumières du plafond, de la porte vers le fond du corridor.")]
    public Light[] lumieres;

    [Tooltip("Les lumières qui montrent l'ordre des couleurs : 1re couleur, 2e couleur, etc.")]
    public Light[] lumieresDuCode;

    public Color couleurBlanche = new Color(1f, 0.97f, 0.89f);

    [Header("Zone éclairée qui suit la poignée")]
    public float intensiteMax = 1f;

    [Tooltip("Combien de lumières sont éclairées autour de la position de la poignée.")]
    public float largeurDegrade = 1.5f;

    [Tooltip("Vitesse à laquelle la zone éclairée glisse vers la position de la poignée.")]
    public float vitesse = 4f;

    [Tooltip("Cocher si la poignée éclaire le mauvais bout du corridor.")]
    public bool inverserSens = false;

    float position;   // position actuelle de la zone éclairée, en « numéro de lumière »


    void OnEnable()  { Capteurs.Interrupteur += QuandInterrupteur; }
    void OnDisable() { Capteurs.Interrupteur -= QuandInterrupteur; }

    void Start()
    {
        position = PositionVisee();   // pas de glissement au démarrage
        MettreAJourCouleurs();
    }

    void Update()
    {
        if (lumieres.Length == 0) return;

        // Glisse doucement vers la position de la poignée (même vitesse peu importe les FPS).
        position = Mathf.Lerp(position, PositionVisee(), 1f - Mathf.Exp(-vitesse * Time.deltaTime));

        for (int i = 0; i < lumieres.Length; i++)
        {
            float distance = Mathf.Abs(i - position);
            float force = Mathf.Clamp01(1f - distance / largeurDegrade);
            force = Mathf.SmoothStep(0f, 1f, force);   // bords plus doux

            lumieres[i].intensity = force * intensiteMax;
        }
    }

    void QuandInterrupteur(bool allume)
    {
        MettreAJourCouleurs();
    }

    /// <summary>Poignée (-1 à 1) → numéro de lumière (0 à la dernière).</summary>
    float PositionVisee()
    {
        float angle = inverserSens ? -Capteurs.AnglePoignee : Capteurs.AnglePoignee;
        float de0a1 = (angle + 1f) * 0.5f;
        return de0a1 * (lumieres.Length - 1);
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
