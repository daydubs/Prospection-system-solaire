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

---

# Révision — Module batterie de base raccordé manuellement

Cette extension concerne le module stationnaire, pas la batterie externe du Crusher. Les sections historiques restent le contexte ; cette révision remplace uniquement le stockage virtuel du réseau de base et son raccordement. Aucun asset de gameplay n'a été modifié pendant la préparation.

# Project Overview
- **Game Title** : Solar Prospector: Corporation (titre historique ; productName actuel : Prospection system solaire).
- **High-Level Concept** : Le joueur construit un accumulateur physique entre le contrôleur électrique et la base. Il stocke l'énergie produite en excès et fournit le complément nécessaire lorsque la production baisse ; le joueur surveille et pilote son utilisation depuis un terminal.
- **Players** : Solo.
- **Inspiration / Reference Games** : Satisfactory, Stationeers, Astroneer, conformément au contexte existant.
- **Tone / Art Direction** : Industriel spatial ; conserver le modèle batterie, ses matériaux et le style sombre/cyan/vert/ambre du contrôleur.
- **Target Platform** : StandaloneWindows64.
- **Screen Orientation / Resolution** : Paysage, UI adaptable ; référence de conception 1920×1080, vérifier aussi 1024×768 (valeurs actuelles PlayerSettings), sans changer la configuration d'affichage.
- **Render Pipeline** : URP 17.6.0, PC_RPAsset ; Unity 6000.6.3f1.

# Game Mechanics
## Core Gameplay Loop
1. Construire le ModuleBatterie via le blueprint et la recette existants, sans modifier leurs coûts.
2. Raccorder manuellement : panneaux → entrées contrôleur ; sortie contrôleur → entrée batterie ; sortie batterie → prise base (ModuleVie). Aucun branchement automatique après placement.
3. Le module construit commence à **0/120 kWh**. Les 88,5 kWh virtuels du contrôleur ne sont pas copiés ni ajoutés.
4. Production utilisée d'abord pour la demande réellement desservie ; seul le surplus recharge la batterie. Si la production est insuffisante, la batterie couvre automatiquement le déficit jusqu'à épuisement.
5. La batterie laisse passer la production disponible lorsqu'elle est vide ou lorsque sa charge/décharge est interdite. Ces autorisations concernent le stockage, pas le disjoncteur général.
6. Débrancher la liaison vers la base supprime la consommation desservie ; la batterie peut continuer à charger si l'amont reste raccordé. Débrancher l'amont conserve son stock mais isole ce premier montage : pas de fonctionnement autonome sans contrôleur raccordé. Les données de circuits et leurs commandes restent sur ce dernier.
7. Batterie pleine : stock borné à 120 kWh, surplus non stocké explicitement identifié. Batterie vide : aucune puissance gratuite ; si la production ne suffit pas, délestage cohérent des circuits et indication de la demande non desservie.
8. Une seule batterie directement entre un contrôleur et une base dans cette version ; plusieurs installations indépendantes possibles, mais pas de chaînage de batteries, réseau maillé ou agrégation globale.
9. Stock conservé pendant déconnexion/reconnexion de la même instance. Pas de nouveau système de sauvegarde entre sessions ni de récupération du stock au démontage dans cette étape.

**Choix confirmés par l'utilisateur** : Contrôleur → batterie → base ; automatique avec secours ; capacité 120 kWh, initialement vide.

**Approches examinées** : accumulateur autonome avec solveur de réseau général (extensible, mais refonte disproportionnée) ; stockage local physique et bilan calculé par le contrôleur explicitement raccordé (retenu : réutilise circuits et télémétrie, pas de double simulation). En termes MDA/SDT, le raccordement et les commandes donnent de l'autonomie au joueur ; un retour clair sur surplus, déficit et stock renforce la maîtrise sans gestion manuelle permanente.

## Controls and Input Methods
- Backend vérifié : activeInputHandler=1 ; Input System 1.20.0, EventSystem avec InputSystemUIInputModule dans MoonScene.
- Réutiliser PlayerBaseController / IPromptInteractable et CableInteractionManager : E pour dérouler/raccorder/débrancher ; clic droit pour annuler.
- E sur le corps du module : ouvrir le terminal de gestion ; les prises ont leurs propres colliders et restent accessibles au raycast.
- Nouveau terminal : boutons souris, fermeture par E/Échap via InputAction événementielle avec délai empêchant la fermeture par la même pression que l'ouverture.
- Bloquer déplacement, regard, inventaire, construction et câbles durant le terminal ; restauration de l'état précédent seulement après fermeture effective. Ne pas reprendre aveuglément le CloseTerminal actuel qui réactive le joueur même à l'initialisation.

