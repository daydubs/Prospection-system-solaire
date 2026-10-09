using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnvironmentalResourceManager))]
public class EnvironmentalResourceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnvironmentalResourceManager manager = (EnvironmentalResourceManager)target;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Actions Rapides / Génération", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        if (GUILayout.Button("Générer les Ressources", GUILayout.Height(32)))
        {
            manager.GenerateAllResources();
        }

        GUI.backgroundColor = new Color(0.95f, 0.4f, 0.35f);
        if (GUILayout.Button("Nettoyer les Ressources", GUILayout.Height(32)))
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Voulez-vous vraiment supprimer toutes les ressources générées dans le conteneur ?", "Oui, supprimer", "Annuler"))
            {
                manager.ClearGeneratedResources();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Presets Environnementaux (Planètes & Satellites)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preset : Lune (Glace en cratère)"))
        {
            Undo.RecordObject(manager, "Apply Moon Preset");
            manager.ApplyPresetMoon();
            EditorUtility.SetDirty(manager);
        }
        if (GUILayout.Button("Preset : Europe (Glace partout)"))
        {
            Undo.RecordObject(manager, "Apply Europa Preset");
            manager.ApplyPresetEuropa();
            EditorUtility.SetDirty(manager);
        }
        if (GUILayout.Button("Preset : Mars"))
        {
            Undo.RecordObject(manager, "Apply Mars Preset");
            manager.ApplyPresetMars();
            EditorUtility.SetDirty(manager);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Ce gestionnaire permet d'automatiser le placement procédural des ressources (Régolite, Glace, etc.) en respectant les échelles demandées (25 à 60) et les contraintes géologiques (ex: restriction de la glace aux cratères pour la Lune, ou glace sur toute la surface pour Europe).", MessageType.Info);

        EditorGUILayout.Space(4);
        DrawDefaultInspector();
    }
}
