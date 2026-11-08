# La Porte — guide du système d'étapes

## 1. Installation dans la scène (5 minutes)

1. **Faire un commit git avant** (pour pouvoir revenir en arrière).
2. Ouvrir `Assets/Scenes/Couloir.unity`.
3. Menu **La Porte → Installer dans la scène ouverte**.
   L'outil crée l'objet `Jeu` et branche ce qui existe déjà (récepteur OSC, lumières `Lights`, lampe sur pied, couleurs). Une fenêtre résume ce qui a été fait, et tout s'annule avec Ctrl+Z.
4. **Ctrl+S** pour sauvegarder la scène.
5. Play, puis **F1** : le panneau opérateur apparaît (sur le *Display 1* de la fenêtre Game).

L'ancien `OSCObjectController` est **désactivé, pas supprimé**. Une fois que tout marche, vous pouvez l'enlever.

### Ce que l'outil crée

```
Jeu                      Capteurs · ReglagesJeu · GestionnaireEtapes · PanneauOperateur · ActiverEcrans
├── 0 - Attente des joueurs        EtapeAttente
├── 1 - Énigme des couleurs        EtapeEnigmeCouleurs
├── 2 - Indices (têtes + journal)  EtapeTimeline
├── 3 - Code du numpad             EtapeCodeNumpad
└── 4 - Porte ouverte              EtapeFin

Lights        + LumieresCorridor   (poignée → zone éclairée, interrupteur → couleurs)
(la lampe)    + LampeSurPied       (interrupteur → on/off, heurtoir → couleur suivante)
```

**L'ordre des enfants de `Jeu` = l'ordre de l'expérience.** Glissez pour réordonner, et décochez une étape pour la retirer.

---

## 2. Les deux sortes de scripts

| | Rôle | Exemples |
|---|---|---|
| **Mécaniques** | Réagissent **toujours** aux objets, peu importe l'étape. | `LumieresCorridor`, `LampeSurPied`, `JournalPersonnages` |
| **Étapes** | Une seule active à la fois. Elles vérifient si l'énigme est réussie. | `EtapeEnigmeCouleurs`, `EtapeCodeNumpad`… |

C'est la même logique que dans les escape rooms : les objets répondent tout le temps, et le « maître du jeu » (ici le `GestionnaireEtapes`) surveille la progression.

---

## 3. Les étapes (d'après « Game loop complète »)

| # | Étape | Ce qui se passe | On passe à la suite quand… | Si l'opérateur saute l'étape |
|---|---|---|---|---|
| 0 | Attente des joueurs | Tout est remis à neuf. Les mécaniques marchent déjà. | Premier contact (sonnette, heurtoir, interrupteur, numpad) ou Page Down | — |
| 1 | Énigme des couleurs (Partie A, ét. 1-3) | **Locataire** : poignée → lumières du corridor, interrupteur → révèle l'ordre des couleurs et allume la lampe. **Visiteur** : heurtoir → couleur suivante, sonnette → sélection. La lampe commence sur une couleur qui n'est pas dans la solution. Mauvaise couleur : la lampe clignote 3 fois et on recommence. | Les couleurs sont sélectionnées dans le bon ordre | Rien à remettre |
| 2 | Indices (Partie A, ét. 4) | Timeline : 3 têtes tombent de la ventilation (corridor), le journal tombe sur la table (pièce). | La Timeline est finie | La Timeline est mise à sa dernière image (têtes au sol) |
| 3 | Code du numpad (Partie B) | Le locataire décrit les têtes, le visiteur trouve les numéros dans le journal. L'interrupteur inverse les personnages flous (`JournalPersonnages`). | Le bon code est entré | Rien à remettre |
| 4 | Porte ouverte | Son / Timeline de fin, envoi OSC optionnel pour déverrouiller. | Après 30 s, l'expérience recommence au début | — |

**Les solutions se règlent sur `Jeu → ReglagesJeu`** : couleurs de la lampe, ordre des couleurs, code du numpad.

---

## 4. Contrat OSC (pour l'équipe TouchDesigner)

Unity écoute sur le **port 7000**, toutes les cartes réseau. L'IP exacte de l'ordi est affichée dans le panneau F1.

