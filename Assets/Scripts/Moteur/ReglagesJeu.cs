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

/// <summary>Une tête : ce qui la distingue (écrit dans le journal), son numéro et son modèle 3D.</summary>
[System.Serializable]
public class Tete
{
    [Tooltip("Ce qui la distingue des autres têtes. C'est le nom écrit dans le journal.")]
    public string description;

    [Tooltip("Son numéro dans le journal. Le code du numpad = les numéros des têtes tombées, dans l'ordre de chute.\n" +
             "Un chiffre de 1 à 9, différent pour chaque tête (pas de 0 : il disparaîtrait au début du code envoyé à TouchDesigner).")]
    public string numero;

    [Tooltip("Le prefab de la tête (Assets/Prefabs/Tetes). Seules les têtes tirées au hasard sont créées pendant le jeu.\n" +
             "Le menu « La Porte → Finaliser les têtes » les assigne tout seul.")]
    public GameObject modele;

    public Tete() { }

    public Tete(string description, string numero)
    {
        this.description = description;
        this.numero = numero;
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

    [Header("Les têtes et le code du numpad (Partie A ét. 4 + Partie B)")]
    [Tooltip("Les têtes possibles. À chaque partie, 3 sont choisies au hasard : ce sont elles qui tombent, dans un ordre au hasard.\n" +
             "Le journal montre toutes les têtes avec leur numéro : celles qui ne tombent pas servent de leurres.")]
    public Tete[] tetes =
    {
        new Tete("Moustachu", "4"),
        new Tete("Chauve",    "7"),
        new Tete("Afro",      "2"),
        new Tete("Lunettes",  "9"),
        new Tete("Rousse",    "5"),
    };

    /// <summary>Combien de têtes tombent à chaque partie (autant que d'emplacements dans la Timeline).</summary>
    public const int NombreDeTetesQuiTombent = 3;

    /// <summary>Les têtes tombées cette partie (leur place dans « tetes »), dans l'ordre de chute.</summary>
    public int[] TetesTombees { get; private set; } = new int[0];

    /// <summary>
    /// Le code à entrer sur le numpad cette partie : les numéros des têtes tombées, dans l'ordre de chute (ex. « 924 »).
    /// Il change à chaque partie : Unity l'envoie à TouchDesigner (voir l'étape « 3 - Code du numpad »).
    /// </summary>
    public string CodeNumpad { get; private set; } = "";


    void Awake()
    {
        Instance = this;
        ChoisirTetes();
    }

    /// <summary>
    /// Tire les têtes qui tombent, dans un ordre au hasard, et calcule le code qui va avec.
    /// Appelé à chaque chargement de la scène : recommencer (ou rejouer une étape) = nouvelles têtes, nouveau code.
    /// </summary>
    public void ChoisirTetes()
    {
        // Mélanger les places 0, 1, 2, 3, 4 (mélange de Fisher-Yates), puis garder les premières.
        int[] melange = new int[tetes.Length];
        for (int i = 0; i < melange.Length; i++) melange[i] = i;
        for (int i = melange.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (melange[i], melange[j]) = (melange[j], melange[i]);
        }

        int nombre = Mathf.Min(NombreDeTetesQuiTombent, melange.Length);
        if (nombre < NombreDeTetesQuiTombent)
        {
            Debug.LogError($"[Têtes] Il faut au moins {NombreDeTetesQuiTombent} têtes dans ReglagesJeu → Tetes (il y en a {tetes.Length}).");
        }

        TetesTombees = new int[nombre];
        CodeNumpad = "";
        for (int i = 0; i < nombre; i++)
        {
            TetesTombees[i] = melange[i];
            CodeNumpad += tetes[melange[i]].numero;
        }

        Debug.Log($"[Têtes] Cette partie : {TexteTetesTombees()} → code {CodeNumpad}");
    }

    /// <summary>Ex. : « Lunettes (9) → Afro (2) → Moustachu (4) ».</summary>
    public string TexteTetesTombees()
    {
        string texte = "";
        for (int i = 0; i < TetesTombees.Length; i++)
        {
            if (i > 0) texte += " → ";
            Tete tete = tetes[TetesTombees[i]];
            texte += $"{tete.description} ({tete.numero})";
        }
        return texte;
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
