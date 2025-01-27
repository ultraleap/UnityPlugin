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

public enum StandardShaderBlendMode
{
    Opaque,
    Cutout,
    Fade,        // Old school alpha-blending mode, fresnel does not affect amount of transparency
    Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
}


[CreateAssetMenu(fileName = "Automatic Render Pipeline Material Shader Updater", menuName = "Ultraleap/AutomaticRenderPipelineMaterialShaderUpdater", order = 0)]
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
            OnAfterConversionToURP = OnAfterConversionToURPLit}
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
        var shaderMatch = GetShaderForPipeline(material);
        if (shaderMatch.foundMatch && material.shader != shaderMatch.shader && shaderMatch.shader != null)
        {
            ExpandoObject state = null;

            if (IsBuiltInRenderPipeline)
            {
                state = shaderMatch.mapping.OnBeforeConversionToBiRP(material, shaderMatch.shader);
            }
            else if (IsUniveralRenderPipeline)
            {
                state = shaderMatch.mapping.OnBeforeConversionToURP(material, shaderMatch.shader);
            }

            material.shader = shaderMatch.shader;

            if (IsBuiltInRenderPipeline)
            {
                shaderMatch.mapping.OnAfterConversionToBiRP(state, material);
            }
            else if (IsUniveralRenderPipeline)
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

    public static ExpandoObject OnBeforeConversionToURPLit(Material material, Shader urpShader)
    {
        if (material.shader != null)
        {
#if NET_4_6 || NET_UNITY_4_8
            dynamic state = new ExpandoObject();

            try
            {
                // Colour data
                state.colour = material.GetColor("_Color");

                // Blend mode data
                state.blendMode = material.GetFloat("_Mode");

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
#else

#endif
        }

        return new ExpandoObject();
    }

#if NET_4_6 || NET_UNITY_4_8

    public static void OnAfterConversionToURPLit(dynamic state, Material material)
    {
        if (material != null && state != null)
        {
            try
            { 
                // Convert the colour channel
                material.SetColor("_BaseColor", state.colour);

                // Texture not required, at least for our plugin materials:
                // material.SetColor("_Color", state.colour);
                // var texture = new Texture2D(1, 1);
                // texture.SetPixel(0,0,state.colour); 

                // Several parameter relate to the blend mode / transparency of a meterial,
                // but appear common to BiRP and URP (see OnBeforeConversionToURPLit).
                StandardShaderBlendMode blendMode = (StandardShaderBlendMode) (int) state.blendMode;

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
#else
    public static void OnAfterConversionToURPLit(dynamic state, Material material)
    {
        if (material != null && state != null && state.Count() > 0)
        {
            try
            { 
                material.SetColor("_Color", state.colour);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
#endif

    public static ExpandoObject OnBeforeConversionFromToBiRPStandard(Material material, Shader urpShader)
    {
        // Do nothing
        return null;
    }

    public static void OnAfterConversionToBiRPStandard(ExpandoObject state, Material material)
    {
        // Do nothing
    }

    static void MarkMaterialModified(Material material)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(material);
#endif
    }

    //public class DisplayController : MonoBehaviour
    //{
    //    public enum SurfaceType
    //    {
    //        Opaque,
    //        Transparent
    //    }

    //    public enum BlendMode
    //    {
    //        Alpha,
    //        Premultiply,
    //        Additive,
    //        Multiply
    //    }

    //    public Material wallMaterial;


    //    public void ChangeWallTransparency(bool transparent)
    //    {
    //        if (transparent)
    //        {
    //            wallMaterial.SetFloat("_Surface", (float)SurfaceType.Transparent);
    //            wallMaterial.SetFloat("_Blend", (float)BlendMode.Alpha);
    //        }
    //        else
    //        {
    //            wallMaterial.SetFloat("_Surface", (float)SurfaceType.Opaque);
    //        }
    //        SetupMaterialBlendMode(wallMaterial);
    //    }

    //    void SetupMaterialBlendMode(Material material)
    //    {
    //        if (material == null)
    //            throw new ArgumentNullException("material");

    //        bool alphaClip = material.GetFloat("_AlphaClip") == 1;
    //        if (alphaClip)
    //            material.EnableKeyword("_ALPHATEST_ON");
    //        else
    //            material.DisableKeyword("_ALPHATEST_ON");

    //        SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
    //        if (surfaceType == 0)
    //        {
    //            material.SetOverrideTag("RenderType", "");
    //            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
    //            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
    //            material.SetInt("_ZWrite", 1);
    //            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    //            material.renderQueue = -1;
    //            material.SetShaderPassEnabled("ShadowCaster", true);
    //        }
    //        else
    //        {
    //            BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
    //            switch (blendMode)
    //            {
    //                case BlendMode.Alpha:
    //                    material.SetOverrideTag("RenderType", "Transparent");
    //                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    //                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    //                    material.SetInt("_ZWrite", 0);
    //                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    //                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    //                    material.SetShaderPassEnabled("ShadowCaster", false);
    //                    break;
    //                case BlendMode.Premultiply:
    //                    material.SetOverrideTag("RenderType", "Transparent");
    //                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
    //                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    //                    material.SetInt("_ZWrite", 0);
    //                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
    //                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    //                    material.SetShaderPassEnabled("ShadowCaster", false);
    //                    break;
    //                case BlendMode.Additive:
    //                    material.SetOverrideTag("RenderType", "Transparent");
    //                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
    //                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
    //                    material.SetInt("_ZWrite", 0);
    //                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    //                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    //                    material.SetShaderPassEnabled("ShadowCaster", false);
    //                    break;
    //                case BlendMode.Multiply:
    //                    material.SetOverrideTag("RenderType", "Transparent");
    //                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
    //                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
    //                    material.SetInt("_ZWrite", 0);
    //                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    //                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    //                    material.SetShaderPassEnabled("ShadowCaster", false);
    //                    break;
    //            }
    //        }
    //    }
    //}

    [System.Serializable]
    public class ShaderMapping
    {
        public bool UseBuiltInRenderPipelineShaderName = true;
        public string BuiltInRenderPipelineShaderName = "Standard";
        public Shader BuiltInRenderPipelineShader = null;
        public Func<Material, Shader, ExpandoObject> OnBeforeConversionToBiRP; // State, material, new shader
        public Action<ExpandoObject, Material> OnAfterConversionToBiRP;        // Carried over state, updated material

        public bool UseUniversalRenderPipelineShaderName = true;
        public string UniversalRenderPipelineShaderName = "Universal Render Pipeline/Lit";
        public Shader UniversalRenderPipelineShader = null;
        public Func<Material, Shader, ExpandoObject> OnBeforeConversionToURP; // State, material, new shader
        public Action<ExpandoObject, Material> OnAfterConversionToURP;        // Carried over state, updated material
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
