using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panneau pour l'opérateur (clavier + souris sans fil hors de la porte). Invisible pour le public.
/// Il s'affiche sur le Display 1 (l'écran de la pièce).
///
///   F1          ouvrir / fermer le panneau (affiche aussi la souris)
///   Page Down   étape suivante (force la réussite de l'étape actuelle)
///   Page Up     étape précédente
///   F5          rejouer l'étape actuelle depuis son début
///   Ctrl + R    recommencer toute l'expérience
///
/// Simuler les capteurs (panneau ouvert OU fermé) :
///   S sonnette · H heurtoir · L interrupteur · ← → poignée · B bon code · M mauvais code
/// Pour l'exposition, décocher « Simulation Clavier » si le clavier est à portée du public.
/// </summary>
public class PanneauOperateur : MonoBehaviour
{
    [Tooltip("Permet de simuler les capteurs avec le clavier (S, H, L, flèches, B, M), que le panneau F1 soit ouvert ou non.\n" +
             "Décocher pour l'exposition si le clavier est à portée du public.")]
    public bool simulationClavier = true;

    [Tooltip("Vitesse de la poignée simulée avec les flèches ← →.")]
    public float vitessePoignee = 1.5f;

    static bool ouvert;   // « static » : le panneau reste ouvert quand la scène se recharge
    Vector2 defilement;


    // ======================= CLAVIER =======================

    void Update()
    {
        Keyboard clavier = Keyboard.current;
        GestionnaireEtapes jeu = GestionnaireEtapes.Instance;
        if (clavier == null || jeu == null) return;

        if (clavier.f1Key.wasPressedThisFrame) ouvert = !ouvert;
        if (!Application.isEditor) Cursor.visible = ouvert;   // dans le build : souris cachée pour le public

        if (clavier.pageDownKey.wasPressedThisFrame) jeu.Suivante();
        if (clavier.pageUpKey.wasPressedThisFrame) jeu.Precedente();
        if (clavier.f5Key.wasPressedThisFrame) jeu.RejouerEtape();
        if (clavier.ctrlKey.isPressed && clavier.rKey.wasPressedThisFrame) jeu.Recommencer();

        // Les touches de simulation marchent aussi panneau fermé (pour tester en voyant la scène).
        if (simulationClavier) SimulerCapteurs(clavier);
    }

    void SimulerCapteurs(Keyboard clavier)
    {
        if (clavier.sKey.wasPressedThisFrame) Capteurs.DeclencherSonnette();
        if (clavier.hKey.wasPressedThisFrame) Capteurs.DeclencherHeurtoir();
        if (clavier.lKey.wasPressedThisFrame) Capteurs.ChangerInterrupteur(!Capteurs.InterrupteurAllume);

        float direction = 0f;
        if (clavier.leftArrowKey.isPressed) direction -= 1f;
        if (clavier.rightArrowKey.isPressed) direction += 1f;
        if (direction != 0f)
        {
            Capteurs.ChangerPoignee(Capteurs.AnglePoignee + direction * vitessePoignee * Time.deltaTime);
        }

        if (clavier.bKey.wasPressedThisFrame) Capteurs.EntrerCodeNumpad(true);
        if (clavier.mKey.wasPressedThisFrame) Capteurs.EntrerCodeNumpad(false);
    }


    // ======================= PANNEAU (F1) =======================

    void OnGUI()
    {
        GestionnaireEtapes jeu = GestionnaireEtapes.Instance;
        if (!ouvert || jeu == null || jeu.Etapes == null) return;

        // Tout est dessiné comme sur un écran 1280 x 720, puis agrandi pour l'écran réel.
        float echelle = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(echelle, echelle, 1f));
        float largeur = Screen.width / echelle;
        GUI.skin.label.richText = true;   // permet <b>, <color=...> dans les textes

        Rect zone = new Rect(10, 10, largeur - 20, 700);
        GUI.Box(zone, "");   // deux boîtes = fond plus opaque
        GUI.Box(zone, "");

