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

---

# Extension ciblée — Batterie transportable du crusher

## Project Overview
- **Game Title** : Solar Prospector: Corporation.
- **High-Level Concept** : Compléter la boucle industrielle lunaire avec une batterie imprimable, transportable dans l’inventaire et installable dans le crusher. Chaque batterie conserve sa propre charge lors des déplacements, installations et retraits.
- **Players** : Solo.
- **Inspiration / Reference Games** : Satisfactory, The Planet Crafter ; logistique et gestion lisible de l’énergie.
- **Tone / Art Direction** : Industriel spatial semi-réaliste ; modèle et icône existants.
- **Target Platform** : Windows 64-bit, Unity 6000.6.3f1.
- **Screen Orientation / Resolution** : Paysage, résolution native existante ; réglages inchangés (secours actuels 1024×768).
- **Render Pipeline** : URP, PC_RPAsset.
- **Décisions utilisateur** : emplacement dédié avec [E], décharge selon activité du crusher et du Rig, conservation pendant la session seulement.
- **Hors périmètre** : recharge, sauvegarde inventaire/monde entre sessions, nouveau panneau, réseau électrique généralisé, ramassage physique de la batterie décorative dans MoonScene, nouveaux coûts de recette.

## Game Mechanics
### Core Gameplay Loop
1. Imprimer une batterie au Lander : le système actuel donne directement l’objet dans l’inventaire ou la barre rapide. Une batterie neuve est pleine et non empilable.
2. Transporter la batterie jusqu’au crusher et viser son logement ; [E] transfère la même instance vers le logement et affiche le modèle installé.
3. Le crusher ne fonctionne qu’avec une batterie chargée installée. Sa sortie électrique continue d’alimenter le Rig via les flexibles existants.
4. Décharger seulement pour le travail effectif : broyage possible (gravats et espace poudre), extraction possible (alimentation valide, liaison matériau correcte et destination non pleine). Aucun débit pour un consommateur bloqué ou au repos.
5. [E] sur le logement occupé remet la même batterie dans l’inventaire/barre rapide, avec sa charge exacte. Sans place libre, refuser le retrait sans toucher à la batterie installée.
6. À zéro, interrompre l’alimentation et la production ; la batterie vide reste récupérable. Aucun transfert ne recharge la batterie.

### Controls and Input Methods
- PlayerSettings vérifiés : `activeInputHandler = 1`, New Input System.
- Réutiliser `IPromptInteractable` et l’action existante `Player.Interact` dans `PlayerBaseController`. Ne pas ajouter de bindings ou polling clavier/souris.
- Collider du logement sur un enfant interactable du crusher : raycast vers logement = batterie, corps = récupération poudre, ports = flexibles.
- Installation : préférer le slot hotbar sélectionné s’il contient une batterie valide ; sinon première batterie de l’inventaire, puis de la hotbar. Le prompt montre la charge de celle réellement choisie.
- Respecter les blocages existants : menus, inventaire, fabrication, construction, manipulation de câble. Pas de contrôleur d’équipement concurrent.

## UI
Réutiliser viseur, notifications et textes de slots ; aucune refonte Canvas ni modification de fonts.

```text
Logement vide : [E] Installer Batterie externe — 74 %
Sans batterie disponible : Batterie externe requise
Logement occupé : [E] Retirer Batterie externe — 62 %
Logement avec batterie vide : [E] Retirer Batterie externe — 0 %
Corps : [E] Récupérer la poudre (12/100) — Broyage — Batterie 62 %
États alternatifs : Sans batterie / Batterie vide / Stock plein / En attente
Slot inventaire ou hotbar : [icône batterie] 62 %
Refus : Inventaire et barre rapide pleins — batterie laissée en place
```
Le modèle installé reste purement visuel et ne doit pas masquer le collider du logement.

## Key Asset & Context
### État vérifié
- `Assets/Items/BaterieExterneItem.asset` : Item id 2603, maxStack 1, modèle et icône assignés. Conserver chemin, GUID et id.
- `Assets/Recipes/Recipe_BatterieExterne.asset` : outputAmount 1, ingrédients actuellement vides ; ne pas inventer de coût.
- `Assets/Prefabs/Base/SousSystem/BatterieExternePrefab.prefab` : Transform/MeshFilter/MeshRenderer seulement. Instance visuelle `BatterieExterne` dans MoonScene ; aucune logique de ramassage actuellement.
- `Assets/Prefabs/Base/SousSystem/CrusherPrefab.prefab` : CrusherUnit et MeshCollider ; PowerPort et RubblePort assignés ; énergie de test activée, aucun logement batterie. Aucun CrusherUnit trouvé dans la scène chargée : tester les crushers construits depuis le prefab.
- `Assets/Scripts/BuildingSystem/Industry/CrusherUnit.cs` : `HasEnergy`, `AdvanceProduction(float)`, `RefreshPower()`, `CollectPowder(Inventory)` ; énergie illimitée temporaire et booléen batterie sans capacité.
- `Assets/Scripts/BuildingSystem/Industry/RigUnit.cs` : `AdvanceProduction(float)`, `IsPowered` ; extrait vers le crusher lié par flexible matériau, sans débit énergétique actuel.
- `Assets/Scripts/BuildingSystem/Cables/ElectricConnectorSocket.cs` : `SetPowered(bool)` propage déjà l’état à l’entrée raccordée.
- `InventorySlot.cs`, `Inventory.cs`, `Hotbar.cs`, `DragContext.cs`, `InventorySlotUI.cs` sous `Assets/Scripts/Inventory/` : item/count et oxygène suivent les transferts ; aucun état batterie.
- `Assets/Scripts/Crafting/CraftingUIManager.cs` et `Assets/Scripts/Managers/GameManager.cs` : ajouts potentiellement partiels suivis de fallbacks ; fabrication retire les ingrédients avant de vérifier l’espace. Sécuriser le chemin batterie sans refonte générale des autres recettes.
- `SaveGameData` ne sauvegarde ni inventaire physique ni machines : la conservation choisie n’inclut pas relance ou destruction des objets lors d’un changement de scène.
- Impact constaté : batterie prefab → Item → recette ; crusher prefab → CrusherBlueprintItem et `Assets/construction/Crusher.asset` ; MoonScene référence le prefab batterie.

### Architecture et alternatives
**Retenu** : définition batterie optionnelle référencée par l’Item existant + état d’instance sérialisable déplacé entre slot et logement. Préserve GUID et ressources, sépare paramètres partagés et charge individuelle, prépare la recharge future.
**Alternative** : réutiliser EquipmentSocket/EquipmentPlacementController ; ghosts disponibles, mais sélection hotbar obligatoire, consommation générique perdant l’état et concurrence des clics/[E]. Plus invasif, non retenu.
Ne jamais stocker la charge sur le ScriptableObject partagé ni détourner `currentOxygen`.

### Fichiers prévus
- Créer `Assets/Scripts/Inventory/BatteryDefinition.cs`, `BatteryInstanceState.cs`, `BatteryInventoryTransfer.cs` et une définition sous `Assets/Items/`.
- Modifier `Assets/Scripts/Inventory/Item.cs` : référence optionnelle de définition. `InventorySlot.cs` : état batterie individuel, helpers de nettoyage/transfert.
- Modifier `Inventory.cs`, `Hotbar.cs`, `DragContext.cs`, `InventorySlotUI.cs` : initialisation, transferts complets, annulation, swaps et pourcentage.
- Créer `Assets/Scripts/BuildingSystem/Industry/CrusherBatterySocket.cs` : installation/retrait, état installé, ancrage visuel, interaction et consommation bornée.
- Modifier `CrusherUnit.cs`, `RigUnit.cs`, `CrusherPrefab.prefab`, `BaterieExterneItem.asset` et les descriptions de `Recipe_BatterieExterne.asset`.
- Modifier le chemin batterie de `CraftingUIManager.cs` et, si nécessaire, les helpers batterie de `GameManager.cs` pour opérations atomiques.
- Aucun changement prévu au mesh/prefab visuel batterie, à MoonScene, aux packages ou aux sauvegardes.
- Tests ciblés dans l’organisation de tests existante à vérifier avant création ; ne pas déplacer toute la production dans une assembly uniquement pour les tests.

### Équilibrage provisoire
« 500 kW » décrit une puissance, pas une capacité ; ne pas inventer une conversion en 500 kWh. Proposer des unités de charge éditables : capacité 100, charge initiale 100, crusher 0,1 unité/seconde de travail, Rig 0,1 unité/seconde de travail. Autonomie indicative 16 min 40 s pour un consommateur, 8 min 20 s pour deux actifs. Mettre les descriptions en cohérence sans inventer de capacité physique. Valeurs provisoires ajustables, recharge reportée.

