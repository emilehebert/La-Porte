using UnityEngine;

/// <summary>
/// PLACEHOLDER — affiche dans la pièce (écran du visiteur) « ENTREZ LE CODE »,
/// puis « CODE FAUX » un moment si TouchDesigner dit que le code est mauvais.
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
        texte.text = erreurRecente ? "CODE FAUX" : "ENTREZ LE CODE";
    }
}
