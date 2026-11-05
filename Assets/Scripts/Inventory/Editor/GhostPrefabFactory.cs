using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InventoryFramework.EditorTools
{
    public static class GhostPrefabFactory
    {
        private static readonly HashSet<string> RemovedLogic = new HashSet<string>
        {
            "BaseBatteryModule", "CrusherUnit", "RigUnit", "CrusherBatterySocket",
            "ElectricConnectorSocket", "MaterialConnectorSocket", "FlexibleCableRenderer", "ConstructibleGhost"
        };

        public static string Review(GameObject source)
        {
            if (source == null || !EditorUtility.IsPersistent(source)) return "Choisir un prefab asset existant.";
            var unknown = new List<string>();
            foreach (var c in source.GetComponentsInChildren<Component>(true))
            {
                if (c == null) { unknown.Add("Missing Script"); continue; }
                if (c is Transform || c is MeshFilter || c is MeshRenderer || c is ModuleSocket || c is Collider || c is Rigidbody || c is AudioSource || c is Light || c is Canvas || c is CanvasRenderer || c is Animator) continue;
                string ns = c.GetType().Namespace ?? string.Empty;
                if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal) || ns.StartsWith("TMPro", StringComparison.Ordinal) || RemovedLogic.Contains(c.GetType().Name)) continue;
                unknown.Add(c.GetType().FullName);
            }
            return unknown.Count == 0 ? string.Empty : "Composants à examiner avant génération : " + string.Join(", ", unknown.Distinct()) + ". Fournir un ghost préparé manuellement pour ce prefab.";
        }

        public static GameObject Create(GameObject source, Material ghostMaterial, string path)
        {
            string issue = Review(source);
            if (!string.IsNullOrEmpty(issue)) throw new InvalidOperationException(issue);
            if (ghostMaterial == null) throw new InvalidOperationException("Matériau ghost requis.");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) throw new InvalidOperationException("Chemin déjà occupé.");
            var map = new Dictionary<Transform, Transform>();
            GameObject root = null;
            try
            {
                foreach (var t in source.GetComponentsInChildren<Transform>(true))
                {
                    var go = new GameObject(t == source.transform ? System.IO.Path.GetFileNameWithoutExtension(path) : t.name);
                    if (root == null) root = go;
                    if (t.parent != null && map.TryGetValue(t.parent, out var parent)) go.transform.SetParent(parent, false);
                    go.transform.localPosition = t.localPosition; go.transform.localRotation = t.localRotation; go.transform.localScale = t.localScale;
                    go.layer = LayerMask.NameToLayer("Ignore Raycast"); go.SetActive(t.gameObject.activeSelf); map[t] = go.transform;
                    var filter = t.GetComponent<MeshFilter>();
                    if (filter != null) go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var renderer = t.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        var r = go.AddComponent<MeshRenderer>();
                        r.sharedMaterials = Enumerable.Repeat(ghostMaterial, renderer.sharedMaterials.Length).ToArray();
                        r.enabled = renderer.enabled; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    }
                }
                foreach (var socket in source.GetComponentsInChildren<ModuleSocket>(true))
                {
                    var copy = map[socket.transform].gameObject.AddComponent<ModuleSocket>();
                    copy.socketSize = socket.socketSize; copy.socketType = socket.socketType; copy.isOccupied = false;
                    if (socket.HatchBlocker != null && map.TryGetValue(socket.HatchBlocker.transform, out var blocker)) copy.HatchBlocker = blocker.gameObject;
                    var col = copy.GetComponent<SphereCollider>(); col.isTrigger = true; col.radius = socket.socketSize / 2f;
                    var sourceCol = socket.GetComponent<SphereCollider>(); if (sourceCol != null) col.center = sourceCol.center;
                }
                root.AddComponent<ConstructibleGhost>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("Impossible d'enregistrer le ghost.");
                Undo.RegisterCreatedObjectUndo(prefab, "Créer ghost dédié");
                return prefab;
            }
            catch { if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path); throw; }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
