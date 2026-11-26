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
7. Menu **La Porte → Brancher les sons** (voir section 7, « Le son »).
8. Menu **La Porte → Ajouter le reflet sur la clé** (voir section 3, « Le reflet de la clé »).
9. Ctrl+S.

Ces menus ne font rien si la scène contient déjà ce qu'ils ajoutent (`Jeu`, `Placeholders (à remplacer)`, `EtincellesPrise`, le reflet de la clé).

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
Reflet (Key)             RefletIndice : le petit reflet qui flashe sur la clé (suit « Key »)
Room Camera              Display 1 : la pièce (VISITEUR)
Corridor Camera          Display 2 : le corridor (LOCATAIRE)
Placeholders (à remplacer)          bips seulement (plus de texte : menu « Retirer les textes placeholder »)
Head_01 à Head_05        les 5 têtes : 2 tombent au hasard à chaque partie (TetesQuiTombent)
Journaux qui tombent     les 5 journaux de la bibliothèque (une affiche par tête) + 2 arrivées sur la table (JournauxQuiTombent)
Sons                     ReglagesSons · SonsInteractions, avec un enfant par pièce : Chambre (gauche), Corridor (droite), Les deux
```

**L'ordre des enfants de `Jeu` = l'ordre de l'expérience.** Glisser pour réordonner, décocher une étape pour la retirer.

---

## 3. Les deux sortes de scripts

| | Rôle | Scripts |
|---|---|---|
| **Mécaniques** | Réagissent **toujours** aux objets, peu importe l'étape. | `LumieresCorridor`, `LampeSurPied`, `LumiereInterrupteur`, `JournalPersonnages`, `CopyLightIntensity`, `EtincellesPrise`, `SonsInteractions`, `AmbianceContinue`, `RefletIndice` |
| **Étapes** | Une seule active à la fois. Elles vérifient si l'énigme est réussie. | `EtapeAttente`, `EtapeEnigmeCouleurs`, `EtapeTimeline`, `EtapeCodeNumpad`, `EtapeFin` |

Comme dans une escape room : les objets répondent tout le temps, et le « maître du jeu » (`GestionnaireEtapes`) surveille la progression. **Seule exception : l'écran titre** (étape 0, section 4) met les objets en pause jusqu'à ce que la scène soit entièrement visible.

### Ce que font les mécaniques (à toutes les étapes)

| Objet physique | Effet dans Unity |
|---|---|
| **Poignée** (locataire) | **Seulement quand l'interrupteur est allumé.** Au repos : la lumière du plafond la plus proche de la caméra du corridor est allumée. Tournée vers le haut **ou** vers le bas : la zone éclairée glisse vers le fond du corridor. `light lefside`, `light middle` et `light rightside` (juste au-dessus de la caméra) suivent l'intensité de `light 3`, la plus proche. **Interrupteur fermé** (tout le corridor allumé) : tourner la poignée fait **scintiller** les lumières à sa position, avec le même scintillement que la lampe sur pied. Ça montre au locataire que la poignée fait quelque chose (réglages *Scintillement* sur `Lights → LumieresCorridor`). |
| **Interrupteur** (locataire) | **Fermé** : **toutes les lumières du corridor sont allumées**, en blanc. **Allumé** : le corridor s'assombrit et seule la zone de la poignée reste éclairée; `light 3`, `light 2`, `light 1` prennent les couleurs de la solution, dans l'ordre (Bleu, Vert, Rouge), les autres restent blanches. Le passage d'un mode à l'autre est progressif (*Vitesse* sur `Lights → LumieresCorridor`). La lampe sur pied s'allume. `Point Light (7)` s'éteint. **Fermé** : tout l'inverse (corridor tout allumé en blanc, lampe éteinte, `Point Light (7)` allumée). **Au début de l'étape 2**, le jeu passe tout seul en **mode lumières** (plafond de la pièce allumé, lampe éteinte, corridor en blanc) et **y reste** jusqu'à ce que quelqu'un bascule l'interrupteur. L'interrupteur physique est alors encore levé : à partir de là, **chaque bascule change de mode**, peu importe sa position (`Capteurs.ForcerModeLumieres`, case *Passer En Mode Lumieres* sur `2 - Indices`). Le corridor reste tout en blanc jusqu'à la fin. **À partir de l'étape 3**, une bascule passe en **mode couleurs** : `Point Light (7)` éteinte, lampe sur pied allumée, et les **chiffres phosphorescents** des journaux tombés brillent en vert. Une autre bascule revient en **mode lumières** : on voit les visages, les chiffres sont invisibles. |
| **Heurtoir** (visiteur) | La lampe sur pied tremble (animation `Lamp_interact`). **Allumée** : elle passe à la couleur suivante (en boucle). **Éteinte** : elle tremble ET sa lumière scintille un instant, puis redevient éteinte (pour montrer qu'on peut interagir avec elle; décocher *Scintiller Si Eteinte* sur `LampeSurPied` pour qu'elle tremble seulement). |
| **Sonnette** (visiteur) | **Lampe éteinte** : des **étincelles** sortent de la prise électrique (« Outlet »), à chaque coup. **Lampe allumée** : pas d'étincelles. À l'étape 1, elle sert aussi à sélectionner la couleur (lampe allumée). |

La lampe sur pied commence sur la **première couleur qui ne fait pas partie de la solution** (avec les réglages actuels : Jaune). On ne peut donc pas réussir le 1er choix par hasard en sonnant tout de suite.

### Le reflet de la clé

Une **étoile lumineuse** flashe sur la clé (`Key`) pour attirer l'œil du visiteur, **sans fin** (elle attend seulement la fin de l'écran titre).

1. Menu **La Porte → Ajouter le reflet sur la clé**, puis Ctrl+S.
2. Déplacer l'objet `Reflet (Key)` à l'endroit voulu sur la clé (l'étoile jaune dans la vue Scene).

Réglages sur `Reflet (Key)` :

| Réglage | Effet |
|---|---|
| Actif | Décoché : plus de flashs |
| Vitesse | 1 = normal, 2 = deux fois plus rapide, 0,5 = deux fois plus lent |
| Repetitions | Nombre de flashs à la suite (1 = « tching », 2 = « tching-tching ») |
| Intervalle | Secondes entre deux séries de flashs |
| Taille Etoile | La taille de l'étoile en mètres (0,35 par défaut) |
| Couleur / Brillance | La couleur, et à quel point elle éclate (5 par défaut, jusqu'à 10) |

**Tester** : Play, **Page Down** (passe l'écran titre), regarder *Display 1* : premier flash après 1,5 s. Pour un flash tout de suite : clic droit sur le titre du composant → **Flasher maintenant**.

---

## 4. Comment se joue l'expérience

| # | Étape | Ce qui se passe | On passe à la suite quand… | Si l'opérateur saute l'étape |
|---|---|---|---|---|
| 0 | Attente des joueurs | Tout est remis à neuf. **Écran titre** sur les deux écrans (voir plus bas). **Les objets sont en pause** jusqu'à la fin du fondu : rien ne change dans le décor, aucun son d'objet. | Le locataire **bascule l'interrupteur** et le visiteur **sonne**, **en même temps** (moins de 1,5 s d'écart, peu importe qui commence). Ou Page Down. Les deux écrans font alors un fondu vers la scène. | — |
| 1 | Énigme des couleurs | **Locataire** : interrupteur → révèle l'ordre des couleurs et allume la lampe; poignée → promène la zone éclairée pour voir chaque couleur. **Visiteur** : heurtoir → couleur suivante; sonnette → sélectionne la couleur affichée (seulement si la lampe est allumée). | Les 3 couleurs sont sélectionnées dans l'ordre : **Bleu → Vert → Rouge**. | Rien à remettre |
| 2 | Indices | Timeline `Indices` (2,1 s) : le jeu passe en **mode lumières** et y reste (plafond allumé, lampe éteinte, corridor en blanc), même si l'interrupteur est encore levé; **2 têtes choisies au hasard** parmi les 5 tombent de la ventilation du corridor, l'une après l'autre, dans un ordre au hasard, et restent pendues à une corde élastique (le heurtoir les fait bouger); **en même temps**, le journal de chaque tête tombe de la bibliothèque sur la table de la pièce (JournauxQuiTombent). | La Timeline est finie | La Timeline est mise à sa dernière image (têtes pendues, journaux sur la table) |
| 3 | Code du numpad | Le locataire décrit les têtes, dans l'ordre où elles sont tombées. Le visiteur trouve leurs numéros sur les journaux tombés : à la lumière il reconnaît les visages, dans le noir (interrupteur levé) il lit le numéro phosphorescent peint sur chaque visage. Il entre le code. **Le code change à chaque partie** : Unity l'envoie à TouchDesigner, qui le vérifie. Aucun texte n'est affiché : seulement des bips (bon / mauvais code). | TouchDesigner envoie « bon code » | Rien à remettre |
| 4 | Porte ouverte | Bips de fin (pas de texte; mettre les vrais objets ou une Timeline de fin sur l'étape). Envoi OSC de la serrure si une adresse est remplie (vide pour l'instant). | Après **30 s**, l'expérience recommence au début | — |

### L'écran titre (étape 0)

- **Sur les deux écrans** : fond noir, le **logo** au centre qui grossit et rapetisse comme un **cœur qui bat lentement** (boum-boum… pause), et en bas le texte « Appuyez sur l'interrupteur et interagissez avec la sonnette en même temps pour commencer l'expérience ».
- **Les objets sont en pause** tant que l'écran titre est là, **fondu compris** (jusqu'à ce que la scène soit entièrement visible) : l'interrupteur ne change pas les lumières ni la lampe, la sonnette ne fait pas d'étincelles, le heurtoir ne fait pas trembler la lampe, la poignée et le numpad ne font rien, et **aucun son d'objet** ne joue (les ambiances continuent). Les joueurs découvrent donc toujours la scène dans son état de départ : **mode lumières**, lampe éteinte, poignée droite. La bascule de l'interrupteur faite pour démarrer ne compte pas : la **prochaine** bascule passera en mode couleurs.
- **Un seul joueur agit** : le logo fait un battement plus fort (« ça a compté, il manque l'autre »).
- **Les deux ensemble** : les deux écrans font un **fondu** de 2,5 s vers la scène, et l'étape 1 commence. Les objets se réveillent à la fin du fondu.
- **Page Down** : même fondu. **Sauter directement à une autre étape** (liste du F1, F5 sur une autre étape) : pas d'écran titre.
- Il revient tout seul à chaque retour au début (fin de l'expérience, Ctrl+R, retour automatique).
- Au clavier : **L** puis **S** (moins de 1,5 s entre les deux).

**Le logo** : mettre le fichier dans **`Assets/Resources/Logo.png`** (PNG avec transparence) : il est trouvé tout seul. Ou le glisser dans *Ecran Titre → Logo* sur `0 - Attente des joueurs`. Sans logo, l'écran affiche « LA PORTE » en texte.

Réglages sur `Jeu → 0 - Attente des joueurs` :

| Réglage | Effet |
|---|---|
| Demarrer Au Premier Contact | Décoché : seul l'opérateur peut démarrer (Page Down) |
| Fenetre Secondes | L'écart maximum entre l'interrupteur et la sonnette pour que ça compte « en même temps » (1,5 s) |
| Afficher Ecran Titre | Décoché : pas d'écran titre ni de pause (la scène est visible pendant l'attente et les objets y réagissent; au démarrage, le jeu revient en mode lumières) |
| Ecran Titre → Logo / Taille Logo | Le logo, et sa hauteur en % de l'écran |
| Ecran Titre → Texte / Taille Texte / Couleur Texte | Le petit texte en bas |
| Ecran Titre → Battements Par Minute | 40 = lent (par défaut). Plus haut = plus rapide |
| Ecran Titre → Amplitude | De combien le logo grossit (0,08 = 8 %) |
| Ecran Titre → Double Battement | Décoché : un seul gonflement doux au lieu du « boum-boum » |
| Ecran Titre → Duree Fondu | La durée du fondu vers la scène (secondes) |

L'écran titre est créé par le script (`EcranTitre`) : **il n'y a rien à mettre dans la scène**. Il se dessine par-dessus les deux caméras (un Canvas par Display). Le panneau F1 reste visible par-dessus, et dit « Objets en pause » pendant la pause. La pause est dans `Capteurs` (`Capteurs.EnPause`) : un nouveau script d'objet qui écoute `Capteurs` est donc en pause tout seul.

### Détails de l'énigme des couleurs (étape 1)

- **Bonne couleur** : la lampe clignote 1 fois + bip aigu.
- **Mauvaise couleur** : la lampe clignote 3 fois + bip grave, et on recommence la combinaison **depuis la première couleur**.
- **Combinaison réussie** : la lampe clignote 4 fois vite + 3 bips qui montent → étape 2.
- **Après l'énigme** (réussie ou sautée) : la lampe devient **mauve** et le reste pour toute la partie. Le heurtoir la fait encore trembler, mais ne change plus sa couleur. Réglage *Couleur Apres Reussite* sur `1 - Énigme des couleurs` (vide = la lampe continue de changer).
- **Sonnette avec la lampe éteinte** : ne compte pas.
- **Indice** : si personne ne cogne ni ne sonne pendant **6 s**, la lampe tremble pour attirer l'œil, et ça recommence toutes les 6 s. **Dès le premier coup de heurtoir de l'étape, l'indice s'arrête pour de bon** (le visiteur a compris). Réglages sur `1 - Énigme des couleurs` : décocher *Indice Actif* pour ne jamais avoir l'indice; *Secondes Avant Indice* pour le délai.

### Détails du numpad (étape 3)

- **Bon code** : 3 bips qui montent → étape 4.
- **Mauvais code** : 2 bips graves (pas de texte). On peut réessayer autant qu'on veut.

### Recommencer

« Recommencer » = **la scène est rechargée et TOUT est remis à zéro**, y compris l'interrupteur (**mode lumières** : lampe éteinte, même si l'interrupteur physique est resté levé) et la poignée (droite). C'est le cas peu importe comment on revient à l'étape 0 : Ctrl+R, bouton, fin de l'expérience, retour automatique, ou Page Up depuis l'étape 1.

Aller directement à une autre étape (ou la rejouer) = recharger la scène, appliquer l'état final des étapes d'avant (ex. : têtes déjà tombées), puis démarrer l'étape choisie. Là, l'interrupteur et la poignée **gardent** leur état.

> **Avec le vrai matériel :** seul un **changement de position** de l'interrupteur compte (TouchDesigner peut envoyer `/switch` en continu). Si l'interrupteur est resté levé, le jeu repart quand même en mode lumières, et la prochaine bascule (vers le haut ou vers le bas) passe en mode couleurs.

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
| **L** | Basculer l'interrupteur (mode lumières ↔ mode couleurs) |
| **L** puis **S** | Démarrer l'expérience depuis l'écran titre (interrupteur + sonnette ensemble) |
| **← →** | Tourner la poignée |
| **B** | Simuler un bon code au numpad |
| **M** | Simuler un mauvais code au numpad |

**Toutes les touches marchent, panneau F1 ouvert OU fermé.** Pour couper les touches de simulation (S, H, L, ← →, B, M), décocher *Simulation Clavier* sur `Jeu → PanneauOperateur`. F1, Page Up/Down, F5 et Ctrl+R marchent toujours.

Dans le panneau F1 :

- boutons Précédente / Rejouer l'étape / Suivante / Recommencer toute l'expérience;
- la liste des étapes : un clic pour y aller directement;
- l'étape en cours : depuis combien de temps, sa note, et sa progression (solution des couleurs, état de la lampe, code attendu, mauvais essais, temps de la Timeline, compte à rebours de la fin);
- des boutons pour simuler les capteurs, et un curseur pour la poignée;
- **Test des enceintes** : « Bip CHAMBRE » / « Bip CORRIDOR » joue 3 bips dans une seule pièce (pour vérifier le câblage);
- le **moniteur OSC** : chaque adresse reçue, sa valeur et il y a combien de temps. Vert = reçu il y a moins de 2 s. Si rien n'arrive, le problème est côté TouchDesigner, Arduino ou réseau, pas dans Unity.

---

## 7. Les écrans et le son

| Écran | Caméra | Qui le voit |
|---|---|---|
| Display 1 | `Room Camera` | Le **visiteur** (la pièce). Le panneau F1 s'affiche ici. |
| Display 2 | `Corridor Camera` | Le **locataire** (le corridor) |

Dans le build, `ActiverEcrans` allume le 2e écran au démarrage. Le build s'ouvre en plein écran et continue de tourner même si la fenêtre n'a pas le focus.

> **À savoir :** la pièce est filmée par `Room Camera` (dans la scène). `Couloir.fbx` n'importe plus de caméra de Blender (voir section 10).

### Le son : chambre à gauche, corridor à droite

Unity n'accepte qu'**un seul AudioListener** (avec deux, un seul entend). Les deux pièces sont donc séparées par la stéréo :

| Pièce | Sortie de l'ordi | Enceinte |
|---|---|---|
| **Chambre** (visiteur) | **Gauche** | celle de la chambre |
| **Corridor** (locataire) | **Droite** | celle du corridor |
| Les deux (bon code, porte ouverte) | gauche + droite | les deux |

Chaque son a un `SortieSon` qui dit dans quelle pièce on l'entend. Les sons sont en **2D** : la position des objets et des caméras ne change rien. Les fichiers sont importés en **mono** (une seule enceinte par pièce).

Réglages sur `Sons → ReglagesSons` (aussi pendant que le jeu roule) :

| Réglage | Effet |
|---|---|
| Inverser Gauche Droite | On entend la chambre dans le corridor? Cocher (ou inverser les câbles) |
| Volume Chambre / Volume Corridor | Le volume de chaque pièce |
| Muet Chambre / Muet Corridor | Pour les tests : écouter une seule pièce |

Le volume d'**un** son : `Volume` sur son `SortieSon`.

**Les fichiers de Gabriel** vont dans `Assets/Audio` (ses sous-dossiers peuvent rester comme ils sont). Le menu **La Porte → Brancher les sons** les trouve **par leur nom de fichier** :

| Fichier | Quand | Pièce |
|---|---|---|
| `bonne-couleur` | Étape 1 : bonne couleur | Chambre |
| `mauvaise-couleur` | Étape 1 : erreur au 1er choix | Chambre |
| `mauvaise-combinaison` | Étape 1 : erreur après au moins une bonne couleur (on perd sa progression) | Chambre |
| `bonne-combinaison` | Étape 1 : combinaison réussie | Chambre |
| `spark` | Les étincelles de la prise (sonnette, lampe éteinte) | Chambre |
| `lamp-moving` | Chaque coup de heurtoir (la lampe tremble) | Chambre |
| `lamp-on` / `lamp-off` | Interrupteur levé / baissé (la lampe s'allume / s'éteint) | Chambre |
| `lum-corridor-eteint` / `lum-corridor-allume` | Interrupteur levé (le corridor s'assombrit) / baissé (tout se rallume). Plus joués à partir de l'étape 2 : le corridor reste tout allumé | Corridor |
| `vent-tete-tombe` | Étape 2 : **une seule fois**, en même temps que l'animation de la vent (coups, charnière, corde). Pour le décaler : *Decalage Son* sur `Têtes qui tombent` | Corridor |
| `journal-tombe` | Chaque journal qui glisse de l'étagère | Chambre |
| `ambiance-appart` / `ambiance-corridor` | En boucle, du lancement à la fin. Ne se coupe pas quand la scène se recharge (`AmbianceContinue`) | Chambre / Corridor |
| `bon-code` / `porte-ouverte` | Étape 3 : bon code / étape 4 : la porte s'ouvre | Les deux |
| `mauvais-code` | Étape 3 : mauvais code | Chambre |

Un son sans fichier garde son **bip** placeholder. Pour ajouter ou changer un son : mettre le fichier dans `Assets/Audio` **avec ce nom exact**, puis refaire le menu (on peut le refaire sans danger). Le rapport du menu liste aussi les fichiers qu'il ne connaît pas. Deux sons se superposent (deux coups de heurtoir rapides = on entend les deux).

---

## 8. Placeholders de test (à remplacer)

Tout est rangé sous **`Placeholders (à remplacer)`**, pour que le jeu se teste du début à la fin. **Il n'y a plus aucun texte placeholder à l'écran** : le menu **La Porte → Retirer les textes placeholder** supprime « PORTE OUVERTE », l'afficheur du numpad (« CODE FAUX ») et l'ancien journal placeholder (cube + pages), et les retire de l'étape « 4 - Porte ouverte » (annulable avec Ctrl+Z, puis Ctrl+S).

| Ce qui manque | Placeholder | Comment tester |
|---|---|---|
| Sons | Bips générés par le code, seulement pour les sons qui n'ont pas encore de fichier (pour l'instant : bon / mauvais code, porte ouverte). Rangés sous `Sons` par le menu « Brancher les sons » | **B** / **M** au numpad, étape 4 |

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

Un journal par tête dans la bibliothèque, avec sur la couverture l'affiche « HELP WANTED » de la tête : un très grand portrait, **sans numéro imprimé** (« APPLICANT REF. No. ____ » est laissé en blanc). Le numéro est griffonné à la main par-dessus le visage, en peinture phosphorescente : invisible à la lumière, il brille en vert quand `Point Light (7)` est éteinte (interrupteur levé), seulement sur les journaux tombés. Dans le noir, la lueur baisse doucement (comme de la vraie peinture phosphorescente) et se recharge à la lumière. À l'étape 2, **les journaux des 2 têtes tombées** glissent hors de l'étagère et tombent sur la table, couverture en haut, **chacun en même temps que sa tête** dans le corridor; les 3 autres restent rangés. Si on saute l'étape, ils sont posés directement sur la table.

Les journaux sont **plus grands** que celui du modèle (`Agrandissement` en haut de `Assets/Scripts/Editor/JournauxLaPorte.cs`, 1,35 voulu) : le menu le limite tout seul pour que le journal debout ne touche pas la planche du dessus (environ x1,15 avec l'étagère actuelle : 38,7 cm entre les planches). Sur la table, les 2 journaux sont posés côte à côte, alignés sur les bords de la table, sans se chevaucher.

Le menu **La Porte → Brancher les journaux** fait tout (on peut le refaire sans danger) :

- crée les matériaux `Assets/Materials/Journal/Journal_<numéro>_<nom>.mat` (copie du matériau de la couverture du journal du décor, avec l'affiche `Assets/Textures/Journal/Affiche_<numéro>_<nom>.png`) et `Journal_tranche.mat` (les côtés, couleur papier);
- range 5 copies du journal du décor (modèle `Couloir`, caché ensuite) à côté de lui sur l'étagère, dans l'ordre de `ReglagesJeu → Tetes`, sous `Journaux qui tombent`;
- crée 2 `Arrivée journal` sur la table, tournées pour que l'affiche se lise à l'endroit depuis la Room Camera;
- met le chiffre phosphorescent (`Assets/Textures/Journal/Chiffre_<numéro>_<nom>.png`, blanc sur noir) en texture d'émission de chaque couverture;
- branche l'étape « 2 - Indices » (Au Debut → Lacher, Etat Final → PoserSurLaTable), le corridor (Au Debut et Etat Final → `Lights` → `ToutAllumer`) et la pièce **pendant l'étape seulement** (Au Debut → `Point Light (7)` → `ToutAllumer`, A La Fin → `SuivreInterrupteur`), et désactive le journal placeholder.

Les journaux partent toujours de la place du journal **dans le modèle Couloir** (sur l'étagère), même si le journal du décor a été déplacé dans la scène. Les journaux et les arrivées déjà créés ne sont pas déplacés (on peut les ajuster à la main), sauf si les journaux ne sont plus dans la bibliothèque ou pas à la bonne taille : refaire le menu les range à nouveau et recalcule les arrivées. **Changer un numéro ou une affiche** : regénérer les affiches (`La Porte/Textures/Journal/generer_affiches_phospho.py`), copier `Proposition_phospho/Affiche_*.png` et `Chiffre_*.png` dans `Assets/Textures/Journal`, puis refaire le menu.

- **Déplacer / tourner une arrivée** = choisir où le journal atterrit. La flèche bleue (avant) = le haut de l'affiche. Sélectionner `Journaux qui tombent` pour voir, en jaune, les journaux posés.

Réglages sur `Journaux qui tombent → JournauxQuiTombent` :

| Réglage | Effet |
|---|---|
| En Meme Temps Que Les Tetes | Coché (par défaut) : chaque journal tombe en même temps que sa tête (mêmes délais que `Têtes qui tombent`) |
| Délai Avant Le Premier / Entre Les Journaux | Seulement si la case du dessus est décochée : quand les journaux tombent (secondes après le début de l'étape) |
| Durée Sortie / Distance Sortie / Bascule | Le journal glisse hors de l'étagère en penchant vers la pièce |
| Élan / Gravité | Petit saut vers le haut au départ, puis la chute (plus de gravité = plus rapide) |
| Rebond / Durée Rebond | Le petit rebond sur la table (0 = aucun) |
| Lumiere Piece | La lumière qui commande les chiffres (vide = `Point Light (7)`, trouvée toute seule) : éteinte = les chiffres brillent |
| Couleur Lueur / Intensite Lueur | Le vert des chiffres et leur force dans le noir |
| Lueur Minimum / Duree Declin / Duree Recharge | Dans le noir, la lueur baisse jusqu'à ce minimum en « Durée Déclin » secondes; à la lumière, elle se recharge en « Durée Recharge » secondes |

### Remplacer un placeholder

- **Têtes et journal** : refaire l'animation dans la Timeline `Assets/Placeholders/Indices.playable` (ou en créer une nouvelle) et l'assigner à l'étape « 2 - Indices ». Comme les têtes changent à chaque partie, **animer 3 emplacements (des objets vides), jamais les têtes elles-mêmes**, puis les mettre dans `TetesQuiTombent → Emplacements`, dans l'ordre de chute. Supprimer les cubes ensuite. Sur le PlayableDirector, décocher *Play On Awake*.
- **Sons** : mettre le fichier dans `Assets/Audio` avec le bon nom (section 7, « Le son ») et refaire **La Porte → Brancher les sons**. Le bip est retiré tout seul.
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

## 10. Modifier le décor dans Blender (`Couloir.fbx`)

Fichier Blender : `La Porte/Blender/Blender 5.1.2/Couloir (réparé).blend` (le Journal y est dans le bon format).
Fichier exporté : `Assets/Model/Couloir.fbx`, toujours **le même nom** (on l'écrase : la scène garde ses réglages).

### Avant

- Faire un **commit git** (ou une copie de `Assets/Model/Couloir.fbx`) : on peut toujours revenir en arrière.

### Dans Blender : les règles

| OK | Dangereux pour les objets qui existent déjà |
|---|---|
| Ajouter des objets (portes, meubles…) | **Ctrl+A (appliquer l'échelle ou la rotation)** : c'est ce qui a cassé Fan, Outlet, Alarm… |
| Modifier la forme en Edit Mode (ex. trous dans le corridor) | **Renommer ou supprimer** `Fan`, `Fan_blade`, `Lamp`, `Outlet`, `Corridor`, `Vent_corridor`, `Journal` : Unity y accroche des choses par leur nom |
| Changer un matériau | **Renommer** les matériaux `Light`, `Light 1`, `Light 2`, `Light 3` : les plafonniers ne changeraient plus de couleur |

Déplacer un de ces objets est permis, mais ce qui y est accroché dans Unity bouge avec (Unity le signale, voir plus bas).

### Les réglages d'export (préréglage « La Porte - Couloir »)

**File → Export → FBX (.fbx)**, en haut à droite **Operator Presets → La Porte - Couloir**, puis enregistrer par-dessus `Assets/Model/Couloir.fbx`.

Si le préréglage a disparu (autre ordi, nouvelle version de Blender) : copier `Outils/Blender/La Porte - Couloir.py` dans
`%APPDATA%\Blender Foundation\Blender\5.1\scripts\presets\operator\export_scene.fbx\` (remplacer `5.1` par la version de Blender),
ou cocher à la main les réglages ci-dessous. **Les 3 lignes en gras sont celles qui comptent** : les autres sont les valeurs par défaut.

| Panneau | Réglage | Valeur |
|---|---|---|
| Include | Limit to : Selected Objects / Visible Objects / Active Collection | ☐ / ☐ / ☐ |
| Include | **Object Types** | **Mesh seulement** (pas Empty, Camera, Lamp, Armature, Other) |
| Include | Custom Properties | ☐ |
| Transform | Scale | 1.00 |
| Transform | Apply Scalings | All Local |
| Transform | Forward / Up | -Z Forward / Y Up |
| Transform | Apply Unit | ☑ |
| Transform | Use Space Transform | ☑ |
| Transform | **Apply Transform** | **☑** (marqué « expérimental » : c'est normal) |
| Geometry | Smoothing | Normals Only |
| Geometry | Export Subdivision Surface | ☐ |
| Geometry | **Apply Modifiers** | **☑** |
| Geometry | Loose Edges / Tangent Space / Triangulate Faces | ☐ / ☐ / ☐ |
| Geometry | Vertex Colors | sRGB (Prioritize Active Color ☐) |
| Armature | Primary / Secondary Bone Axis | Y Axis / X Axis |
| Armature | Armature FBXNode Type | Null |
| Armature | Only Deform Bones / Add Leaf Bones | ☐ / ☑ |
| Bake Animation | Bake Animation | ☑ (Key All Bones ☑, NLA Strips ☑, All Actions ☑, Force Start/End Keying ☑, Sampling Rate 1.00, Simplify 1.00) |
| Path Mode | Path Mode / Embed Textures | Auto / ☐ |
| Batch | Batch Mode | Off |

Ces réglages redonnent **exactement** le `Couloir.fbx` d'origine (vérifié objet par objet dans Unity). Avec « Apply Transform » décoché
ou un autre « Apply Scalings », les objets dont l'échelle n'est pas 1 (Fan, Outlet, Alarm, Switch…) changent de taille ou de place dans Unity.

### Dans Unity

1. Cliquer dans Unity : le FBX est importé **et vérifié tout seul** (`Assets/Scripts/Editor/CouloirImport.cs`) :
   - les lumières et caméras de Blender ne sont jamais importées (elles rendaient tout blanc);
   - les objets dont le nom contient « reference » (ex. `Cegep corridor reference`) sont retirés;
   - chaque objet est comparé à la référence `Assets/Model/Detaches/Couloir.reference.json`.
2. **« ✔ Couloir.fbx conforme à la référence »** dans la Console : c'est bon.
3. **Une fenêtre d'avertissement** (objets qui ont bougé, changé d'échelle ou disparu) :
   - pas voulu → presque toujours les réglages d'export : réexporter avec le préréglage;
   - voulu → menu **La Porte → Accepter Couloir.fbx comme référence**.
4. Tester en Play, **Ctrl+S**, commit.

### Si quelque chose semble cassé

Menu **La Porte → Tout rebrancher (après un export du Couloir)** : vérifie le FBX, rouvre la scène **telle qu'enregistrée**
(les changements pas enregistrés sont d'abord copiés dans `Assets/Scenes/Sauvegardes`), remet ce qui manque (caméra de la chambre,
vent animée, étincelles, sons…), remesure les journaux, cherche les références cassées. Rapport complet dans la Console.

### Ce qui ne dépend plus de Blender

- **Les 5 journaux qui tombent** utilisent leur propre maillage (`Assets/Model/Detaches/Journal.asset`) : modifier le Journal dans Blender ne les change pas.
- **La caméra de la chambre** est `Room Camera`, dans la scène (plus la caméra de Blender).
- **La vent animée** a son propre fichier (`Vent_corridor.fbx`) et son menu (**La Porte → Brancher la vent animée**).
- Ne pas relancer **La Porte → Brancher les journaux** sans raison : il recalcule la place des journaux à partir du Journal de Blender.

---

## 11. Ce qui reste à décider ou à faire

- [ ] **TD** : envoyer l'interrupteur sur `/switch` (sinon, utiliser la touche **L**).
- [ ] **TD** : recevoir le code de la partie sur `/code_attendu`, le comparer à ce qui est tapé, et envoyer `/code` **en continu** (0 / 1 / -1).
- [ ] Ajouter un **OSCTransmitter** (IP et port de l'ordi de TD) et l'assigner sur `Jeu → Capteurs → Transmetteur` : sans lui, TD ne reçoit pas le code.
- [ ] Le **nombre de couleurs** de la combinaison, et quelles lumières du plafond les affichent (pour l'instant : light 3, light 2, light 1 = Bleu → Vert → Rouge).
- [ ] Ce qui se passe quand le visiteur se **trompe de couleur** (pour l'instant : 3 clignotements et on recommence au début de la combinaison).
- [x] Modéliser les **têtes** (3 tombent au hasard à la place des cubes).
- [x] Le **journal** : une affiche par tête, les journaux des têtes tombées tombent sur la table (menu **La Porte → Brancher les journaux**, section 8).
- [x] L'interrupteur et les journaux : remplace l'idée « page floue A / B » (`JournalPersonnages`, plus utilisé) par les chiffres phosphorescents (lumière = visages, noir = numéros).
- [ ] La **porte qui s'ouvre**.
- [x] Les **sons** de Gabriel : branchés (menu **La Porte → Brancher les sons**, section 7).
- [ ] **Gabriel** : les sons qui manquent, `bon-code`, `mauvais-code`, `porte-ouverte` (et, si on veut, la poignée et la corde des têtes).
- [x] La caméra de la pièce : `Room Camera` (dans la scène), avec les réglages de l'ancienne `Camera Room` de Blender (section 10).
- [ ] Une **serrure électrique**? Si oui, donner l'adresse OSC.
- [x] L'**écran titre** (logo qui bat, interrupteur + sonnette ensemble pour commencer, fondu sur les deux écrans).
- [ ] Le **logo** : le mettre dans `Assets/Resources/Logo.png` (pour l'instant, l'écran titre affiche « LA PORTE » en texte).
