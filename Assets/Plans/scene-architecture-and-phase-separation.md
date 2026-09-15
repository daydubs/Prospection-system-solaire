# Project Overview
- **Game Title**: Solar Prospector: Corporation
- **High-Level Concept**: Découpage architectural multi-scènes du jeu en phases étanches (Menu Principal / Pause, Base Terrestre & Joueur à pied, Vol Spatial & Système Solaire). Cette séparation élimine les conflits de coordonnées (téléportation/chute à travers le sol à Y = -600), isole les systèmes physiques et caméras, et permet d'appeler le menu en pause de manière fluide et modulaire.
- **Players**: Solo (Single Player).
- **Inspiration / Reference Games**: *No Man's Sky*, *Subnautica*, *Kerbal Space Program*, *Outer Wilds*.
- **Tone / Art Direction**: Hard Sci-Fi / Futur proche semi-réaliste (URP Lit), interfaces épurées corporate et ambiance spatiale immersive.
- **Target Platform**: PC (Standalone Windows 64-bit).
- **Screen Orientation / Resolution**: Paysage 1920x1080 (16:9).
- **Render Pipeline**: Universal Render Pipeline (URP - PC_RPAsset).

---

# Game Mechanics

## Core Gameplay Loop
1. **Menu Principal & Gestion de Session** :
   - Démarrage du jeu dans `MainMenuScene`. Configuration du pilote, choix de la corporation et de la difficulté, chargement/sauvegarde de slots de jeu.
   - Accès au menu d'options (Audio, Graphismes URP, Sensibilité, Mappage ZQSD / WASD).
   - En cours de partie (Base ou Espace), l'appui sur [Échap] invoque le Menu de Pause (soit en overlay additif, soit via le gestionnaire de pause sans décharger la scène active).
2. **Phase 1 : Base Terrestre (Exploration FPS & Gestion au sol)** :
   - Scène dédiée `EarthBaseScene` centrée à l'origine naturelle `(0, 0, 0)`.
   - Le joueur explore le hangar à pied en vue à la première personne, interagit avec l'écran géant [E] pour configurer la station spatiale orbitale et assembler les pièces modulaires du vaisseau.
   - Lorsque les préparatifs sont terminés, le joueur déclenche le lancement vers l'orbite depuis l'interface du Hub.
3. **Phase 2 : Vol Spatial & Prospection du Système Solaire** :
   - Transition fluide vers `SolarSystemScene`.
   - Pilotage du vaisseau en 3D libre autour du Soleil, des planètes et de la ceinture d'astéroïdes. Minage, exploration et analyse scientifique.
   - Possibilité de revenir atterrir à la base terrestre ou de docker à la station orbitale, rechargeant la scène correspondante avec persistance intégrale des ressources et de l'état du joueur via le `GameManager` persistent (`DontDestroyOnLoad`).

## Controls and Input Methods
- **Nouvel Input System Unity** (`UnityEngine.InputSystem`) avec bascule contextuelle de carte d'actions :
  - **Menu & Pause** :
    - [Échap] : Ouvrir / Fermer le menu pause ou revenir au menu précédent.
    - [Souris / Clic Gauche] : Navigation dans les menus, sélection d'options et réglage des curseurs.
  - **Vue à Pied (Base Terrestre - FPS)** :
    - [Z/Q/S/D] ou [W/A/S/D] : Déplacement au sol.
    - [Souris] : Orientation de la tête et de la caméra (Yaw & Pitch clamped).
    - [Shift Gauche] : Sprint.
    - [Espace] : Saut.
    - [E] ou [Entrée] : Interagir avec l'Écran Géant du Hub d'opérations.
  - **Vue Vaisseau (Vol Spatial)** :
    - [Z/S] : Poussée principale / Freinage inertiel.
    - [Q/D] : Roulis / Lacet.
    - [Shift Gauche] : Boost de postcombustion.
    - [T] : Pilote automatique / Ciblage orbital.
    - [Tab] / [Clic Droit] : Mode Scanner de gisements.

