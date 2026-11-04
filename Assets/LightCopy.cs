using UnityEngine;

public class CopyLightIntensity : MonoBehaviour
{

    public GameObject MainLight;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {

        
        gameObject.GetComponent<Light>().intensity = MainLight.GetComponent<Light>().intensity;
    }
}