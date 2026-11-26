# Les sons de La Porte

> **Pour toi qui fais les sons.** Lis « L'essentiel » (5 minutes), puis attaque la liste en commençant par les **P1**.
> Les durées et les moments écrits dans ce document viennent du jeu lui-même : quand un son doit tomber pile, c'est indiqué.

---

## L'essentiel

- *La Porte* est une escape room pour **2 joueurs**. Chacun regarde un écran qui imite un **œilleton**, en style **PlayStation 1** (basse résolution, image un peu sale).
  - Le **locataire** a une poignée et un interrupteur. Son écran montre le **corridor**.
  - Le **visiteur** a un heurtoir, une sonnette et un numpad. Son écran montre la **pièce**.
- **Il n'y a aucun texte à l'écran.** Les sons sont la seule façon de dire aux joueurs « bravo », « non » ou « il vient de se passer quelque chose ». Les sons de confirmation et d'erreur (préfixe `FB_`) sont donc les plus importants du jeu.
- Pour l'instant, le jeu n'a que des bips de test. Chaque bip disparaît dès que ton son est mis à sa place.

### Les 10 règles d'or

1. **WAV**, 48 kHz ou 44,1 kHz, 16 ou 24 bits.
2. **Mono, toujours.** Unity envoie les sons de la pièce dans le haut-parleur de gauche et ceux du corridor dans celui de droite : un fichier stéréo casserait ça.
3. **Le nom exact de la liste.** Les variantes finissent par `_01`, `_02`, `_03`. Pas d'espace, pas d'accent.
4. **Pas de silence au début.** Le son part à la première milliseconde, au moment exact où le joueur fait son geste. (Une seule exception, indiquée dans la liste.)
5. **Les boucles (`_Boucle`) bouclent parfaitement** : pas de fondu au début ni à la fin, pas de clic à la jonction. 1 à 2 minutes, sauf indication contraire.
6. **Les feedbacks (`FB_`) sont courts et clairs** : on doit les reconnaître par-dessus l'ambiance, et ne pas s'en lasser après 20 essais.
7. **Respecte les durées et les moments indiqués** : ils sont calés sur des animations qui existent déjà.
8. **Niveaux** : `FB_` autour de −3 dBFS en crête, `SFX_` autour de −6 dBFS, ambiances discrètes (autour de −24 LUFS). Les feedbacks passent toujours par-dessus l'ambiance.
9. **Style PS1** : un peu sourd et lo-fi (comme rééchantillonné à 22 kHz), réverbération courte (ce sont de petits espaces). Fais-le dans le fichier : Unity ne transforme rien.
10. **Sons libres de droits seulement** (tes enregistrements, CC0, packs libres de droits). Note la source et la licence de chaque son dans `CREDITS_SONS.txt`.

---

## L'histoire (pour t'inspirer)

- Un vieil immeuble des **années 40-50**, la nuit. Dans la pièce : une lampe sur pied, un frigo, un four, un ventilateur, une table et une bibliothèque. Dans le corridor : des plafonniers, une ventilation au plafond et un tapis rouge.
- Le journal du quartier, *The Evening Courier* (5 cents), publie des petites annonces « HELP WANTED » : *« Night staff wanted, rear door — applicants are known by reference number only. »* Cinq candidats, cinq postes : **Boiler man** (chaudière), **Bookkeeper** (comptable), **Housekeeper** (ménage), **Night porter** (portier de nuit), **Switchboard** (standardiste).
- Ces candidats ne sont jamais repartis : pendant la partie, **leurs têtes tombent de la ventilation du corridor**, pendues à des cordes.
- Un **monstre avec un trousseau de clés** rôde derrière la porte (ses animations arrivent bientôt, voir la section G).

### La palette proposée

