using UnityEngine;

/// <summary>Une couleur avec un nom lisible (affiché dans le panneau opérateur).</summary>
[System.Serializable]
public class CouleurNommee
{
    public string nom;
    public Color couleur = Color.white;

    public CouleurNommee() { }

    public CouleurNommee(string nom, Color couleur)
    {
        this.nom = nom;
        this.couleur = couleur;
    }
}

/// <summary>
/// TOUT ce qu'on peut ajuster dans les énigmes, au même endroit.
/// On change les solutions ici, sans toucher au code.
/// </summary>
public class ReglagesJeu : MonoBehaviour
{
    public static ReglagesJeu Instance { get; private set; }

    [Header("Énigme des couleurs (Partie A)")]
    [Tooltip("Les couleurs que le heurtoir fait défiler sur la lampe sur pied, dans l'ordre.")]
    public CouleurNommee[] couleursLampe =
    {
        new CouleurNommee("Bleu",  new Color(0.20f, 0.45f, 0.85f)),
        new CouleurNommee("Rouge", new Color(0.90f, 0.25f, 0.25f)),
        new CouleurNommee("Vert",  new Color(0.25f, 0.75f, 0.45f)),
        new CouleurNommee("Jaune", new Color(0.95f, 0.75f, 0.20f)),
        new CouleurNommee("Mauve", new Color(0.65f, 0.35f, 0.85f)),
    };

    [Tooltip("La bonne combinaison : le numéro de chaque couleur dans la liste ci-dessus (0 = la première).\n" +
             "C'est aussi l'ordre affiché sur les lumières du corridor.")]
    public int[] ordreCouleurs = { 0, 2, 1 };   // Bleu → Vert → Rouge

    [Header("Énigme du numpad (Partie B)")]
    [Tooltip("Le code à entrer sur le numpad (sert à écrire le journal).\n" +
             "C'est TouchDesigner qui le vérifie : il doit avoir le MÊME code.")]
    public string codeNumpad = "1234";


    void Awake()
    {
        Instance = this;
    }

    public Color Couleur(int index)
    {
        return couleursLampe[Mathf.Clamp(index, 0, couleursLampe.Length - 1)].couleur;
    }

    public string NomCouleur(int index)
    {
        if (index < 0 || index >= couleursLampe.Length) return $"(couleur {index} inexistante!)";
        return couleursLampe[index].nom;
    }

    /// <summary>Ex. : « Bleu → Vert → Rouge ».</summary>
    public string TexteOrdreCouleurs()
    {
        string texte = "";
        for (int i = 0; i < ordreCouleurs.Length; i++)
        {
            if (i > 0) texte += " → ";
            texte += NomCouleur(ordreCouleurs[i]);
        }
        return texte;
    }
}
