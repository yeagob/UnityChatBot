using UnityEngine;
using UnityEditor;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Models.Tools.MCP;
using System.Collections.Generic;

namespace ChatSystem.Editor
{
    [CustomEditor(typeof(ToolConfig))]
    public class ToolConfigEditor : UnityEditor.Editor
    {
        private bool showFunctionDetails = true;
        private bool showParameterProperties = true;
        
        public override void OnInspectorGUI()
        {
            ToolConfig config = (ToolConfig)target;
            
            EditorGUILayout.LabelField("Tool Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
            config.toolId = EditorGUILayout.TextField("Tool ID", config.toolId);
            config.toolName = EditorGUILayout.TextField("Tool Name", config.toolName);
            config.toolType = (Enums.ToolType)EditorGUILayout.EnumPopup("Tool Type", config.toolType);
            
            EditorGUILayout.Space();
            showFunctionDetails = EditorGUILayout.Foldout(showFunctionDetails, "MCP Function Definition", true);
            
            if (showFunctionDetails)
            {
                EditorGUI.indentLevel++;
                
                if (config.function == null)
                {
                    config.function = new FunctionDefinition();
                }
                
                config.function.name = EditorGUILayout.TextField("Name", config.function.name);
                config.function.description = EditorGUILayout.TextField("Description", config.function.description);
                
                if (config.function.parameters == null)
                {
                    config.function.parameters = new ParameterDefinition();
                }
                
                EditorGUILayout.Space();
                showParameterProperties = EditorGUILayout.Foldout(showParameterProperties, "Parameters", true);
                
                if (showParameterProperties)
                {
                    EditorGUI.indentLevel++;
                    DrawPropertiesEditor(config.function.parameters);
                    DrawRequiredList(config.function.parameters);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Execution Settings", EditorStyles.boldLabel);
            config.enabled = EditorGUILayout.Toggle("Enabled", config.enabled);
            config.timeoutMs = EditorGUILayout.IntSlider("Timeout (ms)", config.timeoutMs, 100, 30000);
            config.maxRetries = EditorGUILayout.IntSlider("Max Retries", config.maxRetries, 0, 10);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Preview MCP Format"))
            {
                Debug.Log(config.GetMCPFormat());
            }
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }
        
        private void DrawPropertiesEditor(ParameterDefinition parameters)
        {
            if (parameters.properties == null)
            {
                parameters.properties = new Dictionary<string, PropertyDefinition>();
            }
            
            EditorGUILayout.LabelField("Properties:", EditorStyles.miniBoldLabel);
            
            List<string> keysToRemove = new List<string>();
            Dictionary<string, PropertyDefinition> propertiesToAdd = new Dictionary<string, PropertyDefinition>();
            
            foreach (KeyValuePair<string, PropertyDefinition> kvp in parameters.properties)
            {
                EditorGUILayout.BeginHorizontal();
                
                string newKey = EditorGUILayout.TextField(kvp.Key, GUILayout.Width(150));
                
                if (newKey != kvp.Key)
                {
                    keysToRemove.Add(kvp.Key);
                    propertiesToAdd[newKey] = kvp.Value;
                }
                
                if (kvp.Value == null)
                {
                    parameters.properties[kvp.Key] = new PropertyDefinition();
                }
                
                kvp.Value.type = EditorGUILayout.TextField(kvp.Value.type, GUILayout.Width(100));
                kvp.Value.description = EditorGUILayout.TextField(kvp.Value.description);
                
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    keysToRemove.Add(kvp.Key);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            foreach (string key in keysToRemove)
            {
                parameters.properties.Remove(key);
            }
            
            foreach (KeyValuePair<string, PropertyDefinition> kvp in propertiesToAdd)
            {
                parameters.properties[kvp.Key] = kvp.Value;
            }
            
            if (GUILayout.Button("Add Property"))
            {
                parameters.properties["newProperty"] = new PropertyDefinition { type = "string" };
            }
        }
        
        private void DrawRequiredList(ParameterDefinition parameters)
        {
            if (parameters.required == null)
            {
                parameters.required = new List<string>();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Required Parameters:", EditorStyles.miniBoldLabel);
            
            for (int i = 0; i < parameters.required.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                parameters.required[i] = EditorGUILayout.TextField(parameters.required[i]);
                
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    parameters.required.RemoveAt(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Required Parameter"))
            {
                parameters.required.Add("");
            }
        }
    }
}