| Rôle | Son | Pourquoi |
|---|---|---|
| Confirmation | Clochette de réception d'hôtel (« ding ») | Le portier de nuit |
| Erreur | Buzzer d'un vieux standard téléphonique | La standardiste |
| Grande réussite | Clés et verrous qui s'ouvrent | Les clés du monstre |
| Lumières | Électricité vieillotte : relais, bourdonnement, ampoules qui grésillent | Un immeuble mal entretenu |
| Bruits lointains | Tuyaux de chaudière, téléphone, machine à écrire | Les métiers des annonces |

Ce sont des propositions : tu peux en proposer d'autres, du moment qu'elles restent dans cet univers.

---

## Les deux côtés

| Côté | Qui l'entend | Ce qui s'y passe |
|---|---|---|
| **Pièce** | Le visiteur | La lampe, la prise électrique, les journaux, le frigo, le ventilateur |
| **Corridor** | Le locataire | Les plafonniers, la ventilation, les têtes, le monstre |
| **Les deux** | Tout le monde | Les feedbacks des énigmes, la porte qui s'ouvre |

Chaque son de la liste dit de quel côté il joue.

---

## Le déroulement (ce que vivent les joueurs)

| # | Étape | Ce qui se passe |
|---|---|---|
| 0 | Attente | Tout est calme. L'expérience démarre au premier geste d'un joueur. |
| 1 | Énigme des couleurs | Le locataire découvre 3 couleurs dans le corridor (interrupteur et poignée). Le visiteur change la couleur de la lampe avec le heurtoir et valide avec la sonnette. Il faut les 3 couleurs dans le bon ordre. |
| 2 | Indices (environ 2,5 s) | Les lumières se rallument. Quelque chose frappe 3 fois dans la ventilation, la grille s'ouvre et 2 têtes tombent. En même temps, 2 journaux tombent de la bibliothèque sur la table. |
| 3 | Code | Le locataire décrit les têtes. Le visiteur retrouve leurs numéros dans les journaux (les visages à la lumière, des chiffres phosphorescents dans le noir) et tape un code à 4 chiffres. |
| 4 | Porte ouverte | La porte s'ouvre. 30 s plus tard, tout recommence pour les joueurs suivants. |

---

## La liste

**P1** = indispensable, à livrer en premier · **P2** = important · **P3** = bonus

### A. Ambiances

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `AMB_Corridor_Boucle` | **P1** | Corridor | 1-2 min | Corridor d'immeuble la nuit : grondement grave, air lourd, craquements de bois de temps en temps. Joue tout le temps. |
| `AMB_Piece_Boucle` | **P1** | Pièce | 1-2 min | Petite pièce feutrée, silence épais, un peu de vie (bois, vieux murs). Joue tout le temps. |
| `AMB_Plafonniers_Boucle` | P2 | Corridor | 30 s-1 min | Bourdonnement électrique des ampoules du plafond. Unity monte et baisse son volume avec les lumières (il suit la zone éclairée par la poignée). |
| `AMB_Ventilation_Boucle` | P2 | Corridor | 1 min | Souffle d'air dans un conduit en tôle. Unity le rend plus fort après l'étape 2, quand la grille reste ouverte. |
| `AMB_Frigo_Boucle` | P2 | Pièce | 1-2 min | Ronronnement de vieux frigo, avec un cycle du compresseur (démarrage et arrêt). |
| `AMB_Ventilateur_Boucle` | P2 | Pièce | 30 s | Ventilateur lent (1 tour toutes les 3 s) : souffle des pales et petit cliquetis du moteur. |
| `AMB_Lampe_Boucle` | P2 | Pièce | 10-20 s | Léger grésillement de l'ampoule de la lampe sur pied. Joue seulement quand elle est allumée. |
| `AMB_Tension_1_Boucle` | P3 | Les deux | 1-2 min | Nappe sombre, très discrète (étape 1). |
| `AMB_Tension_2_Boucle` | P3 | Les deux | 1-2 min | La même nappe, dans la même tonalité, mais plus grave et dissonante (étapes 2 et 3). Unity passe de l'une à l'autre en fondu. |
| `AMB_Immeuble_01` à `_08` | P3 | Les deux | 2-6 s chacun | Bruits lointains joués au hasard toutes les 20 à 60 s : tuyaux de chaudière qui cognent, téléphone qui sonne au loin, machine à écrire étouffée, pas à l'étage du dessus, porte qui claque, radio des années 40 à travers un mur. Très loin et étouffés : on doit douter de les avoir entendus. |