# UI
Créer un terminal uGUI/TMP dédié, inspiré de ElectricityTerminalCanvas sans recopier les commandes de générateur/circuits sur la batterie. Réutiliser police et palette existantes ; pas de nouvelle police ni texture générée.

```text
┌ MODULE BATTERIE — Accumulateur de base ───────── [×] ┐
│ État : En charge / Décharge / Pleine / Vide / Isolée │
│                                                    │
│  STOCK                      FLUX                   │
│  [████████░░░░░░]  60 %     Production : … kW       │
│  72,0 / 120,0 kWh           Demande base : … kW     │
│  Pleine dans : …            Charge : … kW          │
│  Autonomie : …              Décharge : … kW        │
│                             Non desservi : … kW    │
│                                                    │
│  ENTRÉE : contrôleur — raccordée / hors tension    │
│  SORTIE : base — raccordée / alimentée             │
│                                                    │
│ [Charge autorisée : ON] [Décharge autorisée : ON]   │
│                    [Fermer — E / Échap]            │
└────────────────────────────────────────────────────┘
```
- États distinguant branchement physique et courant effectif ; textes et voyants, pas de couleur seule.
- Autonomie calculée sur le déficit effectivement couvert ; temps de charge sur le flux réellement stocké. Afficher « — » quand non applicable, éviter division par zéro et faux « en charge » à pleine capacité.
- Télémétrie actualisée à cadence limitée (par exemple 5 Hz) uniquement si le terminal est ouvert ; boutons appliqués immédiatement.
- UI contrôleur et écran 3D affichent le stock du module raccordé, ou « Aucun module batterie raccordé ». Séparer bilan disponible, énergie stockée/déstockée et alimentation aval ; ne pas conserver une batterie fictive de secours.
- Aucun écran 3D ajouté arbitrairement au modèle batterie ; interface de management accessible par interaction comme le contrôleur.

# Key Asset & Context
## État vérifié
- Chemin exact : `Assets/Prefabs/Base/SousSystem/ModuleBatteriePrefab.prefab` (et non Assets/Prefabs/Base/ModuleBatteriePrefab).
- Prefab : MeshCollider et deux ElectricConnectorSocket Input/Output, aucun script de batterie ; les deux ports sont Legacy et sans câble stocké. Un FlexibleCable existe ; ne pas partager le même renderer entre deux liaisons.
- ModuleBatterie et Controller ont une échelle d'import 100 : conserver géométrie, taille physique et matériaux ; placer les éléments logiques en tenant compte des transforms.
- `BaseElectricityController.cs` simule actuellement 120 kWh avec 88,5 kWh initiaux ; compte 4,5 kW par entrée IsPowered et active les scrubbers par recherche globale. Le prefab a déjà baseline solaire=0 (le défaut du script est 4).
- `CableConnectorSocket.CanConnectTo` refuse Legacy↔Input/Output. Une migration cohérente des ports concernés est donc indispensable, sans affaiblir cette protection.
- ModuleVie est actuellement connecté à une entrée contrôleur dans MoonScene : ce branchement historique doit être retiré et son rôle corrigé pour empêcher de le compter comme production solaire.
- Ghost batterie contient des sockets actives : les rendre inertes sans changer sa géométrie.
- Console initiale : aucune erreur. Test Framework 1.8.0 présent.
- Dépendances/dépendants analysés via AssetDatabase : batterie utilisée par MoonScene, blueprint, données de construction et recette ; contrôleur et terminal utilisés par MoonScene et copies _Recovery. Les scripts de câble touchent aussi panneaux, ModuleVie, Player, industrie et test.unity. Ne pas modifier _Recovery, packages, FBX ni matériaux partagés.

