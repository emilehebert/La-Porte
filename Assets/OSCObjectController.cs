
using UnityEngine;
using extOSC;
using System.Linq;
using System.Text.RegularExpressions;

public class OSCObjectController : MonoBehaviour
{
    [Header("OSC")]
    public OSCReceiver receiver;

    [Header("Objects")]
    public Renderer sonette;
    public Renderer code;
    public Transform lightsParent;

    private Color originalSonetteColor;
    private Color originalCodeColor;

    [Header("Light Gradient")]
    public float maxIntensity = 100f;
    public float gradientWidth = 1.5f;
    public float movementSpeed = 8f;

    private Light[] lights;

    private float targetValue = -1f;
    private float currentPosition = 0f;

    void Start()
    {
        // Store original object colors
        if (sonette != null)
            originalSonetteColor = sonette.material.color;

        if (code != null)
            originalCodeColor = code.material.color;

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
        if (message.Values.Count == 0 || sonette == null)
            return;

        float value = message.Values[0].FloatValue;

        sonette.material.color = Mathf.Approximately(value, 1f)
            ? Color.red
            : originalSonetteColor;

        Debug.Log("Sonette: " + value);
    }

    void OnReceiveCode(OSCMessage message)
    {
        if (message.Values.Count == 0 || code == null)
            return;

        float value = message.Values[0].FloatValue;

        code.material.color = Mathf.Approximately(value, 1f)
            ? Color.red
            : originalCodeColor;

        Debug.Log("Code: " + value);
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

        Debug.Log("Rotation: " + targetValue);
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
