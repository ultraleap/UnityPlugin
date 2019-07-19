/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Attachments {

  /// <summary>
  /// This MonoBehaviour is managed by an AttachmentHands component on a parent MonoBehaviour.
  /// Instead of adding AttachmentHand directly to a GameObject, add an AttachmentHands component
  /// to a parent GameObject to manage the construction and updating of AttachmentHand objects.
  /// </summary>
  [AddComponentMenu("")]
  [ExecuteInEditMode]
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

    /// <summary>
    /// Used by AttachmentHands as a hint to help prevent mixing up hand chiralities
    /// when refreshing its AttachmentHand references.
    /// </summary>
    [SerializeField, Disable]
    private Chirality _chirality;

    /// <summary>
    /// Gets the chirality of this AttachmentHand. This is set automatically by the
    /// AttachmentHands parent object of this AttachmentHand.
    /// </summary>
    public Chirality chirality { get { return _chirality; } set { _chirality = value; } }

    /// <summary>
    /// Used by AttachmentHands as a hint to help prevent mixing up hand chiralities
    /// when refreshing its AttachmentHand references.
    /// </summary>
    [SerializeField, Disable]
    private bool _isTracked;

    /// <summary>
    /// Gets the chirality of this AttachmentHand. This is set automatically by the
    /// AttachmentHands parent object of this AttachmentHand.
    /// </summary>
    public bool isTracked { get { return _isTracked; } set { _isTracked = value; } }

    void OnValidate() {
      initializeAttachmentPointFlagConstants();
    }

    void Awake() {
      initializeAttachmentPointFlagConstants();
    }
    
    #if !UNITY_EDITOR
    #pragma warning disable 0414
    #endif
    private bool _isBeingDestroyed = false;
    #if !UNITY_EDITOR
    #pragma warning restore 0414
    #endif
    void OnDestroy() {
      _isBeingDestroyed = true;
    }

    /// <summary>
    /// Returns the AttachmentPointBehaviour child object of this AttachmentHand given a
    /// reference to a single AttachmentPointFlags flag, or null if there is no such child object.
    /// </summary>
    public AttachmentPointBehaviour GetBehaviourForPoint(AttachmentPointFlags singlePoint) {
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

    public void refreshAttachmentTransforms(AttachmentPointFlags points) {
      if (_attachmentPointFlagConstants == null || _attachmentPointFlagConstants.Length == 0) {
        initializeAttachmentPointFlagConstants();
      }

      // First just _check_ whether we'll need to do any destruction or creation
      bool requiresDestructionOrCreation = false;
      foreach (var flag in _attachmentPointFlagConstants) {
        if (flag == AttachmentPointFlags.None) continue;

        if ((!points.Contains(flag) && GetBehaviourForPoint(flag) != null)
             || (points.Contains(flag) && GetBehaviourForPoint(flag) == null)) {
          requiresDestructionOrCreation = true;
        }
      }

      // Go through the work of flattening and rebuilding if it is necessary.
      if (requiresDestructionOrCreation) {
        // Remove parent-child relationships so deleting parent Transforms doesn't annihilate
        // child Transforms that don't need to be deleted themselves.
        flattenAttachmentTransformHierarchy();

        foreach (AttachmentPointFlags flag in _attachmentPointFlagConstants) {
          if (flag == AttachmentPointFlags.None) continue;

          if (points.Contains(flag)) {
            ensureTransformExists(flag);
          }
          else {
            ensureTransformDoesNotExist(flag);
          }
        }

        // Organize transforms, restoring parent-child relationships.
        organizeAttachmentTransforms();
      }

      if (_attachmentPointsDirty) {
        OnAttachmentPointsModified();
        _attachmentPointsDirty = false;
      }
    }

    public void notifyPointBehaviourDeleted(AttachmentPointBehaviour point) {
      #if UNITY_EDITOR
      // Only valid if the AttachmentHand itself is also not being destroyed.
      if (_isBeingDestroyed) return;

      // Refresh this hand's attachment transforms on a slight delay.
      // Only AttachmentHands can _truly_ remove attachment points!
      AttachmentHands attachHands = GetComponentInParent<AttachmentHands>();
      if (attachHands != null) {
        EditorApplication.delayCall += () => { refreshAttachmentTransforms(attachHands.attachmentPoints); };
      }
      #endif
    }

    #region Internal

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

      #if UNITY_EDITOR
      EditorUtility.SetDirty(this);
      #endif
    }

    private void ensureTransformExists(AttachmentPointFlags singlePoint) {
      if (!singlePoint.IsSinglePoint()) {
        Debug.LogError("Tried to ensure transform exists for singlePoint, but it contains more than one set flag.");
        return;
      }

      AttachmentPointBehaviour pointBehaviour = GetBehaviourForPoint(singlePoint);

      if (pointBehaviour == null) {
        // First, see if there's already one in the hierarchy! Might exist due to, e.g. an Undo operation
        var existingPointBehaviour = this.gameObject.GetComponentsInChildren<AttachmentPointBehaviour>()
                                                    .Query()
                                                    .FirstOrDefault(p => p.attachmentPoint == singlePoint);

        // Only make a new object if the transform really doesn't exist.
        if (existingPointBehaviour == AttachmentPointFlags.None) {
          GameObject obj = new GameObject(Enum.GetName(typeof(AttachmentPointFlags), singlePoint));
          #if UNITY_EDITOR
          Undo.RegisterCreatedObjectUndo(obj, "Created Object");
          pointBehaviour = Undo.AddComponent<AttachmentPointBehaviour>(obj);
          #else
          pointBehaviour = obj.AddComponent<AttachmentPointBehaviour>();
          #endif
        }
        else {
          pointBehaviour = existingPointBehaviour;
        }

        #if UNITY_EDITOR
        Undo.RecordObject(pointBehaviour, "Set Attachment Point");
        #endif
        pointBehaviour.attachmentPoint = singlePoint;
        pointBehaviour.attachmentHand = this;
        setBehaviourForPoint(singlePoint, pointBehaviour);

        SetTransformParent(pointBehaviour.transform, this.transform);

        _attachmentPointsDirty = true;

        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
      }
    }

    private static void SetTransformParent(Transform t, Transform parent) {
      #if UNITY_EDITOR
      Undo.SetTransformParent(t, parent, "Set Transform Parent");
      #else
      t.parent = parent;
      #endif
    }

    private void ensureTransformDoesNotExist(AttachmentPointFlags singlePoint) {
      if (!singlePoint.IsSinglePoint()) {
        Debug.LogError("Tried to ensure transform exists for singlePoint, but it contains more than one set flag");
        return;
      }

      var pointBehaviour = GetBehaviourForPoint(singlePoint);
      if (pointBehaviour != null) {
        InternalUtility.Destroy(pointBehaviour.gameObject);
        setBehaviourForPoint(singlePoint, null);

        pointBehaviour = null;

        _attachmentPointsDirty = true;

        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
      }
    }

    private void flattenAttachmentTransformHierarchy() {
      foreach (var point in this.points) {
        SetTransformParent(point.transform, this.transform);
      }
    }

    private void organizeAttachmentTransforms() {
      int siblingIdx = 0;

      // Wrist
      if (wrist != null) {
        wrist.transform.SetSiblingIndex(siblingIdx++);
      }

      // Palm
      if (palm != null) {
        palm.transform.SetSiblingIndex(siblingIdx++);
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
                                                     indexMiddleJoint,
                                                     indexDistalJoint,
                                                     indexTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Middle
      topLevelTransform = tryStackTransformHierarchy(middleKnuckle,
                                                     middleMiddleJoint,
                                                     middleDistalJoint,
                                                     middleTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Ring
      topLevelTransform = tryStackTransformHierarchy(ringKnuckle,
                                                     ringMiddleJoint,
                                                     ringDistalJoint,
                                                     ringTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }

      // Pinky
      topLevelTransform = tryStackTransformHierarchy(pinkyKnuckle,
                                                     pinkyMiddleJoint,
                                                     pinkyDistalJoint,
                                                     pinkyTip);
      if (topLevelTransform != null) {
        topLevelTransform.SetSiblingIndex(siblingIdx++);
      }
    }

    private static Transform[] s_hierarchyTransformsBuffer = new Transform[4];
    /// <summary>
    /// Tries to build a parent-child stack (index 0 is the first parent) of the argument
    /// transforms (they might be null) and returns the top-level parent transform (or null
    /// if there is none).
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
        SetTransformParent(s_hierarchyTransformsBuffer[i], s_hierarchyTransformsBuffer[i - 1]);
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

    /// <summary>
    /// An enumerator that traverses all of the existing AttachmentPointBehaviours beneath an
    /// AttachmentHand.
    /// </summary>
    public struct AttachmentPointsEnumerator {
      private int _curIdx;
      private AttachmentHand _hand;
      private int _flagsCount;

      public AttachmentPointsEnumerator GetEnumerator() { return this; }

      public AttachmentPointsEnumerator(AttachmentHand hand) {
        if (hand != null && hand._attachmentPointFlagConstants != null) {
          _curIdx = -1;
          _hand = hand;
          _flagsCount = hand._attachmentPointFlagConstants.Length;
        }
        else {
          // Hand doesn't exist (destroyed?) or isn't initialized yet.
          _curIdx = -1;
          _hand = null;
          _flagsCount = 0;
        }
      }

      public AttachmentPointBehaviour Current {
        get {
          if (_hand == null) return null;

          return _hand.GetBehaviourForPoint(GetFlagFromFlagIdx(_curIdx));
        }
      }

      public bool MoveNext() {
        do {
          _curIdx++;
        } while (_curIdx < _flagsCount && _hand.GetBehaviourForPoint(GetFlagFromFlagIdx(_curIdx)) == null);

        return _curIdx < _flagsCount;
      }
    }

    private static AttachmentPointFlags GetFlagFromFlagIdx(int pointIdx) {
      return (AttachmentPointFlags)(1 << pointIdx + 1);
    }

    #endregion

  }

}
