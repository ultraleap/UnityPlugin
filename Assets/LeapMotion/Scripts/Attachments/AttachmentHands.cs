using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attachments {

  /// <summary>
  /// Add an GameObject with this script to your scene if you would like to have a
  /// Transform hierarchy that will follow various important points on a hand, whether
  /// for visuals or for logic. The AttachmentHands object will maintain two child
  /// objects, one for each of the player's hands. Use the Inspector to customize
  /// which points you'd like to see in the hierarchy beneath the individual
  /// AttachmentHand objects.
  /// </summary>
  [ExecuteInEditMode]
  public class AttachmentHands : MonoBehaviour {

    public Func<Hand>[] handAccessors = new Func<Hand>[2];

    [SerializeField]
    [OnEditorChange("attachmentPoints")]
    private AttachmentPointFlags _attachmentPoints = AttachmentPointFlags.Palm | AttachmentPointFlags.Wrist;
    public AttachmentPointFlags attachmentPoints {
      get {
        return _attachmentPoints;
      }
      set {
        _attachmentPoints = value;
        refreshAttachmentHandTransforms();
      }
    }

    private AttachmentHand[] _attachmentHands = new AttachmentHand[2];

    void OnValidate() {
      if (handAccessors[0] == null) handAccessors[0] = new Func<Hand>(() => { return Hands.Left; });
      if (handAccessors[1] == null) handAccessors[1] = new Func<Hand>(() => { return Hands.Right; });

      refreshAttachmentHands();
    }

    void Update() {
      using (new ProfilerSample("Attachment Hands Update", this.gameObject)) {
        for (int i = 0; i < _attachmentHands.Length; i++) {
          var attachmentHand = _attachmentHands[i];
          var leapHand = handAccessors[i]();

#if UNITY_EDITOR
          if (Hands.Provider != null) {
            if (leapHand == null && !Application.isPlaying) {
              leapHand = TestHandFactory.MakeTestHand(0, i, i == 0).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(Hands.Provider.transform));
            }
          }
#endif

          using (new ProfilerSample(attachmentHand.gameObject.name + " Update Points")) {
            foreach (var point in attachmentHand.points) {
              point.SetTransformUsingHand(leapHand);
            }
          }
        }
      }
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
      _attachmentHands[0].transform.parent = this.transform;
      _attachmentHands[0].transform.SetSiblingIndex(0);

      if (_attachmentHands[1] == null) {
        GameObject obj = new GameObject();
        _attachmentHands[1] = obj.AddComponent<AttachmentHand>();
      }
      _attachmentHands[1].gameObject.name = "Attachment Hand (Right)";
      _attachmentHands[1].transform.parent = this.transform;
      _attachmentHands[1].transform.SetSiblingIndex(1);
    }

    private void refreshAttachmentHandTransforms() {
      foreach (var hand in _attachmentHands) {
        hand.refreshAttachmentTransforms(_attachmentPoints);
      }
    }

  }


}