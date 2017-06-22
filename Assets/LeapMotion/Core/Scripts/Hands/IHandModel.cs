/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/** IHandModel defines abstract methods as a template for building Leap hand models*/
namespace Leap.Unity {
  public enum Chirality { Left, Right };
  public enum ModelType { Graphics, Physics };

  [ExecuteInEditMode]
  public abstract class IHandModel : MonoBehaviour {
    public event Action OnBegin;
    public event Action OnFinish;
    private bool isTracked = false;
    public bool IsTracked {
      get { return isTracked; }
    }

    public abstract Chirality Handedness { get; set; }
    public abstract ModelType HandModelType { get; }
    public virtual void InitHand() {
    }

    public virtual void BeginHand() {
      if (OnBegin != null) {
        OnBegin();
      }
      isTracked = true;
    }
    public abstract void UpdateHand();
    public virtual void FinishHand() {
      if (OnFinish != null) {
        OnFinish();
      }
      isTracked = false;
    }
    public abstract Hand GetLeapHand();
    public abstract void SetLeapHand(Hand hand);

    /// <summary>
    /// Returns whether or not this hand model supports editor persistence.  This is false by default and must be
    /// opt-in by a developer making their own hand model script if they want editor persistence.
    /// </summary>
    public virtual bool SupportsEditorPersistence() {
      return false;
    }

    public HandPool.ModelGroup group;

#if UNITY_EDITOR
    void Update() {
      if (!EditorApplication.isPlaying && SupportsEditorPersistence()) {
        Transform editorPoseSpace;
        LeapServiceProvider leapServiceProvider = FindObjectOfType<LeapServiceProvider>();
        LeapTransform poseTransform = LeapTransform.Identity;
        if (leapServiceProvider != null) {
          editorPoseSpace = leapServiceProvider.transform;
          poseTransform = TestHandFactory.GetTestPoseLeftHandTransform(leapServiceProvider.editTimePose);
        } else {
          editorPoseSpace = transform;
        }

        Hand hand = TestHandFactory.MakeTestHand(Handedness == Chirality.Left, poseTransform).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(editorPoseSpace));
        //Hand hand = TestHandFactory.MakeTestHand(0, 0, Handedness == Chirality.Left).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(editorPoseSpace));

        if (GetLeapHand() == null) {
          SetLeapHand(hand);
          InitHand();
          BeginHand();
          UpdateHand();
        } else {
          SetLeapHand(hand);
          UpdateHand();
        }
      }
    }
#endif
  }
}
