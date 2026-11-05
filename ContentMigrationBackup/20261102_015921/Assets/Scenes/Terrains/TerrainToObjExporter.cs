using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class TerrainToObjExporter : EditorWindow
{
    private Terrain terrain;
    private int resolution = 128; // Résolution de la grille du mesh exporté

    [MenuItem("Tools/Export Terrain to OBJ")]
    public static void ShowWindow()
    {
        GetWindow<TerrainToObjExporter>("Terrain to OBJ");
    }

    void OnGUI()
    {
        GUILayout.Label("Export Terrain -> OBJ", EditorStyles.boldLabel);
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 32, 512);

        if (GUILayout.Button("Export"))
            Export();
    }

    void Export()
    {
        if (!terrain) { Debug.LogError("Assign a terrain!"); return; }

        string path = EditorUtility.SaveFilePanel("Save OBJ", "", "terrain", "obj");
        if (string.IsNullOrEmpty(path)) return;

        TerrainData td = terrain.terrainData;
        Vector3 pos = terrain.transform.position;
        Vector3 size = td.size;
        int res = resolution + 1;

        StringBuilder sb = new StringBuilder();

        // Vertices (positions + normals via heights)
        float[,] h = td.GetHeights(0, 0, res, res);
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector3 p = new Vector3(
                    (float)x / (res - 1) * size.x + pos.x,
                    h[z, x] * size.y + pos.y,
                    (float)z / (res - 1) * size.z + pos.z
                );
                sb.AppendLine($"v {p.x} {p.y} {p.z}");
                // UV planar (top-down projection)
                sb.AppendLine($"vt {(float)x / (res - 1)} {(float)z / (res - 1)}");
            }
        }

        // Faces
        for (int z = 0; z < res - 1; z++)
        {
            for (int x = 0; x < res - 1; x++)
            {
                int v0 = z * res + x + 1;
                int v1 = z * res + x + 2;
                int v2 = (z + 1) * res + x + 1;
                int v3 = (z + 1) * res + x + 2;
                sb.AppendLine($"f {v0}/{v0} {v1}/{v1} {v3}/{v3}");
                sb.AppendLine($"f {v0}/{v0} {v3}/{v3} {v2}/{v2}");
            }
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log("Exported: " + path);
    }
}