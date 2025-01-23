using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BrokenMaterialProcessor))]
public class BrokenMaterialProcessorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Find Broken Materials"))
        {
            FindBrokenMaterials();
        }
    }

    private static List<string> UltraleapPathIdentifiers= new List<string>() { "Ultraleap Tracking", "Ultraleap Tracking Preview", "com.ultraleap.tracking", "com.ultraleap.tracking.preview" };

    private static void FindBrokenMaterials()
    {
        //List<string> guids = AssetDatabase.FindAssets("t: material", new string[] { "Packages" }).ToList(); // Packages
        List<string> guids = AssetDatabase.FindAssets("t: material").ToList(); // Assets - including samples / examples

        string log = String.Empty;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (UltraleapPathIdentifiers.Any(i => path.Contains(i, System.StringComparison.OrdinalIgnoreCase)))
            {
                Material temp = (Material)AssetDatabase.LoadAssetAtPath(path, typeof(Material));
                //if (temp != null && (temp.shader.name == "" ||
                //    temp.shader.name == "Hidden/InternalErrorShader" ||
                //    temp.shader.name == null ||
                //    temp.shader.name == "Hidden/Universal Render Pipeline/FallbackError"))
                //// || temp.GetTag("RenderPipeline", false) == ""))
                {
                    string data =
                        $"{temp.name}, " +
                        $"{temp.shader.name}, " +
                        $"{path.Split('\\')[0]}, " +
                        $"{(path.Contains("example", System.StringComparison.OrdinalIgnoreCase) ? "Yes, " : "No, ")}" +
                        $"{temp.GetTag("RenderPipeline", false)}";
                    //    temp.shader = Shader.Find("Standard");

                    Debug.Log(data); 
                    
                    log +=(data + Environment.NewLine);   
                }
            }
        }

        if (log.Length > 0)
        {
            File.WriteAllText($"C:\\Users\\MaxPalmer\\Desktop\\ShaderLog.csv", log);
        }

        //AssetDatabase.SaveAssets();
    }
}