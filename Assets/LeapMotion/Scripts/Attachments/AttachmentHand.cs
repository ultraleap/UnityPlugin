using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  /// <summary>
  /// This MonoBehaviour is managed by an AttachmentHands component on a parent MonoBehaviour.
  /// Instead of adding AttachmentHand directly to a GameObject, add an AttachmentHands component
  /// to a parent GameObject to manage the construction and updating of AttachmentHand objects.
  /// </summary>
  [AddComponentMenu("")]
  public class AttachmentHand : MonoBehaviour {

    /// <summary>
    /// Called when the AttachmentHand refreshes its AttachmentPointBehaviour transforms. If the
    /// user unchecks an attachment point in the AttachmentHands inspector, those Transforms will
    /// be destroyed; otherwise, existing Transforms will persist, so be careful not to unnecessarily
    /// duplicate any objects or components you may want to attach via this callback.
    /// 
    /// Also, you can use AttachmentHand.points for an enumerator of all existing AttachmentPointBehaviour
    /// transforms on a given AttachmentHand object.
    /// </summary>
    public Action OnAttachmentPointsModified = () => { };

    #region AttachmentPointBehaviours

    [HideInInspector]
    public AttachmentPointBehaviour wrist;
    [HideInInspector]
    public AttachmentPointBehaviour palm;

    [HideInInspector]
    public AttachmentPointBehaviour thumbProximalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour thumbDistalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour thumbTip;

    [HideInInspector]
    public AttachmentPointBehaviour indexKnuckle;
    [HideInInspector]
    public AttachmentPointBehaviour indexMiddleJoint;
    [HideInInspector]
    public AttachmentPointBehaviour indexDistalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour indexTip;

    [HideInInspector]
    public AttachmentPointBehaviour middleKnuckle;
    [HideInInspector]
    public AttachmentPointBehaviour middleMiddleJoint;
    [HideInInspector]
    public AttachmentPointBehaviour middleDistalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour middleTip;

    [HideInInspector]
    public AttachmentPointBehaviour ringKnuckle;
    [HideInInspector]
    public AttachmentPointBehaviour ringMiddleJoint;
    [HideInInspector]
    public AttachmentPointBehaviour ringDistalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour ringTip;

    [HideInInspector]
    public AttachmentPointBehaviour pinkyKnuckle;
    [HideInInspector]
    public AttachmentPointBehaviour pinkyMiddleJoint;
    [HideInInspector]
    public AttachmentPointBehaviour pinkyDistalJoint;
    [HideInInspector]
    public AttachmentPointBehaviour pinkyTip;

    #endregion

    /// <summary>
    /// Gets an enumerator that traverses all of the AttachmentPoints beneath this AttachmentHand.
    /// </summary>
    public AttachmentPointsEnumerator points { get { return new AttachmentPointsEnumerator(this); } }

    private bool _attachmentPointsDirty = false;

    void OnValidate() {
      initializeAttachmentPointFlagConstants();
    }

    void Awake() {
      initializeAttachmentPointFlagConstants();
    }

    private AttachmentPointFlags[] _attachmentPointFlagConstants;
    private void initializeAttachmentPointFlagConstants() {
      Array flagConstants = Enum.GetValues(typeof(AttachmentPointFlags));
      if (_attachmentPointFlagConstants == null || _attachmentPointFlagConstants.Length == 0) {
        _attachmentPointFlagConstants = new AttachmentPointFlags[flagConstants.Length];
      }
      int i = 0;
      foreach (int f in flagConstants) {
        _attachmentPointFlagConstants[i++] = (AttachmentPointFlags)f;
      }
    }

    public void refreshAttachmentTransforms(AttachmentPointFlags points) {
      foreach (AttachmentPointFlags flag in _attachmentPointFlagConstants) {
        if (flag == AttachmentPointFlags.None) continue;

        if (points.Contains(flag)) {
          ensureTransformExists(flag);
        }
        else {
          ensureTransformDoesNotExist(flag);
        }
      }

      organizeAttachmentTransforms();

      if (_attachmentPointsDirty) {
        OnAttachmentPointsModified();
        _attachmentPointsDirty = false;
      }
    }

    #region Internal

    private AttachmentPointBehaviour getBehaviourForPoint(AttachmentPointFlags singlePoint) {
      AttachmentPointBehaviour behaviour = null;

      switch (singlePoint) {
        case AttachmentPointFlags.None: break;

        case AttachmentPointFlags.Wrist:                behaviour = wrist; break;
        case AttachmentPointFlags.Palm:                 behaviour = palm; break;
            
        case AttachmentPointFlags.ThumbProximalJoint:   behaviour = thumbProximalJoint; break;
        case AttachmentPointFlags.ThumbDistalJoint:     behaviour = thumbDistalJoint; break;
        case AttachmentPointFlags.ThumbTip:             behaviour = thumbTip; break;
            
        case AttachmentPointFlags.IndexKnuckle:         behaviour = indexKnuckle; break;
        case AttachmentPointFlags.IndexMiddleJoint:     behaviour = indexMiddleJoint; break;
        case AttachmentPointFlags.IndexDistalJoint:     behaviour = indexDistalJoint; break;
        case AttachmentPointFlags.IndexTip:             behaviour = indexTip; break;
            
        case AttachmentPointFlags.MiddleKnuckle:        behaviour = middleKnuckle; break;
        case AttachmentPointFlags.MiddleMiddleJoint:    behaviour = middleMiddleJoint; break;
        case AttachmentPointFlags.MiddleDistalJoint:    behaviour = middleDistalJoint; break;
        case AttachmentPointFlags.MiddleTip:            behaviour = middleTip; break;
            
        case AttachmentPointFlags.RingKnuckle:          behaviour = ringKnuckle; break;
        case AttachmentPointFlags.RingMiddleJoint:      behaviour = ringMiddleJoint; break;
        case AttachmentPointFlags.RingDistalJoint:      behaviour = ringDistalJoint; break;
        case AttachmentPointFlags.RingTip:              behaviour = ringTip; break;
            
        case AttachmentPointFlags.PinkyKnuckle:         behaviour = pinkyKnuckle; break;
        case AttachmentPointFlags.PinkyMiddleJoint:     behaviour = pinkyMiddleJoint; break;
        case AttachmentPointFlags.PinkyDistalJoint:     behaviour = pinkyDistalJoint; break;
        case AttachmentPointFlags.PinkyTip:             behaviour = pinkyTip; break;
      }

      return behaviour;
    }

    private void setBehaviourForPoint(AttachmentPointFlags singlePoint, AttachmentPointBehaviour behaviour) {
      switch (singlePoint) {
        case AttachmentPointFlags.None: break;

        case AttachmentPointFlags.Wrist:                wrist = behaviour; break;
        case AttachmentPointFlags.Palm:                 palm = behaviour; break;
            
        case AttachmentPointFlags.ThumbProximalJoint:   thumbProximalJoint = behaviour; break;
        case AttachmentPointFlags.ThumbDistalJoint:     thumbDistalJoint = behaviour; break;
        case AttachmentPointFlags.ThumbTip:             thumbTip = behaviour; break;
            
        case AttachmentPointFlags.IndexKnuckle:         indexKnuckle = behaviour; break;
        case AttachmentPointFlags.IndexMiddleJoint:     indexMiddleJoint = behaviour; break;
        case AttachmentPointFlags.IndexDistalJoint:     indexDistalJoint = behaviour; break;
        case AttachmentPointFlags.IndexTip:             indexTip = behaviour; break;
            
        case AttachmentPointFlags.MiddleKnuckle:        middleKnuckle = behaviour; break;
        case AttachmentPointFlags.MiddleMiddleJoint:    middleMiddleJoint = behaviour; break;
        case AttachmentPointFlags.MiddleDistalJoint:    middleDistalJoint = behaviour; break;
        case AttachmentPointFlags.MiddleTip:            middleTip = behaviour; break;
            
        case AttachmentPointFlags.RingKnuckle:          ringKnuckle = behaviour; break;
        case AttachmentPointFlags.RingMiddleJoint:      ringMiddleJoint = behaviour; break;
        case AttachmentPointFlags.RingDistalJoint:      ringDistalJoint = behaviour; break;
        case AttachmentPointFlags.RingTip:              ringTip = behaviour; break;
            
        case AttachmentPointFlags.PinkyKnuckle:         pinkyKnuckle = behaviour; break;
        case AttachmentPointFlags.PinkyMiddleJoint:     pinkyMiddleJoint = behaviour; break;
        case AttachmentPointFlags.PinkyDistalJoint:     pinkyDistalJoint = behaviour; break;
        case AttachmentPointFlags.PinkyTip:             pinkyTip = behaviour; break;
      }
    }

    private void ensureTransformExists(AttachmentPointFlags singlePoint) {
      if (!singlePoint.IsSinglePoint()) {
        Debug.LogError("Tried to ensure transform exists for singlePoint, but it contains more than one set flag");
        return;
      }

      if (getBehaviourForPoint(singlePoint) == null) {
        GameObject obj = new GameObject(Enum.GetName(typeof(AttachmentPointFlags), singlePoint));
        AttachmentPointBehaviour newPointBehaviour = obj.AddComponent<AttachmentPointBehaviour>();
        newPointBehaviour.attachmentPoint = singlePoint;
        newPointBehaviour.transform.parent = this.transform;

        setBehaviourForPoint(singlePoint, newPointBehaviour);

        _attachmentPointsDirty = true;
      }
    }

    private void ensureTransformDoesNotExist(AttachmentPointFlags singlePoint) {
      if (!singlePoint.IsSinglePoint()) {
        Debug.LogError("Tried to ensure transform exists for singlePoint, but it contains more than one set flag");
        return;
      }

      var pointBehaviour = getBehaviourForPoint(singlePoint);
      if (pointBehaviour != null) {
        DestroyImmediate(pointBehaviour.gameObject);
        pointBehaviour = null;

        _attachmentPointsDirty = true;
      }
    }

    private void organizeAttachmentTransforms() {
      int siblingIdx = 0;

      // Wrist
      if (wrist != null) {
        this.transform.SetSiblingIndex(siblingIdx++);
      }

      // Palm
      if (palm != null) {
        this.transform.SetSiblingIndex(siblingIdx++);
      }

      Transform topLevelTransform;

      // Thumb
      topLevelTransform = tryStackTransformHierarchy(thumbProximalJoint,
                                                     thumbDistalJoint,
                                                     thumbTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Index
      topLevelTransform = tryStackTransformHierarchy(indexKnuckle,
                                                     indexDistalJoint,
                                                     indexMiddleJoint,
                                                     indexTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Middle
      topLevelTransform = tryStackTransformHierarchy(middleKnuckle,
                                                     middleDistalJoint,
                                                     middleMiddleJoint,
                                                     middleTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Ring
      topLevelTransform = tryStackTransformHierarchy(ringKnuckle,
                                                     ringDistalJoint,
                                                     ringMiddleJoint,
                                                     ringTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Pinky
      topLevelTransform = tryStackTransformHierarchy(pinkyKnuckle,
                                                     pinkyDistalJoint,
                                                     pinkyMiddleJoint,
                                                     pinkyTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }
    }

    private static Transform[] s_hierarchyTransformsBuffer = new Transform[4];
    /// <summary>
    /// Tries to build a parent-child stack (index 0 is the first parent) of the argument
    /// transforms (they might be null) and returns the number of transforms that were non-null.
    /// </summary>
    private Transform tryStackTransformHierarchy(params Transform[] transforms) {
      for (int i = 0; i < s_hierarchyTransformsBuffer.Length; i++) {
        s_hierarchyTransformsBuffer[i] = null;
      }

      int hierarchyCount = 0;

      foreach (var transform in transforms.Query().Where(t => t != null)) {
        s_hierarchyTransformsBuffer[hierarchyCount++] = transform;
      }

      for (int i = hierarchyCount - 1; i > 0; i--) {
        s_hierarchyTransformsBuffer[i].parent = s_hierarchyTransformsBuffer[i - 1];
      }

      if (hierarchyCount > 0) {
        return s_hierarchyTransformsBuffer[0];
      }

      return null;
    }

    private static Transform[] s_transformsBuffer = new Transform[4];
    private Transform tryStackTransformHierarchy(params MonoBehaviour[] monoBehaviours) {
      for (int i = 0; i < s_transformsBuffer.Length; i++) {
        s_transformsBuffer[i] = null;
      }

      int tIdx = 0;
      foreach (var behaviour in monoBehaviours.Query().Where(b => b != null)) {
        s_transformsBuffer[tIdx++] = behaviour.transform;
      }

      return tryStackTransformHierarchy(s_transformsBuffer);
    }

    [ThreadStatic]
    private static AttachmentPointBehaviour[] s_attachmentPointsBuffer = new AttachmentPointBehaviour[32];

    /// <summary>
    /// An enumerator that traverses all of the existing AttachmentPointBehaviours beneath an
    /// AttachmentHand.
    /// </summary>
    public struct AttachmentPointsEnumerator {
      private int _curIdx;

      public AttachmentPointsEnumerator GetEnumerator() { return this; }

      public AttachmentPointsEnumerator(AttachmentHand hand) {
        _curIdx = -1;

        // New threads will have to construct their own attachment points buffer.
        if (s_attachmentPointsBuffer == null || s_attachmentPointsBuffer.Length != 32) s_attachmentPointsBuffer = new AttachmentPointBehaviour[32];

        // Just construct the buffer we'll be enumerating across. The enumeration
        // will automatically skip any null indices.
        int pointIdx = 0;
        s_attachmentPointsBuffer[pointIdx++] = hand.wrist;
        s_attachmentPointsBuffer[pointIdx++] = hand.palm;

        s_attachmentPointsBuffer[pointIdx++] = hand.thumbProximalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.thumbDistalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.thumbTip;

        s_attachmentPointsBuffer[pointIdx++] = hand.indexKnuckle;
        s_attachmentPointsBuffer[pointIdx++] = hand.indexMiddleJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.indexDistalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.indexTip;

        s_attachmentPointsBuffer[pointIdx++] = hand.middleKnuckle;
        s_attachmentPointsBuffer[pointIdx++] = hand.middleMiddleJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.middleDistalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.middleTip;

        s_attachmentPointsBuffer[pointIdx++] = hand.ringKnuckle;
        s_attachmentPointsBuffer[pointIdx++] = hand.ringMiddleJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.ringDistalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.ringTip;

        s_attachmentPointsBuffer[pointIdx++] = hand.pinkyKnuckle;
        s_attachmentPointsBuffer[pointIdx++] = hand.pinkyMiddleJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.pinkyDistalJoint;
        s_attachmentPointsBuffer[pointIdx++] = hand.pinkyTip;
      }

      public AttachmentPointBehaviour Current { get { return s_attachmentPointsBuffer[_curIdx]; } }

      public bool MoveNext() {
        do {
          _curIdx++;
        } while (_curIdx < s_attachmentPointsBuffer.Length && s_attachmentPointsBuffer[_curIdx] == null);

        return _curIdx < s_attachmentPointsBuffer.Length;
      }
    }

    #endregion

  }

}