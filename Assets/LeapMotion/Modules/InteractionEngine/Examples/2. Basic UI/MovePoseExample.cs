using UnityEngine;

namespace Leap.Unity.Interaction.Examples {

  public class MovePoseExample : MonoBehaviour {

    public Transform target;
    private Pose _selfToTargetPose = Pose.identity;

    private void OnEnable() {
      _selfToTargetPose = this.transform.ToPose().inverse * target.transform.ToPose();
    }

    private void Start() {
      if (Physics.autoSyncTransforms) {
        Debug.LogWarning(
          "Physics.autoSyncTransforms is enabled. This will cause Interaction "
        + "Buttons and similar elements to 'wobble' when this script is used to "
        + "move a parent transform. You can modify this setting in "
        + "Edit->Project Settings->Physics.");
      }
    }

    private void Update() {
      target.transform.SetPose(this.transform.ToPose() * _selfToTargetPose);
    }

  }

}
