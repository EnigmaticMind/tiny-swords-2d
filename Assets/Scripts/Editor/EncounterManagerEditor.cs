#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using TinySwords2D.Gameplay;
using TinySwords2D.Data;

[CustomEditor(typeof(EncounterManager))]
public class EncounterManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EncounterManager manager = (EncounterManager)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto-Load Encounters from Data/Encounters"))
        {
            LoadEncountersFromFolder(manager);
        }
    }

    private void LoadEncountersFromFolder(EncounterManager manager)
    {
        // Find all EncounterData assets in the specified folder
        string[] guids = AssetDatabase.FindAssets("t:EncounterData", new[] { "Assets/Scripts/Data/Encounters" });
        
        System.Collections.Generic.List<EncounterData> encounters = new System.Collections.Generic.List<EncounterData>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterData encounter = AssetDatabase.LoadAssetAtPath<EncounterData>(path);
            if (encounter != null)
            {
                encounters.Add(encounter);
            }
        }

        // Use reflection to set the private field, or make it public/protected
        SerializedProperty encountersProperty = serializedObject.FindProperty("allEncounters");
        encountersProperty.ClearArray();
        
        for (int i = 0; i < encounters.Count; i++)
        {
            encountersProperty.InsertArrayElementAtIndex(i);
            encountersProperty.GetArrayElementAtIndex(i).objectReferenceValue = encounters[i];
        }
        
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"EncounterManager: Loaded {encounters.Count} encounters from Assets/Scripts/Data/Encounters");
    }
}
#endif