### B. Les objets (actifs à toutes les étapes)

**Heurtoir (visiteur)**

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `SFX_Lampe_Couleur_01` à `_03` | **P1** | Pièce | 0,2-0,4 s | **Très important pour l'énigme.** À chaque coup de heurtoir, la lampe change de couleur : clic d'un filtre qui tourne, ou « tink » de verre coloré. Il doit bien se distinguer du tremblement. |
| `SFX_Lampe_Tremble_01` à `_03` | P2 | Pièce | 0,5 s | La lampe tremble (animation de 0,5 s) : cliquetis du pied en métal et de l'abat-jour. Joue à chaque coup de heurtoir, et toute seule toutes les 6 s comme indice à l'étape 1. |
| `SFX_Lampe_Scintille_01` à `_02` | P2 | Pièce | 0,5 s | Si la lampe est éteinte, elle tremble et sa lumière grésille un instant, puis s'éteint de nouveau. |

**Sonnette (visiteur)**

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `SFX_Prise_Etincelles_01` à `_03` | **P1** | Pièce | 0,4-0,8 s | Sonnette appuyée quand la lampe est éteinte : des étincelles sortent de la prise électrique. Crépitement électrique sec, « zzzap ». |
| `SFX_Sonnette` | P3 | Pièce | 1-1,5 s | Vieille sonnette électrique (« brrring » de timbre à marteau). *Seulement si la vraie sonnette de l'installation ne fait pas déjà de bruit : à confirmer avec l'équipe.* |

**Interrupteur (locataire)**

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `SFX_Corridor_Extinction` | P2 | Corridor | 0,5-1 s | Interrupteur levé : le corridor s'assombrit en 0,12 s. Gros « clonk » de disjoncteur, le bourdonnement tombe d'un coup. |
| `SFX_Couleurs_Apercu` | P2 | Corridor | 0,6 s | Juste après l'extinction, les lumières de couleur restent allumées 0,3 s, puis scintillent 0,3 s en faiblissant jusqu'à s'éteindre : un crépitement qui meurt. |
| `SFX_Corridor_Allumage` | P2 | Corridor | 0,5-1 s | Interrupteur baissé : les ampoules se rallument une à une (« tink, tink, bzzz »). |
| `SFX_Lampe_Allumage` | P2 | Pièce | 0,4 s | La lampe sur pied s'allume avec l'interrupteur (fondu de 0,4 s) : « tink » du filament. |
| `SFX_Lampe_Extinction` | P2 | Pièce | 0,4 s | La lampe s'éteint : petit « tic », le grésillement s'arrête. |
| `SFX_Phospho` | P3 | Pièce | 1-2 s | Étape 3 et après : dans le noir, des chiffres verts phosphorescents apparaissent sur les journaux. Scintillement doux, presque magique. |

**Poignée (locataire)**

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `SFX_Plafonnier_Scintille_01` à `_03` | P2 | Corridor | 0,5 s | Quand le corridor est tout allumé, tourner la poignée fait scintiller les lumières : grésillements et petits claquements d'ampoule. Même rythme que le tremblement de la lampe (0,5 s). |

