/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.HandsModule
{
    /// <summary>
    /// This script makes it easy to swap between two hand meshes. 
    /// It is meant to be used with one hand mesh that includes an arm and one that doesn't.
    /// It attempts to automatically find the meshes based on their names when adding this component or when resetting it.
    /// </summary>
    [ExecuteInEditMode]
    public class ShowArm : MonoBehaviour
    {
        public GameObject meshWithArm;
        public GameObject meshWithoutArm;

        /// <summary>
        /// Toggles the arm.
        /// If true, the mesh without an arm is deactivated and the mesh with an arm is activated
        /// </summary>
        public bool showArm = true;

        bool currentlyShowingArm;


        // Update is called once per frame
        void Update()
        {
            if (showArm != currentlyShowingArm)
            {
                UpdateArm();
            }
        }

        private void Reset()
        {
            AttemptToFindMeshes();

            showArm = true;

            UpdateArm();
        }

        void AttemptToFindMeshes()
        {
            meshWithArm = null;
            meshWithoutArm = null;
            SkinnedMeshRenderer[] meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer.gameObject.name.ToLower().Contains("arm"))
                {
                    if (meshWithArm == null)
                    {
                        meshWithArm = meshRenderer.gameObject;
                    }
                }
                else if (meshWithoutArm == null)
                {
                    meshWithoutArm = meshRenderer.gameObject;
                }
            }
        }

        void UpdateArm()
        {
            if (meshWithArm == null || meshWithoutArm == null)
            {
                return;
            }

            if (showArm)
            {
                meshWithArm.SetActive(true);
                meshWithoutArm.SetActive(false);
            }
            else
            {
                meshWithArm.SetActive(false);
                meshWithoutArm.SetActive(true);
            }

            currentlyShowingArm = showArm;
        }
    }
}