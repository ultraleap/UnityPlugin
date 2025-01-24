using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
[InitializeOnLoad]
static class RenderPipelineValidation
{
    static RenderPipelineValidation()
    {
        foreach (var pipelineHandler in GetAllInstances())
            pipelineHandler.AutoRefreshMaterialShadersForPipeline();
    }

    static List<AutomaticRenderPipelineMaterialShaderUpdater> GetAllInstances()
    {
        var shaderUpdaterInstances = new List<AutomaticRenderPipelineMaterialShaderUpdater>();

        // Find all GUIDs for objects that match the type AutomaticRenderPipelineMaterialShaderUpdater
        var guids = AssetDatabase.FindAssets("t:AutomaticRenderPipelineMaterialShaderUpdater");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var asset = AssetDatabase.LoadAssetAtPath<AutomaticRenderPipelineMaterialShaderUpdater>(path);
            if (asset != null)
                shaderUpdaterInstances.Add(asset);
        }

        return shaderUpdaterInstances;
    }
}
#endif

[CreateAssetMenu(fileName = "AutomaticRenderPipelineMaterialShaderUpdater", menuName = "Ultraleap/AutomaticRenderPipelineMaterialShaderUpdater", order = 0)]
public class AutomaticRenderPipelineMaterialShaderUpdater : ScriptableObject
{ 
    [SerializeField]
    [Tooltip("List of comparable shaders and their associated render pipeline")]
    List<ShaderMapping> shaderMap = new List<ShaderMapping>()
    {
        new ShaderMapping() { UseBuiltInRenderPipelineShaderName = true, BuiltInRenderPipelineShaderName = "Standard", UseUniversalRenderPipelineShaderName = true, UniversalRenderPipelineShaderName = "Universal Render Pipeline/Lit"}
    };

    [SerializeField]
    [Tooltip("If true, all shaders in materials for the Ultraleap packages and samples will be refreshed automatically when the editor opens and when this scriptable object instance is enabled.")]
    bool autoRefreshMaterialShaders = true;

    private readonly List<string> ultraleapPathIdentifiers = new List<string>() { "Ultraleap Tracking", "Ultraleap Tracking Preview", "com.ultraleap.tracking", "com.ultraleap.tracking.preview" };

    private List<Material> ultraleapMaterialsCache = new List<Material>();

    public bool IsBuiltInRenderPipeline
    {
        get
        {
            return GraphicsSettings.currentRenderPipeline == null;
        }
    }

    public bool IsUniveralRenderPipeline
    {
        get
        {
            return GraphicsSettings.currentRenderPipeline.name.Contains("URP") ||
                GraphicsSettings.currentRenderPipeline.name == "UniveralRenderPipeline";
        }
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (Application.isPlaying)
            return;

        AutoRefreshMaterialShadersForPipeline();
    }
#endif

    public void AutoRefreshMaterialShadersForPipeline()
    {
        if (autoRefreshMaterialShaders)
        {
            UpdatePipelineShaders();
        }
    }

    /// <summary>
    /// Applies the appropriate shader to the materials based on the current render pipeline.
    /// </summary>
    public void UpdatePipelineShaders()
    {
        var materials = GetUltraleapMaterialsInPackagesAndAssets(false);

        foreach (Material material in materials)
        {
            if (!MaterialShaderMatchesActiveRenderPipeline(material))
            {
                UpdateMaterialShader(material);
            }
        }
    }

    private void UpdateMaterialShader(Material material)
    {
        Shader targetShader = GetShaderForPipeline(material);
        if (material.shader != targetShader && targetShader != null)
        {
            material.shader = targetShader;
            MarkMaterialModified(material);
        }
    }

    private Shader GetShaderForPipeline(Material material)
    {
        Shader shaderMatch = null;

        foreach (ShaderMapping shaderMapping in shaderMap)
        {
            if (IsBuiltInRenderPipeline)
            {
                shaderMatch = Shader.Find(shaderMapping.BuiltInRenderPipelineShaderName);
            }
            else if (IsUniveralRenderPipeline)
            {
                shaderMatch = Shader.Find(shaderMapping.UniversalRenderPipelineShaderName);
            }
        }

        return shaderMatch;
    }

    private bool MaterialShaderMatchesActiveRenderPipeline(Material material)
    {
        if (IsBuiltInRenderPipeline)
        {
            return material.GetTag("RenderPipeline", false) == "";
        }

        if (IsUniveralRenderPipeline)
        {
            return material.GetTag("RenderPipeline", false) == "UniversalRenderPipeline";
        }

        return true;
    }

    private List<Material> GetUltraleapMaterialsInPackagesAndAssets(bool useCache)
    {
        if (!useCache || ultraleapMaterialsCache.Count == 0)
        {
            ultraleapMaterialsCache.Clear();

            List<string> guids = AssetDatabase.FindAssets("t: material").ToList();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (ultraleapPathIdentifiers.Any(i => path.Contains(i, System.StringComparison.OrdinalIgnoreCase)))
                {
                    Material temp = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                    ultraleapMaterialsCache.Add(temp);
                }
            }
        }

