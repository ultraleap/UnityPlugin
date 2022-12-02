/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Leap
{
    public class PrefabCreateMenu
    {
        [MenuItem("GameObject/Ultraleap/XR/XR Leap Provider Manager", false, 20)]
        public static void CreateProviderXR()
        {
            CreatePrefab("XR Leap Provider Manager");
        }

        [MenuItem("GameObject/Ultraleap/XR/Service Provider (XR)", false, 31)]
        public static void CreateServiceProviderXR()
        {
            CreatePrefab("Service Provider (XR)");
        }

        [MenuItem("GameObject/Ultraleap/XR/Service Provider (OpenXR)", false, 32)]
        public static void CreateServiceProviderOpenXR()
        {
            CreatePrefab("Service Provider (OpenXR)");
        }

        [MenuItem("GameObject/Ultraleap/Non-XR/Service Provider (Desktop)", false, 43)]
        public static void CreateServiceProviderDesktop()
        {
            CreatePrefab("Service Provider (Desktop)");
        }

        [MenuItem("GameObject/Ultraleap/Non-XR/Service Provider (Screentop)", false, 44)]
        public static void CreateServiceProviderScreentop()
        {
            CreatePrefab("Service Provider (Screentop)");
        }

        public static void CreatePrefab(string prefabName)
        {
            var guids = AssetDatabase.FindAssets(prefabName);

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject newObject = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));

                if (newObject != null)
                {
                    HandleObjectCreation(newObject);
                    break;
                }
            }
        }

        static void HandleObjectCreation(GameObject gameObject)
        {
            gameObject = PrefabUtility.InstantiatePrefab(gameObject) as GameObject;

            // Find location
            SceneView lastView = SceneView.lastActiveSceneView;
            gameObject.transform.position = lastView ? lastView.pivot : Vector3.zero;

            // Make sure we place the object in the proper scene, with a relevant name
            StageUtility.PlaceGameObjectInCurrentStage(gameObject);
            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);

            // Record undo, and select
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create Object: {gameObject.name}");

            if (Selection.activeGameObject != null)
            {
                gameObject.transform.parent = Selection.activeGameObject.transform;
            }

            Selection.activeGameObject = gameObject;

            // For prefabs, let's mark the scene as dirty for saving
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}