/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public class HandFadeInAtDistanceFromRealData : MonoBehaviour
    {
        public Chirality chirality;

        [Tooltip("The name of the color property you woud like to fade on the hand shader")]
        public string shaderFadingColorName = "_Color";

        PhysicalHandsManager physManager;
        Renderer rendererToChange;
        HardContactHand hardContactHand;

        void Start()
        {
            FindContactHand();

            rendererToChange = GetComponentInChildren<Renderer>();

            if (rendererToChange != null && rendererToChange.material.HasVector(shaderFadingColorName))
            {
                Vector4 currentColor = rendererToChange.material.GetVector(shaderFadingColorName);
                currentColor[3] = 0;
                rendererToChange.material.SetVector(shaderFadingColorName, currentColor);
            }
            else
            {
                rendererToChange = null;
                Debug.LogWarning($"The Renderer's Material for " + gameObject.name + " does not have the color property named _Color and cannot be faded.", gameObject);
            }
        }

        void Update()
        {
            FindContactHand();

            if (hardContactHand != null && rendererToChange != null)
            {
                Vector4 currentColor = rendererToChange.material.GetVector(shaderFadingColorName);
                float mappedData = Leap.Unity.Utils.Map01(hardContactHand.DistanceFromDataHand, 0, hardContactHand.hardContactParent.teleportDistance);
                currentColor[3] = Mathf.Clamp01(mappedData) + 0.05f;
                rendererToChange.material.SetVector(shaderFadingColorName, currentColor);

                if (currentColor[3] < 0.08f)
                {
                    rendererToChange.enabled = false;
                }
                else
                {
                    rendererToChange.enabled = true;
                }
            }
        }

        void FindContactHand()
        {
            if (hardContactHand != null)
            {
                return;
            }

            if (physManager == null)
            {
                physManager = FindObjectOfType<PhysicalHandsManager>();
            }

            if (physManager != null)
            {
                if (chirality == Chirality.Left)
                {
                    hardContactHand = physManager.ContactParent.LeftHand as HardContactHand;
                }
                else if (chirality == Chirality.Right)
                {
                    hardContactHand = physManager.ContactParent.RightHand as HardContactHand;
                }
            }
        }
    }
}