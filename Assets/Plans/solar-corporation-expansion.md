# Project Overview
- **Game Title**: Solar Prospector: Corporation
- **High-Level Concept**: Dans un futur proche, le joueur incarne le directeur des opérations d'une corporation spatiale pionnière. L'objectif est d'exploiter méthodiquement les ressources de chaque corps céleste du système solaire (de la Terre/Lune jusqu'aux confins de la ceinture de Kuiper), de développer un arbre technologique complet, et de concevoir des vaisseaux, stations orbitales et bases planétaires modulaires jusqu'à l'automatisation intégrale de chaque secteur planétaire.
- **Players**: Solo (Single player).
- **Inspiration / Reference Games**: *Satisfactory*, *Kerbal Space Program*, *No Man's Sky*, *The Planet Crafter*, *X4: Foundations*.
- **Tone / Art Direction**: Hard Sci-Fi / Futur proche semi-réaliste (URP Lit), esthétique industrielle corporate (composants modulaires métalliques, tuyauteries, sas pressurisés, éclairages volumétriques URP).
- **Target Platform**: PC (Standalone Windows 64-bit).
- **Screen Orientation / Resolution**: Paysage 1920x1080 (16:9) / 4K support.
- **Render Pipeline**: Universal Render Pipeline (URP - PC_RPAsset).

---

# Game Mechanics

## Core Gameplay Loop
1. **Prospection & Transit Orbital** :
   - Sélection d'une planète cible depuis le système solaire simulé.
   - Voyage en vaisseau spatial (vol libre, pilote automatique ou propulsion de distorsion).
   - Déploiement d'une Station Spatiale Orbitale autour du corps céleste pour coordonner la logistique et stocker les ressources en orbite.

2. **Atterrissage & Déploiement Initial (Lander / 3D Printer)** :
   - Descente à la surface planétaire en module d'atterrissage (*Lander*).
   - Vue à la 1ère / 3ème personne : le joueur explore le terrain, scanne les gisements (métaux, silicates, glaces/eau, gaz rares, hélium-3).
   - Utilisation de l'outil/imprimante 3D du Lander pour construire les premiers modules de la base au sol (générateur d'énergie, extracteur manuel, sas, conteneurs, module d'habitation).

3. **Construction Modulaire (Bases Planétaires, Stations, Vaisseaux)** :
   - Système de grille et points d'ancrage (*Snap Nodes*) pour emboîter des pièces modulaires (murs, couloirs, plateformes d'atterrissage, réservoirs, ponts de commande, moteurs, boucliers thermiques).
   - Transition fluide joueur : déplacement à pied (FPS/TPS) à l'intérieur des bases planétaires, dans les coursives des stations spatiales en apesanteur/gravité artificielle, et dans le cockpit/coursives des gros vaisseaux.

4. **Automatisation & Logistique Planète-Orbite** :
   - Déploiement de tapis roulants/pipelines de convoyage, foreuses autonomes, raffineries et usines d'assemblage.
   - Construction d'un élévateur spatial ou de navettes cargo automatisées (*Space Freighters*) transférant les minerais raffinés de la base planétaire vers la station spatiale orbitale.
   - **Critère de maîtrise du monde** : Atteindre 100% d'automatisation des flux de ressources sur le corps céleste pour débloquer les technologies de palier supérieur et autoriser l'expédition vers la planète suivante.

5. **Recherche Technologique & Améliorations Corporatives** :
   - Dépense des ressources collectées pour débloquer de nouveaux modules Blender, de nouveaux châssis de vaisseaux, des moteurs à propulsion avancée (chimique -> ionique -> fusion -> antimatière) et des modules de raffinement ultra-rapides.

## Controls and Input Methods
- **Nouvel Input System Unity** (`UnityEngine.InputSystem`) unifié avec cartes d'actions contextuelles :
  - **Vue Vaisseau (Flight Controls)** :
    - [Z/Q/S/D] ou [W/A/S/D] : Poussée / translation (Pitch, Yaw, Roll via [Q/E]).
    - [Shift] : Boost / Postcombustion.
    - [T] : Pilote automatique vers la cible orbitale.
    - [J] : Saut Warp / Propulsion interplanétaire.
    - [F] : Verrouillage / Inspection orbitale.
    - [Clic Droit + Déplacement Souris] : Orientation du regard / ciblage.
  - **Vue Personnage au sol / en intérieur (FPS / TPS)** :
    - [Z/Q/S/D] : Déplacement au sol / nage 6-DOF en microgravité.
    - [Souris] : Regard et visée.
    - [Espace] : Saut / Jetpack de combinaison spatiale.
    - [C] : S'accroupir / Descente jetpack.
    - [E] : Interagir avec les machines, portes de sas, consoles de commande.
    - [V] : Basculer entre Vue 1ère personne et Vue 3ème personne.
    - [Tab] : Ouvrir l'inventaire corporatif et la grille de fabrication.
    - [B] : Ouvrir le menu de Construction Modulaire (Hologrammes de placement, rotation avec [R], validation [Clic Gauche], annulation [Clic Droit]).