## Implementation Steps
1. **Actualiser l’analyse avant édition**.
   - Vérifier dépendances/dépendants, appels aux hooks d’énergie temporaire, tous les transferts de slots, helpers de fabrication, sélection hotbar et gardes d’interaction.
   - Vérifier l’organisation des tests et tout chemin de démontage : si un crusher peut être démonté, exiger retrait préalable de sa batterie ou retour atomique avant destruction.
   - **Dependencies** : approbation de cette extension.
2. **Données individuelles et support inventaire**.
   - Créer définition et état sérialisable : identifiant unique, capacité, charge, pourcentage, consommation bornée. Item porte uniquement la définition partagée ; slot porte l’état individuel.
   - Ajouter les helpers de transfert/nettoyage ; batterie maxStack 1. Ajouter une batterie neuve crée son état une seule fois ; transférer une batterie existante ne recrée jamais sa charge. Quantités multiples = états distincts.
   - Modifier Item/InventorySlot/Inventory/Hotbar et configurer l’asset existant sans changer son identité.
   - **Dependencies** : étape 1.
3. **Transferts et impression atomiques**.
   - BatteryInventoryTransfer centralise recherche, prévalidation et commit ; opérations équivalentes à `TryInstallFromPlayer`, `TryReturnToPlayer`, `TryCreateBatteries`. Aucun ajout partiel suivi d’une réinsertion du lot complet.
   - Adapter MoveOrSwap, DragContext, tous les chemins InventorySlotUI : déplacement, swap, retour, drag annulé, slot O2 refusé. Deux batteries du même Item ne fusionnent pas ; échange possible en conservant chacun des états.
   - CraftSelected utilise le chemin batterie dédié : capacité totale et ressources vérifiées avant retrait ; produire exactement la quantité demandée sans fallback duplicateur. Les autres recettes gardent leur comportement.
   - **Dependencies** : étape 2.
4. **Logement du crusher**.
   - CrusherBatterySocket conserve la même instance transférée ; TryInstall/TryRemove avec succès/échec, vérification de place avant retour. Gardes contextuelles, prompts et notifications.
   - Ajouter BatterySocket et BatteryMount au prefab crusher ; placer le collider et le modèle selon géométrie réelle sans déplacer les ports. Référencer crusher, Item et ancrage.
   - Le modèle installé ne contient aucune logique d’interaction/énergie ; pas d’EquipmentSocket sur le prefab batterie. Retrait détruit uniquement le visuel après transfert réussi.
   - **Dependencies** : étapes 2–3.
5. **Énergie réelle et débit d’activité**.
   - HasEnergy dépend exclusivement d’un logement valide chargé et d’une machine active. Retirer/neutraliser hooks temporaires après analyse des appelants ; supprimer l’alimentation gratuite en gameplay.
   - Crusher.AdvanceProduction progresse seulement pour le temps effectivement payé ; si charge insuffisante, ne travailler que pendant le temps couvert, puis couper. Préserver stocks et progrès partiel à la panne ou au retrait.
   - Rig.AdvanceProduction débite la batterie du crusher réellement lié à son entrée électrique, pas nécessairement sa destination matériau ; vérifier connexions et capacité de réception avant consommation.
   - Chaque consommateur débite son propre travail une seule fois ; fournisseur commun bornant les demandes. Éviter double consommation Rig côté crusher, charge négative et travail impayé selon l’ordre des Update.
   - Couper immédiatement powerOutput à zéro, au retrait ou à la désactivation ; restaurer après installation chargée/réactivation sans réinitialiser l’état.
   - Aucun débit pour un consommateur au repos, bloqué ou mal raccordé ; couvrir précisément les fins d’activité dans une grosse deltaTime.
   - **Dependencies** : étape 4.
6. **Affichage, configuration et régressions**.
   - Pourcentage sur les textes de slots existants, prompts séparés logement/corps/ports ; rafraîchir les UI référencées lors des transferts, pas de recherche globale chaque frame.
   - Paramètres provisoires éditables et descriptions cohérentes ; préserver ingredients, id, GUID, icône et coûts.
   - Vérifier les crushers construits, ghosts sans logique active, modèle hotbar purement visuel ; ne pas rendre la batterie décorative automatiquement récupérable.
   - **Dependencies** : étapes 3–5.
7. **Tests automatisés ciblés puis parcours MoonScene**.
   - Exécuter la boucle impression → transport → installation → production Rig/crusher → retrait → réinstallation ; inspecter Console et compilation.
   - **Dependencies** : étapes 1–6.

## Verification & Testing
- Deux batteries : identifiants distincts, charges différentes indépendantes ; ScriptableObject partagé jamais modifié.
- Batterie partiellement déchargée : déplacement inventaire/hotbar, swap de deux batteries identiques, drag annulé, refus de slot O2, retrait/réinstallation conservent charge/capacité/id exacts ; aucun état résiduel dans slot vide.
- Impression de plusieurs batteries avec place répartie inventaire/hotbar, ou espace insuffisant : quantité exacte, aucune duplication/perte, ingrédients non consommés sur refus.
- Logement vide/occupé, aucune batterie, batterie vide, inventory null et spam [E] : propriétaire unique, actions sûres. Retrait avec inventaire plein mais hotbar libre réussit ; si les deux sont pleins, état installé inchangé.
- Sans batterie ou à zéro : crusher et Rig alimenté arrêtés ; retirer coupe immédiatement sans effacer stocks/progrès. Batterie vide récupérable, aucune recharge implicite.
- Attente gravats, stock poudre plein, Rig débranché, liaison matériau incorrecte, destination pleine : pas de débit du consommateur bloqué. L’autre consommateur peut travailler normalement.
- deltaTime <= 0, grosse deltaTime, charge inférieure au coût d’une frame, deux consommateurs et ordre Update inversé : aucune charge négative, aucune production impayée, coupure immédiate.
- Rig alimenté par crusher A et livrant à B : débiter A ; broyage de B dépend de sa propre batterie. Flexibles et feedback restent fonctionnels.
- Raycast logement/corps/ports : actions distinctes, collider accessible ; menus/construction/câble bloquent correctement. Modèle tenu et installé sans logique concurrente.
- Désactivation/réactivation conserve la charge et coupe/restaure l’énergie. Relance et changement de scène détruisant les objets explicitement non garantis dans cette phase.
- Régression ressources ordinaires, oxygène, drag/swaps, recettes, construction, branchement/débranchement ; aucune nouvelle erreur Console. Compilation Editor et validation Windows si environnement disponible.

---

# Extension ciblée — Importation unifiée des items et modules

## Project Overview
- **Game Title** : Solar Prospector: Corporation (productName actuel : Prospection system solaire).
- **High-Level Concept** : Unifier la création et l’édition des contenus industriels dans un seul outil Editor : saisir les informations communes une fois, sélectionner un prefab existant, puis générer les ScriptableObjects et leurs liens selon les options. Fusionner complètement BlueprintItem dans Item, sans perdre les références des contenus déjà utilisés.
- **Players** : Solo ; outil de production utilisé par le développeur.
- **Inspiration / Reference Games** : Satisfactory, The Planet Crafter ; catalogue industriel cohérent, préparation du contenu simplifiée.
- **Tone / Art Direction** : Industriel spatial semi-réaliste ; conserver modèles, icônes et matériaux existants.
- **Target Platform** : Windows 64-bit ; Editor Unity 6000.6.3f1.
- **Screen Orientation / Resolution** : Paysage, résolution native ; secours PlayerSettings actuel 1024×768, aucune modification.
- **Render Pipeline** : URP, PC_RPAsset ; inchangé.
- **Choix confirmés** : fusion complète maintenant ; importation depuis des prefabs existants.
- **Hors périmètre** : import CSV/par lots, fabrication de modèles FBX, génération graphique d’icônes, ajout automatique des comportements propres aux machines, fusion OxygenBottleItem, nouvel inventaire, équilibrage des recettes ou changement des contrôles.

## Game Mechanics
### Core Gameplay Loop
- La boucle existante reste impression → inventaire/hotbar → construction → exploitation des machines.
- Le chantier porte sur la boucle de production du contenu : ouvrir l’outil → choisir/créer un Item → renseigner informations communes → activer Constructible et/ou Fabricable → sélectionner prefab et paramètres → analyser → créer ou mettre à jour.
- La réduction de saisies et les validations évitent les divergences de présentation et les erreurs d’identité. Dans une lecture MDA, l’outil fiabilise les données des mécaniques sans changer les dynamiques ni l’équilibrage du jeu.
- Coûts d’impression et coûts de construction restent distincts : les unifier pourrait involontairement modifier l’économie ou facturer deux fois.

### Controls and Input Methods
- PlayerSettings vérifié : activeInputHandler=1, New Input System. Aucun binding ou contrôleur ajouté.
- Outil Editor à la souris/clavier ; suivre la convention Editor IMGUI existante (OnInspectorGUI dans EnvironmentalResourceManagerEditor). Ne pas refaire les Canvas runtime.
- Remplacer les tests de type BlueprintItem par une condition sémantique Item.IsConstructible et une référence Item.moduleData valide.

