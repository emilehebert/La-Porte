using UnityEngine;

/// <summary>
/// Étape 0 : l'installation attend les joueurs. Tout est remis à neuf (la scène vient d'être rechargée).
///
/// L'ÉCRAN TITRE est affiché sur les deux écrans (fond noir, logo qui bat comme un cœur, petit texte).
/// L'expérience démarre quand les DEUX joueurs agissent ENSEMBLE :
///   le locataire bascule l'interrupteur ET le visiteur sonne, à moins de « Fenetre Secondes » d'écart
///   (peu importe lequel commence). Les deux écrans font alors un fondu vers la scène.
/// L'opérateur peut aussi démarrer avec Page Down.
///
/// Pendant l'écran titre ET le fondu, les objets sont en pause (Capteurs.EnPause) : rien ne change dans le décor,
/// aucun son d'objet. Seul l'écran titre entend l'interrupteur et la sonnette (le logo fait un battement plus fort).
/// </summary>
public class EtapeAttente : Etape
{
    [Tooltip("Coché : les joueurs démarrent avec l'interrupteur + la sonnette en même temps.\n" +
             "Décoché : seul l'opérateur peut démarrer (Page Down).")]
    public bool demarrerAuPremierContact = true;

    [Tooltip("Écart maximum (secondes) entre la bascule de l'interrupteur et le coup de sonnette pour que ça compte « en même temps ».")]
    public float fenetreSecondes = 1.5f;

    [Tooltip("Adresse OSC envoyée avec 0 pour reverrouiller la porte. Laisser vide s'il n'y a pas de serrure électrique.")]
    public string adresseOscPorte = "";

    [Header("Écran titre")]
    [Tooltip("Décocher pour ne pas afficher d'écran titre : la scène est visible pendant l'attente, et les objets y réagissent.")]
    public bool afficherEcranTitre = true;
    public EcranTitre.Reglages ecranTitre = new EcranTitre.Reglages();

    EcranTitre ecran;
    float heureInterrupteur = float.NegativeInfinity;
    float heureSonnette = float.NegativeInfinity;
    bool demarre;
    bool abonne;

    protected override void Entrer()
    {
        Capteurs.EnvoyerOSC(adresseOscPorte, 0f);

        demarre = false;
        heureInterrupteur = heureSonnette = float.NegativeInfinity;

        // Pendant la pause, les capteurs n'envoient que ces deux événements-là.
        Capteurs.SonnetteEnPause += QuandSonnette;
        Capteurs.InterrupteurEnPause += GesteInterrupteur;
        abonne = true;

        if (afficherEcranTitre) ecran = EcranTitre.Creer(ecranTitre);
    }

    protected override void Sortir()
    {
        Desabonner();

        // Réussie par les joueurs OU sautée par l'opérateur (Page Down) : les deux écrans font le fondu.
        // Les objets restent en pause jusqu'à la fin du fondu.
        if (ecran != null) ecran.Disparaitre();
        ecran = null;
    }

    void OnDestroy() => Desabonner();   // scène rechargée pendant l'écran titre

    void Desabonner()
    {
        if (!abonne) return;
        Capteurs.SonnetteEnPause -= QuandSonnette;
        Capteurs.InterrupteurEnPause -= GesteInterrupteur;
        abonne = false;
    }

    // Sans écran titre (pas de pause), les gestes arrivent par le chemin normal.
    public override void QuandSonnette()
    {
        heureSonnette = Time.time;
        Verifier();
    }

    public override void QuandInterrupteur(bool allume) => GesteInterrupteur();

    void GesteInterrupteur()
    {
        heureInterrupteur = Time.time;
        Verifier();
    }

    void Verifier()
    {
        if (demarre) return;

        bool ensemble = Mathf.Abs(heureInterrupteur - heureSonnette) <= fenetreSecondes;
        if (!ensemble || !demarrerAuPremierContact)
        {
            if (ecran != null) ecran.Coup();   // un des deux a fait sa part : on attend l'autre
            return;
        }

        demarre = true;
        Debug.Log("[Étapes] Interrupteur + sonnette ensemble : l'expérience commence.");

        // Avec l'écran titre, le jeu est déjà en mode lumières (la pause empêche l'interrupteur de le changer).
        // Sans écran titre, on y revient ici. Avant Terminer() : « demarre » ignore l'événement qui en découle.
        Capteurs.ForcerModeLumieres();
        Terminer();
    }

    public override string InfoOperateur()
    {
        string pause = Capteurs.EnPause ? "\nObjets en pause (écran titre) : ni lumières ni sons d'objets." : "";

        if (!demarrerAuPremierContact) return "En attente des joueurs : appuyer sur Page Down pour démarrer." + pause;

        return "En attente des joueurs : interrupteur ET sonnette à moins de " +
               $"{fenetreSecondes:0.#} s d'écart (L puis S au clavier), ou Page Down.\n" +
               $"Interrupteur : {Depuis(heureInterrupteur)} · Sonnette : {Depuis(heureSonnette)}" + pause;
    }

    static string Depuis(float heure)
    {
        return float.IsNegativeInfinity(heure) ? "pas encore" : $"il y a {Time.time - heure:0.0} s";
    }
}
