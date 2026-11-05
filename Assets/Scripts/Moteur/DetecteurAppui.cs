using UnityEngine;

/// <summary>
/// Transforme une suite de valeurs 0 / 1 (reçues en OSC) en « appuis » uniques.
///
/// Ça fonctionne peu importe comment TouchDesigner envoie :
///  - en continu (OSC Out CHOP) : 0 0 0 1 1 1 1 0 0  → un seul appui;
///  - seulement au moment de l'appui (OSC Out DAT) : 1 ... 1  → un appui chaque fois.
///
/// Et jamais deux appuis à moins de « delaiMinimum » secondes d'intervalle
/// (un capteur qui « tremble » ne compte qu'une fois).
/// </summary>
public class DetecteurAppui
{
    readonly float delaiMinimum;

    bool etaitEnfonce;
    float heureDernierMessageEnfonce = -100f;
    float heureDernierAppui = -100f;

    public DetecteurAppui(float delaiMinimum)
    {
        this.delaiMinimum = delaiMinimum;
    }

    /// <summary>Retourne true si cette valeur correspond à un nouvel appui.</summary>
    public bool EstNouvelAppui(float valeur)
    {
        bool enfonce = valeur >= 0.5f;
        bool nouvelAppui = false;

        if (enfonce)
        {
            // C'est un nouvel appui si c'était relâché avant,
            // ou si on n'avait rien reçu depuis un bout (mode « un message par appui »).
            bool vientDEtreEnfonce = !etaitEnfonce || Time.time - heureDernierMessageEnfonce > delaiMinimum;
            bool assezLongtempsApres = Time.time - heureDernierAppui >= delaiMinimum;

            nouvelAppui = vientDEtreEnfonce && assezLongtempsApres;
            if (nouvelAppui) heureDernierAppui = Time.time;

            heureDernierMessageEnfonce = Time.time;
        }

        etaitEnfonce = enfonce;
        return nouvelAppui;
    }
}
