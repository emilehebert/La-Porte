using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Le chef d'orchestre : UNE seule étape active à la fois, dans l'ordre de la Hierarchy.
/// (Comme une liste de cues au théâtre.)
///
/// Comment on recommence ou on saute à une étape (comme un checkpoint dans un jeu) :
///   1. on recharge la scène       → tout revient exactement comme au départ;
///   2. on applique l'état final des étapes d'avant (têtes tombées, etc.);
///   3. on démarre l'étape choisie.
/// Comme ça, il ne reste jamais rien de bizarre d'une partie précédente.
/// </summary>
public class GestionnaireEtapes : MonoBehaviour
{
    public static GestionnaireEtapes Instance { get; private set; }

    [Tooltip("Si personne ne touche à rien pendant ce temps (minutes), l'expérience recommence. 0 = jamais.")]
    public float minutesInactiviteAvantRetour = 10f;

    public List<Etape> Etapes { get; private set; }
    public int IndexActuel { get; private set; } = -1;

    public Etape EtapeActuelle
    {
        get
        {
            if (IndexActuel < 0 || IndexActuel >= Etapes.Count) return null;
            return Etapes[IndexActuel];
        }
    }

    // « static » = survit au rechargement de la scène.
    // C'est ce qui permet de dire « recharge la scène, puis va directement à l'étape 3 ».
    static int etapeDemandee = 0;


    void Awake()
    {
        Instance = this;

        // Les étapes = les enfants actifs qui ont un script Etape, dans l'ordre de la Hierarchy.
        Etapes = new List<Etape>(GetComponentsInChildren<Etape>(false));
        foreach (Etape etape in Etapes) etape.enabled = false;
    }

    void Start()
    {
        if (Etapes.Count == 0)
        {
            Debug.LogError("[Étapes] Aucune étape trouvée! Ajoutez des enfants avec un script Etape.");
            return;
        }

        int depart = Mathf.Clamp(etapeDemandee, 0, Etapes.Count - 1);
        etapeDemandee = 0;

        // Les étapes d'avant sont considérées comme réussies.
        for (int i = 0; i < depart; i++)
        {
            Etapes[i].AppliquerEtatFinal();
        }

        Lancer(depart);
    }

    void OnEnable()
    {
        Capteurs.Sonnette += TransmettreSonnette;
        Capteurs.Heurtoir += TransmettreHeurtoir;
        Capteurs.Interrupteur += TransmettreInterrupteur;
        Capteurs.CodeNumpad += TransmettreCodeNumpad;
    }

    void OnDisable()
    {
        Capteurs.Sonnette -= TransmettreSonnette;
        Capteurs.Heurtoir -= TransmettreHeurtoir;
        Capteurs.Interrupteur -= TransmettreInterrupteur;
        Capteurs.CodeNumpad -= TransmettreCodeNumpad;
    }

    void Update()
    {
        VerifierInactivite();
    }


    // ---------- Appelé par les étapes ----------

    /// <summary>Une étape a été réussie par les joueurs : on passe à la suivante.</summary>
    public void EtapeTerminee(Etape etape)
    {
        if (etape != EtapeActuelle) return;   // sécurité : seule l'étape active peut faire avancer

        etape.Arreter();
        LancerLaSuivante();
    }


    // ---------- Appelé par l'opérateur (panneau F1 et raccourcis clavier) ----------

    /// <summary>Force la réussite de l'étape actuelle et passe à la suivante.</summary>
    public void Suivante()
    {
        Etape etape = EtapeActuelle;
        if (etape == null) return;

        Debug.Log($"[Étapes] L'opérateur saute « {etape.Nom} ».");
        etape.Arreter();
        etape.AppliquerEtatFinal();   // comme si les joueurs l'avaient réussie
        LancerLaSuivante();
    }

    public void Precedente()
    {
        AllerA(Mathf.Max(0, IndexActuel - 1));
    }

    public void RejouerEtape()
    {
        AllerA(IndexActuel);
    }

    public void Recommencer()
    {
        AllerA(0);
    }

    /// <summary>
    /// Recharge la scène et démarre directement à l'étape demandée.
    /// Retour à l'étape 0 (recommencer l'expérience) : le matériel est aussi remis au repos
    /// (interrupteur fermé = lampe éteinte, poignée droite).
    /// </summary>
    public void AllerA(int index)
    {
        Debug.Log($"[Étapes] Rechargement de la scène → étape {index}.");
        if (index == 0) Capteurs.RemettreAZero();
        etapeDemandee = index;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    // ---------- Fonctionnement interne ----------

    void Lancer(int index)
    {
        IndexActuel = index;
        Debug.Log($"[Étapes] ▶ {index} — {EtapeActuelle.Nom}");
        EtapeActuelle.Demarrer();
    }

    void LancerLaSuivante()
    {
        bool ilEnReste = IndexActuel + 1 < Etapes.Count;

        if (ilEnReste) Lancer(IndexActuel + 1);
        else Recommencer();   // c'était la dernière étape : on boucle au début pour les prochains joueurs
    }

    void VerifierInactivite()
    {
        if (minutesInactiviteAvantRetour <= 0f) return;
        if (IndexActuel <= 0) return;   // déjà au début : rien à faire

        float limite = minutesInactiviteAvantRetour * 60f;
        bool personneNeTouche = Time.time - Capteurs.HeureDernierContact > limite;
        bool etapeDureDepuisLongtemps = EtapeActuelle.TempsDansEtape > limite;

        if (personneNeTouche && etapeDureDepuisLongtemps)
        {
            Debug.Log("[Étapes] Personne n'a rien touché depuis longtemps : on recommence.");
            Recommencer();
        }
    }

    // Les capteurs parlent au gestionnaire, qui transmet à l'étape active seulement.
    void TransmettreSonnette()                { if (EtapeActuelle != null) EtapeActuelle.QuandSonnette(); }
    void TransmettreHeurtoir()                { if (EtapeActuelle != null) EtapeActuelle.QuandHeurtoir(); }
    void TransmettreInterrupteur(bool allume) { if (EtapeActuelle != null) EtapeActuelle.QuandInterrupteur(allume); }
    void TransmettreCodeNumpad(bool bonCode)  { if (EtapeActuelle != null) EtapeActuelle.QuandCodeNumpad(bonCode); }
}
