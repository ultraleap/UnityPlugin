/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
