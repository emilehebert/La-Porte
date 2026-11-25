# Handoff — La Porte (Unity 6.3, scène `Couloir`)

> Pour la session Claude Code qui tourne SUR L'ORDI DU LABO, avec accès direct au projet Unity.
> Projet local `C:\Users\6233251.LABORATOIRE.000\Desktop\La-Porte`
> Dépôt GitHub `emilehebert/La-Porte`, branche de travail `claude/charming-goldberg-e9wqfa`
> Rédigé le 2026-11-25 par la session cloud, qui ne peut pas atteindre l'ordi du labo.

---

## 1. Le problème à régler MAINTENANT

L'utilisateur a réexporté le décor de Blender (`Couloir.blend` vers `Couloir.fbx`) et depuis **la scène est cassée**. Il dit que l'export « a écrasé ce qu'il avait fait ».

Ce qu'on voit sur sa capture d'écran de Unity

- dans la vue Scene, presque tout est **blanc pur ou noir**, sans textures ni éclairage normal
- la vue Game (Display 2, le corridor) est **blanche, surexposée**, avec des points jaunes au sol
- dans la Hierarchy, `Room Camera` est grisée (désactivée) et `Couloir` (le FBX) contient encore `Camera` et `Camera Room`

Les réglages d'export utilisés (vus sur ses captures de Blender 5.1.2)

