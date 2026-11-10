using UnityEngine;

/// <summary>
/// Une lumière qui suit l'interrupteur (ex. : la lumière du ventilateur dans la chambre).
///
///  - Normal  : interrupteur allumé = lumière allumée.
///  - Inversé : interrupteur fermé = lumière allumée (donc allumée au début, éteinte quand on lève l'interrupteur).
///
/// L'ampoule (le matériau émissif) s'allume et s'éteint avec la lumière.
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

    Light lumiere;
    PartieEmissive partieEmissive;


    void Awake()
    {
        lumiere = GetComponent<Light>();

        if (ampoule == null) ampoule = GetComponentInParent<Renderer>();
        partieEmissive = new PartieEmissive(ampoule, nameof(LumiereInterrupteur));
    }

    void OnEnable()
    {
        Capteurs.Interrupteur += QuandInterrupteur;
        QuandInterrupteur(Capteurs.InterrupteurAllume);   // au départ : même état que l'interrupteur
    }

    void OnDisable()
    {
        Capteurs.Interrupteur -= QuandInterrupteur;
    }

    void QuandInterrupteur(bool interrupteurAllume)
    {
        bool allumee = inverser ? !interrupteurAllume : interrupteurAllume;

        lumiere.enabled = allumee;

        if (allumee) partieEmissive.Allumer();
        else partieEmissive.Eteindre();
    }
}