## UI
Une fenêtre Editor unique « Contenu — Items et modules », avec sections conditionnelles et mode Créer/Modifier. Inspecteurs permettant d’ouvrir cette fenêtre ; les menus de création séparés BlueprintItem sont retirés après migration.

```text
[Créer] [Modifier un item existant : …]        [Audit / Migration]
Informations communes
  Nom             [                         ]
  Description     [                         ]
  Icône           [Sprite                   ]
  ID              [automatique, lecture seule]
  Stack / Prix / Couleur [                   ]
  Modèle inventaire [prefab optionnel        ]

[ ] Constructible
  Prefab final    [                         ]
  Ghost           [existant] [Générer une copie sûre]
  Terrain seul    [ ]    Temps [            ]
  Coûts de construction [liste Item/quantité]

[ ] Fabricable
  Catégorie       [                         ]
  Quantité sortie [                         ]
  Ingrédients     [liste Item/quantité       ]
  [ ] Informations de recette spécifiques (facultatif)
  Destination    [station(s) / liste globale explicite]

Emplacements : Assets/Items | Assets/construction | Assets/Recipes
[Analyser] → nouveaux assets / mises à jour / conflits / avertissements
[Créer les assets liés] ou [Enregistrer]     [Annuler]
```
- Un seul nom, description et icône pour l’objet. Les modules lisent ces informations depuis leur Item ; pas de synchronisation de copies après chaque modification.
- La recette utilise les helpers existants : nom et description issus de outputItem par défaut. Conserver une surcharge explicite pour les instructions propres à la recette, sans dupliquer silencieusement la description de l’objet.
- Séparer modèle tenu en inventaire et prefab final de construction : ces usages ne sont pas nécessairement interchangeables.
- Choix d’assets existants pour l’édition ; afficher chemin et identité pour éviter de modifier le mauvais contenu. Bouton bloqué en cas de conflit non résolu.
- Désactiver Constructible/Fabricable ne détruit aucun asset lié automatiquement ; avertir des usages existants.

## Key Asset & Context
### État constaté
- Assets/Scripts/Inventory/Item.cs : id int, itemName, description, icon, maxStack, model, basePrice, itemColor, BatteryDefinition. Les champs batterie doivent être préservés.
- Assets/Scripts/BuildingSystem/BlueprintItem.cs : hérite Item, ajoute seulement moduleData ; deuxième menu de création.
- Assets/Scripts/BuildingSystem/BaseModuleData.cs : moduleName, description et icon redondants ; ghostPrefab, finalPrefab, groundPlacementOnly, baseConstructionTime, resourceCosts techniques.
- Assets/Scripts/Crafting/CraftingRecipe.cs : outputItem et helpers GetDisplayName/GetDescription/GetIcon ; la description de recette est déjà une surcharge facultative.
- OxygenBottleItem hérite aussi Item et possède maxOxygenCapacity ; le préserver ainsi que les usages spécialisés.
- 34 Items chargés lors de l’audit, dont des BlueprintItem ; id=1 partagé par CouloirBlueprint Item et SonicFracturer, plusieurs id=0. Certains consommateurs reconnaissent des objets via ids codés en dur (5, 43, 60) : ne pas renuméroter tous les contenus.
- Exemples : Assets/Items/ModuleBatterieBlueprintItem.asset → Assets/construction/ModuleBatterie.asset → prefabs final/ghost ; Assets/Recipes/Recipe_ModuleBatterie.asset produit ce blueprint. MoonScene référence cet ensemble.
- RigItem/RigBlueprintItem et Scrubber Unit/Assets/construction/Scrubber.asset sont des contenus distincts ou usages distincts potentiels : ne pas fusionner leurs assets sur le seul nom.
- Le ghost ModuleGhostBatteriePrefab contient encore ElectricConnectorSocket et FlexibleCableRenderer ; copier un prefab final sans nettoyage pourrait exécuter de la logique en prévisualisation.
- MoonScene : CraftingCanvas globalRecipes=5 ; Lander stationRecipes=11 ; BlueprintInventory availableBlueprints=7 et références panel/button assignées. La découverte AssetDatabase des recettes n’existe qu’en Editor et seulement si la liste globale est vide.
- Impact analysé avec AssetDatabase.GetDependencies et recherche inverse : Item.cs a 54 assets dépendants, BlueprintItem.cs 27, BaseModuleData.cs 38, CraftingRecipe.cs 17 (incluant scènes de récupération). Une migration doit couvrir tout le projet, pas uniquement MoonScene.
- Aucun fichier de tests, asmdef, UXML ou USS trouvé par la recherche ciblée ; vérifier packages de tests avant de choisir le montage de tests.

### Architecture retenue
- Item devient la source des informations communes, possède bool isConstructible + BaseModuleData moduleData ; IsConstructible retourne vrai seulement avec option activée et données valides.
- BaseModuleData conserve les paramètres techniques et référence son Item propriétaire pour afficher nom/description/icône. Les anciens champs sont conservés masqués uniquement pendant la migration, puis supprimés après validation. Valider l’aller-retour Item ↔ module et signaler les modules partagés par plusieurs Items.
- CraftingRecipe demeure un asset séparé pour conserver ses références et permettre plusieurs recettes par sortie. L’outil crée automatiquement une recette seulement sur demande ; si plusieurs existent, demander laquelle éditer plutôt que choisir arbitrairement.
- Garder les assets séparés dans leurs dossiers actuels mais créer/modifier tout depuis une seule fenêtre. « Un seul bloc » signifie un seul workflow, pas un énorme ScriptableObject mélangeant tous les comportements.
- ID int automatique positif pour créations nouvelles, attribué une fois et stable après renommage/déplacement/réimport. Service Editor calculant les IDs occupés de tous les Items, sous-types compris, avant commit. Ne pas utiliser hash du nom, InstanceID, ni OnValidate réattribuant tous les IDs.
- Migration des ID=0 et collisions : rapport par asset, plan de correspondance old/new, vérification des références et usages codés en dur ; confirmation explicite avant réparation. Préserver les IDs existants uniques, notamment batterie 2603. Plusieurs zéros ne signifient pas que les objets sont identiques.
- Alternative non retenue : garder BlueprintItem comme couche de compatibilité permanente, moins risqué mais contraire au choix de fusion complète. Une transition temporaire est autorisée seulement pendant l’exécution de la migration.

### Fichiers à modifier / créer
- Modifier Item.cs, BaseModuleData.cs, CraftingRecipe.cs si nécessaire pour les surcharges explicites.
- Modifier Assets/Scripts/Controllers/PlayerBaseController.cs, Assets/Scripts/BuildingSystem/BuilderController.cs, BlueprintUIManager.cs, Assets/Scripts/UI/PlayerHUDManager.cs et Assets/Scripts/Inventory/HotbarUI.cs ; rechercher ensuite toute autre référence au type.
- BlueprintUIManager garde son composant et son champ availableBlueprints mais devient List<Item>, filtré par IsConstructible, afin de conserver les références sérialisées.
- Créer sous Assets/Scripts/Inventory/Editor/ : ContentAuthoringWindow.cs, ItemEditor.cs, BaseModuleDataEditor.cs, ContentAuthoringService.cs, ItemIdAllocator.cs, ContentMigrationUtility.cs, ContentValidationUtility.cs et GhostPrefabFactory.cs. Namespaces et découpage ajustables aux conventions vérifiées.
- Migrer les assets BlueprintItem réellement trouvés, dans et hors Assets/Items, puis supprimer BlueprintItem.cs et son .meta seulement après vérification complète.
- Préserver chemins, GUID, fileID, coûts, stacks, prix, paramètres batterie/oxygène et références prefabs. Modules et recettes existants ne sont pas recréés par défaut.
- Modifier uniquement les listes de stations/globales choisies et les données réellement migrées des scènes/prefabs dépendants. Ne pas changer le layout runtime ni les modèles/materials sources.

## Implementation Steps
1. **Préflight et sauvegarde de migration** — Dependencies : approbation.
   - Actualiser dépendances/dépendants des scripts et de chaque asset concerné, état de sérialisation, scènes dirty, packages de tests, usages de type et IDs.
   - Exporter un instantané de données et un manifeste GUID/fileID/type/relations ; prévoir sauvegarde récupérable hors dossiers importés et expliquer le retour arrière. Ne pas sauvegarder/écraser une scène dirty sans consentement.
   - Auditer conflits de nom/description/icône entre Item/module/recette, modules orphelins ou partagés, ID=0/collisions et références manquantes. Un conflit réel demande une sélection explicite de valeur canonique ; aucune description différente n’est jetée silencieusement.
