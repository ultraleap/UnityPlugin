/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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

/** HandModelBase defines abstract methods as a template for building Leap hand models*/
namespace Leap.Unity {
  public enum Chirality { Left, Right };
  public enum ModelType { Graphics, Physics };

  [ExecuteInEditMode]
  public abstract class HandModelBase : MonoBehaviour {
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

    [NonSerialized]
    public HandModelManager.ModelGroup group;

#if UNITY_EDITOR
    void Update() {
      if (!EditorApplication.isPlaying && SupportsEditorPersistence()) {
        LeapProvider provider = null;

        //First try to get the provider from a parent HandModelManager
        if (transform.parent != null) {
          var manager = transform.parent.GetComponent<HandModelManager>();
          if (manager != null) {
            provider = manager.leapProvider;
          }
        }

        //If not found, use any old provider from the Hands.Provider getter
        if (provider == null) {
          provider = Hands.Provider;
        }

        Hand hand = null;
        //If we found a provider, pull the hand from that
        if (provider != null) {
          var frame = provider.CurrentFrame;

          if (frame != null) {
            hand = frame.Get(Handedness);
          }
        }

        //If we still have a null hand, construct one manually
        if (hand == null) {
          hand = TestHandFactory.MakeTestHand(Handedness == Chirality.Left, unitType: TestHandFactory.UnitType.LeapUnits);
          hand.Transform(transform.GetLeapMatrix());
        }

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
