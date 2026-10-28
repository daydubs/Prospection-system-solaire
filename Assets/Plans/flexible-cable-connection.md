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
