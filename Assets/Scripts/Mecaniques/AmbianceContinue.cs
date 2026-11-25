using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Une ambiance qui joue en boucle, du lancement du jeu jusqu'à la fin.
///
/// Elle ne se coupe PAS quand la scène se recharge (recommencer, sauter ou rejouer une étape) :
/// la première copie reste, celles des rechargements suivants se retirent toutes seules.
/// (Deux ambiances avec le même nom d'objet = la même ambiance.)
///
/// À mettre sur un objet avec une AudioSource (le menu « La Porte → Brancher les sons » le fait).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbianceContinue : MonoBehaviour
{
    static readonly Dictionary<string, AmbianceContinue> enCours = new Dictionary<string, AmbianceContinue>();


    void Awake()
    {
        if (enCours.TryGetValue(name, out AmbianceContinue dejaLa) && dejaLa != null)
        {
            Destroy(gameObject);   // l'ambiance joue déjà depuis le chargement d'avant : on la laisse continuer
            return;
        }

        enCours[name] = this;
        transform.SetParent(null);       // DontDestroyOnLoad marche seulement sur un objet à la racine
        DontDestroyOnLoad(gameObject);

        AudioSource source = GetComponent<AudioSource>();
        source.loop = true;
        if (!source.isPlaying) source.Play();
    }
}
