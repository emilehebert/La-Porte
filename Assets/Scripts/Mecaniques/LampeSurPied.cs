using System.Collections;
using UnityEngine;

/// <summary>
/// La lampe sur pied dans la pièce (l'écran du VISITEUR).
///
///  - Interrupteur : allume / ferme la lampe.
///  - Heurtoir : fait défiler les couleurs en boucle — seulement si la lampe est allumée.
///    Au départ, la lampe est sur une couleur qui ne fait pas partie de la solution.
///  - La sonnette (sélection d'une couleur) est gérée par l'étape EtapeEnigmeCouleurs.
///  - L'ampoule (le matériau émissif) suit la lumière : éteinte avec elle, et de la même couleur.
///  - Animation « on peut interagir avec moi » (Lamp_interact) : la lampe tremble.
///    Elle joue à chaque coup de heurtoir, allumée OU éteinte (je cogne → la lampe réagit),
///    et l'étape de l'énigme la déclenche aussi quand personne n'a cogné depuis un moment.
///    Seul le MOUVEMENT de l'animation est gardé : la lumière et l'abat-jour restent contrôlés par ce script.
///
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre sur l'objet de la lampe.
/// </summary>
public class LampeSurPied : MonoBehaviour
{
    public Light lumiere;

    [Tooltip("L'objet dont le matériau émissif s'allume avec la lampe.\n" +
             "Vide = l'objet parent de la lumière (le modèle de la lampe).")]
    public Renderer ampoule;

    [Header("Animation « on peut interagir avec moi »")]
    [Tooltip("L'Animator de la lampe (avec Lamp.controller). Vide = celui du modèle parent de la lumière.\n" +
             "Il est mis en pause au démarrage : l'animation ne joue que quand on la demande.")]
    public Animator animateur;

    [Tooltip("Le nom de l'état de l'Animator à jouer.")]
    public string etatAnimation = "Lamp_interact";

    [Tooltip("Combien de fois l'animation se répète chaque fois qu'elle joue (1 = une seule fois).")]
    [Min(1)] public int repetitions = 1;

    [Tooltip("Joue aussi l'animation à chaque coup de heurtoir : le visiteur voit que c'est SON geste qui fait réagir la lampe.")]
    public bool animerAChaqueCoup = true;

    public int IndexCouleur { get; private set; }
    public bool EstAllumee => Capteurs.InterrupteurAllume;
    public bool AnimationEnCours => routineAnimation != null;

    float intensiteNormale;
    Coroutine clignotement;
    Coroutine routineAnimation;
    PartieEmissive partieEmissive;
    Quaternion rotationNormale;   // la position de la lampe au repos (l'animation la fait trembler)


    void Awake()
    {
        intensiteNormale = lumiere.intensity;

        if (ampoule == null) ampoule = lumiere.GetComponentInParent<Renderer>();
        partieEmissive = new PartieEmissive(ampoule, nameof(LampeSurPied));

        if (animateur == null) animateur = lumiere.GetComponentInParent<Animator>();
        if (animateur != null)
        {
            rotationNormale = animateur.transform.localRotation;
            animateur.enabled = false;   // sinon l'animation tourne en boucle dès le départ
        }
    }

    void OnEnable()
    {
        Capteurs.Heurtoir += CouleurSuivante;
        Capteurs.Interrupteur += QuandInterrupteur;
    }

    void OnDisable()
    {
        Capteurs.Heurtoir -= CouleurSuivante;
        Capteurs.Interrupteur -= QuandInterrupteur;
    }

    void Start()
    {
        IndexCouleur = CouleurDeDepart();
        Afficher();
    }

    void LateUpdate()
    {
        // L'animation Lamp_interact anime aussi l'intensité de la lumière et la couleur de l'abat-jour.
        // On ne garde que le mouvement : à chaque image, on annule ces deux-là
        // (LateUpdate passe APRÈS l'Animator). Sinon une lampe éteinte aurait l'air de s'allumer.
        if (!AnimationEnCours) return;

        lumiere.intensity = intensiteNormale;
        partieEmissive.RetirerCouleursAnimees();
    }

