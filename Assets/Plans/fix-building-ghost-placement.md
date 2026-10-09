# Project Overview
- Game Title: Prospection Système Solaire
- High-Level Concept: Jeu de survie, d'exploration et de prospection spatiale à la première personne sur la Lune et dans le système solaire, combinant récolte de ressources, gestion d'inventaire et construction modulaire de base.
- Players: Single player
- Inspiration / Reference Games: Subnautica, Satisfactory, Space Engineers
- Tone / Art Direction: Sci-Fi réaliste / exploration spatiale lunaire
- Target Platform: PC (Standalone Windows 64)
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
1. Le joueur prospecte et extrait des minerais et éléments chimiques (FeO, MgO, TiO2, AL2O3, etc.) à la surface lunaire.
2. Le joueur fabrique ou acquiert des plans de construction (Blueprints, ex. Module Principal, Couloir).
3. Le joueur équipe le plan dans sa barre d'accès rapide (Hotbar/Ceinture).
4. En sélectionnant le plan et avec le Welder (Soudeur) équipé sur sa ceinture, le joueur visualise une prévisualisation holographique semi-transparente ("Ghost") du module dans le monde 3D.
5. Le joueur oriente le module (molette de la souris) ou le connecte magnétiquement aux sockets d'autres modules existants.
6. Clic gauche : le joueur fige l'emplacement du ghost puis maintient le clic gauche pour souder et consommer progressivement les ressources nécessaires jusqu'à l'apparition du module final achevé.

## Controls and Input Methods
- **Sélection Hotbar (1 à 8)** : Équipe le plan ou l'outil correspondant.
- **Orientation / Rotation (Molette de la souris / Scroll)** : Fait pivoter horizontalement le ghost lors de la prévisualisation libre.
- **Clic Gauche (Press)** : Place et fige le ghost à l'emplacement visé si valide.
- **Clic Gauche (Maintenu)** : Construit et soude le module en consommant les ressources requises au fil des secondes.
- **Clic Droit** : Déverrouille le ghost pour le repositionner s'il est déjà figé, ou annule/quitte le mode construction.

# UI
- **Crosshair central** : Point de visée au centre de l'écran.
- **HUD Prompt de Construction** :
  - *Mode prévisualisation* : `[Clic Gauche] Poser : [Nom du module]  |  [Molette] Tourner  |  [Clic Droit] Annuler` (texte vert si valide, rouge si Welder manquant ou inclinaison trop raide).
  - *Mode soudure* : `[Clic Gauche Maintenu] Souder et Construire  |  [Clic Droit] Déplacer`.
- **Ghost Shader / Tint** :
  - Vert semi-transparent (`validColor`) lorsque la surface est plane ou snap à un socket et que le soudeur est équipé.
  - Rouge semi-transparent (`invalidColor`) si le joueur n'a pas le soudeur ou vise une zone invalide/hors de portée.
  - Bleu semi-transparent (`constructingColor`) pendant la phase de soudure.

