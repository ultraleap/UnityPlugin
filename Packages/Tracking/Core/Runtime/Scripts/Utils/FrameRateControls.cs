/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// Provides control of target frame rate.
    /// </summary>
    /// <remarks>
    /// This utility is useful for verifying frame-rate independence of behaviors.
    /// </remarks>
    public class FrameRateControls : MonoBehaviour
    {
        public int targetRenderRate = 60; // must be > 0
        public int targetRenderRateStep = 1;
        public int fixedPhysicsRate = 50; // must be > 0
        public int fixedPhysicsRateStep = 1;
        public KeyCode unlockRender = KeyCode.RightShift;
        public KeyCode unlockPhysics = KeyCode.LeftShift;
        public KeyCode decrease = KeyCode.DownArrow;
        public KeyCode increase = KeyCode.UpArrow;
        public KeyCode resetRate = KeyCode.Backspace;

        // Use this for initialization
        void Awake()
        {
            if (QualitySettings.vSyncCount != 0)
            {
                Debug.LogWarning("vSync will override target frame rate. vSyncCount = " + QualitySettings.vSyncCount);
            }

            Application.targetFrameRate = targetRenderRate;
            Time.fixedDeltaTime = 1f / ((float)fixedPhysicsRate);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(unlockRender))
            {
                if (Input.GetKeyDown(decrease))
                {
                    if (targetRenderRate > targetRenderRateStep)
                    {
                        targetRenderRate -= targetRenderRateStep;
                        Application.targetFrameRate = targetRenderRate;
                    }
                }
                if (Input.GetKeyDown(increase))
                {
                    targetRenderRate += targetRenderRateStep;
                    Application.targetFrameRate = targetRenderRate;
                }
                if (Input.GetKeyDown(resetRate))
                {
                    ResetRender();
                }
            }
            if (Input.GetKey(unlockPhysics))
            {
                if (Input.GetKeyDown(decrease))
                {
                    if (fixedPhysicsRate > fixedPhysicsRateStep)
                    {
                        fixedPhysicsRate -= fixedPhysicsRateStep;
                        Time.fixedDeltaTime = 1f / ((float)fixedPhysicsRate);
                    }
                }
                if (Input.GetKeyDown(increase))
                {
                    fixedPhysicsRate += fixedPhysicsRateStep;
                    Time.fixedDeltaTime = 1f / ((float)fixedPhysicsRate);
                }
                if (Input.GetKeyDown(resetRate))
                {
                    ResetPhysics();
                }
            }
        }

        public void ResetRender()
        {
            targetRenderRate = 60;
            Application.targetFrameRate = -1;
        }

        public void ResetPhysics()
        {
            fixedPhysicsRate = 50;
            Time.fixedDeltaTime = 0.02f;
        }

        public void ResetAll()
        {
            ResetRender();
            ResetPhysics();
        }
    }
}