---

# UI

### 1. Scène Menu Principal (`MainMenuScene`) & Overlay Pause
- **Écran d'Accueil** : Titre corporatif stylisé, bouton "Nouvelle Partie", "Continuer", "Charger Partie", "Options", "Quitter".
- **Panneau Nouvelle Partie** : Saisie du nom du commandant, nom de corporation, cartes de sélection de difficulté (Facile, Normal, Difficile).
- **Panneau Options** : 3 onglets (Audio, Graphismes URP, Jouabilité / Commandes).
- **Mode Pause** : Réutilisation de la même logique d'interface pour la pause en cours de jeu (avec bouton "Reprendre", "Sauvegarder", "Options", "Menu Principal", "Quitter le jeu").

```
+-----------------------------------------------------------------------------------+
| [ASTRAEA CORP]                 SOLAR PROSPECTOR                    [Version 1.0]  |
+-----------------------------------------------------------------------------------+
|  [ NOUVELLE PARTIE ]                                                              |
|  [ CONTINUER ]                                                                    |
|  [ CHARGER PARTIE ]                                                               |
|  [ OPTIONS SYSTÈME ]                                                              |
|  [ QUITTER ]                                                                      |
+-----------------------------------------------------------------------------------+
| [Notification Toast : Partie sauvegardée avec succès !]                          |
+-----------------------------------------------------------------------------------+
```

### 2. HUD Base Terrestre (`EarthBaseScene`)
- Réticule central d'interaction.
- Toast contextuel : `[E] ACCÉDER À L'ÉCRAN GÉANT DU HUB CENTRAL`.
- Modale plein écran du Hub d'Opérations (4 onglets : Station Spatiale, Chantier Vaisseau, Décollage & Vol, Corporation).

---

# Key Asset & Context

### Analyse de la cause racine du bug actuel :
1. **Hack de coordonnées dans la scène monolithique** :
   - `SolarSystem_Root` est situé à `(0, 0, 0)`.
   - `EarthBase_Root` a été positionné à `Y = -600` pour ne pas être visible depuis l'espace, avec un sol à `Y = -603` et le joueur à `Y = -598.59`.
   - À la fermeture du menu, les saccades de delta-temps physique combinées à l'absence de synchronisation des colliders au spawn faisaient traverser le sol au `CharacterController`, le faisant chuter dans le vide vers `Y = -900` (~300 unités de chute libre).
2. **Conflit de caméras et d'AudioListeners** :
   - Deux caméras actives/désactivées manuellement (`Main Camera` et `Player_BaseCamera`) dans la même scène créant des conflits de rendu et de focus de curseur.

### Architecture des Scènes :
1. `Assets/Scenes/MainMenuScene.unity` :
   - Canvas Menu Principal UI, EventSystem, Caméra de fond avec décor spatial URP, MainMenuController.
2. `Assets/Scenes/EarthBaseScene.unity` :
   - Base terrestre relocalisée à `(0, 0, 0)` (Hangar, Estrade, Écran géant, Assemblage du vaisseau, Éclairages URP dédiés).
   - `Player_BaseCapsule` positionné à `(0, 1.08, 0)` sur un sol propre à `Y = 0` avec collider solide et `CharacterController` standard.
   - `EarthBaseController` gérant le Hub d'opérations et la transition vers l'espace.
3. `Assets/Scenes/SolarSystemScene.unity` :
   - Soleil, Planètes, Lunes, Ceinture de 400 astéroïdes, Vaisseau d'exploration et `SpaceshipFlightController`, Skybox spatiale.
4. `Assets/Scripts/SceneTransitionManager.cs` :
   - Gestionnaire singleton de chargement asynchrone des scènes avec fondu d'écran, passage d'état et gestion propre du menu pause.

---

# Implementation Steps