        GUILayout.BeginArea(new Rect(zone.x + 10, zone.y + 10, zone.width - 20, zone.height - 20));
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(420));
        DessinerNavigation(jeu);
        GUILayout.EndVertical();

        GUILayout.Space(20);

        GUILayout.BeginVertical();
        DessinerInfosEtape(jeu);
        GUILayout.Space(10);
        DessinerSimulation();
        GUILayout.Space(10);
        DessinerMoniteurOSC();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DessinerNavigation(GestionnaireEtapes jeu)
    {
        GUILayout.Label("<b><size=18>LA PORTE — panneau opérateur</size></b>");
        GUILayout.Label("F1 fermer · PgDn suivante · PgUp précédente · F5 rejouer · Ctrl+R recommencer");
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Précédente", GUILayout.Height(34))) jeu.Precedente();
        if (GUILayout.Button("Rejouer l'étape", GUILayout.Height(34))) jeu.RejouerEtape();
        if (GUILayout.Button("Suivante >>", GUILayout.Height(34))) jeu.Suivante();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("RECOMMENCER TOUTE L'EXPÉRIENCE", GUILayout.Height(34))) jeu.Recommencer();

        GUILayout.Space(10);
        GUILayout.Label("<b>Aller directement à une étape :</b>");

        defilement = GUILayout.BeginScrollView(defilement);
        for (int i = 0; i < jeu.Etapes.Count; i++)
        {
            bool estActuelle = i == jeu.IndexActuel;
            string texte = estActuelle ? $">  {jeu.Etapes[i].Nom}   (en cours)" : $"    {jeu.Etapes[i].Nom}";

            GUI.enabled = !estActuelle;
            if (GUILayout.Button(texte, GUILayout.Height(30))) jeu.AllerA(i);
            GUI.enabled = true;
        }
        GUILayout.EndScrollView();
    }

    void DessinerInfosEtape(GestionnaireEtapes jeu)
    {
        Etape etape = jeu.EtapeActuelle;
        if (etape == null) return;

        GUILayout.Label($"<b><size=16>Étape en cours : {etape.Nom}</size></b>   ({etape.TempsDansEtape:0} s)");
        if (!string.IsNullOrEmpty(etape.note)) GUILayout.Label($"<i>{etape.note}</i>");

        string info = etape.InfoOperateur();
        if (!string.IsNullOrEmpty(info)) GUILayout.Label(info);
    }

    void DessinerSimulation()
    {
        GUILayout.Label("<b>Simuler les capteurs :</b>");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sonnette (S)", GUILayout.Height(30))) Capteurs.DeclencherSonnette();
        if (GUILayout.Button("Heurtoir (H)", GUILayout.Height(30))) Capteurs.DeclencherHeurtoir();

        string etatInterrupteur = Capteurs.InterrupteurAllume ? "ALLUMÉ" : "fermé";
        if (GUILayout.Button($"Interrupteur (L) : {etatInterrupteur}", GUILayout.Height(30)))
        {
            Capteurs.ChangerInterrupteur(!Capteurs.InterrupteurAllume);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Poignée (← →) : {Capteurs.AnglePoignee:0.00}", GUILayout.Width(170));
        float angle = GUILayout.HorizontalSlider(Capteurs.AnglePoignee, -1f, 1f);
        if (!Mathf.Approximately(angle, Capteurs.AnglePoignee)) Capteurs.ChangerPoignee(angle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Numpad :", GUILayout.Width(100));
        if (GUILayout.Button("Bon code (B)", GUILayout.Height(28))) Capteurs.EntrerCodeNumpad(true);
        if (GUILayout.Button("Mauvais code (M)", GUILayout.Height(28))) Capteurs.EntrerCodeNumpad(false);
        GUILayout.EndHorizontal();
    }

    void DessinerMoniteurOSC()
    {
        GUILayout.Label($"<b>Moniteur OSC</b>  ({Capteurs.TexteConnexion})");

        if (Capteurs.DerniersMessages.Count == 0)
        {
            GUILayout.Label("<color=#ff7070>Aucun message OSC reçu. Vérifier que TouchDesigner roule, " +
                            "et qu'il envoie vers la bonne IP et le bon port.</color>");
            return;
        }

        foreach (var paire in Capteurs.DerniersMessages)
        {
            float ilYA = Time.time - paire.Value.heure;
            string couleur = ilYA < 2f ? "#70ff70" : "#aaaaaa";   // vert = reçu récemment
            GUILayout.Label($"<color={couleur}>{paire.Key}   =   {paire.Value.valeur}      (il y a {ilYA:0.0} s)</color>");
        }
    }
}