## Fichiers prévus
- Créer `Assets/Scripts/BuildingSystem/Electricity/BaseBatteryModule.cs` : charge bornée, capacité, autorisations, références explicites entrée/sortie, télémétrie et IPromptInteractable. API indicative : `ApplyEnergyBalance(productionKW, servedLoadKW, dt)`, `SetChargingAllowed(bool)`, `SetDischargingAllowed(bool)` ; retourne flux réellement appliqués, ne simule pas une seconde fois dans Update.
- Créer `Assets/Scripts/BuildingSystem/Electricity/BaseBatteryTerminalUI.cs` et `Assets/Prefabs/ui/BatteryTerminalCanvas.prefab` : interface liée à l'instance visée, pas au premier module trouvé globalement.
- Modifier BaseElectricityController.cs : stockage physique, bilan unique, états aval et scope local des consommateurs. Préserver GUID/champs historiques pour migration contrôlée, mais aucune utilisation énergétique des anciens champs virtuels.
- Modifier BaseElectricityTerminalUI.cs et BaseElectricityWorldScreenUI.cs pour télémétrie physique et absence de batterie.
- Modifier ElectricConnectorSocket.cs seulement si nécessaire pour notifier explicitement les changements de liaison même sans changement de IsPowered ; conserver comportement Legacy sur assets hors périmètre. Aucune extension arbitraire de CanConnectTo permettant Legacy↔typed.
- Modifier ModuleBatteriePrefab, ControllerPrefab, SolarPanelPrefab, ModuleViePrefab, ModuleGhostBatteriePrefab et MoonScene uniquement pour références, rôles, interaction et branchement historique. Ghost solaire/test.unity : vérifier et adapter leurs ports seulement si nécessaire à la cohérence des références existantes.
- Modifier PlayerBaseController.cs et CableInteractionManager.cs pour verrouillage terminal et conflits ; BaseElectricityTerminalUI si nécessaire pour exclusion mutuelle.
- Tests sous Assets/Tests selon infrastructure/asmdefs existants à inspecter avant création ; pas de package additionnel.

# Implementation Steps
1. **Sécuriser la migration**
   - Refaire l'analyse de dépendances de chaque fichier supplémentaire avant édition ; inspecter références de prises dans MoonScene/test.unity, ghosts, construction et consommateurs ModuleVie/Scrubber.
   - Identifier les consommateurs de la base réellement raccordée, sans singleton global pour le routage ; vérifier infrastructures de tests et verrouillage UI existants.
   - **Dependencies** : aucune.
2. **Implémenter le stockage local**
   - BaseBatteryModule : 120 kWh, initial=0, charge/décharge autorisées par défaut, énergie ΔkWh=kW×dt/3600 ; clamps et validation de dt/capacité, conservation du stock à la coupure.
   - Interdire double application du même pas de simulation et connexions à contrôleur/base non compatibles avec ce montage ; pas de stock partagé entre instances.
   - Pas de pertes, autodécharge ou limite de puissance nouvelle pour ce premier équilibrage ; exposer clairement ces simplifications.
   - **Dependencies** : étape 1.
3. **Intégrer le bilan réseau unique**
   - Contrôleur résout sa batterie par sa sortie et la base par la sortie de cette batterie. Conserver aussi le raccord direct contrôleur→base sans stockage, alimenté seulement par la production disponible.
   - Calculer séparément production réelle des entrées source, demande des circuits actifs, puissance disponible batterie comprise et consommation desservie. Éliminer baseline gratuite et stockage virtuel dans la configuration opérationnelle.
   - Allouer la puissance aux circuits essentiels en priorité puis non essentiels dans un ordre stable ; circuit insuffisamment alimentable = hors service, demande non desservie visible. Éviter oscillations de seuil et rattrapage hors connexion.
   - Déduire surplus/déficit après allocation et appliquer une seule fois au module. Ne pas compter sa décharge comme nouvelle production solaire ; couvrir précisément un pas atteignant vide/plein.
   - Propager alimentation aux ports et consommateurs de la base raccordée uniquement ; enlever l'activation globale des scrubbers dans ce parcours.
   - Garder décisions disjoncteur/modes/circuits sur le contrôleur ; disjoncteur OFF coupe charge/décharge et alimentation de son installation.
   - **Dependencies** : étape 2.
