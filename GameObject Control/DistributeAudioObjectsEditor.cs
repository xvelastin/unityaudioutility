// This script must be placed in "/Assets/Editor" in order to work. If it is in a different folder, builds may fail.

using UnityEngine;
using UnityEditor;

/// <summary>
/// Optional lightweight custom editor that allows for calling DistributeSounds from the Inspector.
/// </summary>
[CustomEditor(typeof(DistributeAudioObjects))]
public class DistributeAudioObjectsEditor : Editor
{
    DistributeAudioObjects script;

    public override void OnInspectorGUI()
    {
        // Allows this to call the target functions
        if (!script)
        {
            script = target as DistributeAudioObjects;
        }

        // Display Buttons for functions
        if (GUILayout.Button("Distribute"))
        {
            script.DistributeSounds();
        }

        if (GUILayout.Button("Clear"))
        {
            script.ClearList();
        }

        base.OnInspectorGUI();
    }
}