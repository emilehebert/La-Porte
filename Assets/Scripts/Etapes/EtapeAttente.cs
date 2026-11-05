using UnityEngine;

/// <summary>
/// Étape 0 : l'installation attend les joueurs. Tout est remis à neuf (la scène vient d'être rechargée).
/// La poignée, l'interrupteur et le heurtoir fonctionnent déjà (ce sont des mécaniques toujours actives).
///
/// L'expérience démarre au premier contact (sonnette, heurtoir, interrupteur, numpad)
/// ou quand l'opérateur appuie sur Page Down.
/// </summary>
public class EtapeAttente : Etape
{
    [Tooltip("Décocher pour que seul l'opérateur puisse démarrer (Page Down).")]
    public bool demarrerAuPremierContact = true;

    [Tooltip("Adresse OSC envoyée avec 0 pour reverrouiller la porte. Laisser vide s'il n'y a pas de serrure électrique.")]
    public string adresseOscPorte = "";

    protected override void Entrer()
    {
        Capteurs.EnvoyerOSC(adresseOscPorte, 0f);
    }

    public override void QuandSonnette()                => PremierContact();
    public override void QuandHeurtoir()                => PremierContact();
    public override void QuandInterrupteur(bool allume) => PremierContact();
    public override void QuandToucheNumpad(int chiffre) => PremierContact();

    void PremierContact()
    {
        if (demarrerAuPremierContact) Terminer();
    }

    public override string InfoOperateur()
    {
        return demarrerAuPremierContact
            ? "En attente des joueurs : démarre au premier contact, ou Page Down."
            : "En attente des joueurs : appuyer sur Page Down pour démarrer.";
    }
}