2. **Modèle unifié et adaptation des consommateurs** — Dependencies : étape 1.
   - Ajouter les propriétés constructibles à Item et les accès aux informations communes pour les modules.
   - Remplacer tous les tests/casts BlueprintItem dans contrôleur joueur, builder, HUD, hotbar et menu de plans. Préserver sélection, obligation du Welder, coût, retrait de l’item placé et équipement tenu.
   - Remplacer les correspondances approximatives par nom pour identifier un plan avec un lien Item/module validé quand possible ; tester les anciens fallbacks avant retrait.
   - Passer availableBlueprints à List<Item> sans renommer le champ ; conserver le script MonoBehaviour existant.
3. **Migration complète des données** — Dependencies : étapes 1–2.
   - Construire un dry-run déterministe affichant chaque conversion et sa portée. Résoudre conflits et modules sans propriétaire avant commit.
   - Convertir chaque BlueprintItem en Item avec option active et moduleData, en préservant identité de l’asset et champs hérités. Tester d’abord une copie temporaire ; si une conversion sérialisée en place est nécessaire, utiliser un outil ciblé sur le m_Script attendu et la représentation vérifiée, jamais un remplacement textuel global.
   - Si GUID/fileID ne peuvent être préservés, arrêter pour proposer une stratégie de remapping explicite : aucune recréation silencieuse cassant les références.
   - Renseigner propriétaire canonique des modules ; effacer les descriptions de recette seulement lorsqu’elles sont réellement des copies validées. Conserver instructions propres aux recettes.
   - Réimporter, recharger scènes/prefabs et comparer manifeste avant/après. Supprimer ancienne classe et champs transitoires seulement lorsque plus aucun asset/consommateur ne les utilise. Migration relancée = aucun changement supplémentaire.
4. **IDs et validations centralisées** — Dependencies : étape 3.
   - Implémenter allocation stable pour nouveaux Items ; gérer duplications sans reprendre l’ID source via le workflow de création.
   - Fournir audit/réparation explicite pour assets historiques à zéro ou collision, avec correspondance par GUID ; pas de réparation automatique au chargement.
   - Valider liens, sorties de recette, quantités, coûts, destinations, icônes, prefabs persistants et chemins. Les contenus existants incomplets sont signalés sans destruction.
5. **Service de création/mise à jour** — Dependencies : étape 4.
   - Créer Item seul, Item+BaseModuleData si Constructible, et CraftingRecipe si Fabricable ; créer les références croisées une fois, réserver chemins et ID avant écriture.
   - Modifier les assets existants en place ; aucune duplication lors d’un second Enregistrer. Plusieurs recettes/modules candidats = choix explicite.
   - Enregistrer la recette dans les stationRecipes/globalRecipes sélectionnées via références sérialisées, sans chercher AssetDatabase en build ni écraser les listes existantes ; garantir l’inclusion de l’asset dans le build.
   - Undo pour éditions ordinaires ; pour transactions multi-assets, prévalidation et rollback en cas d’échec. Ne pas supprimer une recette ou un module existant lorsque la case est décochée.
6. **Ghosts optionnels sûrs et fenêtre unifiée** — Dependencies : étapes 2 et 5.
   - Privilégier un ghost existant. Pour génération, créer une copie dédiée avec géométrie/transforms, matériau ghost existant, ConstructibleGhost et sockets de construction nécessaires ; aucun changement du prefab final.
   - Garder seulement les composants approuvés pour placement ; supprimer la logique machine/énergie/batterie/câbles, rigidbodies actifs, audio et UI interactives. Composant inconnu = revue bloquante, pas deviner les dépendances fonctionnelles.
   - Préserver les ModuleSocket et colliders triggers nécessaires au snapping ; pas de suppression de tous les colliders sans distinction. Vérifier matériau, couches et retour visuel avec BuilderController.
   - Implémenter sections conditionnelles, aperçu, diagnostics, boutons Créer/Enregistrer, accès depuis inspecteurs et état lecture seule de l’ID. La fenêtre ouvre aussi les anciens contenus migrés.
7. **Validation finale, documentation et retrait des chemins obsolètes** — Dependencies : étapes 3–6.
   - Tester création, édition, migration, réparation d’IDs et usage runtime ; contrôler compilation et Console.
   - Documenter workflow, distinction coûts impression/construction, modèle tenu/final, choix des destinations de recette et limites des ghosts.
   - Confirmer qu’aucun BlueprintItem sérialisé, cast ou menu de création ne reste. Ne pas supprimer d’assets jugés doublons uniquement sur leur nom.

## Verification & Testing
- **Migration** : mêmes GUID/fileID, mêmes références dans recettes, coûts, inventaires initialisés, listes et scènes/prefabs ; aucune Missing Script. Relance Editor et deuxième migration sans modification supplémentaire.
- **Données communes** : modifier nom/description/icône de l’Item actualise inventaire, HUD, menu des plans et recette par défaut. Description de recette spécifique conservée. Aucun champ commun du module éditable en double.
- **Création** : item simple ; constructible seul ; fabricable seul ; constructible+fabricable ; paramètres batterie existants conservés ; OxygenBottleItem non converti/perdu.
- **Identité** : nouveaux IDs positifs uniques, invariants après rename/move ; duplicate détecté ; zéros/collision historiques signalés et réparables avec confirmation, IDs spéciaux inchangés.
- **Idempotence** : deux Enregistrer ne produisent aucun nouvel asset ; deux recettes pour un Item demandent choix ; décocher une option ne détruit aucun asset encore référencé.
- **Échec/annulation** : chemin déjà occupé, prefab invalide, icône manquante, référence circulaire incohérente, module partagé, absence de destination et annulation de migration → pas de demi-création, données sources intactes. Tester restauration depuis sauvegarde de migration.
- **Construction** : anciens plans et nouvel Item constructible fonctionnent hotbar → build → snapping/terrain → construction progressive → retrait item ; Welder toujours requis. Item non constructible ne déclenche pas build.
- **Ghosts** : terrain/snapping, rotation, validité/couleur, géométrie correcte ; aucune production, consommation batterie, câble, audio ou UI runtime exécutée pendant la prévisualisation ; matériau source non modifié.
- **Fabrication** : recette nouvellement générée présente dans la station choisie après redémarrage et dans build Windows ; coûts d’impression et construction inchangés ; stations non choisies intactes.
- **Régressions** : inventaire/hotbar, empilements, oxygen, batterie individuelle, crusher/Rig, équipements et plans existants. Tester MoonScene puis autres scènes dépendantes pertinentes ; traiter les archives de récupération sans écraser de travail non sauvegardé.
- **Tests automatiques** : helpers allocation/validation/idempotence et fixtures migration temporaires avec outils de tests déjà disponibles ; ne pas restructurer toute l’assembly production uniquement pour ajouter NUnit. Si indisponibles, fournir validations Editor reproductibles et checks manuels ; installer un package seulement après autorisation.
- **Acceptation** : compilation Editor et build Windows si environnement disponible, aucune erreur Console introduite par le chantier, audit post-migration sans BlueprintItem ni collisions non résolues acceptées silencieusement.

---

# Extension ciblée — Déconstruction des modules avec la découpeuse

# Project Overview
- **Game Title** : Solar Prospector: Corporation (productName : Prospection system solaire).
- **High-Level Concept** : Permettre de réorganiser une base en découpant ses modules terminés. La déconstruction restitue intégralement les ingrédients de leur recette d’impression ainsi que les ressources réellement dépensées pour leur construction, sans perte de contenu ni duplication.
- **Players** : Solo.
- **Inspiration / Reference Games** : Satisfactory, The Planet Crafter ; construction réversible et logistique industrielle.
- **Tone / Art Direction** : Industriel spatial semi-réaliste ; modèle et icône de découpeuse existants, aucun nouvel asset graphique requis.
- **Target Platform** : Windows 64-bit, Unity 6000.6.3f1.
- **Screen Orientation / Resolution** : Paysage, résolution native ; paramètres actuels inchangés.
- **Render Pipeline** : URP 17.6.0, PC_RPAsset ; inchangé.
- **Décisions confirmées** : recette + construction ; maintenir clic gauche ; bloquer tant que contenus/batterie amovible/équipements sont présents ; déconnecter proprement les câbles à la fin.
- **Périmètre** : modules terminés par le joueur pendant la session, récupération préalable des équipements montés nécessaire au démontage, intégration au contrôleur et feedback existants.
- **Hors périmètre** : démontage de ghosts/chantiers incomplets, sauvegarde persistante des remboursements, démolition en cascade, simulation de soutien structurel, nouvelles recettes ou rééquilibrage, nouveaux modèles/sons/animations, transformation automatique des éléments préplacés en modules payés.