### C. Étape 1 : l'énigme des couleurs

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `FB_Bonne_Couleur` | **P1** | Les deux | 0,3-0,6 s | **Confirmation.** Ding clair de clochette de réception et petit clic de relais. La lampe clignote 1 fois (0,3 s). *Bonus :* une 2e version un ton plus haut (`FB_Bonne_Couleur_2`) pour la 2e couleur, pour qu'on sente qu'on progresse. |
| `FB_Mauvaise_Couleur` | **P1** | Les deux | 0,6 s | **Erreur.** Buzzer de standard en 3 pulsations, calées sur les 3 clignotements de la lampe : à **0 s, 0,2 s et 0,4 s**. Grave mais pas agressif : les joueurs vont l'entendre souvent. La combinaison recommence au début. |
| `FB_Combinaison_Reussie` | **P1** | Les deux | 0,8 s max | **Réussite.** 3 dings qui montent, puis un « CLUNK » de relais. La lampe clignote 4 fois vite (à 0 / 0,2 / 0,4 / 0,6 s). ⚠️ L'étape 2 commence **au même instant** : le premier coup dans la ventilation arrive 0,07 s plus tard. Reste court et plutôt aigu pour ne pas le cacher. |

### D. Étape 2 : les indices (minutage exact)

Le temps 0 = le moment où la combinaison est réussie.

| Moment | Fichier | Prio | Côté | Description |
|---|---|---|---|---|
| 0,00 s | `SFX_Lumieres_Rallument` | P2 | Les deux | La pièce et le corridor se rallument : relais, ampoules qui redémarrent (0,5-1 s). |
| 0,00 → 1,63 s | `SFX_Ventilation_Sequence` | **P1** | Corridor | **Un seul fichier de 1,63 s, calé à l'image près.** Quelque chose frappe la grille de la ventilation de l'intérieur : **coup 1 à 0,07 s**, **coup 2 à 0,57 s**, **coup 3 à 1,07 s**. Au 3e coup, la grille s'ouvre d'un coup sur sa charnière, claque et vibre jusqu'à 1,4 s. Coups sourds dans un conduit, tôle qui se déforme. |
| 1,63 s | `SFX_Tete_Chute_01` | **P1** | Corridor | La 1re tête tombe du conduit : glissement dans la tôle, puis souffle de chute (0,3-0,6 s). |
| ≈ 2,2 s | `SFX_Corde_Tension_01` | **P1** | Corridor | La corde se tend d'un coup et retient la tête : « THWUMP » lourd et craquement des fibres. Le moment exact dépend de la physique : Unity le joue quand la corde se tend. |
| ≈ 2,2 s | `STG_Tetes` | P2 | Les deux | Stinger d'horreur (1-3 s), au moment où la 1re tête apparaît : accord dissonant ou cordes graves. |
| 2,23 s / ≈ 2,8 s | `SFX_Tete_Chute_02`, `SFX_Corde_Tension_02` | **P1** | Corridor | La 2e tête : mêmes sons, autre variante. |
| ensuite | `SFX_Corde_Grince_Boucle` | P2 | Corridor | 5-10 s en boucle. La corde grince pendant que la tête se balance; Unity règle le volume selon la vitesse. Chaque coup de heurtoir relance le balancement. |
| 1,63 s et 2,23 s | `SFX_Journal_Glisse_01` à `_02` | P2 | Pièce | Avec chaque tête, son journal glisse hors de l'étagère (0,25 s) : papier qui frotte le bois. |
| juste après | `SFX_Journal_Table_01` à `_02` | P2 | Pièce | Le journal tombe sur la table : « flap » de papier épais sur le bois, puis un petit rebond. |

### E. Étape 3 : le code

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `FB_Mauvais_Code` | **P1** | Les deux | 0,6-0,8 s | **Erreur.** Plus dur que l'erreur des couleurs : 2 pulsations de buzzer (porte refusée, ligne occupée). On peut réessayer sans limite, donc il ne doit pas devenir insupportable. |
| `FB_Bon_Code` | **P1** | Les deux | ≈ 1 s | **Réussite.** Une clé tourne dans la serrure, puis 3 verrous s'ouvrent : « clac, clac, CLAC ». |
| `SFX_Numpad_Touche_01` à `_03` | P3 | Pièce | 0,1 s | Clic de bouton en bakélite à chaque touche. *À confirmer avec l'équipe : c'est TouchDesigner qui reçoit les touches, pas Unity.* |

