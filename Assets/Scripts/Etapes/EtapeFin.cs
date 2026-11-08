using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Fin : le bon code a été entré, la porte s'ouvre.
/// Après quelques secondes, l'expérience recommence au début pour les prochains joueurs.
/// </summary>
public class EtapeFin : Etape
{
    [Tooltip("Optionnel : une Timeline de fin. Décocher « Play On Awake » sur le PlayableDirector.")]
    public PlayableDirector timelineFin;

    public AudioSource sonFin;

    [Tooltip("Objets cachés au départ, qui apparaissent quand la porte s'ouvre (ex. : le placeholder « PORTE OUVERTE »).")]
    public GameObject[] objetsAAfficher;

    [Tooltip("Adresse OSC envoyée avec 1 pour déverrouiller la vraie porte. Laisser vide s'il n'y a pas de serrure électrique.")]
    public string adresseOscPorte = "";

    public float secondesAvantRecommencer = 30f;


    protected override void Entrer()
    {
        Capteurs.EnvoyerOSC(adresseOscPorte, 1f);
        JouerSon(sonFin);
        if (timelineFin != null) timelineFin.Play();

        if (objetsAAfficher != null)
        {
            foreach (GameObject objet in objetsAAfficher)
            {
                if (objet != null) objet.SetActive(true);
            }
        }

        StartCoroutine(AttendrePuisRecommencer());
    }

    IEnumerator AttendrePuisRecommencer()
    {
        yield return new WaitForSeconds(secondesAvantRecommencer);
        Terminer();   // c'est la dernière étape : le gestionnaire recommence au début
    }

    public override string InfoOperateur()
    {
        float reste = Mathf.Max(0f, secondesAvantRecommencer - TempsDansEtape);
        return $"Bravo! Retour au début dans {reste:0} s.";
    }
}
