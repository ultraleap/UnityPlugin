using UnityEngine;
using Leap;
using Leap.Unity;

[RequireComponent(typeof(LeapGui))]
public class GUIWarpPostProcess : MonoBehaviour {
  LeapGui gui;
  void Start() { gui = GetComponent<LeapGui>(); }
  Hand originalHand = new Hand();

  public void fillBones(Hand inHand) {
    originalHand.CopyFrom(inHand);

    ITransformer space = gui.space.GetTransformer(gui.transform);
    Vector3 localPalmPos = gui.transform.InverseTransformPoint(originalHand.PalmPosition.ToVector3());
    Quaternion localPalmRot = gui.transform.InverseTransformRotation(originalHand.Rotation.ToQuaternion());

    inHand.SetTransform(gui.transform.TransformPoint(space.InverseTransformPoint(localPalmPos)),
                        gui.transform.TransformRotation(space.InverseTransformRotation(localPalmPos, localPalmRot)));

    for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
      for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
        Bone origBone = originalHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
        Bone newBone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
        Vector3 localBonePos = gui.transform.InverseTransformPoint(origBone.PrevJoint.ToVector3());
        Quaternion localBoneRot = gui.transform.InverseTransformRotation(origBone.Rotation.ToQuaternion());

        newBone.SetTransform(gui.transform.TransformPoint(space.InverseTransformPoint(localBonePos)),
                             gui.transform.TransformRotation(space.InverseTransformRotation(localBonePos, localBoneRot)));
      }
    }
  }
}