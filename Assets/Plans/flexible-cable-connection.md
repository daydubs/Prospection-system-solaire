# Project Overview
- **Game Title**: Solar Prospector: Corporation
- **High-Level Concept**: Simulation spatiale et d'ingénierie où le joueur prospecte les corps célestes, explore la surface lunaire et installe des bases modulaires. Ce module technique concerne le raccordement physique et interactif d'énergie/données entre le pupitre de contrôle (*ControllerPrefab*) et le module d'habitation principal (*ModuleViePrefab*) via un flexible réaliste.
- **Players**: Solo (Single player).
- **Inspiration / Reference Games**: *Satisfactory*, *Stationeers*, *Kerbal Space Program*, *Death Stranding* (gestion des câbles/ancrages), *Astroneer*.
- **Tone / Art Direction**: Hard Sci-Fi / Industriel spatial semi-réaliste (URP Lit), composants modulaires métalliques renforcés, tuyaux gainés et prises blindées adaptées au vide lunaire.
- **Target Platform**: PC (Standalone Windows 64-bit).
- **Screen Orientation / Resolution**: Paysage 1920x1080 (16:9).
- **Render Pipeline**: Universal Render Pipeline (URP - PC_RPAsset).

---

# Game Mechanics

## Core Gameplay Loop
1. **Identification du besoin énergétique / de liaison** :
   - Le joueur explore la base lunaire et constate que le pupitre *ControllerPrefab* n'est pas alimenté ou n'a pas de liaison opérationnelle avec le *ModuleViePrefab*.
2. **Interaction à la source (Socket A)** :
   - Le joueur s'approche de la prise murale `ConnectorElectricSocket` sur le *ModuleViePrefab* (ou sur le Controller).
   - L'invite textuelle dynamique s'affiche : `[E] Prendre et dérouler le flexible d'alimentation`.
3. **Déroulement en main** :
   - En marchant avec le connecteur en main, le tuyau se génère en temps réel le long d'une courbe souple suspendue reliant la prise fixe et la main/outil du joueur.
   - Si la distance dépasse la longueur maximale du tuyau (~12 mètres), un indicateur visuel avertit d'une tension maximale.
4. **Enclenchement au récepteur (Socket B)** :
   - Le joueur s'approche du `ConnectorElectricSocket` du *ControllerPrefab*.
   - L'invite s'affiche : `[E] Raccorder le flexible`.
   - Les embouts mâles 3D s'enclenchent fermement dans les deux prises femelles. Le tuyau se fige dans une courbe caténaire suspendue naturelle (fléchissement réaliste sous la gravité lunaire).
5. **Activation du système & Déconnexion** :
   - Un signal d'alimentation s'active (`isPowered = true`, voyant lumineux vert sur le contrôleur).
   - Le joueur peut à tout moment viser l'un des embouts et appuyer sur `[E]` pour débrancher ou rembobiner le flexible.

## Controls and Input Methods
- **Nouvel Input System Unity** (`UnityEngine.InputSystem`) couplé à l'architecture existante `PlayerBaseController` / `IPromptInteractable` :
  - **[E]** : Interagir avec la prise (dérouler / brancher / débrancher).
  - **[Clic Droit]** : Annuler le déroulement et rembobiner le câble dans sa prise d'origine.
  - **[Souris / Z-Q-S-D]** : Déplacement FPS standard avec vue du câble en main.

---

# UI

### 1. Invite Réticule Interactif (Intégration `PlayerBaseController`)
- **Au repos sur prise avec câble disponible** :
  `[E] Dérouler le flexible d'alimentation`
- **Pendant le transport du câble vers la prise cible** :
  `[E] Raccorder au module de contrôle` (sur la prise cible)
  `[Clic Droit] Rembobiner le flexible`
- **Câble déjà branché** :
  `[E] Débrancher l'alimentation [Connecté]`

