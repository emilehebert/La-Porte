using UnityEngine;

/// <summary>
/// PLACEHOLDER du journal (Partie A étape 4 + Partie B du document) — à remplacer par le vrai journal.
///
/// Écrit les deux versions de la page à partir des têtes (ReglagesJeu → Tetes) :
///   - toutes les têtes, avec leur numéro. Les têtes qui ne sont pas tombées servent de leurres;
///   - lampe ALLUMÉE : personnes 1, 3 et 5 lisibles; lampe FERMÉE : 2 et 4
///     (l'inversion est faite par JournalPersonnages, sur l'objet « pages »).
/// Le code = les numéros des 3 têtes tombées, dans l'ordre de chute.
/// Les numéros ne changent pas d'une partie à l'autre : le vrai journal peut être une image fixe.
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


    void Start()
    {
        Tete[] tetes = ReglagesJeu.Instance.tetes;

        texteLampeAllumee.text = Page("LAMPE ALLUMÉE", tetes, 0);
        texteLampeFermee.text = Page("LAMPE FERMÉE", tetes, 1);
        pages.SetActive(false);
    }

    void Update()
    {
        bool surLaTable = Vector3.Distance(transform.position, positionSurLaTable) < 0.05f;
        if (pages.activeSelf != surLaTable) pages.SetActive(surLaTable);
    }

    /// <summary>Une version de la page : une personne sur deux est « floue » (??????).</summary>
    static string Page(string titre, Tete[] tetes, int premiereLisible)
    {
        string page = $"JOURNAL (placeholder)\n{titre}\n";
        for (int i = 0; i < tetes.Length; i++)
        {
            bool lisible = i % 2 == premiereLisible;
            page += lisible ? $"{i + 1}. {tetes[i].description.ToUpperInvariant()} = {tetes[i].numero}\n" : $"{i + 1}. ??????\n";
        }
        return page;
    }
}
