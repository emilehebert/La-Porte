using UnityEngine;

/// <summary>
/// Des étincelles sortent de la prise électrique à chaque coup de SONNETTE,
/// seulement quand la lampe est ÉTEINTE (interrupteur fermé).
///
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre sur l'objet qui a le Particle System des étincelles.
/// Le menu « La Porte → Ajouter les étincelles à la prise » crée tout automatiquement.
///
/// L'apparence (nombre, vitesse, couleur...) se règle directement dans le Particle System.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class EtincellesPrise : MonoBehaviour
{
    [Tooltip("Optionnel : un son (grésillement) joué avec les étincelles.")]
    public AudioSource son;

    ParticleSystem etincelles;


    void Awake()
    {
        etincelles = GetComponent<ParticleSystem>();
    }

    void OnEnable()  { Capteurs.Sonnette += QuandSonnette; }
    void OnDisable() { Capteurs.Sonnette -= QuandSonnette; }

    void QuandSonnette()
    {
        if (Capteurs.InterrupteurAllume) return;   // lampe allumée : pas d'étincelles

        // Stop puis Play : relance la gerbe même si la précédente n'est pas finie
        // (les étincelles déjà sorties continuent leur chemin).
        etincelles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        etincelles.Play(true);

        if (son != null) son.Play();
    }
}
