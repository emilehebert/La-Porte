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
4. Menu **La Porte → Brancher les têtes (Head_01 à Head_05)** (voir section 8).
5. Menu **La Porte → Brancher les journaux** (voir section 8, « Les journaux »).
6. Menu **La Porte → Ajouter les étincelles à la prise** (voir section 3).
7. Ctrl+S.

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
Lights                   LumieresCorridor (light 1 à light 3 : Rouge, Vert, Bleu)
light lefside            CopyLightIntensity : copie l'intensité de light 3
light middle             CopyLightIntensity : copie l'intensité de light 3
light rightside          CopyLightIntensity : copie l'intensité de light 3
Light (la lampe sur pied)           LampeSurPied
Point Light (7)          LumiereInterrupteur (Inverser coché)
Étincelles de la prise   Particle System + EtincellesPrise (devant « Outlet »)
Room Camera              Display 1 : la pièce (VISITEUR)
Corridor Camera          Display 2 : le corridor (LOCATAIRE)
Placeholders (à remplacer)          afficheur du numpad, porte ouverte, bips (le journal placeholder est désactivé)
Head_01 à Head_05        les 5 têtes : 2 tombent au hasard à chaque partie (TetesQuiTombent)
Journaux qui tombent     les 5 journaux de la bibliothèque (une affiche par tête) + 2 arrivées sur la table (JournauxQuiTombent)
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
| **Poignée** (locataire) | **Seulement quand l'interrupteur est allumé.** Au repos : la lumière du plafond la plus proche de la caméra du corridor est allumée. Tournée vers le haut **ou** vers le bas : la zone éclairée glisse vers le fond du corridor. `light lefside`, `light middle` et `light rightside` (juste au-dessus de la caméra) suivent l'intensité de `light 3`, la plus proche. **Interrupteur fermé** (tout le corridor allumé) : tourner la poignée fait **scintiller** les lumières à sa position, avec le même scintillement que la lampe sur pied. Ça montre au locataire que la poignée fait quelque chose (réglages *Scintillement* sur `Lights → LumieresCorridor`). |
| **Interrupteur** (locataire) | **Fermé** : **toutes les lumières du corridor sont allumées**, en blanc. **Allumé** : le corridor s'assombrit et seule la zone de la poignée reste éclairée; `light 3`, `light 2`, `light 1` prennent les couleurs de la solution, dans l'ordre (Bleu, Vert, Rouge), les autres restent blanches. Le passage d'un mode à l'autre est progressif (*Vitesse* sur `Lights → LumieresCorridor`). La lampe sur pied s'allume. `Point Light (7)` s'éteint. **Fermé** : tout l'inverse (corridor tout allumé en blanc, lampe éteinte, `Point Light (7)` allumée). **À partir de l'étape 2**, l'interrupteur ne change plus l'éclairage : le corridor (tout en blanc) et `Point Light (7)` sont rallumés et le restent, pour qu'on voie les têtes et les journaux tomber. |
| **Heurtoir** (visiteur) | La lampe sur pied tremble (animation `Lamp_interact`). **Allumée** : elle passe à la couleur suivante (en boucle). **Éteinte** : elle tremble ET sa lumière scintille un instant, puis redevient éteinte (pour montrer qu'on peut interagir avec elle; décocher *Scintiller Si Eteinte* sur `LampeSurPied` pour qu'elle tremble seulement). |
| **Sonnette** (visiteur) | **Lampe éteinte** : des **étincelles** sortent de la prise électrique (« Outlet »), à chaque coup. **Lampe allumée** : pas d'étincelles. À l'étape 1, elle sert aussi à sélectionner la couleur (lampe allumée). |

La lampe sur pied commence sur la **première couleur qui ne fait pas partie de la solution** (avec les réglages actuels : Jaune). On ne peut donc pas réussir le 1er choix par hasard en sonnant tout de suite.

---

## 4. Comment se joue l'expérience

