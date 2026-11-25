using UnityEngine;

/// <summary>
/// Les réglages du son des deux pièces : le volume de chacune, et quelle enceinte est laquelle.
///
///   CHAMBRE  (visiteur)  → sortie GAUCHE de l'ordi
///   CORRIDOR (locataire) → sortie DROITE de l'ordi
///
/// On entend la chambre dans le corridor? Cocher « Inverser Gauche Droite » (ou inverser les câbles).
/// Pour vérifier les enceintes : panneau F1 → « Test des enceintes » (un bip dans une seule pièce).
///
/// À mettre sur l'objet « Sons » (le menu « La Porte → Brancher les sons » le crée).
/// </summary>
public class ReglagesSons : MonoBehaviour
{
    [Tooltip("Décoché : chambre à GAUCHE, corridor à DROITE.\n" +
             "Coché : l'inverse. À cocher si on entend les sons de la chambre dans le corridor.")]
    public bool inverserGaucheDroite = false;

    [Header("Volume de chaque pièce")]
    [Range(0f, 1f)] public float volumeChambre = 1f;
    [Range(0f, 1f)] public float volumeCorridor = 1f;

    [Header("Pour les tests")]
    [Tooltip("Coupe tous les sons de la chambre (pour écouter seulement le corridor).")]
    public bool muetChambre = false;
    [Tooltip("Coupe tous les sons du corridor (pour écouter seulement la chambre).")]
    public bool muetCorridor = false;

    static ReglagesSons instance;
    AudioSource sourceTest;


    void Awake()    { instance = this; }
    void OnEnable() { instance = this; }


    /// <summary>-1 = gauche, 1 = droite, 0 = les deux.</summary>
    public static float Pan(Espace espace)
    {
        if (espace == Espace.LesDeux) return 0f;

        bool inverser = instance != null && instance.inverserGaucheDroite;
        float chambre = inverser ? 1f : -1f;
        return espace == Espace.Chambre ? chambre : -chambre;
    }

    public static float Volume(Espace espace)
    {
        if (instance == null) return 1f;

        float chambre = instance.muetChambre ? 0f : instance.volumeChambre;
        float corridor = instance.muetCorridor ? 0f : instance.volumeCorridor;

        switch (espace)
        {
            case Espace.Chambre:  return chambre;
            case Espace.Corridor: return corridor;
            default:              return Mathf.Max(chambre, corridor);
        }
    }


    // ---------- Test des enceintes (panneau F1) ----------

    /// <summary>Un bip de 1 s dans UNE seule pièce : pour vérifier que chaque enceinte est dans la bonne pièce.</summary>
    public static void Tester(Espace espace)
    {
        if (instance == null)
        {
            Debug.LogError("[Sons] Pas d'objet « Sons » avec ReglagesSons : faire le menu « La Porte → Brancher les sons ».");
            return;
        }
        instance.JouerTest(espace);
    }

    void JouerTest(Espace espace)
    {
        if (sourceTest == null)
        {
            sourceTest = gameObject.AddComponent<AudioSource>();
            sourceTest.playOnAwake = false;
            sourceTest.spatialBlend = 0f;
            sourceTest.clip = BipDeTest();
        }

        sourceTest.Stop();
        sourceTest.panStereo = Pan(espace);
        sourceTest.volume = 0.8f;   // pas touché par les volumes ni le muet : c'est un test du câblage
        sourceTest.Play();
        Debug.Log($"[Sons] Test : bip dans la {(espace == Espace.Chambre ? "CHAMBRE" : "le CORRIDOR")} (sortie {(Pan(espace) < 0 ? "GAUCHE" : "DROITE")}).");
    }

    static AudioClip BipDeTest()
    {
        const int echantillons = 44100;
        float[] son = new float[echantillons];   // 1 seconde : 3 bips
        for (int i = 0; i < son.Length; i++)
        {
            float t = (float)i / echantillons;
            bool allume = (t % 0.33f) < 0.2f;
            son[i] = allume ? 0.4f * Mathf.Sin(2f * Mathf.PI * 660f * t) : 0f;
        }

        AudioClip clip = AudioClip.Create("Test enceinte", son.Length, 1, echantillons, false);
        clip.SetData(son, 0);
        return clip;
    }
}