Pour mettre de la pression pendant la recherche, voir aussi `MON_Respiration_Boucle` (section G).

### F. Étape 4 : la porte s'ouvre

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `SFX_Porte_Ouverture` | **P1** | Les deux | 4-6 s | Long grincement de vieille porte, puis un courant d'air. ⚠️ **Commence par 1 s de silence** (la seule exception à la règle 4) : Unity le joue au même instant que `FB_Bon_Code`, il faut laisser les verrous finir. |
| `MUS_Fin` | P3 | Les deux | 10-25 s | À décider avec l'équipe : soulagement, ou dernier frisson. Doit finir avant 30 s (la partie recommence). |

### G. Le monstre (quand ses animations seront prêtes)

Les animations du monstre contiennent des marqueurs (`son_pas`, `son_cles`, `son_frappe`, `son_cri`, `evt_lumiere`) : Unity joue le son pile à la bonne image. Les durées et les moments seront dans `Monstre_animations.json`; demande-le à l'équipe quand il sera prêt.

| Fichier | Prio | Côté | Durée | Description |
|---|---|---|---|---|
| `MON_Pas_01` à `_06` | P3 | Corridor | 0,3-0,6 s | Pas lourds sur le tapis et le plancher. |
| `MON_Cles_01` à `_03` | P3 | Corridor | 0,5-2 s | Trousseau qui tinte. Une version longue, très proche : il montre les clés à l'œilleton. |
| `MON_Frappe_01` à `_03` | P3 | Corridor | 0,5-1 s | Coups contre la porte. |
| `MON_Cri` | P3 | Corridor | 1-2 s | Jumpscare : cri et stinger, fort et très court. Le son le plus fort du jeu. |
| `MON_Respiration_Boucle` | P3 | Corridor | 10-20 s | Il attend devant la porte : respiration lourde, petits grognements. |
| `MON_Ampoule_Claque` | P3 | Corridor | 0,3-0,5 s | Une ampoule qui claque quand il passe. |

---

## Livrer

| Quoi | Où (dossier partagé « La Porte ») |
|---|---|
| Les ambiances (`AMB_`) | `La Porte/Sons/Ambiances/` |
| Les objets et l'histoire (`SFX_`, `STG_`) | `La Porte/Sons/SFX/` |
| Les feedbacks (`FB_`) | `La Porte/Sons/Feedback/` |
| Le monstre (`MON_`) | `La Porte/Sons/Monstre/` |
| La musique (`MUS_`) | `La Porte/Sons/Musique/` |
| `CREDITS_SONS.txt` (source et licence de chaque son) | `La Porte/Sons/` |

- **Toujours le même nom** : tu écrases l'ancien fichier. Jamais `_v2` ni `_final`.
- **Livre tôt** : les P1 d'abord (on les essaie tout de suite dans le jeu), puis le reste.
- **Où trouver des sons libres** : freesound.org (filtre « Creative Commons 0 »), les packs gratuits Sonniss *GDC Game Audio Bundle*, BBC Sound Effects (usage scolaire seulement).

---

## La liste à cocher

**P1 : indispensable (13)**

- [ ] `FB_Bonne_Couleur`
- [ ] `FB_Mauvaise_Couleur`
- [ ] `FB_Combinaison_Reussie`
- [ ] `FB_Mauvais_Code`
- [ ] `FB_Bon_Code`
- [ ] `SFX_Porte_Ouverture`
- [ ] `SFX_Prise_Etincelles_01` à `_03`
- [ ] `SFX_Lampe_Couleur_01` à `_03`
- [ ] `SFX_Ventilation_Sequence`
- [ ] `SFX_Tete_Chute_01` à `_02`
- [ ] `SFX_Corde_Tension_01` à `_02`
- [ ] `AMB_Corridor_Boucle`
- [ ] `AMB_Piece_Boucle`

