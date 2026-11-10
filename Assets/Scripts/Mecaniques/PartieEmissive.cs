using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La partie qui « brille » d'un objet (son matériau émissif, ex. : l'ampoule de la lampe).
/// Permet de l'éteindre, de la rallumer, ou de changer sa couleur.
///
/// Le matériau Lamp.mat est PARTAGÉ (lampe sur pied + ampoule du ventilateur) :
/// on ne modifie jamais le matériau lui-même, seulement l'objet donné
/// (avec un MaterialPropertyBlock). Les deux lampes restent donc indépendantes.
///
/// Seuls les matériaux de l'objet qui brillent déjà (couleur d'émission pas noire) sont touchés :
/// le métal de la lampe, par exemple, ne se met pas à briller.
/// </summary>
public class PartieEmissive
{
    static readonly int idCouleurEmission = Shader.PropertyToID("_EmissionColor");

    readonly Renderer rendu;
    readonly List<int> emplacements = new List<int>();          // numéros des matériaux émissifs sur l'objet
    readonly List<Color> couleursDeBase = new List<Color>();     // leur couleur d'émission d'origine (HDR)
    readonly MaterialPropertyBlock bloc = new MaterialPropertyBlock();


    public PartieEmissive(Renderer rendu, string nomPourLesErreurs)
    {
        this.rendu = rendu;

        if (rendu == null)
        {
            Debug.LogWarning($"[{nomPourLesErreurs}] Aucun objet à faire briller : l'émission ne suivra pas la lumière.");
            return;
        }

        Material[] materiaux = rendu.sharedMaterials;
        for (int i = 0; i < materiaux.Length; i++)
        {
            Material materiau = materiaux[i];
            if (materiau == null || !materiau.HasProperty(idCouleurEmission)) continue;

            Color couleur = materiau.GetColor(idCouleurEmission);
            if (couleur.maxColorComponent <= 0.001f) continue;   // ce matériau ne brille pas

            emplacements.Add(i);
            couleursDeBase.Add(couleur);
        }

        if (emplacements.Count == 0)
        {
            Debug.LogWarning($"[{nomPourLesErreurs}] « {rendu.name} » n'a aucun matériau émissif (Emission Color noire partout).");
        }
    }

    /// <summary>Rallume avec la couleur d'émission d'origine du matériau.</summary>
    public void Allumer()
    {
        for (int i = 0; i < emplacements.Count; i++) Appliquer(i, couleursDeBase[i]);
    }

    /// <summary>
    /// Rallume avec une autre couleur (ex. : la couleur de la lampe).
    /// La force d'origine (l'intensité HDR du matériau) est gardée : la lampe brille autant, seule la teinte change.
    /// </summary>
    public void Allumer(Color couleur)
    {
        for (int i = 0; i < emplacements.Count; i++)
        {
            float intensite = couleursDeBase[i].maxColorComponent;
            Appliquer(i, couleur * intensite);
        }
    }

    public void Eteindre()
    {
        for (int i = 0; i < emplacements.Count; i++) Appliquer(i, Color.black);
    }

    /// <summary>
    /// Efface la couleur d'émission qu'un Animator a posée sur TOUT l'objet
    /// (ex. : l'animation Lamp_interact anime material._EmissionColor).
    /// Les couleurs posées par ce script, matériau par matériau, restent en place.
    /// </summary>
    public void RetirerCouleursAnimees()
    {
        if (rendu != null) rendu.SetPropertyBlock(null);
    }

    void Appliquer(int i, Color couleur)
    {
        if (rendu == null) return;

        rendu.GetPropertyBlock(bloc, emplacements[i]);
        bloc.SetColor(idCouleurEmission, couleur);
        rendu.SetPropertyBlock(bloc, emplacements[i]);
    }
}
