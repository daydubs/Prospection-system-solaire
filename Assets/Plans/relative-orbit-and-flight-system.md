# Project Overview

- **Game Title**: Solar Prospector: Corporation
- **High-Level Concept**: Simulation spatiale et de prospection dans un système solaire à l'échelle. Le joueur pilote son vaisseau entre les corps célestes en mouvement, se place en orbite autour des planètes et lunes, déploie des stations orbitales et atterrit avec un module (Lander) pour exploiter les ressources à la surface.
- **Players**: Solo (Single player).
- **Inspiration / Reference Games**: *Outer Wilds*, *Kerbal Space Program* (système de patched conics / SOI simplifié), *No Man's Sky*, *Satisfactory*.
- **Tone / Art Direction**: Hard Sci-Fi / Futur proche semi-réaliste (URP Lit), instrumentation de bord technique et claire.
- **Target Platform**: PC (Standalone Windows 64-bit).
- **Screen Orientation / Resolution**: Paysage 1920x1080 (16:9).
- **Render Pipeline**: Universal Render Pipeline (URP - PC_RPAsset).

---

# Game Mechanics

## Core Gameplay Loop
1. **Sélection de destination** : Le joueur consulte la carte du système solaire et cible un astre (ex: Terre, Lune, Station Alpha, Mars).
2. **Transit Interplanétaire / Interlunaire** :
   - En **Vol Libre** ou via le **Pilote Automatique [T]**, le vaisseau navigue vers l'astre cible.
   - En espace profond (hors de la sphère d'influence d'une planète), le vaisseau est dans le référentiel héliocentrique (Soleil).
3. **Capture dans la Sphère d'Influence (SOI) & Référentiel Relatif** :
   - Dès que le vaisseau pénètre dans la Sphère d'Influence (SOI) de l'astre cible, sa physique bascule automatiquement dans le **référentiel relatif** de cet astre : le vaisseau hérite de la vitesse orbitale de l'astre.
   - Si le joueur coupe ses moteurs (`vitesse = 0`), le vaisseau reste parfaitement stationnaire **par rapport à la planète/lune**, évitant que celle-ci ne s'échappe à plusieurs unités/seconde.
4. **Insertion Orbitale & Inspection** :
   - À l'approche de l'altitude orbitale, le Pilote Automatique décélère dans le repère local et verrouille l'orbite (`FlightMode.OrbitInspect`).
   - En vol libre manuel, un indicateur HUD prévient le joueur qu'il est à portée orbitale et une touche dédiée (`[O]`) permet d'insérer le vaisseau en orbite stable circulaire à tout moment.
5. **Opérations & Atterrissage** :
   - Une fois en orbite stable autour de la Lune ou de la Terre, les opérations avancées sont déverrouillées (accès à la console de bord, déploiement de station, et lancement du Lander pour atterrir sur la surface lunaire).

## Controls and Input Methods
- **Vol Libre (Free Flight)** :
  - `[W/A/S/D]` ou `[Z/Q/S/D]` : Poussée / translation relative au corps céleste local.
  - `[Espace] / [C]` : Poussée verticale (Haut / Bas).
  - `[Shift]` : Boost de poussée (Postcombustion).
  - `[Q / E]` : Roulis (Roll).
  - `[Clic Droit + Souris]` : Orientation angulaire (Pitch / Yaw).
- **Navigation & Orbite** :
  - `[T]` : Engager / Interrompre le Pilote Automatique (interception calculée en repère relatif).
  - `[O]` : **Nouveau** : Entrer / Sortir du mode Orbite stable (`OrbitInspect`) à proximité d'un astre.
  - `[J]` : Saut Warp d'urgence (si technologie débloquée).
  - `[F]` : Basculer vue Première / Troisième personne (Cockpit / Vue extérieure).
  - `[L]` : Déclencher la séquence de descente et atterrissage sur la Lune (lorsque placé en orbite lunaire).
