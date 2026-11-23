using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

/// <summary>
/// Joue une Timeline (une petite cinématique), puis passe à l'étape suivante.
///
/// Partie A, étape 4 du document — les indices :
///   - corridor : deux têtes tombent de la ventilation au plafond (physique : TetesQuiTombent, branché ci-dessous);
///   - pièce    : un journal tombe de la bibliothèque sur la table (Timeline).
///
/// Ce qui n'est pas dans la Timeline (ex. : la physique) se branche sur « Au Debut » et « Etat Final »;
/// ce qui ne vaut que pendant l'étape (ex. : la lumière de la pièce forcée allumée) se défait dans « A La Fin ».
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

    [Header("En plus de la Timeline (optionnel)")]
    [Tooltip("Appelé au début de l'étape, en même temps que la Timeline.\nEx. : Têtes qui tombent → TetesQuiTombent.Lacher.")]
    public UnityEvent auDebut = new UnityEvent();

    [Tooltip("Appelé quand on saute l'étape : tout doit être mis dans son état final, tout de suite.\n" +
             "Ex. : Têtes qui tombent → TetesQuiTombent.PoserAuSol.")]
    public UnityEvent etatFinal = new UnityEvent();

    [Tooltip("Appelé quand on quitte l'étape (finie, ou sautée pendant qu'elle jouait) : défaire ce qui ne vaut que\n" +
             "pendant l'étape. Ex. : Point Light (7) → LumiereInterrupteur.SuivreInterrupteur.")]
    public UnityEvent aLaFin = new UnityEvent();


    void Awake()
    {
        if (timeline != null && timeline.playOnAwake)
        {
            Debug.LogWarning($"[{Nom}] Décochez « Play On Awake » sur le PlayableDirector, sinon il joue dès le départ.");
        }
    }

    protected override void Entrer()
    {
        auDebut.Invoke();
        StartCoroutine(Deroulement());
    }

    protected override void Sortir()
    {
        aLaFin.Invoke();
    }

    IEnumerator Deroulement()
    {
        float duree = dureeSansTimeline;

        if (timeline != null)
        {
            timeline.extrapolationMode = DirectorWrapMode.Hold;   // garde la dernière image (journal sur la table)
            timeline.time = 0;
            timeline.Play();
            duree = (float)timeline.duration;
        }

        yield return new WaitForSeconds(duree + attenteApres);
        Terminer();
    }

    /// <summary>Saute directement à la fin (les têtes et le journal déjà tombés).</summary>
    public override void AppliquerEtatFinal()
    {
        etatFinal.Invoke();
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