---

# UI

### 1. HUD Vaisseau & Navigation Interplanétaire
- Réticule de visée dynamique avec calcul de vecteur vitesse (Prograde / Retrograde).
- Panneau latéral gauche escamotable : Carte du système solaire et sélection des corps célestes.
- Panneau latéral droit : Télémétrie de la cible (distance en km/UA, gravité de surface, atmosphère, ressources détectées).
- Jauge d'énergie, carburant, intégrité de coque et poussée vectorielle.

### 2. HUD Personnage (FPS / TPS) & Intérieur
- Jauge d'oxygène (O2), énergie de combinaison/jetpack, intégrité de combinaison.
- Boussole / Mini-radar avec icônes de gisements, du Lander et des balises de balisage.
- Viseur interactif affichant le nom de l'objet ciblé (ex: *"Sas sas pressurisé - [E] Ouvrir"*, *"Gisement de Fer - [Clic] Extraire"*).

### 3. Menu de Construction Modulaire (Build Mode)
- Barre d'outils circulaire ou barre d'onglets (Structures, Énergie, Extraction, Logistique, Habitats, Vaisseaux).
- Prévisualisation holographique (vert = valide / rouge = collision ou ressource manquante) avec système de magnétisme (*Snap Point*).
- Affichage des coûts en ressources en temps réel et prérequis d'énergie électrique.