| # | Étape | Ce qui se passe | On passe à la suite quand… | Si l'opérateur saute l'étape |
|---|---|---|---|---|
| 0 | Attente des joueurs | Tout est remis à neuf. Les mécaniques marchent déjà. | Premier contact : sonnette, heurtoir, interrupteur (qui change d'état) ou numpad. Ou Page Down. (La poignée ne démarre pas l'expérience.) | — |
| 1 | Énigme des couleurs | **Locataire** : interrupteur → révèle l'ordre des couleurs et allume la lampe; poignée → promène la zone éclairée pour voir chaque couleur. **Visiteur** : heurtoir → couleur suivante; sonnette → sélectionne la couleur affichée (seulement si la lampe est allumée). | Les 3 couleurs sont sélectionnées dans l'ordre : **Bleu → Vert → Rouge**. | Rien à remettre |
| 2 | Indices | Timeline `Indices` (2,1 s) : la pièce et le corridor se rallument (la lampe validée, l'interrupteur est levé et la pièce était sombre); **2 têtes choisies au hasard** parmi les 5 tombent de la ventilation du corridor, l'une après l'autre, dans un ordre au hasard, et restent pendues à une corde élastique (le heurtoir les fait bouger); **en même temps**, le journal de chaque tête tombe de la bibliothèque sur la table de la pièce (JournauxQuiTombent). | La Timeline est finie | La Timeline est mise à sa dernière image (têtes pendues, journaux sur la table) |
| 3 | Code du numpad | Le locataire décrit les têtes, dans l'ordre où elles sont tombées. Le visiteur trouve leurs numéros sur les journaux tombés (une affiche « HELP WANTED » par tête, avec son numéro) et entre le code. **Le code change à chaque partie** : Unity l'envoie à TouchDesigner, qui le vérifie. « ENTREZ LE CODE » est affiché dans la pièce. | TouchDesigner envoie « bon code » | Rien à remettre |
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
| Têtes (*Tetes*) et leur numéro | Moustachu **4**, Chauve **7**, Afro **2**, Lunettes **9**, Rousse **5** |
| Code du numpad | Les numéros des 3 têtes tombées, **dans l'ordre de chute**. Ex. : Lunettes → Afro → Moustachu = `924` |

**Le code change à chaque partie**, parce que les têtes sont tirées au hasard à chaque chargement de la scène (recommencer, rejouer une étape ou sauter à une étape = nouvelles têtes, nouveau code). Le code de la partie est affiché dans le panneau F1 (étape 3) et dans la Console.

**C'est toujours TouchDesigner qui vérifie le code**, mais il le reçoit maintenant de Unity sur `/code_attendu` (voir section 5). Les numéros des têtes ne changent pas : le vrai journal peut donc être une image fixe.

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
| Numpad | `/code` | **0** = rien, **1** = bon code, **-1** = mauvais code | **TouchDesigner vérifie le code** (celui reçu sur `/code_attendu`) et envoie **en continu**. Un essai compte quand la valeur **change** (0 → 1 ou 0 → -1) : **revenir à 0 entre deux essais**. La toute première valeur reçue après un (re)chargement de la scène est ignorée (c'est l'état actuel, pas un essai). |
| Code de la partie | `/code_attendu` | un nombre, ex. `924` | **Unity → TD.** Le code à vérifier : il change à chaque partie. Envoyé au début de l'étape 3, puis chaque seconde pendant l'étape. Il faut un `OSCTransmitter` assigné sur `Jeu → Capteurs` (IP et port de l'ordi de TouchDesigner). Adresse modifiable sur `3 - Code du numpad`. |
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
| Retour du numpad | « ENTREZ LE CODE » en bas de l'écran de la pièce (seulement à l'étape 3), « CODE FAUX » 1,5 s si c'est mauvais | **B** / **M** |
| Porte ouverte | « PORTE OUVERTE » en vert sur les deux écrans | Bon code (**B**) |
| Sons | Bips générés par le code (sous `Sons (bips)`) | Toutes les étapes |

### Les têtes

Les 5 têtes sont dans `ReglagesJeu → Tetes` : ce qui les distingue (le nom écrit dans le journal), leur numéro et leur prefab (`Assets/Prefabs/Tetes`). À chaque partie, 3 sont tirées au hasard; les 2 autres ne sont pas créées. Le menu **La Porte → Finaliser les têtes** branche les prefabs et les emplacements (on peut le refaire sans danger).

- **Corde élastique** : chaque tête tombe de son emplacement (objet vide sous `Cinématique des indices / Têtes qui tombent`) et reste **pendue au bout d'une corde** : elle rebondit un peu, puis se balance et se calme.
- **Heurtoir** : chaque coup **pousse les têtes et les fait tourner sur elles-mêmes**, pour que le locataire voie leurs détails.
- **Déplacer un emplacement** = choisir où la corde est attachée. Sélectionner `Têtes qui tombent` pour voir, en jaune, la corde et la tête au repos (en rouge : la tête toucherait le sol, la corde est trop longue).
- Si on saute l'étape, les têtes sont mises directement au bout de leur corde, immobiles.

Réglages sur `Têtes qui tombent → TetesQuiTombent` :

| Réglage | Effet |
|---|---|
| Longueur Corde | 1,6 m = la tête pend à hauteur des yeux de la caméra du corridor |
| Raideur | Plus petit = plus élastique (la tête descend plus bas et rebondit plus) |
| Amortissement | Plus grand = moins de rebonds |
| Freinage Balancement / Rotation | Plus grand = la tête se calme plus vite après la chute ou un coup de heurtoir |
| Poussée Heurtoir / Rotation Heurtoir | La force d'un coup de heurtoir (m/s et tours/s). 0 = le heurtoir ne bouge pas les têtes |
| Épaisseur Corde / Materiau Corde | L'apparence de la corde (par défaut `Assets/Resources/CordeTete`) |

### Les journaux

Un journal par tête dans la bibliothèque, avec sur la couverture l'affiche « HELP WANTED » de la tête (sa photo et son numéro, en très gros pour rester lisible en basse résolution). À l'étape 2, **les journaux des 2 têtes tombées** glissent hors de l'étagère et tombent sur la table, couverture en haut, **chacun en même temps que sa tête** dans le corridor; les 3 autres restent rangés. Si on saute l'étape, ils sont posés directement sur la table.

Les journaux sont **plus grands** que celui du modèle (`Agrandissement` en haut de `Assets/Scripts/Editor/JournauxLaPorte.cs`, 1,35 voulu) : le menu le limite tout seul pour que le journal debout ne touche pas la planche du dessus (environ x1,15 avec l'étagère actuelle : 38,7 cm entre les planches). Sur la table, les 2 journaux sont posés côte à côte, alignés sur les bords de la table, sans se chevaucher.

Le menu **La Porte → Brancher les journaux** fait tout (on peut le refaire sans danger) :

- crée les matériaux `Assets/Materials/Journal/Journal_<numéro>_<nom>.mat` (copie du matériau de la couverture du journal du décor, avec l'affiche `Assets/Textures/Journal/Affiche_<numéro>_<nom>.png`) et `Journal_tranche.mat` (les côtés, couleur papier);
- range 5 copies du journal du décor (modèle `Couloir`, caché ensuite) à côté de lui sur l'étagère, dans l'ordre de `ReglagesJeu → Tetes`, sous `Journaux qui tombent`;
- crée 2 `Arrivée journal` sur la table, tournées pour que l'affiche se lise à l'endroit depuis la Room Camera;
- branche l'étape « 2 - Indices » (Au Debut → Lacher, Etat Final → PoserSurLaTable), ainsi que le rallumage de la pièce et du corridor (Au Debut et Etat Final → `ToutAllumer` sur `Point Light (7)` et sur `Lights`), et désactive le journal placeholder.

Les journaux partent toujours de la place du journal **dans le modèle Couloir** (sur l'étagère), même si le journal du décor a été déplacé dans la scène. Les journaux et les arrivées déjà créés ne sont pas déplacés (on peut les ajuster à la main), sauf si les journaux ne sont plus dans la bibliothèque ou pas à la bonne taille : refaire le menu les range à nouveau et recalcule les arrivées. **Changer un numéro ou une affiche** : regénérer les affiches (`La Porte/Textures/Journal/generer_help_wanted.py`), les copier dans `Assets/Textures/Journal`, puis refaire le menu.

- **Déplacer / tourner une arrivée** = choisir où le journal atterrit. La flèche bleue (avant) = le haut de l'affiche. Sélectionner `Journaux qui tombent` pour voir, en jaune, les journaux posés.

Réglages sur `Journaux qui tombent → JournauxQuiTombent` :

| Réglage | Effet |
|---|---|
| En Meme Temps Que Les Tetes | Coché (par défaut) : chaque journal tombe en même temps que sa tête (mêmes délais que `Têtes qui tombent`) |
| Délai Avant Le Premier / Entre Les Journaux | Seulement si la case du dessus est décochée : quand les journaux tombent (secondes après le début de l'étape) |
| Durée Sortie / Distance Sortie / Bascule | Le journal glisse hors de l'étagère en penchant vers la pièce |
| Élan / Gravité | Petit saut vers le haut au départ, puis la chute (plus de gravité = plus rapide) |
| Rebond / Durée Rebond | Le petit rebond sur la table (0 = aucun) |

### Remplacer un placeholder

- **Têtes et journal** : refaire l'animation dans la Timeline `Assets/Placeholders/Indices.playable` (ou en créer une nouvelle) et l'assigner à l'étape « 2 - Indices ». Comme les têtes changent à chaque partie, **animer 3 emplacements (des objets vides), jamais les têtes elles-mêmes**, puis les mettre dans `TetesQuiTombent → Emplacements`, dans l'ordre de chute. Supprimer les cubes ensuite. Sur le PlayableDirector, décocher *Play On Awake*.
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
- [ ] **TD** : recevoir le code de la partie sur `/code_attendu`, le comparer à ce qui est tapé, et envoyer `/code` **en continu** (0 / 1 / -1).
- [ ] Ajouter un **OSCTransmitter** (IP et port de l'ordi de TD) et l'assigner sur `Jeu → Capteurs → Transmetteur` : sans lui, TD ne reçoit pas le code.
- [ ] Le **nombre de couleurs** de la combinaison, et quelles lumières du plafond les affichent (pour l'instant : light 3, light 2, light 1 = Bleu → Vert → Rouge).
- [ ] Ce qui se passe quand le visiteur se **trompe de couleur** (pour l'instant : 3 clignotements et on recommence au début de la combinaison).
- [x] Modéliser les **têtes** (3 tombent au hasard à la place des cubes).
- [x] Le **journal** : une affiche par tête, les journaux des têtes tombées tombent sur la table (menu **La Porte → Brancher les journaux**, section 8).
- [ ] Décider si l'interrupteur doit encore changer quelque chose sur les journaux (l'ancienne idée « page floue A / B » de `JournalPersonnages` n'est pas utilisée par les affiches).
- [ ] La **porte qui s'ouvre** et les **sons**.
- [ ] Choisir la caméra de la pièce : `Camera Room` (du modèle Blender) ou `Room Camera`.
- [ ] Une **serrure électrique**? Si oui, donner l'adresse OSC.
