using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Le plafonnier au-dessus d'une lumière du corridor prend la COULEUR de cette lumière,
/// et brille autant qu'elle : lumière éteinte = plafonnier éteint, lumière bleue = plafonnier bleu.
///
/// Chaque plafonnier doit avoir SON matériau dans le modèle (Blender : un matériau par plafonnier,
/// ex. « Lights white 1 » pour le plafonnier au-dessus de light 1). Ce script retrouve tout seul
/// les objets qui utilisent ce matériau et les colore (sans modifier le matériau lui-même).
///
/// Le blanc chaud des lumières (LumieresCorridor → Couleur Blanche) est affiché en BLANC PUR sur le plafonnier :
/// la lumière garde sa teinte chaude, mais le plafonnier n'a pas l'air beige. Les couleurs du code restent fidèles.
///
/// Le matériau doit avoir « Emission » coché pour briller.
/// Fonctionne tout le temps, peu importe l'étape.
/// À mettre sur l'objet de la lumière (ex. light 1), avec le matériau de son plafonnier.
/// </summary>
[RequireComponent(typeof(Light))]
public class PlafonnierLumiere : MonoBehaviour
{
    [Tooltip("Le matériau du plafonnier au-dessus de cette lumière (ex. Lights white 1).\n" +
             "Il doit être utilisé par CE plafonnier seulement, et avoir « Emission » coché.")]
    public Material materiauPlafonnier;

    [Tooltip("L'intensité de la lumière qui correspond au plafonnier « à fond ».")]
    public float intensiteReference = 1f;

    [Tooltip("La force de la lueur du plafonnier quand sa lumière est à fond (plus de 1 = plus brillant).")]
    public float forceLueur = 1.5f;

    [Tooltip("Le plafonnier prend aussi la couleur de sa lumière comme couleur de base (pas seulement sa lueur),\n" +
             "autant que la lumière est allumée : éteint, il redevient blanc (on ne devine pas sa couleur).")]
    public bool teinterLaBase = true;

    [Tooltip("Coché : quand la lumière est dans le « blanc » du corridor (un blanc chaud, LumieresCorridor → Couleur Blanche),\n" +
             "le plafonnier s'affiche en blanc pur au lieu de beige. Décoché : il prend la couleur exacte de la lumière.")]
    public bool blancPurPourLeBlanc = true;

    static readonly int idEmission = Shader.PropertyToID("_EmissionColor");
    static readonly int idCouleur = Shader.PropertyToID("_MainColor");

    Light lumiere;
    LumieresCorridor corridor;   // pour connaître le « blanc » des lumières
    MaterialPropertyBlock bloc;
    readonly List<Renderer> rendus = new List<Renderer>();
    readonly List<int> emplacements = new List<int>();   // le numéro du matériau sur chaque objet


    void Awake()
    {
        lumiere = GetComponent<Light>();
        bloc = new MaterialPropertyBlock();
    }

    void Start()
    {
        corridor = FindAnyObjectByType<LumieresCorridor>();

        if (materiauPlafonnier == null)
        {
            Debug.LogWarning($"[Plafonnier] « {name} » : le champ Materiau Plafonnier est vide.");
            enabled = false;
            return;
        }

        if (!materiauPlafonnier.IsKeywordEnabled("_EMISSION"))
        {
            Debug.LogWarning($"[Plafonnier] Cocher « Emission » sur le matériau « {materiauPlafonnier.name} » : sinon le plafonnier ne brille pas.");
        }

        // Tous les objets (et le numéro de leur matériau) qui utilisent ce matériau.
        foreach (Renderer rendu in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Material[] materiaux = rendu.sharedMaterials;
            for (int i = 0; i < materiaux.Length; i++)
            {
                if (materiaux[i] != materiauPlafonnier) continue;
                rendus.Add(rendu);
                emplacements.Add(i);
            }
        }

        if (rendus.Count == 0)
        {
            Debug.LogWarning($"[Plafonnier] « {materiauPlafonnier.name} » n'est utilisé par aucun objet de la scène : " +
                             "l'assigner au plafonnier dans Blender (ou dans les réglages d'import du modèle, onglet Materials).");
        }
    }

    // LateUpdate : après LumieresCorridor (Update), pour reprendre la couleur et l'intensité de cette image-ci.
    void LateUpdate()
    {
        bool allumee = lumiere.isActiveAndEnabled;
        float force = allumee ? Mathf.Clamp01(lumiere.intensity / Mathf.Max(intensiteReference, 0.01f)) : 0f;
        Color couleur = CouleurDuPlafonnier(lumiere.color);

        for (int k = 0; k < rendus.Count; k++)
        {
            if (rendus[k] == null) continue;

            rendus[k].GetPropertyBlock(bloc, emplacements[k]);
            bloc.SetColor(idEmission, couleur * (force * forceLueur));
            // Éteint = blanc neutre : sinon un plafonnier éteint, éclairé par sa voisine, montrerait sa couleur.
            if (teinterLaBase) bloc.SetColor(idCouleur, Color.Lerp(Color.white, couleur, force));
            rendus[k].SetPropertyBlock(bloc, emplacements[k]);
        }
    }

    /// <summary>
    /// La couleur à afficher sur le plafonnier. On « enlève » la teinte chaude du blanc du corridor :
    /// le blanc chaud devient blanc pur, et les couleurs du code (bleu, rouge...) changent à peine.
    /// Pendant un dégradé blanc → couleur, le résultat glisse lui aussi en douceur.
    /// </summary>
    Color CouleurDuPlafonnier(Color couleurLumiere)
    {
        if (!blancPurPourLeBlanc || corridor == null) return couleurLumiere;

        Color blanc = corridor.couleurBlanche;
        return new Color(
            Mathf.Clamp01(couleurLumiere.r / Mathf.Max(blanc.r, 0.01f)),
            Mathf.Clamp01(couleurLumiere.g / Mathf.Max(blanc.g, 0.01f)),
            Mathf.Clamp01(couleurLumiere.b / Mathf.Max(blanc.b, 0.01f)),
            couleurLumiere.a);
    }
}
