using Leap.Unity.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Leap.Unity.Interaction {

  public class InteractionVRController : MonoBehaviour {

    public Chirality chirality;

    public List<Transform> primaryHoverPoints;

    private Vector3 _trackedPosition = Vector3.zero;

    private bool _hasTrackedPositionLastFrame = false;
    private Vector3 _trackedPositionLastFrame = Vector3.zero;

    public override bool isTracked {
      get {
        // Unfortunately, the only alternative to checking the controller's position for
        // whether or not it is tracked is to request the _allocated string array_ of
        // all currently-connected joysticks, which would allocate garbage every frame,
        // so it's unusable.
        return _trackedPosition != Vector3.zero;
      }
    }

    /// <summary>
    /// Gets the VRNode associated with this VR controller.
    /// </summary>
    public VRNode vrNode {
      get { return chirality == Chirality.Left ? VRNode.LeftHand : VRNode.RightHand; }
    }

    public override bool isLeft {
      get { return chirality == Chirality.Left; }
    }

    public override Vector3 velocity {
      get {
        if (_hasTrackedPositionLastFrame) {
          return (_trackedPosition - _trackedPositionLastFrame) / Time.fixedDeltaTime;
        }
        else {
          return Vector3.zero;
        }
      }
    }

    public override ControllerType controllerType {
      get { return ControllerType.VRController; }
    }

    public override InteractionHand intHand {
      get { return null; }
    }

    protected override void onObjectUnregistered(IInteractionBehaviour intObj) { }

    public override Vector3 hoverPoint {
      get { return _trackedPosition; }
    }

    protected override List<Transform> _primaryHoverPoints {
      get { return primaryHoverPoints; }
    }

    protected override void unwarpColliders(Transform primaryHoverPoint, Space.ISpaceComponent warpedSpaceElement) {
      throw new System.NotImplementedException();
    }

    private ContactBone[] _contactBones;
    protected override ContactBone[] contactBones {
      get { return _contactBones; }
    }

    private GameObject _contactBoneParent;
    protected override GameObject contactBoneParent {
      get {  }
    }

    protected override bool initContact() {
      _contactBoneParent = new GameObject("VR Controller Contact Bones");
      _contactBoneParent.transform.parent = this.transform;
    }

    //protected override void getColliderBoneTargetPositionRotation(int contactBoneIndex, out Vector3 targetPosition, out Quaternion targetRotation) {
    //  throw new System.NotImplementedException();
    //}

    //public override List<Vector3> graspManipulatorPoints {
    //  get { throw new System.NotImplementedException(); }
    //}

    //public override Vector3 GetGraspPoint() {
    //  throw new System.NotImplementedException();
    //}

    //protected override void fixedUpdateGraspingState() {
    //  throw new System.NotImplementedException();
    //}

    //protected override bool checkShouldGrasp(out Internal.IInteractionBehaviour objectToGrasp) {
    //  throw new System.NotImplementedException();
    //}

    //protected override bool checkShouldRelease(out Internal.IInteractionBehaviour objectToRelease) {
    //  throw new System.NotImplementedException();
    //}

  }

}
