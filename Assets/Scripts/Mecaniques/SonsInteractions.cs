using UnityEngine;

/// <summary>
/// Les sons des objets physiques, à TOUTES les étapes (comme les autres mécaniques) :
///
///   Heurtoir            → la lampe sur pied tremble                       (chambre)
///   Interrupteur levé   → la lampe s'allume                              (chambre)
///                         le corridor s'assombrit, seule la zone de la poignée reste éclairée (corridor)
///   Interrupteur baissé → la lampe s'éteint                              (chambre)
///                         tout le corridor se rallume                    (corridor)
///
/// À partir de l'étape des têtes, le corridor reste tout allumé peu importe l'interrupteur :
/// ses sons d'interrupteur ne jouent donc plus (rien ne change à l'écran).
///
/// Les sons de la sonnette (couleurs, étincelles), des têtes et des journaux sont sur leurs propres scripts.
/// À mettre sur l'objet « Sons » (le menu « La Porte → Brancher les sons » branche tout).
/// </summary>
public class SonsInteractions : MonoBehaviour
{
    [Header("Chambre")]
    [Tooltip("La lampe sur pied qui tremble, à chaque coup de heurtoir.")]
    public AudioSource sonHeurtoir;
    [Tooltip("La lampe sur pied s'allume (interrupteur levé).")]
    public AudioSource sonLampeAllumee;
    [Tooltip("La lampe sur pied s'éteint (interrupteur baissé).")]
    public AudioSource sonLampeEteinte;

    [Header("Corridor")]
    [Tooltip("Le corridor s'assombrit (interrupteur levé) : seule la zone de la poignée reste éclairée.")]
    public AudioSource sonCorridorSombre;
    [Tooltip("Tout le corridor se rallume (interrupteur baissé).")]
    public AudioSource sonCorridorAllume;

    [Tooltip("Pour savoir si le corridor est resté tout allumé (à partir de l'étape des têtes). Vide = trouvé tout seul.")]
    public LumieresCorridor corridor;


    void Awake()
    {
        if (corridor == null) corridor = FindAnyObjectByType<LumieresCorridor>();
    }

    void OnEnable()
    {
        Capteurs.Heurtoir += QuandHeurtoir;
        Capteurs.Interrupteur += QuandInterrupteur;
    }

    void OnDisable()
    {
        Capteurs.Heurtoir -= QuandHeurtoir;
        Capteurs.Interrupteur -= QuandInterrupteur;
    }

    void QuandHeurtoir()
    {
        SortieSon.Superposer(sonHeurtoir);
    }

    void QuandInterrupteur(bool allume)
    {
        SortieSon.Superposer(allume ? sonLampeAllumee : sonLampeEteinte);

        bool corridorFige = corridor != null && corridor.ToutEstAllume;
        if (!corridorFige) SortieSon.Superposer(allume ? sonCorridorSombre : sonCorridorAllume);
    }
}