    /// <summary>
    /// La première couleur qui ne fait PAS partie de la solution :
    /// le visiteur ne peut pas réussir le 1er choix par hasard en sonnant tout de suite.
    /// </summary>
    int CouleurDeDepart()
    {
        ReglagesJeu reglages = ReglagesJeu.Instance;

        for (int i = 0; i < reglages.couleursLampe.Length; i++)
        {
            if (System.Array.IndexOf(reglages.ordreCouleurs, i) < 0) return i;
        }

        return 0;   // toutes les couleurs font partie de la solution
    }

    void QuandInterrupteur(bool allume)
    {
        Afficher();
    }

    void CouleurSuivante()
    {
        if (animerAChaqueCoup) JouerAnimation();   // la lampe réagit au heurtoir, allumée ou non

        if (!EstAllumee) return;   // condition du document : la couleur ne change que si la lampe est allumée

        int nombreDeCouleurs = ReglagesJeu.Instance.couleursLampe.Length;
        IndexCouleur = (IndexCouleur + 1) % nombreDeCouleurs;
        Afficher();
    }

    void Afficher()
    {
        lumiere.color = ReglagesJeu.Instance.Couleur(IndexCouleur);
        lumiere.intensity = intensiteNormale;
        MontrerAllumee(EstAllumee);
    }

    /// <summary>La lumière ET l'ampoule, toujours ensemble (l'ampoule prend la couleur de la lumière).</summary>
    void MontrerAllumee(bool allumee)
    {
        lumiere.enabled = allumee;

        if (allumee) partieEmissive.Allumer(lumiere.color);
        else partieEmissive.Eteindre();
    }

    /// <summary>Petit retour visuel : la lampe s'éteint et se rallume « fois » fois.</summary>
    public void Clignoter(int fois, float duree)
    {
        if (clignotement != null) StopCoroutine(clignotement);
        clignotement = StartCoroutine(RoutineClignoter(fois, duree));
    }

    IEnumerator RoutineClignoter(int fois, float duree)
    {
        for (int i = 0; i < fois; i++)
        {
            MontrerAllumee(false);
            yield return new WaitForSeconds(duree);
            MontrerAllumee(EstAllumee);
            yield return new WaitForSeconds(duree);
        }
        clignotement = null;
    }


    // ---------- Animation « on peut interagir avec moi » ----------

    /// <summary>
    /// La lampe tremble (animation Lamp_interact), allumée ou éteinte, puis revient droite.
    /// Si elle joue déjà, elle recommence du début.
    /// </summary>
    public void JouerAnimation()
    {
        if (animateur == null) return;

        if (routineAnimation != null) StopCoroutine(routineAnimation);
        routineAnimation = StartCoroutine(RoutineAnimation());
    }

    IEnumerator RoutineAnimation()
    {
        float debut = Time.time;
        animateur.enabled = true;
        animateur.Play(etatAnimation, 0, 0f);
        yield return null;   // une image : l'Animator connaît maintenant la durée de l'animation

        // L'animation est en boucle (Loop Time) : on l'arrête pile à la fin du nombre de répétitions voulu.
        float duree = animateur.GetCurrentAnimatorStateInfo(0).length * repetitions;
        yield return new WaitForSeconds(duree - (Time.time - debut));

        routineAnimation = null;
        RemettreNormal();
    }

    /// <summary>Après l'animation : la lampe droite, son intensité et son ampoule normales.</summary>
    void RemettreNormal()
    {
        animateur.enabled = false;
        animateur.transform.localRotation = rotationNormale;
        partieEmissive.RetirerCouleursAnimees();   // l'Animator a posé sa propre couleur d'émission
        Afficher();
    }
}