# Key Asset & Context
- **Assets/Scripts/BuildingSystem/BuilderController.cs** :
  - *Problème identifié* : La méthode `CreateGhost()` n'était jamais appelée nulle part dans le code, et la classe ne possédait AUCUNE méthode `Update()`. Par conséquent, `UpdateGhostPlacement()` et `HandleBuildingInput()` n'étaient jamais exécutées chaque frame.
  - *Correctif* :
    - Ajouter `Update()` dans `BuilderController` pour mettre à jour la position du ghost et gérer les inputs de pose/soudure.
    - Appeler `CreateGhost()` dès l'entrée en mode construction (`EnterBuildMode`), et détruire/recréer proprement le ghost lors d'un changement de module.
    - Augmenter `buildRange` de 10m à 20m pour permettre le placement confortable de modules imposants (le Module Principal mesurant ~9.4m d'envergure).
    - Ajouter un fallback robuste pour la rotation par molette (`Mouse.current.scroll.ReadValue().y`).
- **Assets/Scripts/BuildingSystem/ConstructibleGhost.cs** :
  - *Problème identifié* : `SetPlacementValidity` et `ConstructProgress` n'accédaient qu'à `r.material` (première matière seulement) au lieu de `r.materials` (toutes les matières du renderer), ignorant les fenêtres du module principal (`WindowsGhost`), et risquaient de lever une exception si un renderer a une matière nulle (`AirtightScreen`).
  - *Correctif* : Parcourir `r.materials` avec vérification `mat != null`, et appliquer la couleur `_BaseColor` (URP) et `_Color`.
- **Assets/Scripts/Controllers/PlayerBaseController.cs** :
  - *Amélioration* : Dans `OnGUI()`, donner la priorité d'affichage au prompt de construction du `BuilderController` par rapport au prompt de minage, évitant que viser près d'un rocher ne masque les instructions de construction.
- **Assets/Scripts/Controllers/MainMenuController.cs** :
  - *Problème secondaire identifié dans les logs* : `ShowNotification` tente de démarrer une coroutine alors que le GameObject est inactif.
  - *Correctif* : Vérifier `isActiveAndEnabled` avant `StartCoroutine`.

# Implementation Steps
### Step 1: Réparer l'instanciation, la boucle Update et les inputs dans BuilderController
- **Description**: 
  - Dans `Assets/Scripts/BuildingSystem/BuilderController.cs` :
    1. Ajouter la méthode `Update()` qui appelle `CreateGhost()` si besoin, puis `UpdateGhostPlacement()` et `HandleBuildingInput()`.
    2. Dans `EnterBuildMode(BaseModuleData moduleData)`, vérifier si le ghost existe déjà ou s'il s'agit d'un nouveau module ; détruire l'ancien ghost et instancier le nouveau via `CreateGhost()`.
    3. Dans `ExitBuildMode()`, détruire le ghost et réinitialiser les drapeaux.
    4. Porter la distance de construction `buildRange` par défaut à 20f (ou 25f).
    5. Utiliser `playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))` pour une visée robuste au centre de l'écran, avec fallback si `playerCamera` n'est pas assignée.
    6. Ajouter une lecture sécurisée de la molette (`Mouse.current.scroll.ReadValue().y`) en complément de l'Input Action.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

### Step 2: Améliorer la teinte et la robustesse de ConstructibleGhost
- **Description**:
  - Dans `Assets/Scripts/BuildingSystem/ConstructibleGhost.cs` :
    1. Mettre à jour `SetPlacementValidity` et `ConstructProgress` pour itérer sur tous les matériaux (`r.materials`) de chaque renderer avec vérification de nullité (`if (mat == null) continue`).
    2. Appliquer la teinte sur `_BaseColor` (standard URP) avec fallback `_Color`.
    3. S'assurer que le ghost s'initialise correctement et que les colliders non-sockets sont désactivés pour ne pas gêner le raycast.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

### Step 3: Priorité d'affichage du HUD et correction mineure des notifications
- **Description**:
  - Dans `Assets/Scripts/Controllers/PlayerBaseController.cs` :
    - Dans `OnGUI()`, afficher en priorité les instructions de construction si `builderController.isBuildModeActive && builderController.currentModuleToBuild != null`, avant les prompts de minage ou d'interaction générique.
  - Dans `Assets/Scripts/Controllers/MainMenuController.cs` :
    - Protéger `ShowNotification` avec `if (!isActiveAndEnabled) return;` pour éviter l'erreur de coroutine sur canvas inactif.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

# Verification & Testing
1. **Test visuel du Ghost en mode jeu** :
   - Lancer MoonScene en mode Play.
   - S'assurer d'avoir le Welder dans la barre de raccourcis (ou le récupérer dans la base).
   - Sélectionner le slot contenant le Blueprint du Module Principal ou du Couloir.
   - Constater que le Ghost 3D apparaît immédiatement devant la caméra du joueur et suit le point de visée.
2. **Test de rotation** :
   - Utiliser la molette de la souris : vérifier que le ghost pivote fluidement sur l'axe Y.
3. **Test de couleur / validité** :
   - Viser le sol plat lunaire : le ghost doit être vert semi-transparent.
   - Viser le ciel ou une pente trop abrupte : le ghost doit être rouge semi-transparent.
4. **Test de pose et de soudure (Clic Gauche)** :
   - Premier clic gauche : le ghost se fige à l'emplacement sélectionné.
   - Maintien du clic gauche : les ressources (FeO, TiO2, etc.) se décomptent, le ghost prend une teinte bleue de construction, puis le prefab final (`PrincipalPrefab` ou `CouloirCompletPrefab`) est instancié à la fin du délai.
5. **Test d'annulation (Clic Droit)** :
   - Vérifier que le clic droit défige le ghost ou annule la prévisualisation sans erreur dans la console Unity.
