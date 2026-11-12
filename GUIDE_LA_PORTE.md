# La Porte — guide du projet Unity

> Ce guide décrit **ce que fait le projet en ce moment** (scène `Assets/Scenes/Couloir.unity`).
> Si le jeu et ce guide ne disent pas la même chose, c'est le jeu qui a raison : corrigez le guide.

---

## 1. Démarrer

La scène `Couloir` est **déjà installée** (objet `Jeu` + placeholders de test).

1. Ouvrir `Assets/Scenes/Couloir.unity`.
2. Play.
3. **F1** : le panneau opérateur apparaît (sur le *Display 1* de la fenêtre Game).

Dans l'éditeur, choisir *Display 1* (la pièce) ou *Display 2* (le corridor) en haut de la fenêtre Game.

### Installer dans une autre scène (seulement si on repart de zéro)

1. Faire un commit git avant.
2. Menu **La Porte → Installer dans la scène ouverte** : crée l'objet `Jeu` avec ses 5 étapes et branche le récepteur OSC. Les lumières `Lights`, la lampe sur pied et les couleurs sont branchées seulement si l'ancien script `OSCObjectController` est dans la scène (il a été retiré de `Couloir`) : sinon, le rapport dit quoi brancher à la main. Annulable avec Ctrl+Z.
3. Menu **La Porte → Ajouter les placeholders de test** (voir section 8).
4. Menu **La Porte → Ajouter les étincelles à la prise** (voir section 3).
5. Ctrl+S.

Ces menus ne font rien si la scène contient déjà ce qu'ils ajoutent (`Jeu`, `Placeholders (à remplacer)`, `EtincellesPrise`).

---

## 2. Ce qu'il y a dans la scène

```
Jeu                      Capteurs · ReglagesJeu · GestionnaireEtapes · PanneauOperateur · ActiverEcrans
├── 0 - Attente des joueurs        EtapeAttente
├── 1 - Énigme des couleurs        EtapeEnigmeCouleurs
├── 2 - Indices (têtes + journal)  EtapeTimeline
├── 3 - Code du numpad             EtapeCodeNumpad
└── 4 - Porte ouverte              EtapeFin

OSCReceiver              reçoit l'OSC de TouchDesigner (port 7000)
Lights                   LumieresCorridor (light 1 à light 4)
light lefside            CopyLightIntensity : copie l'intensité de light 4
light rightside          CopyLightIntensity : copie l'intensité de light 4
Light (la lampe sur pied)           LampeSurPied
Point Light (7)          LumiereInterrupteur (Inverser coché)
Étincelles de la prise   Particle System + EtincellesPrise (devant « Outlet »)
Room Camera              Display 1 : la pièce (VISITEUR)
Corridor Camera          Display 2 : le corridor (LOCATAIRE)
Placeholders (à remplacer)          têtes, journal, afficheur du numpad, porte ouverte, bips
```

**L'ordre des enfants de `Jeu` = l'ordre de l'expérience.** Glisser pour réordonner, décocher une étape pour la retirer.

---

## 3. Les deux sortes de scripts

| | Rôle | Scripts |
|---|---|---|
| **Mécaniques** | Réagissent **toujours** aux objets, peu importe l'étape. | `LumieresCorridor`, `LampeSurPied`, `LumiereInterrupteur`, `JournalPersonnages`, `CopyLightIntensity`, `EtincellesPrise` |
| **Étapes** | Une seule active à la fois. Elles vérifient si l'énigme est réussie. | `EtapeAttente`, `EtapeEnigmeCouleurs`, `EtapeTimeline`, `EtapeCodeNumpad`, `EtapeFin` |

Comme dans une escape room : les objets répondent tout le temps, et le « maître du jeu » (`GestionnaireEtapes`) surveille la progression.

### Ce que font les mécaniques (à toutes les étapes)

