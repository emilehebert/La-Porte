using UnityEngine;

/// <summary>
/// PLACEHOLDER — affiche dans la pièce (écran du visiteur) « CODE FAUX » un moment
/// si TouchDesigner dit que le code est mauvais. Rien le reste du temps
/// (plus de « ENTREZ LE CODE » : les journaux suffisent).
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
        texte.text = erreurRecente ? "CODE FAUX" : "";
    }
}
