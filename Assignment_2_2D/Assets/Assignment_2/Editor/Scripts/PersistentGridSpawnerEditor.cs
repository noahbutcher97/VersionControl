using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PersistentGridSpawner))]
public class PersistentGridSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector UI
        DrawDefaultInspector();

        // Reference the target script (PersistentGridSpawner)
        PersistentGridSpawner gridSpawner = (PersistentGridSpawner)target;

        // Button to generate the grid in Edit Mode
        if (GUILayout.Button("Generate Grid"))
        {
            gridSpawner.GenerateGridInEditor();
        }

        // Button to clear the grid in Edit Mode
        if (GUILayout.Button("Clear Grid"))
        {
            gridSpawner.ClearGridInEditor();
        }
    }
}