| Objet physique | Effet dans Unity |
|---|---|
| **Poignée** (locataire) | Au repos : la lumière du plafond la plus proche de la caméra du corridor est allumée. Tournée vers le haut **ou** vers le bas : la zone éclairée glisse vers le fond du corridor. `light lefside` et `light rightside` suivent l'intensité de `light 4`. |
| **Interrupteur** (locataire) | **Allumé** : `light 3`, `light 2`, `light 1` prennent les couleurs de la solution, dans l'ordre (Bleu, Vert, Rouge); les autres restent blanches. La lampe sur pied s'allume. `Point Light (7)` s'éteint. Le journal montre la version « lampe allumée ». **Fermé** : tout l'inverse (lumières blanches, lampe éteinte, `Point Light (7)` allumée, version « lampe fermée »). |
| **Heurtoir** (visiteur) | La lampe sur pied tremble (animation `Lamp_interact`). **Allumée** : elle passe à la couleur suivante (en boucle). **Éteinte** : elle tremble ET sa lumière scintille un instant, puis redevient éteinte (pour montrer qu'on peut interagir avec elle; décocher *Scintiller Si Eteinte* sur `LampeSurPied` pour qu'elle tremble seulement). |
| **Sonnette** (visiteur) | **Lampe éteinte** : des **étincelles** sortent de la prise électrique (« Outlet »), à chaque coup. **Lampe allumée** : pas d'étincelles. À l'étape 1, elle sert aussi à sélectionner la couleur (lampe allumée). |

La lampe sur pied commence sur la **première couleur qui ne fait pas partie de la solution** (avec les réglages actuels : Jaune). On ne peut donc pas réussir le 1er choix par hasard en sonnant tout de suite.

---

## 4. Comment se joue l'expérience

| # | Étape | Ce qui se passe | On passe à la suite quand… | Si l'opérateur saute l'étape |
|---|---|---|---|---|
| 0 | Attente des joueurs | Tout est remis à neuf. Les mécaniques marchent déjà. | Premier contact : sonnette, heurtoir, interrupteur (qui change d'état) ou numpad. Ou Page Down. (La poignée ne démarre pas l'expérience.) | — |
| 1 | Énigme des couleurs | **Locataire** : poignée → lumières du corridor; interrupteur → révèle l'ordre des couleurs et allume la lampe. **Visiteur** : heurtoir → couleur suivante; sonnette → sélectionne la couleur affichée (seulement si la lampe est allumée). | Les 3 couleurs sont sélectionnées dans l'ordre : **Bleu → Vert → Rouge**. | Rien à remettre |
| 2 | Indices | Timeline `Indices` (2,1 s) : 3 têtes tombent de la ventilation du corridor, l'une après l'autre (ORANGE, CYAN, ROSE); le journal tombe de la bibliothèque sur la table de la pièce. | La Timeline est finie | La Timeline est mise à sa dernière image (têtes au sol, journal sur la table) |
| 3 | Code du numpad | Le locataire décrit les têtes, dans l'ordre où elles sont tombées. Le visiteur trouve leurs numéros dans le journal (l'interrupteur change les personnes lisibles) et entre le code. **TouchDesigner vérifie le code.** « ENTREZ LE CODE » est affiché dans la pièce. | TouchDesigner envoie « bon code » | Rien à remettre |
| 4 | Porte ouverte | « PORTE OUVERTE » en vert sur les deux écrans, bips de fin. Envoi OSC de la serrure si une adresse est remplie (vide pour l'instant). | Après **30 s**, l'expérience recommence au début | — |

### Détails de l'énigme des couleurs (étape 1)

- **Bonne couleur** : la lampe clignote 1 fois + bip aigu.
- **Mauvaise couleur** : la lampe clignote 3 fois + bip grave, et on recommence la combinaison **depuis la première couleur**.
- **Combinaison réussie** : la lampe clignote 4 fois vite + 3 bips qui montent → étape 2.
- **Sonnette avec la lampe éteinte** : ne compte pas.
- **Indice** : si personne ne cogne ni ne sonne pendant **6 s**, la lampe tremble pour attirer l'œil, et ça recommence toutes les 6 s. **Dès le premier coup de heurtoir de l'étape, l'indice s'arrête pour de bon** (le visiteur a compris). Réglages sur `1 - Énigme des couleurs` : décocher *Indice Actif* pour ne jamais avoir l'indice; *Secondes Avant Indice* pour le délai.

### Détails du numpad (étape 3)

- **Bon code** : 3 bips qui montent → étape 4.
- **Mauvais code** : 2 bips graves + « CODE FAUX » pendant 1,5 s, puis « ENTREZ LE CODE ». On peut réessayer autant qu'on veut.

### Recommencer

« Recommencer » = **la scène est rechargée et TOUT est remis à zéro**, y compris l'interrupteur (fermé : **lampe éteinte**) et la poignée (droite). C'est le cas peu importe comment on revient à l'étape 0 : Ctrl+R, bouton, fin de l'expérience, retour automatique, ou Page Up depuis l'étape 1.

Aller directement à une autre étape (ou la rejouer) = recharger la scène, appliquer l'état final des étapes d'avant (ex. : têtes déjà tombées), puis démarrer l'étape choisie. Là, l'interrupteur et la poignée **gardent** leur état.

> **Avec le vrai matériel :** si l'interrupteur est resté levé et que TouchDesigner envoie `/switch` en continu, la lampe se rallumera au message suivant (et l'expérience démarrera). Baisser l'interrupteur avant de recommencer.

