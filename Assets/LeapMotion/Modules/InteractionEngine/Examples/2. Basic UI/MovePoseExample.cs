using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction.Examples {

  public class MovePoseExample : MonoBehaviour {

    public Transform target;
    private Pose _selfToTargetPose = Pose.identity;

    private void OnEnable() {
      _selfToTargetPose = this.transform.ToPose().inverse * target.transform.ToPose();
    }

    private void Update() {
      target.transform.SetPose(this.transform.ToPose() * _selfToTargetPose);
    }

  }

}
