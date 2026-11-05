using UnityEngine;

/// <summary>
/// Allume le 2e écran dans le build (Unity n'utilise que le 1er écran par défaut).
///   Room Camera     → Display 1 : l'écran du VISITEUR (la pièce)
///   Corridor Camera → Display 2 : l'écran du LOCATAIRE (le corridor)
///
/// Dans l'éditeur, ça ne fait rien : choisir « Display 1 / Display 2 » en haut de la fenêtre Game.
/// </summary>
public class ActiverEcrans : MonoBehaviour
{
    static bool dejaFait;   // une seule fois, même si la scène se recharge

    void Start()
    {
        if (dejaFait) return;
        dejaFait = true;

        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        Debug.Log($"[Écrans] {Display.displays.Length} écran(s) détecté(s).");
    }
}