**Retour automatique** : si personne ne touche à rien pendant **10 minutes** (et que l'étape dure depuis 10 minutes), l'expérience recommence au début. Ne s'applique pas à l'étape 0. Réglable sur `Jeu → GestionnaireEtapes` (0 = jamais). « Toucher » = sonnette, heurtoir, interrupteur, numpad, ou poignée qui bouge (plus que *Seuil Bruit Poignee*, sur `Jeu → Capteurs` : l'augmenter si le capteur tremble et que le retour automatique n'arrive jamais).

### Les solutions (`Jeu → ReglagesJeu`)

| Réglage | Valeur actuelle |
|---|---|
| Couleurs de la lampe | Bleu, Rouge, Vert, Jaune, Mauve (dans l'ordre du heurtoir) |
| Ordre des couleurs | `0, 2, 1` = Bleu → Vert → Rouge |
| Code du numpad | `1234` |

**Le code du numpad est vérifié par TouchDesigner.** `ReglagesJeu → codeNumpad` sert seulement à écrire le journal. Si on change le code, **le changer aussi dans TouchDesigner**.

---

## 5. Contrat OSC (pour l'équipe TouchDesigner)

Unity écoute sur le **port 7000**. L'IP exacte de l'ordi est affichée dans le panneau F1.
Les valeurs peuvent être des float, des int, des booléens ou du texte (« 1 »).

| Objet | Adresse | Valeur | À noter |
|---|---|---|---|
| Poignée | `/rotation` | -1 à 1 | En continu. 0 = au repos. |
| Sonnette | `/sonette` | 1 appuyée, 0 relâchée | En continu ou seulement à l'appui : les deux marchent. Anti-rebond 0,3 s. |
| Heurtoir | `/vibration` | 1 = coup détecté | Anti-rebond 0,25 s. |
| Interrupteur | `/switch` | 1 allumé, 0 fermé | Seul un **changement** compte. |
| Numpad | `/code` | **0** = rien, **1** = bon code, **-1** = mauvais code | **TouchDesigner vérifie le code** et envoie **en continu**. Un essai compte quand la valeur **change** (0 → 1 ou 0 → -1) : **revenir à 0 entre deux essais**. La toute première valeur reçue après un (re)chargement de la scène est ignorée (c'est l'état actuel, pas un essai). |
| *(optionnel)* Serrure | au choix (ex. `/porte`) | 1 ouvrir, 0 verrouiller | Unity → TD. Remplir `Adresse Osc Porte` sur les étapes 0 et 4, et assigner un `OSCTransmitter` sur `Jeu → Capteurs`. **Pas utilisé pour l'instant.** |

Les adresses, les anti-rebonds et le seuil de bruit de la poignée se changent dans l'Inspector (`Jeu → Capteurs`), sans toucher au code.

---

## 6. Fiche opérateur (à imprimer et coller à côté du clavier)

| Touche | Action |
|---|---|
| **F1** | Ouvrir / fermer le panneau (et la souris dans le build) |
| **Page Down** | Étape suivante (force la réussite) |
| **Page Up** | Étape précédente |
| **F5** | Rejouer l'étape en cours depuis son début |
| **Ctrl + R** | Recommencer toute l'expérience |
| **S** | Simuler la sonnette |
| **H** | Simuler le heurtoir |
| **L** | Basculer l'interrupteur |
| **← →** | Tourner la poignée |
| **B** | Simuler un bon code au numpad |
| **M** | Simuler un mauvais code au numpad |

**Toutes les touches marchent, panneau F1 ouvert OU fermé.** Pour couper les touches de simulation (S, H, L, ← →, B, M), décocher *Simulation Clavier* sur `Jeu → PanneauOperateur`. F1, Page Up/Down, F5 et Ctrl+R marchent toujours.

Dans le panneau F1 :

- boutons Précédente / Rejouer l'étape / Suivante / Recommencer toute l'expérience;
- la liste des étapes : un clic pour y aller directement;
- l'étape en cours : depuis combien de temps, sa note, et sa progression (solution des couleurs, état de la lampe, code attendu, mauvais essais, temps de la Timeline, compte à rebours de la fin);
- des boutons pour simuler les capteurs, et un curseur pour la poignée;
- le **moniteur OSC** : chaque adresse reçue, sa valeur et il y a combien de temps. Vert = reçu il y a moins de 2 s. Si rien n'arrive, le problème est côté TouchDesigner, Arduino ou réseau, pas dans Unity.

---

## 7. Les écrans

| Écran | Caméra | Qui le voit |
|---|---|---|
| Display 1 | `Room Camera` | Le **visiteur** (la pièce). Le panneau F1 s'affiche ici. |
| Display 2 | `Corridor Camera` | Le **locataire** (le corridor) |

Dans le build, `ActiverEcrans` allume le 2e écran au démarrage. Le build s'ouvre en plein écran et continue de tourner même si la fenêtre n'a pas le focus.

> **À savoir :** le modèle `Couloir.fbx` importe aussi sa propre caméra, `Camera Room`. Il y a donc deux caméras pour la pièce : à décider laquelle garder (voir section 10).

---

## 8. Placeholders de test (à remplacer)

Tout est rangé sous **`Placeholders (à remplacer)`**, pour que le jeu se teste du début à la fin.

| Ce qui manque | Placeholder | Comment tester |
|---|---|---|
| Têtes | 3 cubes **orange, cyan, rose** qui tombent de la ventilation (écran du corridor) | Réussir l'énigme des couleurs, ou Page Down |
| Journal | Cube blanc qui tombe de la bibliothèque sur la table (écran de la pièce) | Idem |
| Pages du journal | Texte jaune au-dessus du journal, visible une fois le journal sur la table | Interrupteur (**L**) pour changer de version |
| Retour du numpad | « ENTREZ LE CODE » en bas de l'écran de la pièce (seulement à l'étape 3), « CODE FAUX » 1,5 s si c'est mauvais | **B** / **M** |
| Porte ouverte | « PORTE OUVERTE » en vert sur les deux écrans | Bon code (**B**) |
| Sons | Bips générés par le code (sous `Sons (bips)`) | Toutes les étapes |

### Ce que dit le journal placeholder (avec le code `1234`)

Le code est découpé entre les 3 têtes, **dans l'ordre où elles tombent** : ORANGE = 12, CYAN = 3, ROSE = 4.

| Lampe allumée | Lampe fermée |
|---|---|
| 1. ORANGE = 12 | 1. ?????? |
| 2. ?????? | 2. BLANC = 58 *(leurre)* |
| 3. GRIS = 71 *(leurre)* | 3. ?????? |
| 4. ?????? | 4. CYAN = 3 |
| 5. ROSE = 4 | 5. ?????? |
| 6. ?????? | 6. NOIR = 96 *(leurre)* |

Si on change le code dans `ReglagesJeu`, le journal se met à jour tout seul (et il faut le changer dans TouchDesigner aussi).

### Remplacer un placeholder

- **Têtes et journal** : refaire l'animation dans la Timeline `Assets/Placeholders/Indices.playable` (ou en créer une nouvelle), l'assigner à l'étape « 2 - Indices », puis supprimer les cubes. Sur le PlayableDirector, décocher *Play On Awake*.
- **Pages du journal** : mettre les deux vraies images sur `JournalPersonnages` (*Version Lampe Allumee* / *Version Lampe Fermee*), puis supprimer les textes.
- **Sons** : glisser le vrai son dans l'*AudioClip* de l'AudioSource. Le bip ne joue plus dès qu'un vrai son est là.
- **Porte ouverte** : remplacer les objets dans `4 - Porte ouverte → Objets A Afficher` (ou mettre une Timeline de fin dans *Timeline Fin*).

Quand tout est remplacé, supprimer `Placeholders (à remplacer)`, `Assets/Placeholders` et `Assets/Scripts/Placeholders`.

---

## 9. Ajouter une étape

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

Aussi disponibles : `Sortir()`, `QuandHeurtoir()`, `QuandInterrupteur(bool allume)`, `QuandCodeNumpad(bool bonCode)`.

2. Créer un GameObject enfant de `Jeu`, à la bonne place, et y mettre le script.

**Règles d'or :**

- Pour attendre, utiliser des **coroutines** (`StartCoroutine`) plutôt que `async` / `Task.Delay`. Elles s'arrêtent toutes seules quand on saute ou recommence une étape.
- Un script qui écoute `Capteurs` s'abonne dans `OnEnable` et se **désabonne dans `OnDisable`**.
- Les animations scriptées (têtes, journal, porte) se font dans une **Timeline**, avec *Play On Awake* décoché. Le système peut alors sauter directement à la fin.

---

## 10. Ce qui reste à décider ou à faire

- [ ] **TD** : envoyer l'interrupteur sur `/switch` (sinon, utiliser la touche **L**).
- [ ] **TD** : vérifier le code du numpad et envoyer `/code` **en continu** (0 / 1 / -1), avec le même code que `ReglagesJeu → codeNumpad`.
- [ ] Le **code du numpad** (`1234` pour l'instant), le même dans Unity et dans TD.
- [ ] Le **nombre de couleurs** de la combinaison, et quelles lumières du plafond les affichent (pour l'instant : light 3, light 2, light 1 = Bleu → Vert → Rouge).
- [ ] Ce qui se passe quand le visiteur se **trompe de couleur** (pour l'instant : 3 clignotements et on recommence au début de la combinaison).
- [ ] Modéliser les **têtes** et le **journal**, et refaire la Timeline de l'étape 2.
- [ ] Les deux versions de la page du journal (floue A / B) pour `JournalPersonnages`.
- [ ] La **porte qui s'ouvre** et les **sons**.
- [ ] Choisir la caméra de la pièce : `Camera Room` (du modèle Blender) ou `Room Camera`.
- [ ] Une **serrure électrique**? Si oui, donner l'adresse OSC.
