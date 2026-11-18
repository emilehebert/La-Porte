using UnityEngine;

/// <summary>
/// Copie l'intensité (et, si coché, la couleur) d'une autre lumière, à chaque image.
/// Ex. : « light lefside » et « light rightside » suivent « light middle » :
/// les trois lumières blanches près de la caméra du corridor s'allument et s'éteignent ensemble.
/// (light middle est la 1re lumière de LumieresCorridor : allumée quand la poignée est au repos.)
///
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre directement sur l'objet de la lumière qui copie.
/// </summary>
[RequireComponent(typeof(Light))]
public class CopyLightIntensity : MonoBehaviour
{
    [Tooltip("L'objet de la lumière à copier (ex. : light middle).")]
    public GameObject MainLight;

    [Tooltip("Copier aussi la couleur de la lumière suivie. Décocher pour garder la couleur de cette lumière.")]
    public bool copierCouleur = true;

    Light lumiere;
    Light lumiereACopier;


    void Awake()
    {
        lumiere = GetComponent<Light>();
        if (MainLight != null) lumiereACopier = MainLight.GetComponent<Light>();

        if (lumiereACopier == null)
        {
            Debug.LogError($"[CopyLightIntensity] « {name} » : le champ Main Light est vide ou n'a pas de Light.");
            enabled = false;
        }
    }

    // LateUpdate : après LumieresCorridor (Update), pour copier la valeur de cette image-ci.
    void LateUpdate()
    {
        lumiere.intensity = lumiereACopier.intensity;
        if (copierCouleur) lumiere.color = lumiereACopier.color;
    }
}
