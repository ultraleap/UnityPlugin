using Leap;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        GetAllInstances();
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

public enum StandardShaderBlendMode
{
    Opaque,
    Cutout,
    Fade,        // Old school alpha-blending mode, fresnel does not affect amount of transparency
    Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
}

/// <summary>
/// <see langword="class"> used to capture shader state information </see>
/// </summary>
public class ShaderState
{
    public Color Colour;
    public float Smoothness;

    public bool HasMainTexture;
    public Texture MainTexture;

    public float BlendMode;
}

[CreateAssetMenu(fileName = "Automatic Render Pipeline Material Shader Updater", menuName = "Ultraleap/AutomaticRenderPipelineMaterialShaderUpdater", order = 0)]
[Serializable]
public class AutomaticRenderPipelineMaterialShaderUpdater : ScriptableObject
{ 
    [SerializeField]
    [Tooltip("List of comparable shaders and their associated render pipeline")]
    List<ShaderMapping> shaderMap = new List<ShaderMapping>()
    {
        new ShaderMapping() { 
            UseBuiltInRenderPipelineShaderName = true, 
            BuiltInRenderPipelineShaderName = "Standard", 
            OnBeforeConversionToBiRP = OnBeforeConversionFromToBiRPStandard, 
            OnAfterConversionToBiRP = OnAfterConversionToBiRPStandard,

            UseUniversalRenderPipelineShaderName = true, 
            UniversalRenderPipelineShaderName = "Universal Render Pipeline/Lit",
            OnBeforeConversionToURP = OnBeforeConversionToURPLit,
            OnAfterConversionToURP = OnAfterConversionToURPLit},

        new ShaderMapping()
        {
            UseBuiltInRenderPipelineShaderName = true,
            BuiltInRenderPipelineShaderName = "Leagacy Shaders/Diffuse",
            //OnBeforeConversionToBiRP = OnBeforeConversionFromToBiRPStandard,
            //OnAfterConversionToBiRP = OnAfterConversionToBiRPStandard,

            UseUniversalRenderPipelineShaderName = true,
            UniversalRenderPipelineShaderName = "Universal Render Pipeline/Simple Lit",
            //OnBeforeConversionToURP = OnBeforeConversionToURPLit,
            //OnAfterConversionToURP = OnAfterConversionToURPLit}
        }
    };


    [Tooltip("If true, the user will be prompted to confirm the conversion, even if automatic is on. This is to prevent unwanted upgrades")]
    public bool PromptUserToConfirmConversion = true;

    [SerializeField]
    //[HideInInspector]
    public int NumberOfTimesUserRejectedPrompt = 0;

    [SerializeField]
    //[HideInInspector]
    public bool AutomaticConversionIsOffForPluginInProject = false;

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
            return GraphicsSettings.currentRenderPipeline != null; // Will need to change this when we support HDRP
        }
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (Application.isPlaying)
            return;

        AutoRefreshMaterialShadersForPipeline();
    }

    public void AutoRefreshMaterialShadersForPipeline(bool silentMode = false)
    {
        if (UltraleapSettings.AutomaticallyUpgradeMaterialsToCurrentRenderPipeline &&
            AutomaticConversionIsOffForPluginInProject == false &&
            FoundPluginMaterialsThatDontMatchCurrentRenderPipeline())
        {
            bool goAhead = false;

            if (PromptUserToConfirmConversion && !silentMode)
            {
                if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs)
                {
                    int option = EditorUtility.DisplayDialogComplex("Convert Ultraleap Plugin Materials",
                        "Materials have been detected in the Ultraleap plugin that don't match the current project's chosen render pipeline." +
                        "Would you like to convert these materials to the current render pipeline?",
                        "Yes, but don't ask each time",
                        "No",
                        "Yes and ask next time too");

                    switch (option)
                    {
                        // OK - "Yes, but don't ask each time"
                        case 0:
                            goAhead = true;
                            PromptUserToConfirmConversion = false;
                            break;

                        // Cancel - "No"
                        case 1:
                            goAhead = false;
                            NumberOfTimesUserRejectedPrompt++;
                            break;

                        // Alt - "Yes and ask next time too"
                        case 2: 
                            goAhead = true;
                            break;

                        default:
                            goAhead = false;
                            break;
                    }

                    // Let's give the user the chance to stop being nagged ...
                    if (NumberOfTimesUserRejectedPrompt >= 2 && NumberOfTimesUserRejectedPrompt <=5)
                    {
                        if (EditorUtility.DisplayDialog("Convert Ultraleap Plugin Materials", "You've said no to converting materials a few times now. Do you want to turn this (automatic) feature off?", "Yes", "No"))
                        {
                            AutomaticConversionIsOffForPluginInProject = true;
                        }
                    }

                    // Save this change ...
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                }
            }
            else if (!PromptUserToConfirmConversion)
            {
                goAhead = true;
            }

            if (goAhead)
            {
                UpdatePipelineShaders();
            }
        }
    }

    private bool FoundPluginMaterialsThatDontMatchCurrentRenderPipeline()
    {
        var materials = GetUltraleapMaterialsInPackagesAndAssets(false);

        foreach (Material material in materials)
        {
            if (!MaterialShaderMatchesActiveRenderPipeline(material)) 
            {
                return true;
            }
        }

        return false;
    }