| Réglage | Valeur utilisée |
|---|---|
| Limit to | Selected Objects **coché** (je lui ai dit de le décocher, il ne l'a peut-être pas fait) |
| Object Types | **Mesh seulement** (puis je lui ai dit Empty + Mesh) |
| Scale / Apply Scalings | 1.00 / All Local |
| Forward / Up | -Z Forward / Y Up |
| Apply Unit, Use Space Transform | cochés |
| Apply Transform | décoché |
| Smoothing / Apply Modifiers | Face / coché |
| Animation | coché |
| Dossier d'export | `...\Desktop\La Porte\Blender\Blender 5.1.2\Couloir.fbx` (le dossier Blender, avec une ESPACE, PAS le projet Unity `La-Porte`) |

### Erreur de la session cloud à corriger

La session cloud a dit de **décocher Camera** à l'export en croyant que le jeu utilise `Room Camera`. C'est **faux**. Dans la scène du dépôt, `Room Camera` a `m_IsActive 0`. La caméra de la pièce (Display 1, le visiteur) est donc **`Camera Room`, qui vient du FBX**. Exporter sans les caméras fait disparaître la caméra de la pièce. Le guide (section 7) dit encore `Room Camera`, il est à corriger.

### Hypothèses, dans l'ordre où les vérifier

1. **Les matériaux ont perdu leur remap.** `Assets/Model/Couloir.fbx.meta` remappe 23 matériaux **par leur nom** vers `Assets/Materials/*.mat` (Carpet, Carpet red, Ceiling, Curtains, Door, Drawers, Electronics, Fridge, Journal, Journal_face_1, Lamp, Lever, Light, Light 1, Light 2, Light 3, Metal, Oven, Shadow, Vent, Wall White Plaster, White wood, Wood). Si un nom de matériau a changé dans Blender, ou si le `.meta` a été remplacé ou régénéré, Unity prend des matériaux par défaut (blancs). C'est le suspect principal pour « tout blanc ».
2. **Des objets manquent** parce que Selected Objects était coché, ou parce que les Empty n'étaient pas exportés. Les scripts qui cherchent des objets par nom (`Outlet`, `Library`, `Table`, `Vent_corridor`, le journal du décor) et les menus `La Porte` en dépendent.
3. **La caméra de la pièce a disparu** si le nouveau FBX a été exporté sans caméras (voir l'erreur plus haut).
4. **Le git pull a touché quelque chose.** Peu probable, parce que les commits de la branche ne modifient ni la scène ni le FBX (seulement des `.cs` et le guide). Mais à vérifier si des modifications locales non commitées ont disparu.

### Marche à suivre pour diagnostiquer

1. NE RIEN SAUVEGARDER dans Unity avant d'avoir compris (pas de Ctrl+S).
2. `git status` et `git diff --stat`, pour voir si `Assets/Model/Couloir.fbx`, son `.meta` ou `Assets/Scenes/Couloir.unity` ont changé.
3. `git stash list` et `git reflog -15`, pour retrouver un travail local qui aurait été « écrasé ».
4. Lire la fin de `%LOCALAPPDATA%\Unity\Editor\Editor.log` (erreurs d'import du FBX, matériaux introuvables).
5. Comparer les noms de matériaux du nouveau FBX avec la liste `externalObjects` du `.meta`.
6. **Pour revenir en arrière tout de suite**, si le FBX ou son `.meta` ont changé
   `git checkout -- Assets/Model/Couloir.fbx Assets/Model/Couloir.fbx.meta`
   puis laisser Unity réimporter. La scène doit redevenir comme avant.
7. Ensuite seulement, réexporter proprement (section 5) et vérifier que les remaps tiennent.

---

## 2. Le projet en bref

Une installation interactive à deux joueurs dans deux espaces physiques.
- **Le visiteur**, dans la « chambre » (Display 1). Il a une sonnette, un heurtoir et un numpad.
- **Le locataire**, dans le « corridor » (Display 2). Il a une poignée de porte et un interrupteur.

Le matériel passe par Arduino puis TouchDesigner, et arrive en OSC dans Unity (port 7000, script `Capteurs`). Unity utilise le pipeline **HauntedPSX** (look PS1).

**Tout est expliqué dans `GUIDE_LA_PORTE.md`. Le lire en entier avant de toucher à quoi que ce soit.**

### Conventions de l'utilisateur, à respecter absolument

- Tout est **en français**, y compris les noms de scripts, de variables, de méthodes et de commentaires.
- Les dossiers `Assets/Scripts/Moteur` (système), `Etapes` (héritent de `Etape`), `Mecaniques` (réagissent toujours) et `Editor` (menus `La Porte → ...`).
- Les mécaniques s'abonnent à `Capteurs` dans `OnEnable` et se désabonnent dans `OnDisable`.
- Les étapes utilisent des **coroutines** (jamais async) et ont `AppliquerEtatFinal()` (pour le saut d'étape) et `InfoOperateur()` (pour le panneau F1).
- Un champ vide ne doit jamais planter. On retrouve l'objet tout seul quand c'est possible, avec un `Debug.LogWarning` qui dit de le rebrancher.
- Des `[Tooltip]` en français clair qui disent l'effet concret, et tous les réglages dans l'Inspector.
- Les menus éditeur sont **idempotents** (on peut les refaire sans danger), annulables avec Ctrl+Z et terminés par un rapport ✔ / ⚠.
- **`GUIDE_LA_PORTE.md` est mis à jour à chaque changement.**
- Les commits sont en français et décrivent le résultat.
- L'utilisateur veut des réponses **directes et honnêtes**. Il faut le contredire quand il a tort et dire franchement quand on n'est pas sûr. Dans les textes qu'il demande d'écrire, il ne veut **ni deux-points ni points-virgules**.

---

## 3. Où on est rendu (tout est poussé sur `claude/charming-goldberg-e9wqfa`)

### Sons de Gabriel (fini, testé par l'utilisateur)

- Les fichiers du Drive « Sonore » (dossiers `Interactions` et `Maquette Sonore`) sont dans `Assets/Audio`.
- Le menu **La Porte → Brancher les sons** (`Editor/SonsLaPorte.cs`) les trouve par leur nom de fichier, les règle en mono et branche tout. L'utilisateur l'a lancé avec succès (29 sons trouvés).
- **Le routage.** Unity n'accepte qu'un seul AudioListener, donc on sépare par la stéréo. La chambre sort à GAUCHE et le corridor à DROITE (`Moteur/SortieSon.cs`, avec `Espace` Chambre / Corridor / LesDeux). Les réglages sont dans `Moteur/ReglagesSons.cs` sur l'objet `Sons` (volumes, muet, inverser). Le panneau F1 a des boutons « Bip CHAMBRE / Bip CORRIDOR » pour tester les enceintes.
- `Mecaniques/SonsInteractions.cs` gère le heurtoir (lamp-moving), la lampe on/off et les lumières du corridor.
- `Mecaniques/AmbianceContinue.cs` garde les ambiances en boucle, sans coupure quand la scène se recharge.
- **Le son de la vent** (`vent-tete-tombe`) joue UNE fois, au début de la Timeline de l'étape 2, synchronisé avec l'animation de la vent (vérifié dans le montage Reaper de Gabriel). On peut le décaler avec *Decalage Son* sur `Têtes qui tombent`. **À valider à l'oreille par l'utilisateur.**
- Il manque encore des fichiers de Gabriel, donc ces sons gardent un bip (`bon-code`, `mauvais-code`, `porte-ouverte`).

### Interrupteur (fini, à tester)

- `Capteurs.InterrupteurAllume` est le MODE du jeu. `false` = **mode lumières** (plafond de la pièce allumé, lampe éteinte, corridor blanc). `true` = **mode couleurs** (lampe allumée, plafond éteint, corridor sombre avec les couleurs du code et la poignée).
- Au début de l'étape 2, `Capteurs.ForcerModeLumieres()` passe en mode lumières et y reste (case *Passer En Mode Lumieres* sur `2 - Indices`). Ensuite, **chaque bascule de l'interrupteur change de mode**, peu importe sa position physique (`modeInverse` dans `Capteurs`). Recommencer repart aussi en mode lumières.

### Lampe (fini, à tester)

- Après l'énigme des couleurs (réussie ou sautée), la lampe devient **mauve et le reste**. Le heurtoir la fait encore trembler mais ne change plus sa couleur (`LampeSurPied.FixerCouleur`, champ *Couleur Apres Reussite* sur `1 - Énigme des couleurs`).

### Rien de tout ça n'a été compilé par la session cloud

Il n'y a pas de Unity dans le cloud. Les premiers changements ont compilé chez l'utilisateur (le menu des sons a marché). **Les derniers (interrupteur, lampe mauve, son de la vent) n'ont pas encore été vérifiés dans Unity.** Regarder la Console.

---

## 4. Le but

Finir l'intégration du son et stabiliser la scène pour l'expérience complète (étapes 0 à 4). Dans l'ordre

1. **Réparer la scène après l'export Blender** (section 1). C'est la priorité.
2. Vérifier que tout compile et tester l'expérience de bout en bout avec F1 (S, H, L, flèches, B, M, Page Down).
3. Faire valider à l'oreille la synchro du son de la vent et l'équilibre des volumes.
4. Corriger le guide (`Camera Room` du FBX est la vraie caméra de la pièce) et ajouter les bons réglages d'export.
5. Plus tard, intégrer les sons manquants quand Gabriel les fournit (nom exact dans `Assets/Audio`, puis refaire le menu).
6. Plus tard, à la fin, aider l'utilisateur à brancher les enceintes (voir le guide, section 7, « Le son »).

---

## 5. Les réglages d'export Blender corrigés

À écrire dans le guide (section 7) une fois vérifiés.

| Réglage | Valeur |
|---|---|
| Limit to | **rien de coché** |
| Object Types | **Empty, Camera, Mesh** (Camera OBLIGATOIRE, car `Camera Room` est la caméra de la pièce) |
| Scale / Apply Scalings | 1.00 / All Local |
| Forward / Up | -Z Forward / Y Up |
| Apply Unit, Use Space Transform | cochés |
| Apply Transform | décoché |
| Geometry | Smoothing Face, Apply Modifiers coché |
| Animation | comme avant (coché) |
| Fichier | **`Assets/Model/Couloir.fbx` du projet Unity**, en remplaçant l'ancien |

- **Les noms des matériaux dans Blender ne doivent pas changer**, sinon le remap du `.meta` saute (la liste est en section 1).
- Un plafonnier du corridor a besoin d'**un matériau à lui** (Light 1, Light 2, Light 3).
- Pour la vent animée (`Vent_corridor.fbx`), ce sont les mêmes réglages Transform, avec **Bake Animation** coché, **NLA Strips** et **All Actions** décochés et **Apply Modifiers** décoché. Ensuite, le menu **La Porte → Brancher la vent animée**.

---

## 6. Contrôler l'éditeur Unity

Une session Claude Code locale peut lire et modifier les fichiers du projet, lancer git et lire `Editor.log`. Elle **ne peut pas cliquer dans Unity** ni lire la Console en direct sans un serveur MCP pour Unity installé dans le projet (il existe des paquets communautaires). Si l'utilisateur veut ça, chercher un MCP Unity compatible avec Unity 6.3, l'installer **avec son accord** et le documenter dans le guide. Sinon, travailler par les fichiers et lui demander de lancer les menus et de copier la Console.
