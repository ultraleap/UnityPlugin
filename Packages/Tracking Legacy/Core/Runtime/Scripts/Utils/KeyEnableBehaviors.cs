/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class KeyEnableBehaviors : MonoBehaviour
    {
        public List<Behaviour> targets;
        [Header("Controls")]
        public KeyCode unlockHold = KeyCode.None;
        public KeyCode toggle = KeyCode.Space;

        // Update is called once per frame
        void Update()
        {
            if (unlockHold != KeyCode.None &&
                !Input.GetKey(unlockHold))
            {
                return;
            }
            if (Input.GetKeyDown(toggle))
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].enabled = !targets[i].enabled;
                }
            }
        }
    }
}