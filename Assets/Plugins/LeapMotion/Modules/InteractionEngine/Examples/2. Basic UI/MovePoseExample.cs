/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Interaction.Examples {

  public class MovePoseExample : MonoBehaviour {

    public Transform target;
    private Pose _selfToTargetPose = Pose.identity;

    private void OnEnable() {
      _selfToTargetPose = this.transform.ToPose().inverse * target.transform.ToPose();
    }

    private void Start() {
#if UNITY_2017_2_OR_NEWER
      if (Physics.autoSyncTransforms) {
        Debug.LogWarning(
          "Physics.autoSyncTransforms is enabled. This will cause Interaction "
        + "Buttons and similar elements to 'wobble' when this script is used to "
        + "move a parent transform. You can modify this setting in "
        + "Edit->Project Settings->Physics.");
      }
#endif
    }

    private void Update() {
      target.transform.SetPose(this.transform.ToPose() * _selfToTargetPose);
    }

  }

}
