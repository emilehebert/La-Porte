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

    [Tooltip("Son numéro dans le journal. Le code du numpad = les numéros des 2 têtes tombées, dans l'ordre de chute (4 chiffres).\n" +
             "DEUX chiffres (10 à 99), différent pour chaque tête. Pas de 0 au début : il disparaîtrait du code envoyé à TouchDesigner.")]
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
             "C'est aussi l'ordre affiché sur les lumières du corridor.\n" +
             "Avec « Couleurs Au Hasard » coché, seul le NOMBRE de couleurs compte ici (une par lumière du code) :\n" +
             "les couleurs elles-mêmes sont tirées au hasard à chaque partie.")]
    public int[] ordreCouleurs = { 0, 2, 1 };   // Bleu → Vert → Rouge (si pas au hasard)

    [Tooltip("Coché : à chaque partie, la combinaison est tirée au hasard (des couleurs toutes différentes),\n" +
             "pour que ce ne soit pas toujours la même. Décoché : on garde « Ordre Couleurs » tel quel.")]
    public bool couleursAuHasard = true;

    [Header("Les têtes et le code du numpad (Partie A ét. 4 + Partie B)")]
    [Tooltip("Les têtes possibles. À chaque partie, 2 sont choisies au hasard : ce sont elles qui tombent, dans un ordre au hasard.\n" +
             "Chaque tête a un numéro à 2 chiffres : 2 têtes = un code à 4 chiffres.\n" +
             "Le journal montre toutes les têtes avec leur numéro : celles qui ne tombent pas servent de leurres.")]
    public Tete[] tetes =
    {
        new Tete("Moustachu", "47"),
        new Tete("Chauve",    "73"),
        new Tete("Afro",      "25"),
        new Tete("Lunettes",  "91"),
        new Tete("Rousse",    "58"),
    };

    /// <summary>Combien de têtes tombent à chaque partie (il faut au moins autant d'emplacements sur « Têtes qui tombent »).</summary>
    public const int NombreDeTetesQuiTombent = 2;

    /// <summary>Combien de chiffres a le numéro de chaque tête (2 têtes × 2 chiffres = un code à 4 chiffres).</summary>
    public const int ChiffresParTete = 2;

    /// <summary>Les têtes tombées cette partie (leur place dans « tetes »), dans l'ordre de chute.</summary>
    public int[] TetesTombees { get; private set; } = new int[0];

    /// <summary>
    /// Le code à entrer sur le numpad cette partie : les numéros des têtes tombées, dans l'ordre de chute (ex. « 9125 »).
    /// Il change à chaque partie : Unity l'envoie à TouchDesigner (voir l'étape « 3 - Code du numpad »).
    /// </summary>
    public string CodeNumpad { get; private set; } = "";


    void Awake()
    {
        Instance = this;
        if (couleursAuHasard) ChoisirCouleurs();
        VerifierNumeros();
        ChoisirTetes();
    }

    /// <summary>
    /// Tire au hasard la combinaison de couleurs (autant que dans « ordreCouleurs », toutes différentes).
    /// Appelé à chaque chargement de la scène, comme les têtes : recommencer = nouvelle combinaison.
    /// Il reste toujours au moins une couleur hors de la combinaison : c'est la couleur de départ de la lampe.
    /// </summary>
    public void ChoisirCouleurs()
    {
        int nombre = Mathf.Min(ordreCouleurs.Length, couleursLampe.Length - 1);
        if (nombre < ordreCouleurs.Length)
        {
            Debug.LogError($"[Couleurs] Il faut au moins {ordreCouleurs.Length + 1} couleurs dans ReglagesJeu → Couleurs Lampe " +
                           $"({ordreCouleurs.Length} pour la combinaison + 1 pour le départ de la lampe).");
        }

        int[] melange = Melanger(couleursLampe.Length);
        ordreCouleurs = new int[nombre];
        for (int i = 0; i < nombre; i++) ordreCouleurs[i] = melange[i];

        Debug.Log($"[Couleurs] Cette partie : {TexteOrdreCouleurs()}");
    }

    /// <summary>Les nombres 0, 1, 2... (n-1) dans un ordre au hasard (mélange de Fisher-Yates).</summary>
    static int[] Melanger(int n)
    {
        int[] melange = new int[n];
        for (int i = 0; i < n; i++) melange[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (melange[i], melange[j]) = (melange[j], melange[i]);
        }
        return melange;
    }

    /// <summary>Chaque numéro doit avoir 2 chiffres, sans 0 au début, et être différent des autres.</summary>
    void VerifierNumeros()
    {
        for (int i = 0; i < tetes.Length; i++)
        {
            string numero = tetes[i].numero ?? "";
            bool bonFormat = numero.Length == ChiffresParTete && numero[0] != '0';
            foreach (char c in numero) bonFormat &= char.IsDigit(c);

            if (!bonFormat)
            {
                Debug.LogError($"[Têtes] « {tetes[i].description} » a le numéro « {numero} » : il faut {ChiffresParTete} chiffres, sans 0 au début (ex. 47).");
            }

            for (int j = 0; j < i; j++)
            {
                if (tetes[j].numero == numero)
                {
                    Debug.LogError($"[Têtes] « {tetes[i].description} » et « {tetes[j].description} » ont le même numéro ({numero}).");
                }
            }
        }
    }

    /// <summary>
    /// Tire les têtes qui tombent, dans un ordre au hasard, et calcule le code qui va avec.
    /// Appelé à chaque chargement de la scène : recommencer (ou rejouer une étape) = nouvelles têtes, nouveau code.
    /// </summary>
    public void ChoisirTetes()
    {
        // Mélanger les places 0, 1, 2, 3, 4, puis garder les 2 premières.
        int[] melange = Melanger(tetes.Length);

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

    /// <summary>Ex. : « Lunettes (91) → Afro (25) ».</summary>
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
