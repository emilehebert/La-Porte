
using UnityEngine;
using extOSC;
using System.Linq;
using System.Text.RegularExpressions;

public class OSCObjectController : MonoBehaviour
{
    [Header("OSC")]
    public OSCReceiver receiver;

    [Header("Objects")]

    public GameObject lampe;
    public Transform lightsParent;
    private int sonettePressed = 0;

    public Color[] lampColors = new Color[]
    {
        new Color(0.20f, 0.45f, 0.85f), // Blue
        new Color(0.90f, 0.25f, 0.25f), // Red
        new Color(0.25f, 0.75f, 0.45f), // Green
        new Color(0.95f, 0.75f, 0.20f), // Yellow
        new Color(0.65f, 0.35f, 0.85f)  // Purple
    };
    private int lampIndex = 0;

    private Color originalSonetteColor;
    private Color originalCodeColor;

    [Header("Light Gradient")]
    public float maxIntensity = 1f;
    public float gradientWidth = 1.5f;
    public float movementSpeed = 4f;

    private Light[] lights;

    private float targetValue = -1f;
    private float currentPosition = 0f;

    void Start()
    {


        // Register OSC addresses
        if (receiver == null)
        {
            Debug.LogError("OSC Receiver is not assigned!");
            enabled = false;
            return;
        }

        receiver.Bind("/sonette", OnReceiveSonette);
        receiver.Bind("/code", OnReceiveCode);
        receiver.Bind("/rotation", OnReceiveRotation);

        // Find and sort the lights numerically
        if (lightsParent == null)
        {
            Debug.LogError("Lights Parent is not assigned!");
            enabled = false;
            return;
        }

        lights = lightsParent
            .Cast<Transform>()
            .OrderBy(t =>
            {
                Match match = Regex.Match(t.name, @"\d+");
                return match.Success
                    ? int.Parse(match.Value)
                    : int.MaxValue;
            })
            .Select(t => t.GetComponentInChildren<Light>())
            .Where(l => l != null)
            .ToArray();

        if (lights.Length == 0)
        {
            Debug.LogError("No Light components found!");
            enabled = false;
            return;
        }

        // Start all lights switched off
        foreach (Light light in lights)
        {
            light.intensity = 0f;
        }

        Debug.Log($"Found {lights.Length} lights.");
    }

    void OnReceiveSonette(OSCMessage message)
    {

        
        if (message.Values.Count == 0 ){
            return;
        }
            
        float value = message.Values[0].FloatValue;
        Debug.Log(sonettePressed);
        Debug.Log(value);
        if(sonettePressed == 1 && value == 0){
            sonettePressed = 0;
        }
        

        if(sonettePressed == 0 && value == 1){
            sonettePressed = 1;
            if(lampIndex < lampColors.Length -1 ){
                lampIndex += 1;
                
            }else
            {
                lampIndex = 0;
            }
        }



        lampe.GetComponent<Light>().color = lampColors[lampIndex];

        
    }

    void OnReceiveCode(OSCMessage message)
    {
        if (message.Values.Count == 0 )
            return;

        float value = message.Values[0].FloatValue;

        
    }

    void OnReceiveRotation(OSCMessage message)
    {
        if (message.Values.Count == 0)
            return;

        targetValue = Mathf.Clamp(
            message.Values[0].FloatValue,
            -1f,
            1f
        );
    }

    void Update()
    {
        if (lights == null || lights.Length == 0)
            return;

        // Map OSC range -1..1 to light index 0..last
        float targetPosition =
            (targetValue + 1f) * 0.5f * (lights.Length - 1);

        // Smooth the gradient's movement
        currentPosition = Mathf.Lerp(
            currentPosition,
            targetPosition,
            1f - Mathf.Exp(-movementSpeed * Time.deltaTime)
        );

        // Calculate the intensity of each light
        for (int i = 0; i < lights.Length; i++)
        {
            float distance = Mathf.Abs(i - currentPosition);

            float intensity01 = Mathf.Clamp01(
                1f - distance / gradientWidth
            );

            // Smooth falloff
            intensity01 = intensity01 * intensity01 *
                          (3f - 2f * intensity01);

            // Actual Unity Light intensity: 0 to maxIntensity
            lights[i].intensity = intensity01 * maxIntensity;
        }
    }
}
