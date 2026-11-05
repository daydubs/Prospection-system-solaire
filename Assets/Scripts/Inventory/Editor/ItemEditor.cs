using UnityEditor;
using UnityEngine;

namespace InventoryFramework.EditorTools
{
    [CustomEditor(typeof(Item), true)]
    public class ItemEditor : UnityEditor.Editor
    {
        [MenuItem("Assets/Create/Inventory/Item unifié")]
        private static void CreateItem() => ContentAuthoringWindow.Open();

        public override void OnInspectorGUI()
        {
            var item = (Item)target;
            EditorGUILayout.HelpBox("Nom, description et icône sont la source unique pour l'inventaire, la construction et les recettes. Modifier le contenu depuis le bloc unifié.", MessageType.Info);
            if (GUILayout.Button("Ouvrir Items et modules", GUILayout.Height(30))) ContentAuthoringWindow.Open(item);
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true)) DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}
