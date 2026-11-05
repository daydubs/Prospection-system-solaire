using UnityEditor;
using UnityEngine;

namespace InventoryFramework.EditorTools
{
    [CustomEditor(typeof(BaseModuleData))]
    public class BaseModuleDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var module = (BaseModuleData)target;
            EditorGUILayout.LabelField(module.moduleName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(module.description, EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Icône de l'Item", module.icon, typeof(Sprite), false);
            EditorGUILayout.HelpBox("Informations communes issues de l'Item, sans copie sur le module.", MessageType.Info);
            using (new EditorGUI.DisabledScope(module.Item == null))
                if (GUILayout.Button("Modifier dans Items et modules", GUILayout.Height(30))) ContentAuthoringWindow.Open(module.Item);
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true)) DrawPropertiesExcluding(serializedObject, "m_Script", "_deconstructionRecipe");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_deconstructionRecipe"));
            serializedObject.ApplyModifiedProperties();
            var receipt = new ModuleRefundReceipt().Snapshot(module, true);
            if (receipt.Error != null) EditorGUILayout.HelpBox(receipt.Error, MessageType.Error);
            else if (receipt.Note != null) EditorGUILayout.HelpBox(receipt.Note, MessageType.Info);
        }
    }
}
