using UnityEngine;

/// <summary>
/// PLACEHOLDER du journal (Partie A étape 4 + Partie B du document) — à remplacer par le vrai journal.
///
/// Écrit les deux versions de la page à partir du code (ReglagesJeu → codeNumpad),
/// pour que les tests restent justes même si on change le code :
///   - 6 personnes : 3 sont les têtes tombées (dans l'ordre du code), 3 sont des leurres;
///   - lampe ALLUMÉE : personnes 1, 3 et 5 lisibles; lampe FERMÉE : 2, 4 et 6
///     (l'inversion est faite par JournalPersonnages, sur l'objet « pages »).
/// Exemple avec le code 1234 : ORANGE = 12, CYAN = 3, ROSE = 4.
///
/// Les pages n'apparaissent qu'une fois le journal tombé sur la table.
/// </summary>
public class JournalPlaceholder : MonoBehaviour
{
    [Tooltip("L'objet qui a JournalPersonnages (les deux versions de la page).")]
    public GameObject pages;
    public TextMesh texteLampeAllumee;
    public TextMesh texteLampeFermee;

    [Tooltip("Position du journal une fois tombé sur la table : les pages apparaissent à ce moment.")]
    public Vector3 positionSurLaTable;

    // Les 3 têtes, dans l'ordre où elles tombent (= l'ordre du code). Mêmes couleurs que les cubes.
    static readonly string[] tetes = { "ORANGE", "CYAN", "ROSE" };
    static readonly string[] leurres = { "BLANC", "GRIS", "NOIR" };
    static readonly string[] numerosLeurres = { "58", "71", "96" };


    void Start()
    {
        string[] numeros = DecouperCode(ReglagesJeu.Instance.codeNumpad, tetes.Length);

        // Personnes 1 à 6 : les têtes sont réparties dans les deux versions de la page,
        // pour que le locataire doive jouer avec l'interrupteur.
        string[] noms    = { tetes[0],   leurres[0],        leurres[1],        tetes[1],   tetes[2],   leurres[2] };
        string[] valeurs = { numeros[0], numerosLeurres[0], numerosLeurres[1], numeros[1], numeros[2], numerosLeurres[2] };

        texteLampeAllumee.text = Page("LAMPE ALLUMÉE", noms, valeurs, 0);
        texteLampeFermee.text = Page("LAMPE FERMÉE", noms, valeurs, 1);
        pages.SetActive(false);
    }

    void Update()
    {
        bool surLaTable = Vector3.Distance(transform.position, positionSurLaTable) < 0.05f;
        if (pages.activeSelf != surLaTable) pages.SetActive(surLaTable);
    }

    /// <summary>Une version de la page : une personne sur deux est « floue » (??????).</summary>
    static string Page(string titre, string[] noms, string[] valeurs, int premiereLisible)
    {
        string page = $"JOURNAL (placeholder)\n{titre}\n";
        for (int i = 0; i < noms.Length; i++)
        {
            bool lisible = i % 2 == premiereLisible;
            page += lisible ? $"{i + 1}. {noms[i]} = {valeurs[i]}\n" : $"{i + 1}. ??????\n";
        }
        return page;
    }

    /// <summary>« 1234 » en 3 morceaux → « 12 », « 3 », « 4 ».</summary>
    static string[] DecouperCode(string code, int morceaux)
    {
        string[] resultat = new string[morceaux];
        int position = 0;

        for (int i = 0; i < morceaux; i++)
        {
            int longueur = code.Length / morceaux + (i < code.Length % morceaux ? 1 : 0);
            resultat[i] = longueur > 0 ? code.Substring(position, longueur) : "?";
            position += longueur;
        }
        return resultat;
    }
}