# Game Mechanics
## Core Gameplay Loop
1. Imprimer un module, sélectionner son item constructible puis terminer sa construction au Welder comme actuellement.
2. Enregistrer un reçu de déconstruction propre à l’instance : ingrédients d’impression par module quand une recette explicite existe et que l’item a effectivement été retiré ; ressources de pose réellement consommées. Aucun remboursement de dépenses supposées.
3. Sélectionner **la découpeuse dans le slot actif** de la hotbar, viser un collider du module dans la portée d’interaction existante et maintenir clic gauche.
4. Durée initiale égale au temps de construction du module, bornée à une durée minimale positive ; conserver une option technique de durée spécifique si nécessaire. Relâcher, changer de cible/outil, perdre la portée ou ouvrir une UI annule et remet cette tentative à zéro.
5. Si le module contient des stocks, une batterie amovible, un inventaire non vide ou des équipements montés, afficher la raison et refuser avant toute découpe. Le joueur doit d’abord récupérer/vider ces éléments. Revalider juste avant la restitution.
6. À la fin, vérifier que tout le remboursement tient dans inventaire + hotbar ; restituer le lot complet, déconnecter les liens, libérer les sockets voisins, puis détruire seulement l’instance ciblée. Sans place : aucun matériau ajouté, module intact.
7. L’absence de pénalité favorise l’expérimentation et l’autonomie (SDT). La découpe progressive crée une action intentionnelle et un retour lisible (MDA), contrairement au clic instantané plus risqué. La récupération automatique de tous les contenus n’est pas retenue : elle exigerait des conversions spécifiques à chaque machine.

### Règles de remboursement et d’identité
- Ajouter un lien explicite vers la recette de déconstruction sur BaseModuleData. Préremplir seulement si une recette unique produit exactement son Item ; plusieurs candidates = choix explicite, jamais première trouvée arbitrairement. Les liens sérialisés doivent fonctionner en build sans AssetDatabase.
- Copier les quantités dans le reçu au moment de la construction ; une modification ultérieure de recette/coûts ne change pas le remboursement d’un module déjà construit.
- Recette sans ingrédients = coût d’impression nul. Recette absente = remboursement des seuls coûts de pose enregistrés et diagnostic explicite ; ne pas inventer une recette ou retourner le blueprint en plus de ses ingrédients.
- Recette avec outputAmount > 1 : répartir les ingrédients par unité seulement si les quantités sont divisibles exactement ; sinon bloquer la configuration de remboursement et demander une règle explicite. Pas d’arrondi générant/perdant des ressources.
- Additionner les lignes identiques et fusionner ingrédients d’impression + coûts de pose sans écraser une entrée. Identité canonique par référence Item ; signaler les IDs conflictuels plutôt que fusionner des assets différents au même ID.
- Les coûts de pose doivent provenir des consommations réussies, y compris plusieurs ticks ou une pause par manque de ressources. Ne pas rembourser le coût théorique d’un ghost non payé.
- Un module créé sans retrait d’item (ex. TestBuilder/debug) n’obtient aucun remboursement d’impression. Ne pas changer les accès debug existants sans nécessité ; les marquer explicitement non payés.
- Pas de double restitution de blueprint + recette. État terminal sur l’instance pour empêcher deux validations dans la même frame.
- Préplacés/décor, copies tenues et ghosts : aucune autorisation implicite. Seules les instances avec reçu initialisé par la construction deviennent démontables dans cette phase.

## Controls and Input Methods
- PlayerSettings vérifié : activeInputHandler=1 ; package New Input System 1.20.0.
- Réutiliser Player.Attack avec abonnements started/performed/canceled et nettoyage OnDisable ; état de maintien utilisé par le contrôleur pour progresser. Ne pas ajouter de polling Mouse/Keyboard ni une deuxième action concurrente pour découper.
- Injecter la gestion dans PlayerBaseController, avec service/composant de déconstruction sans contrôleur input indépendant. La découpeuse sélectionnée réserve Attack et ne déclenche pas le minage automatique d’un autre outil.
- Réutiliser cameraTransform et interactRange ; ciblage au premier obstacle, résolution de la racine via un marqueur de module construit, colliders enfants inclus. Pas de recherche par nom de prefab ni de démolition à travers les murs.
- Respecter construction, placement d’équipement, pause, inventaire, plans, hub, fabrication, terminaux électriques/batterie, canMove et CableInteractionManager.InteractionsBlocked/IsCarryingCable.
- Interact reste disponible pour retirer une batterie ou récupérer les produits avant découpe ; aucune démolition via E.

# UI
Réutiliser le viseur/prompt **OnGUI existant dans PlayerBaseController** et les notifications de MainMenuController ; pas de nouveau Canvas, de font ou de refonte HUD.

```text
                     •
 [Clic gauche maintenu] Déconstruire : Couloir
          Découpe : 45 %  [████░░░░░░]
     Restitution : FeO × 5 · MgO × 2 · TiO2 × 1

Blocages : Vider le crusher (gravats/poudre)
           Retirer la batterie externe
           Retirer les équipements montés
           Inventaire et barre rapide : espace insuffisant
Équipement monté : [Clic gauche maintenu] Retirer le Scrubber
Succès : Module déconstruit — matériaux récupérés
```
- Priorité du prompt : construction existante ; découpe de la cible admissible avec outil sélectionné ; minage/interaction existants. Les raisons de refus ne masquent pas inutilement les actions E de récupération.
- Afficher un avertissement lisible pour coût nul ou recette absente ; aucune notification de gain fictif. Liste longue abrégée si nécessaire pour préserver la lisibilité.
- Fermer/effacer progression et cible sur interruption ou UI bloquante ; conserver lisibilité dans les résolutions actuelles.

# Key Asset & Context
## État constaté
- Assets/Items/decoupeuse.asset : Item non constructible, id=5434, modèle Assets/Prefabs/Item/DecoupeusePrefab.prefab ; Recipe_decoupeuse.asset existe. Aucun comportement de déconstruction de module actuellement.
- BuilderController.FinalizeConstruction instancie le prefab final, occupe les ModuleSocket puis appelle RemovePlacedModuleItem ; l’instance finale ne conserve actuellement ni BaseModuleData ni reçu.
- ConstructibleGhost consomme progressivement BaseModuleData.resourceCosts ; consommation par GameManager. Les dictionnaires actuels écrasent les lignes dupliquées : normaliser les coûts pour un reçu fiable.
- BaseModuleData.Item lie l’Item propriétaire ; CraftingRecipe est séparé, outputItem/outputAmount/ingredients. Les coûts d’impression et de pose sont distincts.
- Recettes constructibles trouvées : Recipe_Crusher, Recipe_ModuleBatterie, Recipe_Rig et SolarPanel, chacune outputAmount=1 et **ingredients vides**. Ne pas remplir ces recettes arbitrairement.
- Coûts de pose actuels : Couloir FeO×5, MgO×2, TiO2×1 ; Principal FeO×5, TiO2×1 ; autres modules audités sans coût de pose. Certains modules n’ont aucune recette associée ; MosuleBaterie n’a pas de finalPrefab.
- PlayerBaseController : CheckInteraction raycast caméra, HandleInteractionInput pour E/minage, HandleBuildMode selon sélection hotbar, prompts OnGUI. Plusieurs systèmes font actuellement du polling : la nouvelle voie de découpe reste événementielle et isolée.
- Player MoonScene : caméra et inventaire du Builder correctement assignés ; HotbarUI et Inventory/Hotbar reliés. TestBuilder a des références nulles résolues au Start.
- ModuleSocket ne conserve que isOccupied et hatchBlocker ; pas de référence au voisin. La déconstruction exige un suivi de connexion précis pour restaurer les parois et permettre de reconstruire.
- CableConnectorSocket.Disconnect est disponible ; OnDisable déconnecte aussi. Réutiliser les implémentations électriques/matériau et préserver le câble du propriétaire survivant.
- CrusherUnit expose RubbleStock/PowderStock et BatterySocket.InstalledBattery ; RigUnit n’a qu’un compteur totalExtracted et progression, aucun stock récupérable à confondre avec des ressources.
- EquipmentSocket expose isOccupied/occupiedEquipment. ScrubberUnit.Dismantle existe mais ne vérifie pas que l’ajout réussit avant de détruire ; il faut sécuriser et rendre cette action accessible avant de bloquer un habitat équipé.
- BaseBatteryModule stocke de l’énergie intrinsèque, pas une batterie inventaire amovible. La charge électrique n’est pas un matériau de recette et n’est pas restituée ; indiquer cette perte d’énergie lors du démontage de l’accumulateur, sans créer d’objet batterie fictif.
- MoonScene contient des modules préplacés (vie, sas, panneau solaire, batterie) ; ne pas leur attribuer un reçu sur la seule correspondance prefab.
- Dépendances directes/inverses inspectées via AssetDatabase.GetDependencies : scripts partagés par Player.prefab, Player_BaseCapsule.prefab, MoonScene, EarthBaseScene, SampleScene, test et archives _Recovery. Préserver ces archives et ne pas modifier des scènes dirty sans accord.
- Test Framework 1.8.0 installé ; rechercher le montage de tests existant avant de choisir fixtures Editor/PlayMode.

