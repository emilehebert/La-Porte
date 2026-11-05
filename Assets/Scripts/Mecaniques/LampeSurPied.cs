using System.Collections;
using UnityEngine;

/// <summary>
/// La lampe sur pied dans la pièce (l'écran du VISITEUR).
///
///  - Interrupteur : allume / ferme la lampe.
///  - Heurtoir : fait défiler les couleurs en boucle — seulement si la lampe est allumée.
///  - La sonnette (sélection d'une couleur) est gérée par l'étape EtapeEnigmeCouleurs.
///
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre sur l'objet de la lampe.
/// </summary>
public class LampeSurPied : MonoBehaviour
{
    public Light lumiere;

    public int IndexCouleur { get; private set; }
    public bool EstAllumee => Capteurs.InterrupteurAllume;

    float intensiteNormale;
    Coroutine clignotement;


    void Awake()
    {
        intensiteNormale = lumiere.intensity;
    }

    void OnEnable()
    {
        Capteurs.Heurtoir += CouleurSuivante;
        Capteurs.Interrupteur += QuandInterrupteur;
    }

    void OnDisable()
    {
        Capteurs.Heurtoir -= CouleurSuivante;
        Capteurs.Interrupteur -= QuandInterrupteur;
    }

    void Start()
    {
        Afficher();
    }

    void QuandInterrupteur(bool allume)
    {
        Afficher();
    }

    void CouleurSuivante()
    {
        if (!EstAllumee) return;   // condition du document : la lampe doit être allumée

        int nombreDeCouleurs = ReglagesJeu.Instance.couleursLampe.Length;
        IndexCouleur = (IndexCouleur + 1) % nombreDeCouleurs;
        Afficher();
    }

    void Afficher()
    {
        lumiere.enabled = EstAllumee;
        lumiere.color = ReglagesJeu.Instance.Couleur(IndexCouleur);
        lumiere.intensity = intensiteNormale;
    }

    /// <summary>Petit retour visuel : la lampe s'éteint et se rallume « fois » fois.</summary>
    public void Clignoter(int fois, float duree)
    {
        if (clignotement != null) StopCoroutine(clignotement);
        clignotement = StartCoroutine(RoutineClignoter(fois, duree));
    }

    IEnumerator RoutineClignoter(int fois, float duree)
    {
        for (int i = 0; i < fois; i++)
        {
            lumiere.enabled = false;
            yield return new WaitForSeconds(duree);
            lumiere.enabled = EstAllumee;
            yield return new WaitForSeconds(duree);
        }
        clignotement = null;
    }
}
