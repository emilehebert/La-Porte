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

    [Header("Indice : montrer qu'on peut cogner pour changer la lampe")]
    [Tooltip("Personne n'a cogné ni sonné depuis ce temps (secondes) → la lampe s'anime pour attirer l'œil\n" +
             "(allumée ou éteinte). Elle recommence tant que personne n'essaie. 0 = jamais.")]
    public float secondesAvantIndice = 6f;

    [Header("Sons (optionnels)")]
    public AudioSource sonBonneCouleur;
    public AudioSource sonMauvaiseCouleur;
    public AudioSource sonReussite;

    int progression;             // combien de bonnes couleurs ont été sélectionnées de suite
    float heureDerniereAction;   // dernier coup de heurtoir ou de sonnette (ou dernier indice)


    void Awake()
    {
        // Filet de sécurité : si le lien a été perdu dans la scène, on retrouve la lampe tout seul.
        if (lampe == null)
        {
            lampe = FindAnyObjectByType<LampeSurPied>();

            if (lampe != null) Debug.LogWarning($"[{Nom}] Champ « Lampe » vide : « {lampe.name} » trouvée automatiquement. Pensez à la rebrancher dans la scène.");
            else Debug.LogError($"[{Nom}] Aucune LampeSurPied dans la scène : l'énigme des couleurs ne peut pas fonctionner.");
        }
    }

    protected override void Entrer()
    {
        progression = 0;
        heureDerniereAction = Time.time;
    }

    void Update()   // seulement pendant cette étape (le gestionnaire désactive les autres)
    {
        if (lampe == null || secondesAvantIndice <= 0f) return;

        if (Time.time - heureDerniereAction >= secondesAvantIndice)
        {
            lampe.JouerAnimation();
            heureDerniereAction = Time.time;
        }
    }

    public override void QuandHeurtoir()
    {
        heureDerniereAction = Time.time;   // le visiteur essaie : pas besoin d'indice pour l'instant
    }

    public override void QuandSonnette()
    {
        heureDerniereAction = Time.time;

        if (lampe == null) return;        // erreur déjà signalée (et affichée dans le panneau F1)
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