### Étape 1 : Création du Gestionnaire de Transition et de Pause Multi-Scènes
- **Description** : Développer `SceneTransitionManager.cs` (DontDestroyOnLoad) capable de charger les scènes (`MainMenuScene`, `EarthBaseScene`, `SolarSystemScene`) de manière asynchrone avec fondu de transition, de sauvegarder la scène précédente et de gérer le menu de pause universel sans conflit.
- **Assigned role** : `developer`
- **Dependencies** : None
- **Parallelizable** : Yes

### Étape 2 : Création de la Scène `MainMenuScene`
- **Description** : Créer la scène autonome `Assets/Scenes/MainMenuScene.unity`. Y intégrer la caméra de présentation, l'éclairage de fond, l'EventSystem, le Canvas du menu principal et adapter `MainMenuController.cs` pour déclencher les transitions de scènes via `SceneTransitionManager`.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1
- **Parallelizable** : No

### Étape 3 : Création de la Scène `EarthBaseScene` et Recentrage du Joueur à (0,0,0)
- **Description** : Créer `Assets/Scenes/EarthBaseScene.unity`. Extraire la base terrestre de la scène globale et la recentrer à l'origine naturelle `(0, 0, 0)`. Positionner le sol à `Y = 0`, le joueur à `Y = 1.08`, configurer les colliders physiques pour supprimer définitivement le bug de chute sous le sol, et intégrer le Hub d'opérations.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1
- **Parallelizable** : No

### Étape 4 : Création de la Scène `SolarSystemScene`
- **Description** : Créer `Assets/Scenes/SolarSystemScene.unity`. Isoler le système solaire (Soleil, 8 planètes, lunes, ceinture d'astéroïdes), le vaisseau d'exploration et les contrôleurs de vol spatial.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1
- **Parallelizable** : Yes

### Étape 5 : Enregistrement dans les Build Settings & Connexion Complète
- **Description** : Ajouter les 3 scènes dans les `EditorBuildSettings` (Index 0: `MainMenuScene`, Index 1: `EarthBaseScene`, Index 2: `SolarSystemScene`). Connecter les boutons "Lancer Mission" (vers base terrestre), "Décoller vers l'espace" (vers système solaire), et "Retour Base / Atterrissage" (vers base).
- **Assigned role** : `developer`
- **Dependencies** : Étape 2, Étape 3, Étape 4
- **Parallelizable** : No

### Étape 6 : Validation du Menu Pause Universel et Stabilité Physique
- **Description** : Vérifier que la touche [Échap] permet d'ouvrir le menu de pause en cours de partie dans la base et dans l'espace, de régler les options, de sauvegarder ou de retourner au menu principal, sans aucun saut de coordonnées ni chute de joueur.
- **Assigned role** : `developer`
- **Dependencies** : Étape 5
- **Parallelizable** : No

---

# Verification & Testing

1. **Test de Démarrage & Menu Principal** :
   - Lancer la scène `MainMenuScene`.
   - Vérifier le fonctionnement des onglets Son / Graphismes / Commandes.
   - Cliquer sur "Nouvelle Partie", configurer le pilote et lancer : transition fluide vers `EarthBaseScene`.
2. **Test Physique & Anti-Chute dans la Base Terrestre** :
   - Dans `EarthBaseScene`, vérifier que le joueur apparaît debout sur le sol du hangar sans aucune téléportation ni chute à travers le plancher.
   - Se déplacer avec ZQSD/WASD, sauter avec Espace, interagir avec l'Écran Géant avec [E].
   - Ouvrir et fermer le menu de pause ([Échap]) : s'assurer que le joueur reste parfaitement stable à sa position sans glitch de collision.
3. **Test de Décollage & Vol Spatial** :
   - Depuis l'écran géant de la base, cliquer sur "Décoller vers l'espace" : chargement propre de `SolarSystemScene`.
   - Vérifier le pilotage du vaisseau, l'absence de la base dans la vue spatiale, et la simulation des orbites.
4. **Test de Sauvegarde / Chargement & Retour Menu** :
   - Mettre en pause dans l'espace, cliquer sur "Menu Principal", puis recharger la sauvegarde : vérifier la persistance exacte des crédits et de la progression.