| Objet | Adresse | Valeur | À noter |
|---|---|---|---|
| Poignée | `/rotation` | -1 à 1 | En continu, c'est correct. |
| Sonnette | `/sonette` | 1 appuyée, 0 relâchée | En continu ou seulement à l'appui : les deux marchent. |
| Heurtoir | `/vibration` | 1 = coup détecté | Anti-rebond de 0,25 s, réglable dans `Capteurs`. |
| Interrupteur | `/switch` | 1 allumé, 0 fermé | **Nouvelle adresse, à ajouter côté TD.** |
| Numpad | `/code` | le chiffre 0 à 9 | **UN message par touche appuyée** (OSC Out DAT), **pas** en continu. |
| *(optionnel)* Serrure | au choix (ex. `/porte`) | 1 ouvrir, 0 verrouiller | Unity → TD. Remplir l'adresse sur les étapes 0 et 4, et assigner un `OSCTransmitter` sur `Capteurs`. |

Les adresses se changent dans l'Inspector (`Jeu → Capteurs`), sans toucher au code.

---

## 5. Fiche opérateur (à imprimer et coller à côté du clavier)

| Touche | Action |
|---|---|
| **F1** | Ouvrir / fermer le panneau (et la souris) |
| **Page Down** | Étape suivante (force la réussite) |
| **Page Up** | Étape précédente |
| **F5** | Rejouer l'étape en cours depuis son début |
| **Ctrl + R** | Recommencer toute l'expérience |
| **S** / **H** | Simuler la sonnette / le heurtoir |
| **L** | Basculer l'interrupteur |
| **← →** | Tourner la poignée |
| **0 à 9** | Numpad |

Les touches de simulation (S, H, L, ← →, 0 à 9) marchent **seulement quand le panneau F1 est ouvert**. Panneau fermé, quelqu'un qui tape sur le clavier n'agit pas sur le jeu.

Dans le panneau F1, vous avez aussi :

- la liste des étapes : un clic pour y aller directement;
- la solution et la progression de l'énigme en cours;
- le **moniteur OSC** : chaque adresse reçue, sa valeur et il y a combien de temps. Vert = reçu il y a moins de 2 s. Si rien n'arrive, le problème est côté TouchDesigner, Arduino ou réseau, pas dans Unity.

L'expérience recommence aussi toute seule après **10 minutes** sans contact (réglable sur `GestionnaireEtapes`).

---

## 6. Ajouter une étape

1. Créer un script qui hérite de `Etape` et remplir seulement ce qu'il faut :

```csharp
public class EtapeExemple : Etape
{
    public AudioSource voix;

    protected override void Entrer()             { JouerSon(voix); }
    public override void QuandSonnette()         { Terminer(); }   // → étape suivante
    public override void AppliquerEtatFinal()    { /* état du monde si on saute l'étape */ }
    public override string InfoOperateur()       => "Texte affiché dans le panneau F1";
}
```

2. Créer un GameObject enfant de `Jeu`, à la bonne place, et y mettre le script.

**Règles d'or :**

- Pour attendre, utiliser des **coroutines** (`StartCoroutine`) plutôt que `async` / `Task.Delay`. Elles s'arrêtent toutes seules quand on saute ou recommence une étape.
- Un script qui écoute `Capteurs` s'abonne dans `OnEnable` et se **désabonne dans `OnDisable`**.
- Les animations scriptées (têtes, journal, porte) se font dans une **Timeline**, avec *Play On Awake* décoché. Le système peut alors sauter directement à la fin.

---

## 7. Ce qui reste à décider ou à faire

- [ ] **TD** : envoyer l'interrupteur sur `/switch`. D'ici là, la lampe reste fermée avec le vrai matériel : utiliser la touche **L**.
- [ ] **TD** : confirmer le format du numpad (un message par touche?).
- [ ] Le **code du numpad** (`ReglagesJeu → codeNumpad`, `1234` pour l'instant).
- [ ] Le **nombre de couleurs** de la combinaison, et quelles lumières du plafond les affichent. Repris de l'ancien code pour l'instant : light 3, light 2, light 1 = Bleu → Vert → Rouge.
- [ ] Ce qui se passe quand le visiteur se **trompe de couleur**. Pour l'instant : 3 clignotements et on recommence au début de la combinaison.
- [ ] Modéliser les **têtes** et le **journal**, puis faire la Timeline de l'étape 2. Sans Timeline, l'étape attend 3 s.
- [ ] Les deux versions de la page du journal (floue A / B) pour `JournalPersonnages`.
- [ ] Une **serrure électrique**? Si oui, donner l'adresse OSC.
