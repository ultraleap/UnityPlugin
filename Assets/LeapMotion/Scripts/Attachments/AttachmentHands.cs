using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity {

  [ExecuteInEditMode]
  public class AttachmentHands : MonoBehaviour {

    public Func<Hand> leftHandAccessor;
    public Func<Hand> rightHandAccessor;

    [SerializeField]
    private AttachmentPoints _attachmentPoints = new AttachmentPoints();

    private AttachmentHand[] _attachmentHands = new AttachmentHand[2];

    void OnValidate() {
      if (leftHandAccessor == null) leftHandAccessor = new Func<Hand>(() => { return Hands.Left; });
      if (rightHandAccessor == null) rightHandAccessor = new Func<Hand>(() => { return Hands.Right; });

      refreshAttachmentHands();
    }

    private void refreshAttachmentHands() {
      int handsIdx = 0;
      foreach (Transform child in this.transform) {
        var attachmentHand = child.GetComponent<AttachmentHand>();
        if (attachmentHand != null) {
          _attachmentHands[handsIdx++] = attachmentHand;
        }
        if (handsIdx == 2) break;
      }

#if UNITY_EDITOR
      PrefabType prefabType = PrefabUtility.GetPrefabType(this.gameObject);
      if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab) {
        return;
      }
#endif

      if (_attachmentHands[0] == null) {
        GameObject obj = new GameObject();
        _attachmentHands[0] = obj.AddComponent<AttachmentHand>();
      }
      _attachmentHands[0].gameObject.name = "Attachment Hand (Left)";
      _attachmentHands[0].handAccessor = leftHandAccessor;
      _attachmentHands[0].transform.parent = this.transform;

      if (_attachmentHands[1] == null) {
        GameObject obj = new GameObject();
        _attachmentHands[1] = obj.AddComponent<AttachmentHand>();
      }
      _attachmentHands[1].gameObject.name = "Attachment Hand (Right)";
      _attachmentHands[1].handAccessor = leftHandAccessor;
      _attachmentHands[1].transform.parent = this.transform;
    }


    [System.Serializable]
    public struct AttachmentPoints {
      public bool wrist;
      public bool palm;

      // Thumb, Finger 0
      public bool thumbProximalJoint;
      public bool thumbDistalJoint;
      public bool thumbTip;

      // Index, Finger 1
      public bool indexKnuckle;
      public bool indexMiddleJoint;
      public bool indexDistalJoint;
      public bool indexTip;

      // Middle, Finger 2
      public bool middleKnuckle;
      public bool middleMiddleJoint;
      public bool middleDistalJoint;
      public bool middleTip;

      // Ring, Finger 3
      public bool ringKnuckle;
      public bool ringMiddleJoint;
      public bool ringDistalJoint;
      public bool ringTip;

      // Pinky, Finger 4
      public bool pinkyKnuckle;
      public bool pinkyMiddleJoint;
      public bool pinkyDistalJoint;
      public bool pinkyTip;

      //public AttachmentPoints() {
      //  this.palm = true;

      //  this.wrist = false;
      //  this.thumbProximalJoint = false; this.thumbDistalJoint = false; this.thumbTip = false;
      //  this.indexKnuckle = false; this.indexMiddleJoint = false; this.indexDistalJoint = false; this.indexTip = false;
      //  this.middleKnuckle = false; this.middleMiddleJoint = false; this.middleDistalJoint = false; this.middleTip = false;
      //  this.ringKnuckle = false; this.ringMiddleJoint = false; this.ringDistalJoint = false; this.ringTip = false;
      //  this.pinkyKnuckle = false; this.pinkyMiddleJoint = false; this.pinkyDistalJoint = false; this.pinkyTip = false;
      //}
    }

  }


}