## Fichiers à créer / modifier
- Créer Assets/Scripts/BuildingSystem/ConstructedModule.cs : identité, reçu immuable, état de déconstruction, accès aux blocages et connexions possédées.
- Créer Assets/Scripts/BuildingSystem/ModuleDeconstructionController.cs : cible, progression et validation ; pas de carte d’input indépendante.
- Créer Assets/Scripts/Inventory/InventoryRefundTransaction.cs : simulation et commit tout-ou-rien d’un lot multi-items sur inventaire + hotbar.
- Modifier BuilderController.cs et ConstructibleGhost.cs : reçu de consommation, initialisation de l’instance terminée, validation du retrait réel de l’item et normalisation des coûts.
- Modifier BaseModuleData.cs : référence de recette explicite et éventuel paramètre de durée ; conserver coûts et noms de champs existants.
- Modifier Assets/Scripts/Controllers/PlayerBaseController.cs : branche Attack événementielle pour découpe et feedback OnGUI ; conserver autres interactions.
- Modifier ModuleSocket.cs : suivi du voisin/libération précise compatible avec SetConnected et hatchBlocker.
- Modifier ScrubberUnit.cs : retrait préalable sûr et accessible ; adapter EquipmentSocket/EquipmentPlacementController seulement si nécessaire pour identifier libération/propriétaire.
- Modifier Assets/Scripts/Inventory/Editor/BaseModuleDataEditor.cs, ContentAuthoringService.cs, ContentAuthoringWindow.cs et ContentValidationUtility.cs si nécessaire pour assigner/valider la recette sans recréer les assets.
- Mettre à jour en place les BaseModuleData avec recette unique prouvée et les références découpeuse du joueur réel dans Assets/Prefabs/player/Player.prefab et MoonScene selon overrides observés. Ajouter le marqueur aux instances runtime à leur finalisation plutôt qu’à tous les prefabs finaux/décors.
- Cables/ElectricConnectorSocket.cs, MaterialConnectorSocket.cs, CableInteractionManager.cs : inspection et tests ; modifications seulement si les Disconnect existants laissent effectivement une référence invalide.
- Fichiers de tests ciblés sous Assets/Tests/Editor et/ou PlayMode selon montage existant ; pas de restructuration globale des assemblies.

# Implementation Steps
1. **Préflight et impact précis** — Dependencies : approbation de cette extension.
   - Actualiser dépendances/dépendants de chaque script et asset avant édition ; inspecter scène dirty, instances/overrides joueur, colliders et listes de recette runtime.
   - Vérifier intégralement APIs Disconnect, blocages UI/câbles, retrait batterie, équipement et consommateurs des ModuleSocket avant modification.
   - Auditer recettes absentes/vides/multiples, coûts invalides, outputAmount et IDs ; rapport sans modifier l’économie. Ne pas sauver automatiquement les archives ou scènes non concernées.
2. **Reçu et configuration de remboursement** — Dependencies : étape 1.
   - Ajouter liens de recettes aux données/outil d’auteur, assigner les quatre candidates uniques actuelles ; coûts vides restent vides.
   - Ajouter modèle de reçu par instance, normalisation des lignes, validation des quantités/identités et restrictions multi-sorties ; snapshot indépendant des ScriptableObjects.
   - Enregistrer chaque consommation de pose réussie dans ConstructibleGhost ; transférer le reçu au module final et ajouter part d’impression seulement si l’Item a été retiré avec succès.
   - Conserver API FinalizeConstruction compatible pour les consommateurs existants ; appels sans provenance payée ne créent pas de remboursement fictif.
3. **Transaction inventaire/hotbar** — Dependencies : étape 2.
   - Simuler l’insertion de tout le lot sur copies des slots : compléter stacks puis slots libres inventaire, puis hotbar ; capacité calculée globalement pour tous les ingrédients.
   - Commit uniquement si tout tient, avec contrôle d’état initial et rollback des slots en cas d’échec ; ne pas enchaîner naïvement Inventory.AddItem dont false peut suivre une insertion partielle.
   - Préserver états batterie/oxygène déjà en slots et les événements UI ; ne pas recharger ni transformer des instances existantes. Recette nécessitant item à état spécial : validation dédiée, jamais insertion ordinaire aveugle.
4. **Préconditions et retrait préalable** — Dependencies : étapes 2–3.
   - Refuser crusher avec gravats/poudre ou batterie amovible installée, hôte avec équipements montés, inventaire possédé non vide ; raison ciblée et contrôles sur la racine du module uniquement.
   - Mettre un hook extensible pour futurs contenus ; ne pas parcourir/détruire un module voisin emboîté comme s’il était contenu.
   - Sécuriser ScrubberUnit.Dismantle via la transaction, libérer hostSocket uniquement après ajout réussi, rafraîchir BasePowerReceiver ; permettre son retrait ciblé avec découpeuse avant celui de l’habitat. Le retrait rend l’équipement existant, pas les ingrédients du module hôte.
   - Conserver récupération poudre/batterie actuelle ; si un contenu audité n’a pas de voie de retrait sûre, signaler et garder le blocage plutôt que le perdre.
5. **Connexions et finalisation sûre** — Dependencies : étapes 2–4.
   - Enregistrer les paires de ModuleSocket à la construction, adapter SetConnected sans casser ses usages existants ; déconnexion d’un seul module libère exactement les voisins concernés et restaure hatchBlocker.
   - Pour liens legacy préplacés, revalider le voisin survivant contre les modules restant effectivement adjacents ; ne pas libérer toutes les prises de la scène.
   - Revalider contenu/capacité/cible, prendre un verrou terminal, arrêter la production du module, restituer le lot, appeler Disconnect sur ses ports et supprimer l’instance. Un échec précommit laisse le monde intact ; vérifier que la phase finale est déterministe et restaurer les slots/état si elle échoue.
   - Câble porté = blocage global ; connexion électrique/matériau normale = débranchement propre, sans inventer un remboursement de câble intégré. Préserver renderer/plug du module survivant selon l’ownership actuel.
6. **Ciblage et maintien de découpe** — Dependencies : étapes 4–5.
   - Brancher Player.Attack par événements, cache HotbarUI/caméra ; vérifier Item sélectionné par référence découpeuse sérialisée, jamais simple présence dans l’inventaire ni fallback de nom/id codé en dur.
   - Raycast premier obstacle/collider enfant vers ConstructedModule ou équipement démontable ; ignorer previews, ghosts, objets tenus, décor et instances non initialisées.
   - Progression bornée, interruption à zéro, invalidation de la cible ; bloquer Attack minage pendant usage de découpeuse et respecter tous les états UI/construction/câbles.
   - Ajouter les références uniquement aux joueurs utilisés ; ne pas ajouter automatiquement une découpeuse starter ni corriger les quatre EquipmentPlacementController en doublon hors périmètre.
7. **Feedback et configuration finale** — Dependencies : étape 6.
   - Étendre OnGUI avec cible, pourcentage, barre simple et restitution ; notifications de refus et succès, pas de spam frame par frame.
   - Vérifier absence de prompt périmé, actions E de récupération, affichage coût nul/recette absente et charge intrinsèque non remboursée.
   - Rafraîchir inventaire/hotbar uniquement au commit ; aucune modification de fonts, modèles/materials ou autres menus.
8. **Tests et régressions** — Dependencies : étapes 1–7.
   - Exécuter tests ciblés et parcours MoonScene, inspection Console, compilation Editor puis Windows si environnement disponible. Documenter limites de session et contenus préplacés non démontables.

