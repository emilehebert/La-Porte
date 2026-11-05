using UnityEngine;

/// <summary>
/// Partie A (étapes 1 à 3 du document) : l'énigme des couleurs.
///
///   Locataire : la poignée promène la lumière dans le corridor;
///               l'interrupteur révèle l'ordre des couleurs (et allume la lampe du visiteur).
///   Visiteur  : le heurtoir change la couleur de la lampe;
///               la sonnette sélectionne la couleur affichée.
///
/// Réussie quand les couleurs sont sélectionnées dans le bon ordre (ReglagesJeu → ordreCouleurs).
/// Une mauvaise couleur → la lampe clignote 3 fois et on recommence la combinaison du début.
/// </summary>
public class EtapeEnigmeCouleurs : Etape
{
    public LampeSurPied lampe;

    [Header("Sons (optionnels)")]
    public AudioSource sonBonneCouleur;
    public AudioSource sonMauvaiseCouleur;
    public AudioSource sonReussite;

    int progression;   // combien de bonnes couleurs ont été sélectionnées de suite


    protected override void Entrer()
    {
        progression = 0;
    }

    public override void QuandSonnette()
    {
        if (!lampe.EstAllumee) return;   // condition du document : la lampe doit être allumée

        int[] ordre = ReglagesJeu.Instance.ordreCouleurs;
        if (ordre.Length == 0)
        {
            Debug.LogError("[Énigme des couleurs] ReglagesJeu → ordreCouleurs est vide!");
            return;
        }

        int couleurAttendue = ordre[progression];

        if (lampe.IndexCouleur == couleurAttendue)
        {
            progression++;

            if (progression >= ordre.Length)
            {
                JouerSon(sonReussite);
                lampe.Clignoter(4, 0.1f);
                Terminer();
            }
            else
            {
                JouerSon(sonBonneCouleur);
                lampe.Clignoter(1, 0.15f);
            }
        }
        else
        {
            progression = 0;
            JouerSon(sonMauvaiseCouleur);
            lampe.Clignoter(3, 0.1f);
        }
    }

    public override string InfoOperateur()
    {
        if (lampe == null) return "<color=#ff7070>Assigner la LampeSurPied sur cette étape!</color>";

        ReglagesJeu reglages = ReglagesJeu.Instance;
        string etatLampe = lampe.EstAllumee ? "allumée" : "<color=#ff7070>FERMÉE</color>";

        return $"Progression : {progression} / {reglages.ordreCouleurs.Length}\n" +
               $"Solution : {reglages.TexteOrdreCouleurs()}\n" +
               $"Lampe : {etatLampe}, couleur affichée : {reglages.NomCouleur(lampe.IndexCouleur)}";
    }
}
