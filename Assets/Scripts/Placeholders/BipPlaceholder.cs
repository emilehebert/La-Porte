using UnityEngine;

/// <summary>
/// PLACEHOLDER — un bip fabriqué par le code (aucun fichier son nécessaire).
///
/// À mettre sur un objet avec une AudioSource. Si un vrai son est déjà mis dans
/// l'AudioSource (champ AudioClip), ce script ne fait rien : il suffit donc
/// de glisser le vrai son pour le remplacer.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BipPlaceholder : MonoBehaviour
{
    [Tooltip("Hauteur du bip, en Hz (220 = grave, 880 = aigu).")]
    public float frequence = 880f;

    [Tooltip("Durée d'un bip, en secondes.")]
    public float duree = 0.15f;

    [Tooltip("Nombre de bips à la suite (ex. : 3 pour une réussite).")]
    public int nombreDeBips = 1;

    [Tooltip("Chaque bip suivant est plus aigu de ce facteur. 1 = toujours la même note.")]
    public float montee = 1f;

    const int Echantillons = 44100;   // par seconde


    void Awake()
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source.clip != null) return;   // un vrai son est déjà là

        int parBip = Mathf.RoundToInt(duree * Echantillons);
        int pause = parBip / 2;
        float[] son = new float[(parBip + pause) * Mathf.Max(1, nombreDeBips)];

        for (int bip = 0; bip < nombreDeBips; bip++)
        {
            float note = frequence * Mathf.Pow(montee, bip);
            int debut = bip * (parBip + pause);

            for (int i = 0; i < parBip; i++)
            {
                float volume = 0.3f * (1f - (float)i / parBip);   // s'éteint doucement, sans « clic »
                son[debut + i] = volume * Mathf.Sin(2f * Mathf.PI * note * i / Echantillons);
            }
        }

        AudioClip clip = AudioClip.Create(name, son.Length, 1, Echantillons, false);
        clip.SetData(son, 0);

        source.clip = clip;
        source.playOnAwake = false;
    }
}