### 4. Panneau de Gestion Corporative & Arbre Technologique
- Écran de monitoring de la production planétaire (% d'automatisation, débit/min de fer, titane, hélium-3).
- Arbre de recherche arborescent découpé en Tiers (Tier 1: Terrestre/Lunaire, Tier 2: Martien/Astéroïdes, Tier 3: Jovien, Tier 4: Confins Solaires).

```
+-----------------------------------------------------------------------------------+
| [CORP LOGO]  SOLAR PROSPECTOR - RESSOURCES TOTALES : Fe: 1,420 | Ti: 850 | He3: 40|
+-----------------------------------------------------------------------------------+
|  [ARBRE R&D]   |             ZONE DE VISUALISATION CENTRALE                       |
| - Propulsion 2 |   (Vue 1ère/3ème personne OU Vue spatiale orbitale)              |
| - Raffinerie 3 |                                                                  |
| - Lander Mk II |   [Hologramme Pièce Modulaire]  <-- Snap Node Vert               |
+----------------+------------------------------------------------------------------+
| STATUT BASE: Énergie: 120/150 kW | Automatisation: 87% [████████░░]              |
| [B] Mode Construction | [Tab] Inventaire | [V] Vue 1P/3P | [E] Interagir          |
+-----------------------------------------------------------------------------------+
```

---

# Key Asset & Context

### Scripts Existants & Réutilisables :
- `Assets/Scripts/CelestialBody.cs` : Gestion orbitale, rotation et rayon des planètes/lunes.
- `Assets/Scripts/SolarSystemManager.cs` : Gestion globale de la simulation, sélection de cible et échelle de temps.
- `Assets/Scripts/SpaceshipFlightController.cs` : Contrôleur de vol spatial (vol libre, pilote auto, warp).

### Nouveaux Scripts & Architecture à créer :
1. **Système de Ressources & Inventaire** :
   - `ResourceItem.cs` (ScriptableObject) : Définition des types de minerais, lingots, pièces composées.
   - `InventorySystem.cs` : Gestion de stock pour joueur, coffres, modules de base, soutes de vaisseau.
2. **Contrôleur Joueur à Pied (1P/3P & Gravité)** :
   - `PlayerCharacterController.cs` : Déplacement terrestre, mode gravité zéro/intérieur de station, bascule fluide 1ère/3ème personne avec Cinemachine/CameraRig.
3. **Système de Construction Modulaire (Blender Snap System)** :
   - `ModularSnapPoint.cs` : Points d'ancrage avec types de connecteurs compatibles (porte, couloir, mur, tuyau).
   - `ModularBuildingManager.cs` : Placement des hologrammes, calcul de collision, déduction des ressources et instanciation réseau/locale.
   - `ModularPieceData.cs` (ScriptableObject) : Propriétés du module (mesh Blender, coût, consommation électrique, slots de fixation).
4. **Machines d'Extraction & Automatisation** :
   - `ResourceDeposit.cs` : Gisements de ressources disséminés sur le terrain.
   - `ExtractorMachine.cs` : Foreuse automatique extrayant des minerais au fil du temps.
   - `ConveyorBelt.cs` / `LogisticsNode.cs` : Acheminement physique ou logique des ressources.
   - `OrbitalCargoLauncher.cs` : Navette ou canon électromagnétique transférant les ressources du sol vers la station spatiale.
5. **Gestionnaire de Corporation & Progression Planétaire** :
   - `CorporationTechTree.cs` : Nœuds technologiques déblocables avec ressources.
   - `PlanetAutomationTracker.cs` : Calcul du taux d'automatisation de la planète active pour valider la transition vers la prochaine étape.
6. **Structure des Pièces Modulaires Blender** :
   - Dossier `Assets/Art/Models/Modular/` : Pièces normalisées (bases 2x2m, 4x4m, sas 1x2m) pour bases au sol, stations et vaisseaux.

---

# Implementation Steps

### Étape 1 : Fondations des Ressources et Données Corporatives
- **Description** : Créer le framework de ressources sous forme de ScriptableObjects (`ResourceItem`, `RecipeData`), ainsi que la classe générique d'inventaire `InventorySystem` et le système d'économie de la corporation (`CorporationManager`).
- **Assigned role** : `developer`
- **Dependencies** : None
- **Parallelizable** : Yes

### Étape 2 : Contrôleur Joueur Hybride 1ère / 3ème Personne
- **Description** : Implémenter le contrôleur de personnage au sol `PlayerCharacterController` avec support du nouvel Input System, bascule de caméra 1P/3P (zoom molette ou touche V), système d'interaction [E] et gestion de l'oxygène/énergie.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1
- **Parallelizable** : No

### Étape 3 : Système de Construction Modulaire (Snap & Build System)
- **Description** : Développer le système de construction `ModularBuildingManager` et `ModularSnapPoint`. Permettre la sélection de pièces (générateurs, extracteurs, couloirs, plateformes d'atterrissage), la visualisation de l'hologramme vert/rouge, l'aimantation sur les points d'ancrage et la construction via l'imprimante 3D du Lander.
- **Assigned role** : `developer`
- **Dependencies** : Étape 2
- **Parallelizable** : No

### Étape 4 : Extraction Planétaire, Raffinage et Automatisation des Flux
- **Description** : Créer les composants de gameplay `ResourceDeposit`, `ExtractorMachine`, `SmelterMachine` et `ConveyorBelt/Pipe`. Implémenter le calcul de rendement énergétique et le transfert automatique d'inventaire machine-à-machine.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1, Étape 3
- **Parallelizable** : Yes

### Étape 5 : Station Spatiale Orbitale & Logistique Sol-Espace
- **Description** : Implémenter la station spatiale orbitale modulaire autour de chaque planète avec zone de stockage en microgravité, et créer la navette cargo automatisée `OrbitalCargoShuttle` reliant la base terrestre à la station.
- **Assigned role** : `developer`
- **Dependencies** : Étape 3, Étape 4
- **Parallelizable** : No

### Étape 6 : Construction Modulaire de Vaisseaux Spatiaux
- **Description** : Étendre le système modulaire à l'assemblage de vaisseaux spatiaux personnalisés (cockpit, coque, propulseurs, soutes). Intégrer les caractéristiques physiques (masse, poussée, consommation) avec `SpaceshipFlightController`.
- **Assigned role** : `developer`
- **Dependencies** : Étape 3, Étape 5
- **Parallelizable** : Yes

### Étape 7 : Arbre Technologique et Progression Planétaire (Mastery Loop)
- **Description** : Implémenter le `CorporationTechTree` et le système de validation d'automatisation planétaire `PlanetAutomationTracker`. Définir les conditions de victoire/départ vers une nouvelle planète du système solaire avec bilan financier et technologique.
- **Assigned role** : `developer`
- **Dependencies** : Étape 4, Étape 5, Étape 6
- **Parallelizable** : No

### Étape 8 : Interface Utilisateur Moderne (UI Toolkit / Canvas UGUI)
- **Description** : Remplacer l'UI de diagnostic actuelle par des interfaces graphiques soignées : HUD Joueur (1P/3P), HUD Vaisseau, Menu radial de construction 3D, Panneau de l'arbre technologique et télémétrie des usines automatisées.
- **Assigned role** : `developer`
- **Dependencies** : Étape 1 à 7
- **Parallelizable** : No

---

# Verification & Testing

1. **Test d'Interaction Joueur (1P / 3P)** :
   - Vérifier la fluidité des déplacements sur le terrain planétaire.
   - Tester l'interaction avec le Lander et la bascule entre vue première personne et troisième personne sans à-coup de caméra.
2. **Test du Système de Construction Modulaire** :
   - Vérifier le magnétisme précis des points de fixation (*Snap Points*) des modules exportés depuis Blender ou créés en placeholders.
   - Tester le refus de pose si les ressources corporatives sont insuffisantes ou en cas d'obstruction physique.
3. **Test de la Boucle d'Automatisation** :
   - Poser une foreuse sur un gisement de titane, la relier à un générateur d'énergie et à un coffre de stockage.
   - Vérifier que les ressources s'accumulent sans intervention du joueur et sont transférées vers la station spatiale par la navette cargo.
4. **Test de l'Arbre Technologique & Vaisseaux Modulaires** :
   - Dépenser les ressources automatisées dans l'arbre R&D pour débloquer un propulseur plus performant.
   - Assembler un vaisseau personnalisé, embarquer à bord et décoller en orbite vers une autre lune/planète du système solaire.
5. **Test de Performance URP & Scalabilité** :
   - Tester la scène avec plusieurs dizaines de modules et de machines en fonctionnement pour s'assurer d'un framerate stable (>60 FPS).
