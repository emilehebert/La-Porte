using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Joue une Timeline (une petite cinématique), puis passe à l'étape suivante.
///
/// Partie A, étape 4 du document — les indices :
///   - corridor : trois têtes tombent de la ventilation au plafond;
///   - pièce    : un journal tombe de la bibliothèque sur la table.
/// → UNE Timeline avec les deux animations dedans.
///
/// Important : sur le PlayableDirector, DÉCOCHER « Play On Awake ».
/// Pas encore de Timeline? Laisser vide : l'étape attend « dureeSansTimeline » secondes.
/// </summary>
public class EtapeTimeline : Etape
{
    public PlayableDirector timeline;

    [Tooltip("Si aucune Timeline n'est assignée : temps d'attente avant de passer à la suite (pour tester).")]
    public float dureeSansTimeline = 3f;

    [Tooltip("Temps d'attente supplémentaire après la fin de la Timeline.")]
    public float attenteApres = 0f;


    void Awake()
    {
        if (timeline != null && timeline.playOnAwake)
        {
            Debug.LogWarning($"[{Nom}] Décochez « Play On Awake » sur le PlayableDirector, sinon il joue dès le départ.");
        }
    }

    protected override void Entrer()
    {
        StartCoroutine(Deroulement());
    }

    IEnumerator Deroulement()
    {
        float duree = dureeSansTimeline;

        if (timeline != null)
        {
            timeline.extrapolationMode = DirectorWrapMode.Hold;   // garde la dernière image (têtes au sol)
            timeline.time = 0;
            timeline.Play();
            duree = (float)timeline.duration;
        }

        yield return new WaitForSeconds(duree + attenteApres);
        Terminer();
    }

    /// <summary>Saute directement à la fin de la Timeline (les têtes et le journal déjà tombés).</summary>
    public override void AppliquerEtatFinal()
    {
        if (timeline == null) return;

        timeline.extrapolationMode = DirectorWrapMode.Hold;
        timeline.Play();
        timeline.time = timeline.duration;
        timeline.Evaluate();
    }

    public override string InfoOperateur()
    {
        if (timeline == null) return $"Pas de Timeline assignée : attente de {dureeSansTimeline:0} s.";
        return $"Timeline : {timeline.time:0.0} / {timeline.duration:0.0} s";
    }
}
