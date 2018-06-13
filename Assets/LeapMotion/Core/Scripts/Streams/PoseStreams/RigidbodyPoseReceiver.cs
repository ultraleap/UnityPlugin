using UnityEngine;

namespace Leap.Unity.Streams {

  public class RigidbodyPoseReceiver : MonoBehaviour,
                                       IStreamReceiver<Pose> {

    public Rigidbody body;

    public enum MovementMode {
      SetPositionRotationDirectly,
      CallMoveFunctions
    }
    public MovementMode movementMode = MovementMode.CallMoveFunctions;

    [Header("Relative Pose")]
    public bool preserveRelativePoseOnOpen = false;

    [Header("Other Settings")]
    public bool ignoreRotations = false;

    private bool _waitingForRelativePose = false;
    private Pose _relativePose = Pose.identity;

    private void Reset() {
      if (body == null) body = GetComponent<Rigidbody>();
    }
    private void OnValidate() {
      if (body == null) body = GetComponent<Rigidbody>();
    }

    public void Close() {
      _waitingForRelativePose = false;
    }

    public void Open() {
      if (preserveRelativePoseOnOpen) {
        _waitingForRelativePose = true;
      }
    }

    public void Receive(Pose data) {
      if (_waitingForRelativePose) {
        _relativePose = data.inverse * body.GetPose();

        if (ignoreRotations) {
          _relativePose
            = data.WithRotation(Quaternion.identity).inverse
              * body.GetPose();
        }

        _waitingForRelativePose = false;
      }

      if (ignoreRotations) {
        data = data.WithRotation(Quaternion.identity);
      }

      if (body != null && this.gameObject.activeInHierarchy && this.enabled) {
        setBodyPose(data * _relativePose);
      }
    }

    private void setBodyPose(Pose pose) {
      if (body == null) return;

      switch (movementMode) {
        case MovementMode.SetPositionRotationDirectly:
          body.SetPose(pose);
          break;
        case MovementMode.CallMoveFunctions:
          body.MovePose(pose);
          break;
      }
    }

  }

}