        return ultraleapMaterialsCache;
    }

    static void MarkMaterialModified(Material material)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(material);
#endif
    }


    [System.Serializable]
    public class ShaderMapping
    {
        public bool UseBuiltInRenderPipelineShaderName = true;
        public string BuiltInRenderPipelineShaderName = "Standard";
        public Shader BuiltInRenderPipelineShader = null;

        public bool UseUniversalRenderPipelineShaderName = true;
        public string UniversalRenderPipelineShaderName = "Universal Render Pipeline/Lit";
        public Shader UniversalRenderPipelineShader = null;
    }


#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for the shader mappping class.
    /// </summary>
    [CustomPropertyDrawer(typeof(ShaderMapping))]
    public class ShaderMappingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty UseBiRPShaderNameProperty = property.FindPropertyRelative("UseBuiltInRenderPipelineShaderName");
            SerializedProperty BiRPShaderNameProperty = property.FindPropertyRelative("BuiltInRenderPipelineShaderName");
            SerializedProperty BiRPShaderProperty = property.FindPropertyRelative("BuiltInRenderPipelineShader");

            SerializedProperty UseURPShaderNameProperty = property.FindPropertyRelative("UseUniversalRenderPipelineShaderName");
            SerializedProperty URPShaderNameProperty = property.FindPropertyRelative("UniversalRenderPipelineShaderName");
            SerializedProperty URPShaderProperty = property.FindPropertyRelative("UniversalRenderPipelineShader");

            // Draw Material without the header.
            position.height = singleLineHeight;

            // Built in render pipeline section
            EditorGUI.LabelField(position, "Built-In Render Pipeline Shader", EditorStyles.boldLabel);
            position.y += singleLineHeight + verticalSpacing;
            EditorGUI.PropertyField(position, UseBiRPShaderNameProperty);
            position.y += singleLineHeight + verticalSpacing;
            if (UseBiRPShaderNameProperty.boolValue)
            {
                EditorGUI.PropertyField(position, BiRPShaderNameProperty);
                position.y += singleLineHeight + verticalSpacing;
            }
            else
            {
                EditorGUI.PropertyField(position, BiRPShaderProperty);
                position.y += singleLineHeight + verticalSpacing;
            }

            // URP section
            EditorGUI.LabelField(position, "Universal Render Pipeline Shader", EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + verticalSpacing;
            EditorGUI.PropertyField(position, UseURPShaderNameProperty);
            position.y += singleLineHeight + verticalSpacing;
            if (UseURPShaderNameProperty.boolValue)
            {
                EditorGUI.PropertyField(position, URPShaderNameProperty);
                position.y += singleLineHeight + verticalSpacing;
            }
            else
            {
                EditorGUI.PropertyField(position, URPShaderProperty);
                position.y += singleLineHeight + verticalSpacing;
            }
            
            // Draw a separator line at the end.
            position.y += verticalSpacing / 2; // Extra space for the line.
            position.height = 1;
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.gray);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int baseFieldCount = 3; // The Material field, the two toggles, and one for an optional field.
            int extraLineCount = property.FindPropertyRelative("UseBuiltInRenderPipelineShaderName").boolValue ? 0 : 1;
            extraLineCount += property.FindPropertyRelative("UseUniversalRenderPipelineShaderName").boolValue ? 0 : 1;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = EditorGUIUtility.standardVerticalSpacing;
            float headerHeight = EditorGUIUtility.singleLineHeight; // No longer need extra height for headers.
            // Calculate height for fields and headers
            float fieldsHeight = baseFieldCount * singleLineHeight + (baseFieldCount - 1 + extraLineCount) * verticalSpacing;
            // Allow space for header, separator line, and a bit of padding before the line.
            float headersHeight = 2 * (headerHeight + verticalSpacing);
            float separatorSpace = verticalSpacing / 2 + 1; // Additional vertical spacing and line height.
            return fieldsHeight + headersHeight + separatorSpace + singleLineHeight * 1.5f;
        }
    }

    /// <summary>
    /// Custom editor MaterialPipelineHandler
    /// </summary>
    [CustomEditor(typeof(AutomaticRenderPipelineMaterialShaderUpdater)), CanEditMultipleObjects]
    public class AutomaticRenderPipelineMaterialShaderUpdaterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // Draw the "Refresh Shaders" button
            if (GUILayout.Button("Refresh Shaders"))
            {
                foreach (var t in targets)
                {
                    var handler = (AutomaticRenderPipelineMaterialShaderUpdater)t;
                    handler.UpdatePipelineShaders();
                }
            }
        }
    }
}
#endif
