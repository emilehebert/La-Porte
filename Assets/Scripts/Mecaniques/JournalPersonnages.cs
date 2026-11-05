using UnityEngine;

/// <summary>
/// Partie B, « étape X » du document : dans le journal, la moitié des personnages est floue.
/// Allumer / fermer la lampe (interrupteur du locataire) inverse lesquels sont visibles.
///
/// Le plus simple : faire DEUX versions de la page dans Photoshop
///   - version A : personnages 1, 3, 5 nets — 2, 4, 6 flous
///   - version B : l'inverse
/// puis mettre chaque version sur un objet (Quad) ENFANT du journal, au même endroit.
/// </summary>
public class JournalPersonnages : MonoBehaviour
{
    [Tooltip("La version de la page à montrer quand la lampe est ALLUMÉE.")]
    public GameObject versionLampeAllumee;

    [Tooltip("La version de la page à montrer quand la lampe est FERMÉE.")]
    public GameObject versionLampeFermee;


    void OnEnable()
    {
        Capteurs.Interrupteur += QuandInterrupteur;
        Afficher();
    }

    void OnDisable()
    {
        Capteurs.Interrupteur -= QuandInterrupteur;
    }

    void QuandInterrupteur(bool allume)
    {
        Afficher();
    }

    void Afficher()
    {
        bool allume = Capteurs.InterrupteurAllume;
        if (versionLampeAllumee != null) versionLampeAllumee.SetActive(allume);
        if (versionLampeFermee != null) versionLampeFermee.SetActive(!allume);
    }
}
