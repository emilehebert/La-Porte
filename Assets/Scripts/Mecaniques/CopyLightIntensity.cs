using UnityEngine;

/// <summary>
/// Copie l'intensité d'une autre lumière, à chaque image.
/// Ex. : « light lefside » et « light rightside » suivent « light 4 »,
/// que LumieresCorridor allume et éteint selon la poignée.
///
/// Seule l'intensité est copiée (pas la couleur).
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre directement sur l'objet de la lumière qui copie.
/// </summary>
[RequireComponent(typeof(Light))]
public class CopyLightIntensity : MonoBehaviour
{
    [Tooltip("L'objet de la lumière à copier (ex. : light 4).")]
    public GameObject MainLight;

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
    }
}