### 2. Indicateur d'Élongation / Portée
- Réticule devenant orange/rouge si le joueur s'approche de la distance maximale (ex: > 10m).

```
                      [ VUE JOUEUR FPS ]
 
       [ModuleViePrefab]                   [ControllerPrefab]
    +--------------------+              +--------------------+
    |  [Prise Murale]    |              |     [Console]      |
    +--------o-----------+              +---------o----------+
              \                                  /
               \__   Courbe Caténaire / Bézier  /
                  \____                      __/
                       \____________________/
                         (Fléchissement 3D)
 
           Réticule : [E] Raccorder l'alimentation
```

---

# Key Asset & Context

### 1. Spécifications pour la modélisation Blender
L'utilisateur peut concevoir dans Blender les assets suivants (sans avoir à gérer d'os ni de déformation complexe dans Blender) :
- **Plug mâle (Embout de raccordement)** :
  - Petit modèle 3D de prise cylindrique / raccord baïonnette spatial (ex: ~10 à 15 cm de diamètre, 20 cm de profondeur).
  - Point de pivot (Origin) placé exactement à la base où le tuyau souple commence à sortir.
  - Deux instances seront automatiquement instanciées et orientées aux deux extrémités du câble par Unity.
- **Matériau / Textures du tuyau** :
  - Texture PBR répétable (seamless) : aspect caoutchouc noir armé, gaine tressée ou soufflet métallique annelé (Albedo, Normal Map, Metallic/Smoothness).
  - Le script Unity appliquera automatiquement un tiling UV le long de la longueur du flexible.

### 2. Scripts existants & Points d'ancrage dans `MoonScene`
- `MoonScene.unity` :
  - `ModuleViePrefab/ConnectorElectricSocket` : Position `(654.22, 12.17, 368.76)`
  - `ConnectorElectricSocket` (root) : Position `(661.31, 11.36, 368.28)` -> à réassocier proprement à `ControllerPrefab`.
  - Distance entre les deux prises : ~7.15 mètres.