**P2 : important (18)**

- [ ] `AMB_Plafonniers_Boucle`
- [ ] `AMB_Ventilation_Boucle`
- [ ] `AMB_Frigo_Boucle`
- [ ] `AMB_Ventilateur_Boucle`
- [ ] `AMB_Lampe_Boucle`
- [ ] `SFX_Lampe_Tremble_01` à `_03`
- [ ] `SFX_Lampe_Scintille_01` à `_02`
- [ ] `SFX_Corridor_Extinction`
- [ ] `SFX_Couleurs_Apercu`
- [ ] `SFX_Corridor_Allumage`
- [ ] `SFX_Lampe_Allumage`
- [ ] `SFX_Lampe_Extinction`
- [ ] `SFX_Plafonnier_Scintille_01` à `_03`
- [ ] `SFX_Lumieres_Rallument`
- [ ] `STG_Tetes`
- [ ] `SFX_Corde_Grince_Boucle`
- [ ] `SFX_Journal_Glisse_01` à `_02`
- [ ] `SFX_Journal_Table_01` à `_02`

**P3 : bonus (15)**

- [ ] `AMB_Tension_1_Boucle`
- [ ] `AMB_Tension_2_Boucle`
- [ ] `AMB_Immeuble_01` à `_08`
- [ ] `SFX_Sonnette`
- [ ] `SFX_Phospho`
- [ ] `FB_Bonne_Couleur_2`
- [ ] `SFX_Numpad_Touche_01` à `_03`
- [ ] `MUS_Fin`
- [ ] `MON_Pas_01` à `_06`
- [ ] `MON_Cles_01` à `_03`
- [ ] `MON_Frappe_01` à `_03`
- [ ] `MON_Cri`
- [ ] `MON_Respiration_Boucle`
- [ ] `MON_Ampoule_Claque`
- [ ] `CREDITS_SONS.txt`

---

## À régler avec l'équipe

- La vraie sonnette sonne-t-elle déjà? Sinon, il faut `SFX_Sonnette`.
- Les haut-parleurs : un par côté (gauche = pièce, droite = corridor)?
- La fin : soulagement ou dernier frisson?
- Les touches du numpad : est-ce TouchDesigner qui joue leur son?

---

## Pour l'équipe Unity (intégration)

Tous les sons sont en 2D (*Spatial Blend* = 0). Le côté se règle avec *Stereo Pan* sur l'AudioSource : −1 = pièce, +1 = corridor, 0 = les deux.

| Son | Où le mettre | Prêt? |
|---|---|---|
| `FB_Bonne_Couleur`, `FB_Mauvaise_Couleur`, `FB_Combinaison_Reussie` | `Placeholders (à remplacer) → Sons (bips)` → « Bonne couleur », « Mauvaise couleur », « Combinaison réussie » : champ *AudioClip* de l'AudioSource | Oui : le bip s'arrête tout seul |
| `FB_Mauvais_Code`, `FB_Bon_Code` | « Mauvais code », « Bon code » (même endroit) | Oui |
| `SFX_Porte_Ouverture` | « Porte ouverte » (même endroit) | Oui |
| `SFX_Prise_Etincelles` | `Étincelles de la prise → EtincellesPrise → Son` (ajouter une AudioSource) | Oui, mais une seule variante |
| `SFX_Ventilation_Sequence`, `SFX_Lumieres_Rallument` | Une piste *Audio* dans la Timeline `Assets/Animation/Indices/Indices.playable`, à 0 s | Oui, sans code |
| Tout le reste : ambiances, lampe, interrupteur, têtes, cordes, journaux, variantes au hasard | — | Non : il faut ajouter des points de branchement dans le code |