4. **Configurer les connexions joueur**
   - Sources solaires=Output ; contrôleur entrées=Input, sortie=Output ; batterie entrée=Input, sortie=Output ; base=Input. Vérifier branchement dans les deux sens de transport.
   - Batterie entrée et sortie : un flexible disponible par port avec renderer indépendant et plug/matériau existants. Les liaisons sont libres après placement, aucune référence fixe à une instance de scène dans le prefab.
   - Nettoyer symétriquement les déconnexions et rafraîchir le contrôleur même si les ports étaient hors tension ; protéger des boucles/self-links et propriétaires incompatibles.
   - MoonScene : retirer les liens historiques base→entrée contrôleur ; laisser le module batterie et ses deux nouveaux liens débranchés pour le raccordement joueur.
   - Ghost batterie : aucun terminal, aucune simulation ni socket interactive ; ne pas toucher aux coûts/recettes.
   - **Dependencies** : étapes 2–3.
5. **Créer le terminal et adapter les affichages**
   - Construire BatteryTerminalCanvas avec CanvasScaler, layout adaptable, références sérialisées complètes et boutons réellement câblés ; binder au module visé.
   - Adapter les affichages contrôleur au stock physique et à la liaison aval ; supprimer textes trompeurs sur charge/prises raccordées mais hors tension.
   - InputAction fermeture, délai d'ouverture, abonnements propres ; ouvrir un seul terminal à la fois. Bloquer inventaire/construction/câbles ; restaurer l'état précédent sur fermeture, disparition du module ou changement de scène sans débloquer un autre menu.
   - **Dependencies** : étapes 3–4.
6. **Tester et valider visuellement**
   - Exécuter tests de bilan et raccordement ; essayer la construction réelle dans MoonScene puis les deux flexibles manuels et le terminal. Tester deux installations pour indépendance.
   - Contrôler rendu, lisibilité, compilation/console et build Windows si configuration disponible.
   - **Dependencies** : étapes 4–5.

# Verification & Testing
- **Énergie** : 1 kW de surplus pendant 3600 s simulées ajoute exactement 1 kWh ; déficit équivalent retire 1 kWh. Tester dt=0, valeurs invalides, capacité nulle, vide/plein atteints pendant un pas ; jamais stock négatif ni >120.
- **Conservation** : production = énergie consommée + variation du stock + surplus perdu (conversion unités cohérente). Aucun 88,5 kWh caché, aucun stockage quand module absent, aucune décharge comptée une seconde fois comme solaire.
- **Raccordement** : construire un module vide ; brancher les deux liaisons dans les deux ordres, refuser ports incompatibles/occupés, limites de portée et self-link. Aucun câble automatique ; débrancher depuis chaque extrémité supprime la liaison entière sans perte du stock.
- **Surplus/secours** : production suffisante→base alimentée et surplus stocké ; production moindre→décharge ; pleine→surplus perdu ; vide→délestage et demande non servie. Tester absence panneaux, générateur OFF/ON et tous modes.
- **Commandes** : charge interdite conserve le surplus non stocké ; décharge interdite interdit le secours ; production directe continue de passer ; disjoncteur OFF stoppe les flux de l'installation.
- **Coupures** : sortie base débranchée→demande desservie=0, charge amont possible ; amont débranché→stock conservé et montage isolé. Reconnexion reprend sans rattrapage du temps débranché.
- **UI** : ouvrir sur chaque module distinct, état temps réel, valeurs identiques au contrôleur, « Aucun module » si absent, pas de division par zéro. Tester E/Échap/bouton/overlay, pas de fermeture immédiate à l'ouverture ni d'ouverture simultanée avec inventaire/câble/construction.
- **Cycle de vie** : disparition d'un module ouvert/connecté, désactivation/rechargement scène, plusieurs modules : pas de listener/plug/UI orphelin, contrôles restaurés correctement, aucun état global partagé.
- **Non-régression** : panneaux et ModuleVie migrés, connexion directe sans batterie, industrie Crusher–Rig et batteries externes inchangées, Ghosts inertes, test.unity contrôlée, meshes/échelles/matériaux/blueprints conservés.
- **Visuel** : 1920×1080 et 1024×768, textes français lisibles, aucun chevauchement, prises accessibles malgré MeshCollider, câbles indépendants correctement orientés.
- **Hors périmètre** : remplacement/recharge d'items batterie externes, sauvegarde durable du stock et des liens, réseau maillé ou batteries en série, génération de modèle/texture, refonte atmosphère complète, équilibrage solaire jour/nuit, nouveaux packages.