- Interfaces existantes :
  - `Assets/Scripts/Interface/IPromptInteractable.cs`
  - `Assets/Scripts/Interface/IInteractable.cs`
  - `Assets/Scripts/Controllers/PlayerBaseController.cs` (gère la détection par Raycast et l'affichage GUI du prompt `[E]`).

### 3. Nouveaux Scripts C# à créer
- `FlexibleCableRenderer.cs` :
  - Calcule la courbe de Bézier cubique 3D / caténaire entre 2 points avec vecteurs tangents respectant l'orientation des prises (`Socket.forward`).
  - Génère dynamiquement le Mesh tubulaire 3D (cylindre extrudé le long de la courbe avec UV coordonnées étirées/répétées et normales lissées).
  - Inclut un paramètre `sag` (fléchissement vertical simulant la gravité lunaire de 1.62 m/s²).
- `ElectricConnectorSocket.cs` :
  - Implémente `IPromptInteractable`.
  - Gère la machine à états de la prise (Disponible, En cours de déroulement, Connecté).
  - Référence le prefab de prise mâle (Plug) et pilote `FlexibleCableRenderer`.
  - Émet les événements d'alimentation électrique (`OnPowerStateChanged(bool isPowered)`).
- `CableInteractionHandler.cs` (ou intégré à la socket) :
  - Suivi de la prise libre attachée à la caméra/main du joueur pendant le déroulement.

---

# Implementation Steps

### Étape 1 : Modélisation dans Blender & Préparation des Assets
- **Description** :
  - Modéliser dans Blender l'embout mâle de connexion (*Connector_Plug.fbx*) et exporter avec l'origine au point de jonction du câble.
  - Créer le matériau URP pour le câble (*M_FlexibleHose*) avec Normal Map ondulée/annelée et répétition UV en longueur.
- **Fichiers / Assets** :
  - `Assets/Art/Models/Sockets/Connector_Plug.fbx`
  - `Assets/Art/Materials/M_FlexibleHose.mat`
- **Dependencies** : None
- **Parallelizable** : Yes

### Étape 2 : Moteur de Génération de Tube Souple Procédural (`FlexibleCableRenderer.cs`)
- **Description** :
  - Développer le script générant le maillage cylindrique tubulaire 3D extrudé le long d'une courbe de Bézier cubique.
  - Calculer les tangentes en fonction des normales de chaque socket pour que le tuyau sorte perpendiculairement aux parois avant de fléchir vers le bas.
  - Calculer l'affaissement vertical paramétrable selon la distance pour simuler la gravité lunaire.
  - Ajouter le calcul des UVs en coordonnées cylindriques et le calcul des tangentes/normales pour le shader URP Lit.
- **Fichiers / Assets** :
  - `Assets/Scripts/BuildingSystem/Cables/FlexibleCableRenderer.cs`
- **Dependencies** : Étape 1
- **Parallelizable** : No

### Étape 3 : Composant de Prise Électrique Intelligente (`ElectricConnectorSocket.cs`)
- **Description** :
  - Créer le script `ElectricConnectorSocket` implémentant `IPromptInteractable` et l'attacher aux deux objets `ConnectorElectricSocket`.
  - Rendre le `ConnectorElectricSocket` orphelin enfant direct de `ControllerPrefab` pour maintenir la hiérarchie propre.
  - Configurer les références vers la prise cible ou la détection par proximité.
  - Ajouter le déclenchement des états : `Empty`, `CableAttached`, `Connected`.
- **Fichiers / Assets** :
  - `Assets/Scripts/BuildingSystem/Cables/ElectricConnectorSocket.cs`
  - `Assets/Prefabs/Base/SousSystem/ControllerPrefab.prefab` (Mise à jour hiérarchie socket)
  - `Assets/Prefabs/Base/ModuleVie/ModuleViePrefab.prefab` (Mise à jour socket)
- **Dependencies** : Étape 2
- **Parallelizable** : No

### Étape 4 : Système de Déroulement et Connexion Joueur (`CableInteractionManager.cs`)
- **Description** :
  - Gérer l'interaction du joueur : quand le joueur appuie sur `[E]` sur la prise du Module Vie, l'embout du câble est saisi et suit le joueur (attaché temporairement à un point d'ancrage en main / caméra).
  - La courbe de `FlexibleCableRenderer` se met à jour en temps réel à chaque frame.
  - Quand le joueur s'approche de la prise du `ControllerPrefab` et appuie sur `[E]`, l'embout s'aimante (snap) sur la prise cible, la connexion est scellée et le circuit s'active.
  - Gérer l'annulation via `[Clic Droit]` pour ranger le câble.
- **Fichiers / Assets** :
  - `Assets/Scripts/BuildingSystem/Cables/CableInteractionManager.cs`
- **Dependencies** : Étape 3
- **Parallelizable** : No

### Étape 5 : Intégration Visuelle et Énergétique dans `MoonScene`
- **Description** :
  - Valider l'enclenchement des plugs sur les deux sockets dans la scène `MoonScene`.
  - Optionnel : activer un voyant LED vert ou l'écran du `ControllerPrefab` une fois le flexible branché pour donner un feedback clair au joueur.
- **Fichiers / Assets** :
  - `Assets/Scenes/MoonScene.unity`
- **Dependencies** : Étape 4
- **Parallelizable** : No

---

# Verification & Testing

1. **Test de la Courbe et Rendu Procédural** :
   - Tester le rendu du cylindre tubulaire en mode Éditeur et en mode Jeu.
   - Vérifier que la courbure est douce, sans cassure, et que la texture s'étire uniformément sans artefact d'UV.
   - Vérifier que les normales réagissent correctement à la lumière directionnelle lunaire (URP Lit).
2. **Test d'Interaction en Jeu (FPS)** :
   - Lancer le Play Mode dans `MoonScene`.
   - Viser la prise électrique du module de vie : constater l'affichage de l'invite `[E] Dérouler le flexible d'alimentation`.
   - Appuyer sur `[E]` : vérifier que le câble se déroule en suivant les déplacements du joueur sans saccade ni à-coup.
   - Viser la prise du contrôleur : appuyer sur `[E]`, vérifier que l'embout se magnétise exactement sur la socket et que le tuyau se fige avec son fléchissement naturel.
3. **Test d'Annulation et Portée Maximale** :
   - Dérouler le câble et appuyer sur `[Clic Droit]` : vérifier le rembobinage immédiat et propre.
   - Essayer d'éloigner le joueur au-delà de la longueur maximale du câble : vérifier que le câble ne s'étire pas indéfiniment.
4. **Test de Sauvegarde et Persistance** :
   - Vérifier que l'état branché/débranché est conservé lors du changement d'état ou rechargement.

---

# Révision — Rig / Crusher : deux liaisons distinctes au sol

Cette révision étend le système existant. Les sections précédentes restent le contexte historique de la liaison ModuleVie–Controller ; les choix ci-dessous font foi pour le Rig et le Crusher. Aucun asset de gameplay n'a été modifié pendant la préparation.

# Project Overview
- **Game Title** : Solar Prospector: Corporation (titre du plan existant).
- **High-Level Concept** : Installation industrielle lunaire placée par le joueur : le Crusher fournit l'électricité au Rig et reçoit ses gravats par un tuyau distinct. Les liaisons physiques et la production provisoire donnent une boucle industrielle testable avant la création de l'item batterie.
- **Players** : Solo, conformément au plan existant.
- **Inspiration / Reference Games** : Satisfactory, Stationeers, Astroneer — références du plan existant.
- **Tone / Art Direction** : Industriel spatial semi-réaliste, conservation des modèles et matériaux fournis.
- **Target Platform** : Windows 64 bits.
- **Screen Orientation / Resolution** : Paysage ; référence historique 1920×1080, sans changement de résolution.
- **Render Pipeline** : URP, PC_RPAsset ; Unity 6000.6.3f1.

# Game Mechanics
## Core Gameplay Loop
1. Construire le Rig et le Crusher via les blueprints et le placement au sol existants, sans changer les coûts ou la mécanique de soudure.
2. Dérouler manuellement depuis la zone centrale du Rig deux liaisons indépendantes : un câble électrique vers la sortie électrique du Crusher et un tuyau de gravats vers son entrée matière.
3. Les embouts sont distingués par leur géométrie, leur matériau et les invites. Deux ancrages voisins dans la zone centrale, et non deux maillages superposés exactement au même point.
4. Le Crusher utilise temporairement un booléen d'alimentation de test illimitée, activé par défaut et explicitement identifié comme provisoire dans l'Inspector. Il ne dépend pas du réseau de la base et aucune batterie-item n'est créée.
5. Le Rig produit 1 unité interne de gravats toutes les 5 secondes lorsqu'il reçoit effectivement du courant et dispose d'une liaison matière vers un Crusher compatible ayant de la place. Sans liaison ou capacité, production en pause, sans rattrapage massif à la reconnexion.
6. Le Crusher transforme les gravats en poudre à un ratio 1:1. Valeur initiale de traitement : 1 unité par cycle de 5 secondes, configurable pour tester l'équilibre de la chaîne. Entrée et sortie ont chacune une capacité interne configurable de 100 unités ; sortie pleine = arrêt du broyage, puis arrêt de l'amont lorsque l'entrée est pleine.
7. Sur le corps du Crusher, E récupère la poudre vers l'inventaire. Une insertion réussie d'une unité retire exactement une unité du stock ; inventaire plein = reste conservé dans la machine.
8. Couper l'énergie de test arrête immédiatement le Rig et le broyage. Débrancher uniquement le tuyau arrête les nouveaux apports mais permet au Crusher de finir les gravats déjà stockés tant qu'il est alimenté. Débrancher le câble arrête le Rig mais pas le Crusher autonome.

**Décisions confirmées** : raccordement manuel, suivi du terrain sans points intermédiaires, production provisoire, énergie Crusher → Rig ; paramètres de test, ratio 1:1, récupération avec E et énergie provisoire illimitée approuvés.

**Approches examinées** : raccordement automatique (rapide mais ambigu avec plusieurs machines), choix des cibles pendant placement (aperçu clair mais extension importante du Builder), raccordement manuel (retenu, cohérent avec les interactions existantes et l'autonomie du joueur). Pour les tuyaux, suivi direct du terrain retenu plutôt que points de routage manuels.

## Controls and Input Methods
- Backend vérifié : `activeInputHandler = 1`, New Input System.
- Conserver `PlayerBaseController`, `IPromptInteractable`, et la construction existante.
- E sur une prise : dérouler / raccorder / débrancher ; E sur le corps du Crusher : récupérer la poudre.
- Clic droit : annuler le déroulement via une `InputAction` événementielle, et non ajouter de lecture de l'ancien Input Manager.
- Bloquer les interactions de câble pendant construction active ou menus ouverts pour éviter les conflits d'annulation et d'interaction.
- Le point en main et l'embout utilisent le prefab du type de liaison porté. Validation de compatibilité, disponibilité et longueur avant connexion, y compris pour appels directs et restauration d'état.
- Portée initiale conservée à 15 m ; vérifier la longueur réelle du trajet au sol, pas seulement la distance entre sockets. Rembobinage toujours possible même si la cible est incompatible.

# UI
Réutiliser les invites du réticule déjà affichées par `PlayerBaseController` ; aucun nouveau Canvas, menu ou terminal.

```text
Rig, centre : [prise courant] [raccord gravats]
               |                    |
               +-- câble au sol ----+----> Crusher (prises distinctes)

Viser prise : [E] Dérouler le câble électrique / le tuyau de gravats
Viser cible : [E] Raccorder au Crusher
Refus : Type incompatible / Prise occupée / Hors portée
Connecté : [E] Débrancher le câble électrique / le tuyau de gravats
Corps Crusher : [E] Récupérer la poudre (N/100)
               Énergie de test / Sans énergie / Stock plein / En attente
```
Le texte distingue connexion physique et alimentation effective. Les colliders des prises doivent être accessibles au raycast sans être masqués par le collider du corps. Réutiliser les rafraîchissements d'inventaire existants après récupération.

# Key Asset & Context
## État réellement observé
- `Assets/Prefabs/Base/SousSystem/RigPrefab.prefab` : MeshFilter, MeshRenderer et MeshCollider ; aucune logique industrielle, aucun ancrage de câble.
- `Assets/Prefabs/Base/SousSystem/CrusherPrefab.prefab` : MeshFilter et MeshRenderer uniquement ; ajouter collider et logique opérationnelle.
- `Assets/Prefabs/Cables/ConnectorTuyauxPrefab.prefab` : modèle fourni, sans collider, script ou `CableExit` ; conserver le visuel et l'encapsuler avec un ancrage logique si nécessaire.
- `Assets/Art/Materials/M_FlexibleTuyaux.mat` : matériau fourni exact, actuellement sans dépendants ; l'affecter au tuyau matière seulement. Conserver `M_FlexibleHose.mat` pour l'électricité.
- Les trois prefabs ont des transforms d'import à échelle 100 et rotation proche de X=270°. Ne pas appliquer aveuglément l'échelle monde 1 aux meshes FBX : conserver la conversion d'import sous un wrapper logique à échelle 1. Tester les dimensions physiques des embouts et placer `CableExit` sur leur sortie réelle.
- `Assets/Items/RegolitePowder.asset` : Item existant, `id=0`, `itemName` vide, `maxStack=1`, modèle assigné. Compléter nom, description, pile de 100 et identifiant unique non nul après inventaire complet des IDs ; conserver modèle et ne pas inventer une icône.
- `Assets/Items/RigBlueprintItem.asset` et `Assets/Items/CrusherBlueprintItem.asset` référencent les prefabs et données de construction ; `Assets/construction/Crusher.asset` est déjà `groundPlacementOnly=true`.
- `BuilderController.FinalizeConstruction(...)` instancie le prefab final. Des composants correctement configurés sur les prefabs rendent chaque machine opérationnelle sans connexion automatique ni recherche globale du Crusher le plus proche.
- `ElectricConnectorSocket.ConnectTo(...)` et `OnConnectedFrom(...)` mettent actuellement `isPowered=true` simplement en branchant ; aucune distinction matière/courant, compatibilité ou rôle entrée/sortie. Corriger ce comportement pour les machines nouvelles sans casser les liaisons historiques.
- `CableInteractionManager` est aujourd'hui électrique uniquement, possède un seul prefab porté et ne valide pas les cibles. Le rendu utilise une Bézier affaissée sans contact au terrain.
- `Inventory.AddItem(Item,int)` peut ajouter partiellement puis retourner false. Récupérer unité par unité pour garantir l'absence de pertes et de duplication, sans modifier l'inventaire global.
- Terrain actif de MoonScene : TerrainCollider sur Default ; filtrer explicitement les surfaces et exclure machines, joueurs, triggers et ghosts.

## Assets à créer / modifier
- Modifier `Assets/Scripts/BuildingSystem/Cables/ElectricConnectorSocket.cs` : garder classe et GUID ; faire reposer l'interaction partagée sur un composant commun, préserver les champs sérialisés électriques et la valeur par défaut historique.
- Créer `Assets/Scripts/BuildingSystem/Cables/CableConnectorSocket.cs` : états de liaison, rôles/type, références de renderer et plug, validation partagée. API envisagée : `CanConnectTo(target, out reason)`, `ConnectTo(target)`, `Disconnect()`.
- Créer `Assets/Scripts/BuildingSystem/Cables/MaterialConnectorSocket.cs` : entrée/sortie de gravats, sans signal d'alimentation électrique.
- Modifier `CableInteractionManager.cs` pour accepter le composant commun et le prefab de la source, vérifier les callers de `CarryingSourceSocket`, nettoyer les objets temporaires et utiliser une action d'annulation événementielle.
- Modifier `FlexibleCableRenderer.cs` : mode optionnel suivi de sol, désactivé par défaut pour préserver les autres câbles ; longueur calculée, masque de sol, dégagement/rayon et cache.
- Créer `Assets/Scripts/BuildingSystem/Industry/RigUnit.cs` et `CrusherUnit.cs` : logique locale de production, références explicites aux sockets, stocks bornés, énergie provisoire et récupération.
- Modifier RigPrefab et CrusherPrefab ; ajouter ancrages, petits colliders interactifs, références typées et profils de rendu. Inspecter les ghosts pour s'assurer qu'aucune production ni interaction n'y est active ; ne les modifier que si nécessaire.
- Modifier ConnectorTuyauxPrefab pour normaliser sa structure logique et fournir `CableExit`, sans redimensionner son modèle ni changer son matériau artistique.
- Modifier RegolitePowder.asset comme décrit ; ne pas créer batterie ou item de gravats à cette étape (gravats = stock interne).
- Ajouter tests sous `Assets/Tests/` si l'infrastructure Unity Test Framework disponible le permet ; sinon validations manuelles reproductibles et contrôles Editor. Ne pas imposer de dépendance/package nouveau non approuvé.

## Impacts et protections
L'analyse des dépendances via AssetDatabase a identifié MoonScene, les blueprints, recettes et le Lander comme dépendants des prefabs. Les scripts électriques/rendu sont également utilisés par ModuleVie, Controller, SolarPanel, Player et test.unity. Les defaults doivent donc rester compatibles, sans toucher aux copies `_Recovery`, aux packages ou aux sources FBX. Refaire l'analyse avant édition de chaque asset supplémentaire.

# Implementation Steps
1. **Sécuriser le contexte et la compatibilité**
   - Relire les références sérialisées des sockets existantes, les plug prefabs électriques, le prefab Player et les guards de menus/construction ; analyser leurs dépendances avant changement si une modification devient nécessaire.
   - Inventorier tous les Item IDs du projet pour configurer RegolitePowder sans collision.
   - Vérifier tests existants/package Test Framework et console initiale. Ne pas modifier pipeline, defines ou packages.
   - **Dependencies** : aucune.
2. **Introduire la liaison commune sans migration destructive**
   - Créer CableConnectorSocket et MaterialConnectorSocket ; adapter ElectricConnectorSocket en gardant le script existant et ses données.
   - Séparer connexion physique, énergie effective et flux matière. Ajouter rôles source/récepteur pour les nouveaux ports, avec mode historique compatible par défaut.
   - Pour le duo industriel : Crusher = source courant + récepteur gravats ; Rig = récepteur courant + source gravats. Le Rig détient les deux flexibles même lorsqu'il reçoit le courant.
   - Refuser types/rôles incompatibles, self-link, ports occupés et liaisons doublées. Disconnect symétrique, destruction d'une machine = nettoyage de son pair, sans affecter les autres liaisons.
   - **Dependencies** : étape 1.
3. **Adapter le transport et les interactions**
   - CableInteractionManager : transporter un port commun ; sélectionner l'embout selon la source ; valider avant de quitter l'état porté ; cible refusée = garder la liaison en main.
   - Brancher une InputAction d'annulation avec abonnements/désabonnements propres, gardes de menus et arbitrage avec Builder.
   - Nettoyer EndPlug à l'annulation, les plugs et anchors temporaires à la destruction/désactivation.
   - **Dependencies** : étape 2.
4. **Rendre les flexibles posés au sol**
   - FlexibleCableRenderer : conserver la courbe actuelle par défaut. Activer le profil terrain uniquement sur les deux flexibles industriels.
   - Construire une descente douce depuis les embouts vers le sol, échantillonner le TerrainCollider par raycasts filtrés, garder la ligne centrale au-dessus du terrain d'au moins le rayon plus une petite marge, préserver les sorties et tangentes des prises.
   - Recalculer UV et longueur après adaptation ; refuser connexion >15 m de trajet. Préciser visuellement l'absence de support si une portion traverse un trou ; pas de routage autour des rochers ni simulation Rigidbody/Joints.
   - Ne pas laisser une simple interpolation lissée replonger sous le terrain : revalider les points finaux. Réutiliser buffers de mesh et cache ; pas de raycasts permanents sur un lien statique ni recherche globale par frame.
   - **Dependencies** : étape 2 ; test porté complet après étape 3.
5. **Implémenter la boucle provisoire**
   - RigUnit : minuterie configurable 5 s, vérification alimentation et connexion matière ; transférer atomiquement une unité au Crusher connecté uniquement si son entrée l'accepte.
   - CrusherUnit : énergie de test illimitée configurable, propagation du courant à son pair électrique Rig, minuterie de broyage configurable, entrée/sortie bornées à 100 et conversion 1:1 sans stock négatif.
   - Exposer un point d'intégration explicite pour la future alimentation batterie ; aucune consommation réelle de batterie, autonomie, UI batterie ou item fictif.
   - Configurer RegolitePowder ; récupération via `IPromptInteractable`, insertion unitaire dans l'inventaire existant et décompte après réussite uniquement. Échec = stock préservé.
   - **Dependencies** : étapes 1 et 2.
6. **Configurer les prefabs fournis**
   - Placer les deux ports voisins au centre physique du Rig en tenant compte des axes FBX ; placer ports Crusher sur les raccords visibles appropriés.
   - Affecter plug électrique existant au courant, ConnectorTuyauxPrefab et M_FlexibleTuyaux à la matière ; wrappers et CableExit dimensionnés d'après les meshes réels.
   - Ajouter collider du Crusher et colliders de port non masqués ; référencer RigUnit/CrusherUnit et item poudre. Aucune référence croisée vers une instance de scène imposée dans les prefabs.
   - Ne pas modifier coûts, recettes, textures ou géométrie FBX. Chaque nouveau prefab construit fonctionne indépendamment ; ghosts restent inertes.
   - **Dependencies** : étapes 3 à 5.
7. **Valider la chaîne et les non-régressions**
   - Construire les deux machines dans MoonScene via le joueur et tester les variantes décrites ci-dessous. Corriger seulement les assets concernés et vérifier leurs impacts avant édition.
   - Contrôler compilation et console, puis build Windows si l'environnement et la configuration de build le permettent.
   - **Dependencies** : étape 6.

# Verification & Testing
- **Placement réel** : construire depuis les deux blueprints, vérifier pose au terrain, dimensions intactes, consommation de blueprint inchangée, absence d'interactions/production sur ghosts.
- **Liaisons distinctes** : deux origines centrales accessibles ; matériau et embouts corrects, y compris embout en main. Impossible de raccorder tuyau à prise électrique ; le refus ne perd pas le flexible porté.
- **Indépendance** : brancher/débrancher les deux types dans les deux ordres ; aucune alimentation issue du tuyau, aucune matière issue du câble. Débrancher depuis chacune des extrémités nettoie correctement les deux ports.
- **Énergie** : Crusher seul garde son énergie provisoire ; Rig reçoit uniquement via le câble. Énergie de test OFF = Rig arrêté et broyage arrêté, stocks conservés ; ON = reprise sans rattrapage de temps passé hors service.
- **Production** : 5 s opérationnelles donnent une unité d'apport ; broyage 1:1, compteurs bornés, comportement stable à différents framerates. Entrée/sortie pleines arrêtent les opérations sans perte ni création gratuite ; vider la sortie permet reprise.
- **Inventaire** : récupération vide, complète et partielle ; tester une place restante avec plusieurs unités en machine, puis inventaire plein. Vérifier conservation totale poudre machine + inventaire et affichage du nom/stack correctement configurés.
- **Terrain** : sol plat, pente, petite bosse, extrémités à hauteurs différentes, rotations et échelles des prefabs, petit écart, portée limite et trou sans collider. Pas de tube sous le terrain sur les portions supportées ni étirement après connexion refusée. Pas de promesse de contournement d'obstacles.
- **Plusieurs machines** : deux Rig et deux Crusher ; alimentation et matière suivent les ports réellement raccordés, pas un singleton ou le voisin le plus proche. Détruire une machine connectée ou portée ne laisse pas de lien ni plug orphelin.
- **Contrôles** : E, clic droit, menus/inventaire, construction et alternance électricité/matière ; pas de double interaction ni d'annulation du mauvais système.
- **Régression** : ModuleVie–Controller et SolarPanel, prefab Player et test.unity ; champs existants conservés, mode suspendu inchangé, pas de modification involontaire des matériaux partagés.
- **Performance et build** : pas de reconstruction/raycast continu sur liens statiques ; ressources mesh et actions libérées. Compilation sans nouvelle erreur et build StandaloneWindows64 lorsque disponible.
- **Hors périmètre** : batterie-item réelle, équilibrage définitif, gisements, animation de gravats, fabrication/imprimante 3D, routage autour des obstacles, multijoueur et sauvegarde persistante de nouveaux stocks/liens. Le test historique de persistance n'autorise pas à créer un nouveau système de sauvegarde dans cette révision.