- **Contrôle du Temps & Pause** :
  - `[Espace]` (sur panneau d'interface) : Pause de la simulation.
  - Sélecteur de vitesse de simulation : `x1`, `x2`, `x5`, `x10`, `x50`.

---

# UI

### 1. HUD de Vol & Référentiel Dynamique
- **Indicateur de Référentiel Céleste (SOI)** :
  - En haut à gauche ou au centre du HUD de vol : affiche le corps céleste dominant actuel (ex: `RÉFÉRENTIEL : LUNE` ou `RÉFÉRENTIEL : TERRE` ou `ESPACE PROFOND : SOLEIL`).
  - Affiche la **vitesse relative** par rapport à cet astre (ex: `V. Relative : 18.4 m/s`) plutôt qu'une vitesse absolue incohérente.
- **Indicateur d'Insertion Orbitale** :
  - À portée de l'astre cible (< rayon de capture orbitale) : bannière discrète `[O] STABILISER EN ORBITE`.
  - Quand le mode `OrbitInspect` est actif : affichage `ORBITE STABLE SYNCHRONISÉE - ALTITUDE : 15.2 u`.

### 2. Panneau de Télémétrie Cible & Atterrissage
- Mise à jour du bouton d'atterrissage sur la Lune dans `SolarSystemUI` :
  - Détection fiable de la présence du vaisseau dans la SOI lunaire ou en mode `OrbitInspect`.
  - Affichage clair de l'état : `[🛬 ATTERRIR SUR LA LUNE]` actif dès que le vaisseau est capturé en orbite lunaire.

---

# Key Asset & Context

### 1. `Assets/Scripts/SolarSystem/CelestialBody.cs`
- **Ajout** : Paramètre `soiRadius` (Sphère d'Influence) calculé dynamiquement ou configurable (ex: basé sur `orbitRadius` pour les lunes ou `bodyRadius * 15f`).
- **Ajout** : Méthode `public bool IsPositionInSOI(Vector3 worldPos)` pour tester la pénétration du vaisseau.
- **Mise à jour** : Fiabilisation de `GetVelocity()` pour s'assurer que la vitesse absolue prend en compte la hiérarchie imbriquée (Lune -> Terre -> Soleil) sans sur-évaluation d'échelle (`lossyScale`).

### 2. `Assets/Scripts/Controllers/SpaceshipFlightController.cs`
- **Ajout** : Propriété `public CelestialBody currentReferenceBody { get; private set; }`.
- **Ajout** : Système de détection de la SOI dominante dans `Update` / `FixedUpdate`.
- **Mise à jour de `UpdateFreeFlight()`** :
  - Si `currentReferenceBody != null`, appliquer le déplacement du corps hôte : `transform.position += currentReferenceBody.GetVelocity() * simDt;`.
  - Le vecteur `velocity` du vaisseau devient la **vitesse relative** par rapport à l'astre local.
- **Mise à jour de `UpdateAutopilot()`** :
  - Guidage prédictif prenant en compte le repère relatif de la destination.
  - Phase de freinage progressive : décélération à vitesse relative quasi-nulle à l'approche du rayon orbital cible.
  - Transition propre et automatique vers `FlightMode.OrbitInspect` sans dépasser l'astre.
- **Ajout** : Touche `[O]` pour basculer manuellement en mode `OrbitInspect` si le vaisseau est à portée de la cible.
- **Rééquilibrage des vitesses** :
  - Augmentation de `normalSpeed` (actuellement à 0.08, valeur trop faible pour le voyage libre) à une vitesse de manœuvre locale réaliste (~5 à 15 u/s).
  - Augmentation du boost (`boostMultiplier = 3f à 5f`).

### 3. `Assets/Scripts/Managers/SolarSystemManager.cs`
- **Ajout** : Méthode utilitaire `public CelestialBody GetDominantCelestialBody(Vector3 worldPos)` pour déterminer automatiquement dans quelle SOI se trouve le joueur.
- **Mise à jour** : Gestion fluide du passage Terre -> Lune -> Autres astres.

### 4. `Assets/Scripts/SolarSystem/ui/SolarSystemUI.cs`
- **Mise à jour** : Affichage HUD de la SOI active, du vecteur vitesse relative, et du statut de capture orbitale.
- Bouton interactif pour stabiliser l'orbite `[O]` et bouton d'atterrissage réactif.

---

# Implementation Steps

### Step 1: Modélisation de la Sphère d'Influence (SOI) sur les corps célestes
- **Description** : Ajouter la définition de la Sphère d'Influence (`soiRadius`) dans `CelestialBody.cs`. Calculer un rayon de SOI adapté pour chaque type de corps (Soleil = infini/global, Planètes = espace entourant leurs lunes/satellites, Lunes = sphère locale immédiate). Vérifier et consolider le calcul de `GetVelocity()`.
- **Assigned role** : developer
- **Dependencies** : None
- **Parallelizable** : No

### Step 2: Implémentation du Référentiel Relatif dans le contrôleur de vol
- **Description** : Dans `SpaceshipFlightController.cs`, introduire `currentReferenceBody`. Dans `UpdateFreeFlight()`, additionner le vecteur déplacement de l'astre local pour que le vaisseau reste stationnaire par rapport à la planète quand il n'accélère pas. Recalibrer les vitesses de base de vol libre (`normalSpeed`, `acceleration`, `deceleration`) pour une maniabilité agréable.
- **Assigned role** : developer
- **Dependencies** : Step 1
- **Parallelizable** : No

### Step 3: Perfectionnement du Pilote Automatique et de la Capture Orbitale
- **Description** : Adapter `UpdateAutopilot()` dans `SpaceshipFlightController.cs` pour effectuer une trajectoire d'interception précise vers la destination sélectionnée, freiner doucement dans le repère relatif de l'astre cible, et déclencher sans accroc la transition vers `OrbitInspect`. Ajouter le raccourci clavier `[O]` pour permettre au joueur de se placer en orbite ou d'en sortir manuellement.
- **Assigned role** : developer
- **Dependencies** : Step 2
- **Parallelizable** : No

### Step 4: Mise à jour du SolarSystemManager et de la sélection de repère
- **Description** : Dans `SolarSystemManager.cs`, intégrer `GetDominantCelestialBody(Vector3 position)` et notifier les changements de repère. Permettre un passage naturel de l'orbite terrestre à l'orbite lunaire lors du trajet Terre-Lune.
- **Assigned role** : developer
- **Dependencies** : Step 2, Step 3
- **Parallelizable** : Yes

### Step 5: Amélioration du HUD et affichage de la télémétrie orbitale
- **Description** : Mettre à jour `SolarSystemUI.cs` pour afficher le nom du corps de référence actuel, la vitesse relative, l'indication d'insertion orbitale `[O]`, et s'assurer que le bouton d'atterrissage sur la Lune s'active de manière fiable dès que l'orbite lunaire est stabilisée.
- **Assigned role** : developer
- **Dependencies** : Step 3, Step 4
- **Parallelizable** : Yes

---

# Verification & Testing

### 1. Test du vol stationnaire en orbite (Référentiel Relatif)
- Lancer la scène `SolarSystemScene`.
- Placer le vaisseau en vol libre (`FreeFlight`) près de la Terre ou de la Lune.
- Relâcher toutes les commandes : vérifier que le vaisseau accompagne fidèlement le corps céleste sans être laissé en arrière par la course orbitale de la planète.
- Piloter avec `[W/A/S/D]` : vérifier que le déplacement s'effectue naturellement autour de l'astre.

### 2. Test du voyage et de la capture orbitale (Terre vers Lune)
- Cibler la "Lune" dans le système solaire.
- Enclencher le Pilote Automatique avec `[T]` (ou voler manuellement vers la Lune).
- Vérifier que le vaisseau accélère, s'oriente vers le point d'interception, et ralentit à l'approche de la Lune.
- Constater que le vaisseau entre automatiquement dans la SOI de la Lune, passe en `FlightMode.OrbitInspect`, et tourne harmonieusement autour de la Lune.

### 3. Test de l'atterrissage sur la Lune
- Une fois en orbite lunaire (soit après le transit automatique/manuel, soit dès le départ), vérifier que le bouton `[🛬 ATTERRIR SUR LA LUNE (LANDER) [L]]` dans le panneau UI s'affiche en vert/actif.
- Presser `[L]` ou cliquer sur le bouton et confirmer la transition vers la scène `MoonScene`.

### 4. Test aux échelles de temps accélérées (TimeScale x2, x5, x10)
- Accélérer le temps via l'interface du système solaire.
- Vérifier que le repère relatif compense exactement le multiplicateur de vitesse et que l'orbite reste stable sans dérive numérique.
