using UnityEngine;

/// <summary>Dans quel espace on entend un son.</summary>
public enum Espace
{
    Chambre,    // le visiteur
    Corridor,   // le locataire
    LesDeux     // les deux enceintes en même temps (ex. : la porte qui s'ouvre)
}

/// <summary>
/// Envoie le son de cette AudioSource dans la bonne pièce : la CHAMBRE ou le CORRIDOR.
///
/// Unity n'accepte qu'UN SEUL AudioListener (avec deux, un seul entend). On sépare donc les deux espaces
/// avec la stéréo : la chambre sort à GAUCHE, le corridor à DROITE (inversable sur « Sons → ReglagesSons »).
/// Physiquement : la sortie gauche de l'ordi va à l'enceinte de la chambre, la droite à celle du corridor.
///
/// Le son est en 2D : il ne dépend pas de la position de l'objet ni des caméras.
/// À mettre sur chaque objet qui a une AudioSource. Le menu « La Porte → Brancher les sons » le fait tout seul.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SortieSon : MonoBehaviour
{
    [Tooltip("Où on entend ce son. Chambre = enceinte de gauche, Corridor = enceinte de droite, Les Deux = les deux.")]
    public Espace espace = Espace.Chambre;

    [Tooltip("Le volume de CE son (1 = tel que Gabriel l'a mixé). Le volume de chaque pièce se règle sur « Sons → ReglagesSons ».")]
    [Range(0f, 1f)] public float volume = 1f;

    AudioSource source;


    void Awake()
    {
        source = GetComponent<AudioSource>();
        Appliquer();
    }

    // Chaque image : on peut changer les volumes ou inverser gauche / droite pendant que le jeu roule.
    void Update()
    {
        Appliquer();
    }

    void Appliquer()
    {
        source.spatialBlend = 0f;   // 2D : seulement la gauche / droite compte
        source.panStereo = ReglagesSons.Pan(espace);
        source.volume = volume * ReglagesSons.Volume(espace);
    }


    /// <summary>
    /// Joue le son PAR-DESSUS ceux déjà en cours (ne plante pas si le champ est vide).
    /// Ex. : deux coups de heurtoir rapides = on entend les deux, le 2e ne coupe pas le 1er.
    /// </summary>
    public static void Superposer(AudioSource son)
    {
        if (son != null && son.clip != null) son.PlayOneShot(son.clip);
    }
}
