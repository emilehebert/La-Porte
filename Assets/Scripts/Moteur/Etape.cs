using UnityEngine;

/// <summary>
/// Une étape de l'expérience.
///
/// Chaque étape est un GameObject ENFANT de l'objet qui a le GestionnaireEtapes.
/// L'ordre dans la Hierarchy = l'ordre de l'expérience (glisser pour réordonner).
/// Décocher le GameObject d'une étape = la retirer de l'expérience.
///
/// Pour créer une nouvelle étape : faire un script qui hérite de Etape
/// et remplir SEULEMENT les méthodes dont on a besoin. Exemple :
///
///     public class EtapeExemple : Etape
///     {
///         protected override void Entrer()      { /* ce qui se passe au début */ }
///         public override void QuandSonnette()  { Terminer(); /* on passe à la suivante */ }
///     }
///
/// Pour attendre (secondes, animation...), utiliser des coroutines (StartCoroutine) :
/// elles s'arrêtent toutes seules quand on quitte l'étape.
/// </summary>
public abstract class Etape : MonoBehaviour
{
    [TextArea(2, 5)]
    [Tooltip("Note pour l'opérateur, affichée dans le panneau F1.")]
    public string note;

    public string Nom => gameObject.name;
    public bool EstActive { get; private set; }
    public float TempsDansEtape => EstActive ? Time.time - heureDebut : 0f;

    float heureDebut;


    // ---------- À remplir dans vos étapes (toutes optionnelles) ----------

    /// <summary>Quand l'étape commence.</summary>
    protected virtual void Entrer() { }

    /// <summary>Quand on quitte l'étape (réussie, sautée ou recommencée).</summary>
    protected virtual void Sortir() { }

    /// <summary>
    /// Met le monde INSTANTANÉMENT dans l'état « cette étape est finie »
    /// (ex. : les têtes déjà tombées). Utilisé quand l'opérateur saute par-dessus l'étape.
    /// </summary>
    public virtual void AppliquerEtatFinal() { }

    /// <summary>Texte affiché dans le panneau opérateur (progression, solution...).</summary>
    public virtual string InfoOperateur() => "";

    public virtual void QuandSonnette() { }
    public virtual void QuandHeurtoir() { }
    public virtual void QuandInterrupteur(bool allume) { }
    public virtual void QuandCodeNumpad(bool bonCode) { }


    // ---------- Outils pour vos étapes ----------

    /// <summary>À appeler quand l'étape est réussie : on passe à la suivante.</summary>
    protected void Terminer()
    {
        GestionnaireEtapes.Instance.EtapeTerminee(this);
    }

    /// <summary>Joue un son s'il y en a un (ne plante pas si le champ est vide).</summary>
    protected void JouerSon(AudioSource son)
    {
        if (son != null) son.Play();
    }


    // ---------- Utilisé par le GestionnaireEtapes seulement ----------

    public void Demarrer()
    {
        EstActive = true;
        enabled = true;
        heureDebut = Time.time;
        Entrer();
    }

    public void Arreter()
    {
        StopAllCoroutines();
        Sortir();
        EstActive = false;
        enabled = false;
    }
}
