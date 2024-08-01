/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
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

        [MenuItem("GameObject/Ultraleap/Tracking/XR Leap Provider Manager", false, 1),
            MenuItem("Ultraleap/Tracking/XR Leap Provider Manager", false, 1)]
        public static void CreateProviderXR()
        {
            CreatePrefab("XR Leap Provider Manager");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (XR)", false, 2),
            MenuItem("Ultraleap/Tracking/Service Provider (XR)", false, 2)]
        public static void CreateServiceProviderXR()
        {
            CreatePrefab("Service Provider (XR)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (OpenXR)", false, 3),
            MenuItem("Ultraleap/Tracking/Service Provider (OpenXR)", false, 3)]
        public static void CreateServiceProviderOpenXR()
        {
            CreatePrefab("Service Provider (OpenXR)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/XRHands Leap Provider", false, 4),
            MenuItem("Ultraleap/Tracking/XRHands Leap Provider", false, 4)]
        public static void CreateXRHandsLeapProvider()
        {
            CreatePrefab("XRHands Leap Provider");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (Desktop)", false, 101),
            MenuItem("Ultraleap/Tracking/Service Provider (Desktop)", false, 101)]
        public static void CreateServiceProviderDesktop()
        {
            CreatePrefab("Service Provider (Desktop)");
        }

        [MenuItem("GameObject/Ultraleap/Tracking/Service Provider (Screentop)", false, 102),
            MenuItem("Ultraleap/Tracking/Service Provider (Screentop)", false, 102)]
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
            bool created = false;

#if UNITY_URP_AVAILABLE
            var urpHands = CreatePrefab("Capsule Hands (URP) Variant");
            created = urpHands == null ? false : true;
#endif

            if (!created)
                CreatePrefab("CapsuleHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Low Poly Hands", false, 21),
            MenuItem("Ultraleap/Hands/Low Poly Hands", false, 21)]
        public static void CreateLowPolyHands()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("LowPolyHandsWithArms (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("LowPolyHandsWithArms");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Ghost Hands (with arms)", false, 22),
            MenuItem("Ultraleap/Hands/Ghost Hands (with arms)", false, 22)]
        public static void CreateGenericHand_Arm()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("Ghost Hands (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("GenericHand_Arm");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Ghost Hands", false, 23),
            MenuItem("Ultraleap/Hands/Ghost Hands", false, 23)]
        public static void CreateGhostHands()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("Ghost Hands (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("GhostHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Outline Hands", false, 24),
            MenuItem("Ultraleap/Hands/Outline Hands", false, 24)]
        public static void CreateOutlineHands()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("Outline Hands (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("OutlineHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Skeleton Hands", false, 25),
            MenuItem("Ultraleap/Hands/Skeleton Hands", false, 25)]
        public static void CreateSkeletonHands()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("Skeleton Hands (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("SkeletonHands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Ghost Capsule Hands", false, 26),
            MenuItem("Ultraleap/Hands/Ghost Capsule Hands", false, 26)]
        public static void CreateGhostCapsuleHands()
        {
            bool created = false;

#if UNITY_URP_AVAILABLE
            if(QualitySettings.renderPipeline != null) // Only use URP models when there is an active render pipeline
            {
                var urpHands = CreatePrefab("Ghost Hands (URP) Variant");
                created = urpHands == null ? false : true;
            }
#endif

            if (!created)
                CreatePrefab("Ghost Capsule Hands");
        }

        [MenuItem("GameObject/Ultraleap/Hands/Attachment Hands", false, 27),
            MenuItem("Ultraleap/Hands/Attachment Hands", false, 27)]
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

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Physical Hands Manager", false, 20),
            MenuItem("Ultraleap/Physical Interaction/Physical Hands Manager", false, 20)]
        public static void CreatePhysicalHandsManagerMenu()
        {
            GameObject physicalHandsManager = CreatePrefab("Physical Hands Manager");
            if (physicalHandsManager != null)
            {
                var physHandsManager = physicalHandsManager.GetComponent<Leap.PhysicalHands.PhysicalHandsManager>();
                // Ensure that there is a contact parent at runtime
                if (physHandsManager != null)
                {
                    physHandsManager.SetContactMode(physHandsManager.contactMode);
                }
            }
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Physical Hands Button", false, 35),
            MenuItem("Ultraleap/Physical Interaction/Physical Hands Button", false, 35)]
        public static void CreatePhysicalHandsButton()
        {
            CreatePrefab("Physical Hands Button");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Physical Hands Button Toggle", false, 36),
            MenuItem("Ultraleap/Physical Interaction/Physical Hands Button Toggle", false, 36)]
        public static void CreatePhysicalHandsToggle()
        {
            CreatePrefab("Physical Hands Button Toggle");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Physical Hands Slider", false, 37),
            MenuItem("Ultraleap/Physical Interaction/Physical Hands Slider", false, 37)]
        public static void CreatePhysicalHandsSlider()
        {
            CreatePrefab("Physical Hands Slider");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/3D UI Panel", false, 38),
            MenuItem("Ultraleap/Physical Interaction/3D UI Panel", false, 38)]
        public static void Create3DUIPanel()
        {
            CreatePrefab("Physical Hands 3D UI Panel");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Attachment Hand Menu", false, 50),
            MenuItem("Ultraleap/Physical Interaction/Attachment Hand Menu", false, 50)]
        public static void CreateAttachmentHandMenu()
        {
            CreatePrefab("Attachment Hand Menu");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Anchorable Object", false, 51),
            MenuItem("Ultraleap/Physical Interaction/Anchorable Object", false, 51)]
        public static void CreateAnchorableObject()
        {
            CreatePrefab("Anchorable Object");
        }

        [MenuItem("GameObject/Ultraleap/Physical Interaction/Anchor", false, 52),
            MenuItem("Ultraleap/Physical Interaction/Anchor", false, 52)]
        public static void CreateAnchor()
        {
            CreatePrefab("Anchor");
        }

        #endregion

        public static GameObject CreatePrefab(string prefabName)
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
                        return HandleObjectCreation(newObject);
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
                    return HandleObjectCreation(newObject);
                }
            }

            return null;
        }

        static GameObject HandleObjectCreation(GameObject gameObject)
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

            return gameObject;
        }
    }
}