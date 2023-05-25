/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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
        #region Providers

        [MenuItem("GameObject/Ultraleap/Tracking/XR Leap Provider Manager", false, 20),
            MenuItem("Ultraleap/Tracking/XR Leap Provider Manager", false, 20)]
        public static void CreateProviderXR()
        {
            CreatePrefab("XR Leap Provider Manager");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (XR)", false, 31),
            MenuItem("Ultraleap/Tracking/Service Provider (XR)", false, 31)]
        public static void CreateServiceProviderXR()
        {
            CreatePrefab("Service Provider (XR)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (OpenXR)", false, 32),
            MenuItem("Ultraleap/Tracking/Service Provider (OpenXR)", false, 32)]
        public static void CreateServiceProviderOpenXR()
        {
            CreatePrefab("Service Provider (OpenXR)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (Desktop)", false, 45),
            MenuItem("Ultraleap/Tracking/Service Provider (Desktop)", false, 45)]
        public static void CreateServiceProviderDesktop()
        {
            CreatePrefab("Service Provider (Desktop)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (Screentop)", false, 46),
            MenuItem("Ultraleap/Tracking/Service Provider (Screentop)", false, 46)]
        public static void CreateServiceProviderScreentop()
        {
            CreatePrefab("Service Provider (Screentop)");
        }

        #endregion

        #region Hands

        [MenuItem("GameObject/Ultraleap/Hands/Capsule Hands", false, 20),
            MenuItem("Ultraleap/Hands/Capsule Hands", false, 20)]
        public static void CreateCapsuleHands()
        {
            CreatePrefab("CapsuleHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Low Poly Hands", false, 21),
            MenuItem("Ultraleap/Hands/Low Poly Hands", false, 21)]
        public static void CreateLowPolyHands()
        {
            CreatePrefab("LowPolyHandsWithArms");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Ghost Hands (with arms)", false, 22),
            MenuItem("Ultraleap/Hands/Ghost Hands (with arms)", false, 22)]
        public static void CreateGenericHand_Arm()
        {
            CreatePrefab("GenericHand_Arm");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Ghost Hands", false, 23),
            MenuItem("Ultraleap/Hands/Ghost Hands", false, 23)]
        public static void CreateGhostHands()
        {
            CreatePrefab("GhostHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Outline Hands", false, 24),
            MenuItem("Ultraleap/Hands/Outline Hands", false, 24)]
        public static void CreateOutlineHands()
        {
            CreatePrefab("OutlineHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Skeleton Hands", false, 25),
            MenuItem("Ultraleap/Hands/Skeleton Hands", false, 25)]
        public static void CreateSkeletonHands()
        {
            CreatePrefab("SkeletonHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Attachment Hands", false, 26),
            MenuItem("Ultraleap/Hands/Attachment Hands", false, 26)]
        public static void CreateAttachmentHands()
        {
            CreatePrefab("Attachment Hands");
        }

        #endregion

        #region Pose Detection

        [MenuItem("GameObject/Ultraleap/Pose Detection/Pose Detector", false, 20),
            MenuItem("Ultraleap/Pose Detection/Pose Detector", false, 20)]
        public static void CreatePoseDetectorCapsuleHands()
        {
            CreatePrefab("Pose Detector");
        }

        #endregion

        #region Interaction

        [MenuItem("GameObject/Ultraleap/Interaction/Interaction Manager", false, 20),
            MenuItem("Ultraleap/Interaction/Interaction Manager", false, 20)]
        public static void CreateInteractionManager()
        {
            CreatePrefab("Ultraleap Interaction Manager");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/Interaction Cube", false, 21),
            MenuItem("Ultraleap/Interaction/Interaction Cube", false, 21)]
        public static void CreateInteractionCube()
        {
            CreatePrefab("Interaction Cube");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/3D Button", false, 22),
            MenuItem("Ultraleap/Interaction/3D Button", false, 22)]
        public static void CreateSimple3dButton()
        {
            CreatePrefab("3D Button");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/3D UI Panel", false, 24),
            MenuItem("Ultraleap/Interaction/3D UI Panel", false, 24)]
        public static void Create3dUI()
        {
            CreatePrefab("3D UI Panel");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/Attachment Hand Menu", false, 25),
            MenuItem("Ultraleap/Interaction/Attachment Hand Menu", false, 25)]
        public static void CreateAttachmentHandMenu()
        {
            CreatePrefab("Attachment Hand Menu");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/Anchorable Object", false, 26),
            MenuItem("Ultraleap/Interaction/Anchorable Object", false, 26)]
        public static void CreateAnchorableObject()
        {
            CreatePrefab("Anchorable Object");
        }

        [MenuItem("GameObject/Ultraleap/Interaction/Anchor", false, 27),
            MenuItem("Ultraleap/Interaction/Anchor", false, 27)]
        public static void CreateAnchor()
        {
            CreatePrefab("Anchor");
        }

        #endregion

        public static void CreatePrefab(string prefabName)
        {
            var guids = AssetDatabase.FindAssets(prefabName);

            // look for exact matched first
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.Contains("Ultraleap") && !assetPath.Contains("ultraleap"))
                {
                    continue;
                }

                string[] assetPathSplit = assetPath.Split('/', '\\', '.');

                if (assetPathSplit[assetPathSplit.Length - 2] == prefabName && assetPathSplit[assetPathSplit.Length - 1] == "prefab")
                {
                    GameObject newObject = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));

                    if (newObject != null)
                    {
                        HandleObjectCreation(newObject);
                        return;
                    }
                }
            }

            // fallback to near-matches
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