# Verification & Testing
- **Boucle payée** : recette de test non vide + pose payante → construire → découper → quantités exactes de tous les ingrédients + coûts de pose ; même Item dans plusieurs lignes et deux sources fusionné correctement.
- **Vraies données** : Couloir et Principal rendent leurs coûts réellement consommés ; Crusher/Rig/panneau/module batterie avec recettes vides n’inventent aucun matériau. Recette absente et prefab absent donnent un diagnostic cohérent.
- **Provenance** : blueprint non retiré, construction debug gratuite, module préplacé, ghost, chantier incomplet et copie tenue ne permettent pas de remboursement fictif. Changer une recette après construction ne change pas le reçu.
- **Capacité** : stocks partiels, plusieurs ingrédients partageant les derniers slots, inventaire plein/hotbar libre, deux pleins, maxStack<=0, lots trop grands : tout ou rien, aucun gain/perte partiel ; slots batterie/oxygène existants strictement inchangés.
- **Robustesse** : ingrédients nulls/négatifs, recette multiple, outputAmount nul/non divisible, IDs conflictuels et overflow de quantités : refus explicite, aucune économie modifiée silencieusement.
- **Input** : découpeuse dans inventaire seulement ou autre slot = pas de découpe ; active + maintien = progression ; relâche, cible changée, portée perdue, pause/menu/fabrication/terminal/câble porté/canMove=false = arrêt sans restitution. Une tenue continue ne détruit pas successivement plusieurs modules par accident.
- **Raycast** : collider enfant/port/porte, obstacle devant, Ignore Raycast, ghost et joueur tenu ; seule la bonne racine payée est supprimée. Équipement monté ciblé est retiré distinctement.
- **Contenus** : crusher gravats/poudre, batterie externe pleine/vide, habitat avec Scrubber et inventaire occupé bloquent. Vider/retirer rend la cible admissible ; totalExtracted du Rig n’est pas un stock. Refus de retrait Scrubber si aucune place garde équipement/socket/alimentation intacts.
- **Connexions** : modules A–B–C, démolition B, voisins préplacés, reconstruction au même socket, hatch blockers ; ne pas supprimer A/C ni libérer des connexions unrelated. Pas de démolition en cascade.
- **Câbles/énergie** : Rig↔Crusher et panneau↔contrôleur↔accumulateur ; ports peer remis à zéro, énergie coupée, câbles survivants récupérables, aucun renderer/fake plug fantôme ni MissingReferenceException.
- **Atomicité monde** : deux validations même frame, grosse deltaTime, cible détruite/désactivée pendant découpe, stock alimenté pendant maintien, échec prévalidation : un seul remboursement ou aucun ; état terminal et destruction cohérents.
- **Régressions** : construction progressive/Welder, rotation/snapping, retrait blueprint, impression, minage, hotbar, oxygène, batterie individuelle, équipements, portes et UI/câbles. Tester joueurs/prefabs partagés dans les scènes pertinentes sans toucher aux archives.
- **Acceptation** : compilation et tests disponibles, aucune nouvelle erreur Console issue du chantier, parcours construction → vider/retirer → découpe → matériaux → reconstruction réussi ; économie nulle des recettes vides clairement documentée.

---

# Extension ciblée — Réacteur Sabatier sur sol glacé

# Project Overview
- **Game Title** : Solar Prospector: Corporation (Prospection system solaire).
- **High-Level Concept** : Compléter le réacteur lunaire existant pour produire Water et CH4 récupérables par le joueur. La machine se pose sur le terrain mais ne produit que lorsque son point d'extraction repose sur le sol glacé des cratères.
- **Players** : Solo.
- **Inspiration / Reference Games** : Satisfactory, The Planet Crafter ; prospection puis production et collecte.
- **Tone / Art Direction** : Industriel spatial semi-réaliste ; réutiliser modèle, textures et icônes existants.
- **Target Platform** : Windows 64-bit, Unity 6000.6.3f1.
- **Screen Orientation / Resolution** : Paysage, paramètres actuels inchangés.
- **Render Pipeline** : URP, PC_RPAsset ; inchangé.
- **Décisions confirmées** : reconnaissance par la couche de terrain RockIce_Craters ; glace seulement, sans énergie ni intrants.
- **Hors périmètre** : simulation chimique réelle, CO2/H2, câbles, consommation des rochers de glace, nouveaux modèles, consommation alimentaire/carburant des produits, sauvegarde intersessions et production hors ligne.
- La production est une simplification de gameplay, et non une réaction Sabatier physiquement complète. La description de l'Item devra annoncer le comportement réel.

# Game Mechanics
## Core Gameplay Loop
1. Imprimer l'Item Réacteur Sabatier avec la recette existante au Lander, puis utiliser les contrôles de construction et le Welder existants.
2. Poser le module sur un TerrainCollider, sans snapping aux modules. La pose sur terrain sec reste autorisée : c'est le fonctionnement, et non la construction, qui exige de la glace.
3. Un point d'extraction sous le centre de la machine identifie le terrain réellement sous elle et échantillonne sa couche RockIce_Craters. Seuil initial éditable : poids de couche >= 0,5. Pas de reconnaissance par nom d'objet ou couleur de texture, ni indice de couche codé en dur.
4. Sur sol glacé, produire un lot Water + CH4 par cycle. Valeurs initiales proposées à l'approbation : 1 Water et 1 CH4 toutes les 10 secondes, capacité 10 de chaque produit. Il s'agit d'unités d'inventaire, pas de proportions chimiques.
5. Si le sol est sec/absent, la couche manque ou l'un des stocks ne peut recevoir un lot complet, suspendre la production sans perdre les stocks ni la progression partielle. Aucune accumulation du temps d'arrêt et aucun débordement.
6. Viser la machine et appuyer sur [E] pour transférer les produits disponibles vers inventaire + hotbar. Une collecte partielle est possible si l'espace manque ; les produits non transférés restent dans la machine.
7. Vider les deux stocks avant déconstruction avec la découpeuse. L'interface extensible de garde de déconstruction existe déjà.
8. La contrainte géographique rend la prospection utile (MDA : emplacement -> organisation de base -> satisfaction de production). L'absence d'intrants rend cette première boucle accessible ; elle ne prétend pas simuler une chimie réaliste.

## Controls and Input Methods
- PlayerSettings vérifié : activeInputHandler = 1, New Input System.
- Réutiliser IPromptInteractable et Player.Interact via PlayerBaseController ; aucune nouvelle action ni polling clavier/souris.
- Réutiliser les contrôles actuels de placement, rotation, Welder et découpeuse ; aucun nouveau contrôleur de joueur.
- Respecter les gardes des menus, construction et manipulation de câbles. La collecte ne doit pas contourner les restrictions existantes.

# UI
Réutiliser le prompt au viseur et les notifications existants ; aucun nouveau Canvas ni modification des fonts.

```text
                   •
[E] Récupérer — Réacteur Sabatier
Water : 3/10 | CH4 : 3/10 — Production

États : Sol non glacé / Aucun terrain / Stock plein
        Configuration incomplète / Production
Succès : Water × 2, CH4 × 1 récupérés
Refus : Inventaire et barre rapide pleins
Découpe : Vider le réacteur Sabatier (Water/CH4)
```
- Prompt dynamique et stocks visibles avant collecte ; notification avec quantités réellement reçues.
- Le ghost conserve la couleur actuelle de validité de construction : vert ne signifie pas nécessairement que le sol est glacé. La machine terminée indique clairement son état de fonctionnement.

# Key Asset & Context
## État constaté et impact
- Assets/Prefabs/Base/SousSystem/ReacteurSabatierPrefab.prefab : Transform, MeshFilter, MeshRenderer seulement ; pas de Collider ou de logique. Modèle WaterCollector.fbx et matériau WaterCollectorMat présents.
- Assets/Items/Reacteur sabatier.asset : id 5436, constructible, modèle inventaire référant actuellement le prefab final. Ajouter une logique au prefab sans séparer cet usage risquerait de produire dans la main du joueur.
- Assets/construction/Reacteur sabatier.asset : groundPlacementOnly = true, durée 5 s, coûts de pose vides, ghost/final et recette de déconstruction déjà liés. Préserver ces valeurs et identités.
- Assets/Prefabs/Base/SousSystem/Ghost_Reacteur sabatier.prefab : géométrie et ConstructibleGhost, aucune logique machine ; le conserver ainsi.
- Assets/Recipes/Recipe_Reacteur sabatier.asset : outputAmount = 1, trois ingrédients existants ; ne pas les modifier. Référence runtime déjà présente dans les 14 stationRecipes du Lander et les 8 globalRecipes du CraftingCanvas.
- Assets/Items/Water.asset : id 5435, maxStack = 1, modèle WaterPrefab et icône déjà assignés.
- Assets/Items/CH4.asset : id 5437, maxStack = 1, modèle CH4Prefab et icône déjà assignés. Ne pas recréer les deux Items ni modifier leurs stacks.
- MoonScene : Terrain de 1000 × 20 × 1000 ; couche RockIce_Craters présente, 10074 pixels alpha avec poids >= 0,5. Échantillonnage spatial à 5 m : 1493 points glacés, dont 1487 compatibles avec la pente de construction actuelle. Trente RockIce existent aussi ; ils ne conditionneront pas le réacteur.
- EnvironmentalResourceManager possède déjà une règle glace InCratersOnly ; ne pas régénérer toute la distribution et ne pas repeindre globalement le terrain.
- BuilderController.cs impose déjà TerrainCollider pour groundPlacementOnly, pente hit.normal.y > 0,7 et compensation du pivot par le bas du renderer. Aucune réécriture prévue.
- InventoryRefundTransaction.TryApply(amounts, inventory, hotbar, commit, out reason) permet simulation et transaction tout-ou-rien pour Items ordinaires. Éviter Inventory.AddItem en lot, dont un échec peut suivre une insertion partielle.
- ConstructedModule.cs appelle déjà IModuleDeconstructionGuard.GetDeconstructionBlockReason() ; l'implémenter sur la machine sans changer le système de déconstruction.
- Dépendances/dépendants inspectés via AssetDatabase.GetDependencies et recherche inverse : prefab final -> Item/module ; module -> Item/ghost/recette/final ; couche glace -> TerrainData. MoonScene était non dirty à l'inspection ; revalider avant toute sauvegarde.

