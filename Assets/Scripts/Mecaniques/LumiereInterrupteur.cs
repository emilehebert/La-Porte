using UnityEngine;

/// <summary>
/// Une lumière qui suit l'interrupteur (ex. : la lumière du ventilateur dans la chambre).
///
///  - Normal  : interrupteur allumé = lumière allumée.
///  - Inversé : interrupteur fermé = lumière allumée (donc allumée au début, éteinte quand on lève l'interrupteur).
///
/// L'ampoule (le matériau émissif) s'allume et s'éteint avec la lumière, en fondu (pas d'un coup).
///
/// PENDANT l'étape des têtes seulement (ToutAllumer) : la lumière se rallume et reste allumée peu importe
/// l'interrupteur, pour qu'on voie les journaux tomber dans la pièce. En quittant l'étape (SuivreInterrupteur),
/// elle suit de nouveau l'interrupteur : levé = éteinte, et les chiffres phosphorescents des journaux brillent.
/// Branché sur « 2 - Indices » : Au Debut → ToutAllumer, A La Fin → SuivreInterrupteur.
///
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre directement sur l'objet de la lumière.
/// </summary>
[RequireComponent(typeof(Light))]
public class LumiereInterrupteur : MonoBehaviour
{
    [Tooltip("Coché : la lumière est allumée quand l'interrupteur est fermé, et s'éteint quand on l'allume.")]
    public bool inverser = false;

    [Tooltip("L'objet dont le matériau émissif s'allume avec la lumière.\n" +
             "Vide = l'objet parent de la lumière (ex. : le modèle du ventilateur).")]
    public Renderer ampoule;

    [Tooltip("Durée (secondes) du fondu quand la lumière s'allume ou s'éteint. 0 = instantané.")]
    public float dureeFondu = 0.4f;

    Light lumiere;
    PartieEmissive partieEmissive;
    float intensiteNormale;
    float niveau;   // 0 = éteinte, 1 = allumée; glisse vers ce que l'interrupteur demande
    bool toutAllume;   // vrai pendant l'étape des têtes

    /// <summary>0 = éteinte, 1 = allumée (avec le fondu). Ex. : les chiffres phosphorescents brillent quand elle est éteinte.</summary>
    public float Niveau => Mathf.SmoothStep(0f, 1f, niveau);


    /// <summary>
    /// Étape des têtes : la lumière se rallume (en fondu) et reste allumée, peu importe l'interrupteur,
    /// jusqu'à SuivreInterrupteur. Appelé par l'étape des têtes (Au Debut).
    /// </summary>
    public void ToutAllumer()
    {
        toutAllume = true;
    }

    /// <summary>La lumière suit de nouveau l'interrupteur (en fondu). Appelé à la fin de l'étape des têtes (A La Fin).</summary>
    public void SuivreInterrupteur()
    {
        toutAllume = false;
    }

    void Awake()
    {
        lumiere = GetComponent<Light>();
        intensiteNormale = lumiere.intensity;

        if (ampoule == null) ampoule = GetComponentInParent<Renderer>();
        partieEmissive = new PartieEmissive(ampoule, nameof(LumiereInterrupteur));
    }

    void OnEnable()
    {
        niveau = Cible();   // au départ : directement dans le même état que l'interrupteur, sans fondu
        Appliquer();
    }

    void Update()
    {
        float cible = Cible();
        if (niveau == cible) return;

        niveau = dureeFondu > 0f ? Mathf.MoveTowards(niveau, cible, Time.deltaTime / dureeFondu) : cible;
        Appliquer();
    }

    float Cible()
    {
        if (toutAllume) return 1f;
        bool allumee = inverser ? !Capteurs.InterrupteurAllume : Capteurs.InterrupteurAllume;
        return allumee ? 1f : 0f;
    }

    void Appliquer()
    {
        float force = Mathf.SmoothStep(0f, 1f, niveau);   // début et fin du fondu plus doux

        lumiere.intensity = intensiteNormale * force;
        lumiere.enabled = force > 0f;

        if (force > 0f) partieEmissive.AllumerAvecForce(force);
        else partieEmissive.Eteindre();
    }
}
