using UnityEngine;

/// <summary>
/// Les lumières au plafond du corridor (l'écran du LOCATAIRE).
///
///  - Poignée au repos : la lumière la plus proche de la caméra du corridor est allumée.
///    Poignée tournée (vers le haut OU vers le bas) : la zone éclairée avance vers le fond du corridor.
///  - Interrupteur fermé : lumières blanches.
///    Interrupteur allumé : un filtre de couleur révèle l'ordre de l'énigme (réglé dans ReglagesJeu).
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

    [Tooltip("Vitesse à laquelle la zone éclairée glisse vers la position de la poignée.")]
    public float vitesse = 4f;

    float position;   // position actuelle de la zone éclairée, en « numéro de lumière » (0 = la plus proche de la caméra)


    void OnEnable()  { Capteurs.Interrupteur += QuandInterrupteur; }
    void OnDisable() { Capteurs.Interrupteur -= QuandInterrupteur; }

    void Start()
    {
        TrierDeLaCameraVersLeFond();
        position = PositionVisee();   // pas de glissement au démarrage
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