## Architecture retenue et fichiers
- Créer Assets/Scripts/BuildingSystem/Industry/SabatierUnit.cs : MonoBehaviour, IPromptInteractable, IModuleDeconstructionGuard ; stocks par instance, progression, état du sol, récupération sûre.
- Créer Assets/Scripts/BuildingSystem/Industry/FrozenGroundProbe.cs : référence TerrainLayer sérialisée, point d'extraction, seuil, intervalle de contrôle ; trouver le terrain sous le point, convertir ses coordonnées monde en coordonnées alpha, récupérer l'indice par référence de couche.
- Modifier ReacteurSabatierPrefab.prefab : collider physique/interactif, point d'extraction, composants et références Water/CH4/couche glace. Ne pas déplacer arbitrairement le mesh ou modifier le FBX.
- Créer Assets/Prefabs/Base/SousSystem/ReacteurSabatierVisual.prefab : copie visuelle seule, sans scripts ni collider interactif. Référencer cette copie dans Item.model ; préserver le prefab final de construction et le ghost existant.
- Modifier uniquement description et model de Assets/Items/Reacteur sabatier.asset ; conserver id/GUID/icône/prix/liaisons.
- MoonScene et son TerrainData : inspection et tests uniquement par défaut. Si le contrôle visuel révèle un fond de cratère sans glace accessible, proposer une correction locale avant édition, sans écraser le terrain entier. Aucun réacteur décoratif préplacé nécessaire : il sera construit par le joueur.
- Aucun changement prévu à BuilderController, PlayerBaseController, Inventory, Water/CH4, recette, couches sources ou packages ; adapter uniquement si un défaut d'intégration réel est établi et documenté.
- Alternative étudiée non retenue : détection de RockIce proche ou exigence couche + gisement ; plus dépendante du minage et contraire au choix utilisateur.

# Implementation Steps
1. **Préflight ciblé** — Dependencies : approbation de cette extension.
   - Actualiser dépendances de chaque asset à modifier/créer, vérifier scènes dirty, interfaces, montage de tests et rafraîchissement UI existant.
   - Examiner transform/scale/pivot du final et du ghost, ordre de finalisation de construction et visibilité du collider depuis la caméra.
   - Vérifier visuellement les zones glacées au fond des cratères et l'accessibilité au joueur ; ne pas déclencher GenerateAllResources.
2. **Détection du sol glacé** — Dependencies : étape 1.
   - FrozenGroundProbe interroge le premier support sous le point d'extraction, ignore colliders propres à la machine et triggers ; support non TerrainCollider = arrêt.
   - Terrain trouvé via son collider, pas Terrain.activeTerrain ; vérifier limites X/Z, position et taille réelles, indice de couche par référence et orientation du tableau alpha.
   - Échantillonnage interpolé cohérent au bord des couches, seuil éditable 0,5. Contrôle initial puis toutes les 0,5 s ; ne pas allouer une alphamap complète chaque frame. Invalider le cache si TerrainData ou couches changent.
   - Terrain absent, hors limites ou couche manquante : état explicite et échec fermé.
3. **Production et stockage** — Dependencies : étape 2.
   - SabatierUnit.AdvanceProduction(float deltaTime), propriétés WaterStock/CH4Stock et état diagnostique.
   - Valider intervalle/capacités/Items ; pas de production avec références nulles ou identiques, valeur temporelle invalide ou quantité négative.
   - Produire les deux sorties atomiquement par cycle seulement si elles tiennent ; calcul borné par capacité pour les grosses deltaTime. Pause sur sol invalide/stock plein, reprise sans multiplication du temps d'arrêt.
   - OnDisable stoppe le travail ; stocks et progression restent dans l'instance. Aucun état stocké sur les ScriptableObjects partagés.
4. **Collecte et déconstruction** — Dependencies : étape 3.
   - Résoudre inventaire joueur et sa hotbar par les références existantes. Construire un lot collectable par simulations de transaction, en alternant Water/CH4 puis complétant avec la ressource qui tient encore.
   - Commit unique du lot exact via InventoryRefundTransaction ; décrémenter uniquement après succès. Si rien ne tient, ne toucher à aucun stock. Pas de fallback réajoutant un lot entier après insertion partielle.
   - Implémenter garde de déconstruction tant que WaterStock ou CH4Stock > 0 ; récupération possible même sur sol sec ou production suspendue.
   - Rafraîchir les vues inventaire/hotbar existantes après succès, notifications aux quantités exactes, sans recherche globale permanente.
5. **Configuration des prefabs et intégration** — Dependencies : étapes 2–4.
   - Ajouter collider adapté au volume réel et point d'extraction sous le centre ; références sérialisées persistantes.
   - Créer le visuel pur pour Item.model puis mettre à jour description de gameplay ; finalPrefab, ghost et coûts restent inchangés.
   - Vérifier que seule la machine finale produit ; aucun comportement sur item tenu, aperçu ou ghost. Conserver la pose sur terrain sec autorisée et l'assise au sol existante.
6. **Tests et parcours lunaire** — Dependencies : étapes 1–5.
   - Utiliser le montage de tests déjà disponible, sinon validations Editor reproductibles sans restructuration globale des assemblies.
   - Imprimer -> construire au fond d'un cratère glacé -> attendre -> récupérer Water/CH4 -> vider -> déconstruire ; tester également terrain sec.
   - Compilation Editor, Console, puis build Windows si environnement disponible ; ne pas annoncer un test ou build non exécuté.

# Verification & Testing
- **Sol** : couche RockIce_Craters sous la machine autorise ; terrain sec, couche absente, support structure/rocher, point hors terrain ou aucun terrain interdisent. Indice de couche réordonné, plusieurs terrains, terrain décalé et bords alpha correctement gérés.
- **Indépendance du minage** : enlever/miner un RockIce voisin n'arrête pas une machine posée sur la couche glacée ; un rocher proche d'une machine sur terrain sec ne l'active pas.
- **Production** : exactement 1 Water + 1 CH4 par 10 s aux valeurs initiales ; pas de production avant un cycle, en ghost ou dans la main. Deux machines ont stocks et compteurs indépendants.
- **Capacité** : arrêt si une sortie est pleine ; jamais de lot partiel ni stock supérieur à 10. Stock/progression préservés sur perte de glace, pause et désactivation ; pas de rattrapage du temps suspendu.
- **Robustesse** : deltaTime <= 0, NaN/infinie, grosse deltaTime, références invalides, changement de terrain/couches ; aucun overflow, stock négatif ou boucle non bornée.
- **Collecte** : inventaire vide, place pour un seul objet, hotbar seule libre, deux pleins et stocks asymétriques ; seules les quantités réellement ajoutées sont retirées. maxStack=1 respecté ; batteries/oxygène déjà présents inchangés.
- **Interaction** : premier obstacle, collider de la machine accessible, spam [E], menus/construction/câble porté ; aucun transfert à distance ou pendant une UI bloquante.
- **Déconstruction** : stocks non vides bloquent ; vider permet découpe et remboursement de la recette existante, sans rendre en plus les produits ou le blueprint.
- **Terrain et régressions** : glace visible dans les fonds de cratères, machine non flottante, réutilisation recette au Lander, Welder, rotation et construction sur sec conservés ; autres machines et archives intactes.
- **Acceptation** : parcours complet réussi, items récupérés dans inventaire/hotbar avec leurs modèles existants, aucune nouvelle erreur Console et descriptions cohérentes avec la simplification choisie. Persistance intersessions explicitement non garantie.

## Ajustement de périmètre confirmé pendant l'exécution
- Le test runtime a établi que la recette existante consomme une Batterie Externe × 1, Plaque de Blindage × 5 et Construction Component × 2. Le remboursement générique refuse les Items à état individuel, donc le démontage ne peut pas rendre cette batterie sans définir comment conserver sa charge lors de l'impression.
- **Choix utilisateur : reporter la déconstruction.** Ce choix remplace les objectifs de démontage/remboursement ci-dessus pour cette version ; les coûts et la recette restent inchangés.
- Le réacteur reste récupérable pour ses sorties Water/CH4, mais sa découpe est bloquée par une raison explicite, y compris après vidage. Aucune perte de contenu, aucune nouvelle batterie chargée créée artificiellement.
- Les tests finaux vérifient le maintien de ce blocage et l'absence de restitution fictive, au lieu d'exiger le démontage du réacteur.
- Résultat des validations runtime finales : 28 contrôles réussis, dont construction par BuilderController, sol glacé réel de MoonScene, production, collecte partielle inventaire/hotbar, terrain sec, réordonnancement des couches et déconstruction différée. Imprimante/menu parcourus par inspection des références runtime ; parcours manuel et build Windows non exécutés.