#endif

    /// <summary>
    /// Applies the appropriate shader to the materials based on the current render pipeline.
    /// </summary>
    public void UpdatePipelineShaders()
    {
        var materials = GetUltraleapMaterialsInPackagesAndAssets(false);
        int count = 0;

        foreach (Material material in materials)
        {
            if (!MaterialShaderMatchesActiveRenderPipeline(material))
            {
                UpdateMaterialShader(material);
                count++;    
            }
        }


#if UNITY_EDITOR
        if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs)
        {
            EditorUtility.DisplayDialog("Material Upgrade Status", $"Upgraded {count} materials to the current render pipeline", "OK");
        }
#endif

    }

    private void UpdateMaterialShader(Material material)
    {
        // A potential improvement for conversion might be to use the render pipeline converter API discussed here:
        // NB that will only work on non custom shaders.
        // https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@15.0/manual/features/rp-converter.html

        var shaderMatch = GetShaderForPipeline(material);
        if (shaderMatch.foundMatch && material.shader != shaderMatch.shader && shaderMatch.shader != null)
        {
            ShaderState state = null;

            if (IsBuiltInRenderPipeline && shaderMatch.mapping.OnBeforeConversionToBiRP != null)
            {
                state = shaderMatch.mapping.OnBeforeConversionToBiRP(material, shaderMatch.shader);
            }
            else if (IsUniveralRenderPipeline && shaderMatch.mapping.OnBeforeConversionToURP != null)
            {
                state = shaderMatch.mapping.OnBeforeConversionToURP(material, shaderMatch.shader);
            }

            material.shader = shaderMatch.shader;

            if (IsBuiltInRenderPipeline && shaderMatch.mapping.OnAfterConversionToBiRP != null)
            {
                shaderMatch.mapping.OnAfterConversionToBiRP(state, material);
            }
            else if (IsUniveralRenderPipeline && shaderMatch.mapping.OnAfterConversionToURP != null)
            {
                shaderMatch.mapping.OnAfterConversionToURP(state, material);
            }

            MarkMaterialModified(material);
        }
    }

    private (bool foundMatch, Shader shader, ShaderMapping mapping) GetShaderForPipeline(Material material)
    {
        Shader shaderMatch = null;
        ShaderMapping shaderMappingMatch = null;
        bool foundMatch = false;    

        foreach (ShaderMapping shaderMapping in shaderMap)
        {
            if (CurrentMaterialShaderIsAMappingMatch(material, shaderMapping))
            {
                if (IsBuiltInRenderPipeline)
                {
                    if (shaderMapping.UseBuiltInRenderPipelineShaderName && shaderMapping.BuiltInRenderPipelineShaderName != null)
                    {
                        shaderMatch = Shader.Find(shaderMapping.BuiltInRenderPipelineShaderName);
                        shaderMappingMatch = shaderMapping;
                        foundMatch = true;
                        break;
                    }
                    else if (!shaderMapping.UseBuiltInRenderPipelineShaderName && material.shader != shaderMapping.BuiltInRenderPipelineShader)
                    {
                        shaderMatch = shaderMapping.BuiltInRenderPipelineShader;
                        shaderMappingMatch = shaderMapping;
                        foundMatch = true;
                        break;
                    }
                }
                else if (IsUniveralRenderPipeline)
                {
                    if (shaderMapping.UseUniversalRenderPipelineShaderName && shaderMapping.UniversalRenderPipelineShaderName != material.shader.name)
                    {
                        shaderMatch = Shader.Find(shaderMapping.UniversalRenderPipelineShaderName);
                        shaderMappingMatch = shaderMapping;
                        foundMatch = true;
                        break;
                    }
                    else if (!shaderMapping.UseUniversalRenderPipelineShaderName && material.shader != shaderMapping.UniversalRenderPipelineShader)
                    {
                        shaderMatch = shaderMapping.UniversalRenderPipelineShader;
                        shaderMappingMatch = shaderMapping;
                        foundMatch = true;
                        break;
                    }
                }
            }
        }

        return (foundMatch, shaderMatch, shaderMappingMatch);

        // Does the current map entry match the shader that needs to be converted?
        bool CurrentMaterialShaderIsAMappingMatch(Material material, ShaderMapping shaderMapping)
        {
            if (IsBuiltInRenderPipeline)
            {
                if (shaderMapping.UseUniversalRenderPipelineShaderName && shaderMapping.UniversalRenderPipelineShaderName == material.shader.name)
                {
                    return true;
                }
                else if (!shaderMapping.UseUniversalRenderPipelineShaderName && material.shader == shaderMapping.UniversalRenderPipelineShader)
                {
                    return true;
                }
                    
            }
            else if (IsUniveralRenderPipeline)
            {
                if (shaderMapping.UseBuiltInRenderPipelineShaderName && shaderMapping.BuiltInRenderPipelineShaderName == material.shader.name)
                {
                    return true;
                }
                else if (!shaderMapping.UseBuiltInRenderPipelineShaderName && material.shader == shaderMapping.BuiltInRenderPipelineShader)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private bool MaterialShaderMatchesActiveRenderPipeline(Material material)
    {
        if (IsBuiltInRenderPipeline)
        {
            return material.GetTag("RenderPipeline", false) == "";
        }

        if (IsUniveralRenderPipeline)
        {
            return material.GetTag("RenderPipeline", true) == "UniversalPipeline";
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

    public static ShaderState OnBeforeConversionToURPLit(Material material, Shader urpShader)
    {
        if (material.shader != null)
        {
            ShaderState state = new ShaderState();

            try
            {
                // Colour data
                state.Colour = material.GetColor("_Color");
                state.Smoothness = material.GetFloat("_Glossiness"); // _Glossiness maps to smoothness, which is used in the editor
               
                if (material.mainTexture != null)
                {
                    state.HasMainTexture = true;
                    state.MainTexture = material.mainTexture;    
                }
                else
                {
                    state.HasMainTexture = false;   
                }

                // Blend mode data
                state.BlendMode = material.GetFloat("_Mode");

                // These values for transparency / opaque appear common beteween render pipelines and don't appear to need converting:
                // state.SrcBlend = material.GetInt("_SrcBlend");
                // state.DstBlend = material.GetInt("_DstBlend");
                // state.ZWrite = material.GetInt("_ZWrite");
                // state.IsAlphatestOn = material.IsKeywordEnabled("_ALPHATEST_ON");
                // state.IsAplhaBlendOn = material.IsKeywordEnabled("_ALPHABLEND_ON");
                // state.IsAplhaPremultiplyOn = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            } 

            return state;
        }

        return null;
    }

    public static void OnAfterConversionToURPLit(ShaderState state, Material material)
    {
        if (material != null && state != null)
        {
            try
            { 
                // Convert the colour channel
                material.SetColor("_BaseColor", state.Colour);

                if (state.HasMainTexture)
                {
                    material.mainTexture = state.MainTexture;
                }

                material.SetFloat("_Smoothness", state.Smoothness); 

                // Texture not required, at least for our plugin materials:
                // material.SetColor("_Color", state.colour);
                // var texture = new Texture2D(1, 1);
                // texture.SetPixel(0,0,state.colour); 

                // Several parameter relate to the blend mode / transparency of a meterial,
                // but appear common to BiRP and URP (see OnBeforeConversionToURPLit).
                StandardShaderBlendMode blendMode = (StandardShaderBlendMode) (int) state.BlendMode;

                switch (blendMode)
                {
                    case StandardShaderBlendMode.Opaque:
                        material.SetOverrideTag("RenderType", "");
                        material.renderQueue = -1;
                        break;

                    case StandardShaderBlendMode.Cutout:
                        material.SetFloat("_Blend", 0);
                        material.SetFloat("_Surface", 1);
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        break;

                    case StandardShaderBlendMode.Fade:
                        material.SetFloat("_Blend", 0);
                        material.SetFloat("_Surface", 1);
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        break;

                    case StandardShaderBlendMode.Transparent:
                        material.SetFloat("_Blend", 0);
                        material.SetFloat("_Surface", 1);
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                        if (material.name == "Glass")
                        {
                            // Make the Physical Hands Glass Material more see-through and reflective to match the BiRP appearance more closely
                            material.SetFloat("_Smoothness", 1.0f);
                            material.SetFloat("_Metallic", 0.1f);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public static ShaderState OnBeforeConversionFromToBiRPStandard(Material material, Shader urpShader)
    {
        // Do nothing
        return null;
    }

    public static void OnAfterConversionToBiRPStandard(ShaderState state, Material material)
    {
        // Do nothing
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
        public Func<Material, Shader, ShaderState> OnBeforeConversionToBiRP; // State, material, new shader
        public Action<ShaderState, Material> OnAfterConversionToBiRP;        // Carried over state, updated material

        public bool UseUniversalRenderPipelineShaderName = true;
        public string UniversalRenderPipelineShaderName = "Universal Render Pipeline/Lit";
        public Shader UniversalRenderPipelineShader = null;
        public Func<Material, Shader, ShaderState> OnBeforeConversionToURP; // State, material, new shader
        public Action<ShaderState, Material> OnAfterConversionToURP;        // Carried over state, updated material
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
    /// Custom editor AutomaticRenderPipelineMaterialShaderUpdater
    /// </summary>
    [CustomEditor(typeof(AutomaticRenderPipelineMaterialShaderUpdater)), CanEditMultipleObjects]
    public class AutomaticRenderPipelineMaterialShaderUpdaterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // Draw the "Refresh Shaders" button
            if (GUILayout.Button("Force Material Update for Render Pipeline"))
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
