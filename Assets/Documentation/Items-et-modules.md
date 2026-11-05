# Items et modules — workflow unifié

## Ouvrir
**Tools → Contenu → Items et modules** (ou **Assets → Create → Inventory → Item unifié**).
Sélectionner un Item puis son bouton **Ouvrir Items et modules** permet de modifier un contenu existant.

## Créer
1. Cliquer **Nouvel Item**. Renseigner nom, description, icône Sprite, stack, prix et modèle inventaire optionnel.
2. Cocher **Constructible** si l'Item représente un module à poser. Choisir son prefab final et son ghost, puis temps et coûts de construction.
3. Cocher **Fabricable** pour créer une recette. Renseigner catégorie, quantité et ingrédients. Choisir une station et/ou une liste globale destination (composant de scène ou de prefab).
4. **Analyser**, puis **Créer les assets liés**. L'ID positif est attribué à cet instant et reste stable lors des renommages et déplacements.

Les nouveaux assets sont créés sous `Assets/Items`, `Assets/construction`, `Assets/Recipes` avec chemins libres. Si le nom existe, un suffixe distingue la nouvelle création. **Enregistrer** un contenu existant conserve ses assets, GUID et ID.

## Une seule source d'informations communes
L'Item contient le nom, la description et l'icône. Le module référence son Item propriétaire et lit directement ces informations. La recette utilise l'Item produit par défaut ; ses champs de nom et description sont des surcharges facultatives pour des instructions de fabrication différentes. Laisser ces champs vides évite toute copie inutile.

**Important :** le modèle inventaire (visuel tenu/ramassé) et le prefab final (machine fonctionnelle) sont distincts. Les coûts d'impression et de construction restent également distincts ; l'outil n'invente aucun coût ou composant de gameplay.

## Édition et options
- Sélectionner l'Item dans le champ supérieur ; les brouillons ne modifient pas l'asset avant Enregistrer.
- Plusieurs recettes pour un Item : choisir celle à éditer ou confirmer une recette supplémentaire.
- Désactiver Constructible conserve ses données, mais empêche le mode construction pour cet Item.
- Désactiver Fabricable conserve les recettes existantes et **ne les retire pas du jeu**. Cette case contrôle uniquement leur création/édition dans le workflow.
- Annuler abandonne le brouillon. Un ghost créé par son bouton dédié reste un asset indépendant et disponible.
- La sauvegarde d'une recette complète les listes choisies sans remplacer leurs éléments. Pour une scène déjà modifiée, une confirmation est demandée avant sauvegarde.
- L'inspecteur des Item/modules affiche les valeurs en lecture seule ; utiliser la fenêtre pour les modifier.

## Ghost dédié
Le bouton **Créer un ghost géométrique dédié** génère une nouvelle hiérarchie géométrique, le matériau ghost, ConstructibleGhost et les ModuleSocket nécessaires. Aucun changement au prefab final. Aucune logique de machine, batterie, câble, audio ou UI n'est copiée. Les sockets conservent leur taille, leur type, leurs blockers et leurs triggers.
Un composant non reconnu bloque la génération : examiner le prefab et fournir un ghost préparé manuellement, sans contourner cette protection. Les renderers skinnés ne sont pas pris en charge par ce générateur statique.

## Audit et IDs
**Audit du catalogue** signale IDs nuls/dupliqués, icônes absentes, liens incohérents et prefabs manquants. **Réparer l'ID sélectionné** demande une confirmation, sauvegarde cet asset et écrit une correspondance GUID/ancien/nouvel ID sous `ContentMigrationBackup/` (hors Assets). Vérifier les usages numériques externes avant une réparation. Les IDs ne sont jamais réattribués au simple chargement ou réimport.

## Migration de ce projet
- Les 12 anciens BlueprintItem ont été convertis en Item en place, avec mêmes chemins/GUID/fileID.
- Les informations communes des Items ont été retenues selon le choix utilisateur. Les anciennes données des modules sont dans la sauvegarde.
- ID 1 conservé pour Sonic Fracturer ; couloir → 5425 ; anciens zéros → 5426 à 5432. Les autres IDs existants, dont batterie 2603, sont inchangés.
- Scrubber (`Assets/construction/Scrubber.asset`) reste un Item non constructible, ID 43, modèle conservé.
- Le module vide `MosuleBaterie.asset` est conservé avec son Item propriétaire `MosuleBaterieItem.asset`, non constructible, à compléter avant usage.
- Les noms de fichiers historiques contenant « Blueprint » sont conservés pour éviter des déplacements inutiles ; ils ne correspondent plus à un type BlueprintItem.
- Rig/RigBlueprint et les deux Scrubber ne sont pas fusionnés : leurs usages peuvent différer.

Sauvegarde initiale : `ContentMigrationBackup/20261102_015921/` avec sources, assets, metas, manifeste et rapport. Retour arrière : fermer Unity, restaurer les fichiers Assets originaux et metas, retirer les nouveaux outils/Item propriétaire créés par ce chantier, rouvrir Unity. Ne pas restaurer seulement un ancien asset Blueprint sans son ancien code.

## Vérifications reproductibles
**Tools → Contenu → Vérifier le workflow (fixtures temporaires)** exécute les checks Editor de création, IDs, édition, liens, inclusion recette, validation refusée, ghost sûr et conservation des types batterie/oxygène. Les fixtures créées sont supprimées en fin de test, même en cas d'échec.

## Résultats de validation
- Checks Editor exécutés avec succès : création, IDs uniques, édition sans duplication, GUID stable, rejet d'une création sans icône, liens Item/module/recette, recette persistante dans un prefab station après réimport, conservation des données en décochant les options, batterie et sous-type oxygène.
- Test Play Mode réussi : Item constructible reconnu, lien exact avec le module, Item ordinaire refusé comme plan, ghost généré sans logique machine/batterie/câble, construction bloquée sans Welder, finalisation avec Welder, consommation exacte d'un Item et conservation du Welder.
- Identités des 34 Items originaux vérifiées : GUID/fileID conservés. Catalogue final : 35 Items, IDs positifs uniques, liens des modules cohérents ; 7 références valides dans le menu des plans de MoonScene.
- Ancien type BlueprintItem absent des scripts et aucune référence sérialisée à son script. Neuf noms de recettes identiques au nom de leur Item ont été vidés : affichage inchangé, instructions spécifiques conservées.
- Fenêtre inspectée visuellement ; description passée en champ multilignes. Compilation Editor réussie et aucune erreur Console au contrôle final. Les outils Editor ne font pas partie des assemblies Player.
- Fixtures et captures temporaires supprimées ; MoonScene propre, Editor hors Play Mode.

**Restent non vérifiés** : build Windows complet, redémarrage complet de l'Editor, restauration réelle de la sauvegarde, parcours manuel complet de snapping/rotation et régressions industrielles dans toutes les scènes. Le test runtime utilise une construction isolée, pas une simulation exhaustive de tous les gestes du joueur.

**Avertissements de contenu conservés** : icône absente sur `MosuleBaterieItem.asset`, `New Item.asset` et `RegolitePowder.asset`. Aucun visuel n'a été inventé ; attribuer une icône avant leur enregistrement dans le workflow unifié.

