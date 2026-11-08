using UnityEngine;

/// <summary>
/// PLACEHOLDER — affiche dans la pièce (écran du visiteur) ce qui est tapé sur le numpad :
/// « CODE : 12__ », puis « CODE FAUX » un moment si le code est mauvais.
/// N'affiche rien en dehors de l'étape du numpad.
/// </summary>
public class AfficheurNumpadPlaceholder : MonoBehaviour
{
    public TextMesh texte;

    [Tooltip("Combien de temps « CODE FAUX » reste affiché (secondes).")]
    public float dureeMessageErreur = 1.5f;


    void Update()
    {
        GestionnaireEtapes jeu = GestionnaireEtapes.Instance;
        EtapeCodeNumpad etape = jeu != null ? jeu.EtapeActuelle as EtapeCodeNumpad : null;

        if (etape == null)
        {
            texte.text = "";
            return;
        }

        bool erreurRecente = Time.time - etape.HeureDerniereErreur < dureeMessageErreur;
        texte.text = erreurRecente ? "CODE FAUX" : "CODE : " + etape.SaisieAvecTirets;
    }
}
