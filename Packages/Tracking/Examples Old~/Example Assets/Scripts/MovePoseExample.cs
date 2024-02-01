/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using Leap.Unity;
using UnityEngine;

namespace Leap.InteractionEngine.Examples
{
    public class MovePoseExample : MonoBehaviour
    {
        public Transform target;
        private Pose _selfToTargetPose = Pose.identity;

        private void OnEnable()
        {
            _selfToTargetPose = this.transform.ToPose().inverse().mul(target.transform.ToPose());
        }

        private void Start()
        {
            if (Physics.autoSyncTransforms)
            {
                Debug.LogWarning(
                    "Physics.autoSyncTransforms is enabled. This will cause Interaction "
                + "Buttons and similar elements to 'wobble' when this script is used to "
                + "move a parent transform. You can modify this setting in "
                + "Edit->Project Settings->Physics.");
            }
        }

        private void Update()
        {
            target.transform.SetPose(this.transform.ToPose().mul(_selfToTargetPose));
        }
    }
}