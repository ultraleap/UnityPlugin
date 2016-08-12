using UnityEngine;
using System.Collections;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity {

  /**
   * Detects when specified fingers are in an extended or non-extended state.
   * 
   * You can specify whether each finger is extended, not extended, or in either state.
   * This detector activates when every finger on the observed hand meets these conditions.
   * 
   * If added to a IHandModel instance or one of its children, this detector checks the
   * finger state at the interval specified by the Period variable. You can also specify
   * which hand model to observe explicitly by setting handModel in the Unity editor or 
   * in code.
   * 
   * @since 4.1.2
   */
  public class ExtendedFingerDetector : Detector {
    /**
     * The interval at which to check finger state.
     * @since 4.1.2
     */
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds

    /**
     * The IHandModel instance to observe. 
     * Set automatically if not explicitly set in the editor.
     * @since 4.1.2
     */
    [AutoFind(AutoFindLocations.Parents)]
    [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
    public IHandModel HandModel = null;
  
    /** The required thumb state. */
    public PointingState Thumb = PointingState.Either;
    /** The required index finger state. */
    public PointingState Index = PointingState.Either;
    /** The required middle finger state. */
    public PointingState Middle = PointingState.Either;
    /** The required ring finger state. */
    public PointingState Ring = PointingState.Either;
    /** The required pinky finger state. */
    public PointingState Pinky = PointingState.Either;

    [Range(0,5)]
    public int MinimumExtendedCount = 0;
    [Range(0, 5)]
    public int MaximumExtendedCount = 5;

    private IEnumerator watcherCoroutine;

    void OnValidate() {
      int required = 0, forbidden = 0;
      PointingState[] stateArray = { Thumb, Index, Middle, Ring, Pinky };
      foreach(PointingState state in stateArray) {
        switch (state) {
          case PointingState.Extended:
            required++;
            break;
          case PointingState.NotExtended:
            forbidden++;
            break;
          default:
            break;
        }
        MinimumExtendedCount = Math.Max(required, MinimumExtendedCount);
        MaximumExtendedCount = Math.Min(5 - forbidden, MaximumExtendedCount);
        MaximumExtendedCount = Math.Max(required, MaximumExtendedCount);
      }
    
    }

    void Awake () {
      watcherCoroutine = extendedFingerWatcher();
    }
  
    void OnEnable () {
      StartCoroutine(watcherCoroutine);
    }
  
    void OnDisable () {
      StopCoroutine(watcherCoroutine);
      Deactivate();
    }
  
    IEnumerator extendedFingerWatcher() {
      Hand hand;
      while(true){
        bool fingerState = false;
        if(HandModel != null && HandModel.IsTracked){
          hand = HandModel.GetLeapHand();
          if(hand != null){
            fingerState = matchFingerState(hand.Fingers[0], Thumb)
              && matchFingerState(hand.Fingers[1], Index)
              && matchFingerState(hand.Fingers[2], Middle)
              && matchFingerState(hand.Fingers[3], Ring)
              && matchFingerState(hand.Fingers[4], Pinky);

            int extendedCount = 0;
            for (int f = 0; f < 4; f++) {
              if (hand.Fingers[f].IsExtended) {
                extendedCount++;
              }
            }
            fingerState = fingerState && 
                         (extendedCount <= MaximumExtendedCount) && 
                         (extendedCount >= MinimumExtendedCount);
            if(HandModel.IsTracked && fingerState){
              Activate();
            } else if(!HandModel.IsTracked || !fingerState) {
              Deactivate();
            }
          }
        } else if(IsActive){
          Deactivate();
        }
        yield return new WaitForSeconds(Period);
      }
    }

    private bool matchFingerState (Finger finger, PointingState requiredState) {
      return (requiredState == PointingState.Either) ||
             (requiredState == PointingState.Extended && finger.IsExtended) ||
             (requiredState == PointingState.NotExtended && !finger.IsExtended);
    }

    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos && HandModel != null) {
        PointingState[] state = { Thumb, Index, Middle, Ring, Pinky };
        Hand hand = HandModel.GetLeapHand();
        int extendedCount = 0;
        int notExtendedCount = 0;
        for (int f = 0; f < 5; f++) {
          Finger finger = hand.Fingers[f];
          if (finger.IsExtended) extendedCount++;
          else notExtendedCount++;
          if (matchFingerState(finger, state[f]) && 
             (extendedCount <= MaximumExtendedCount) && 
             (extendedCount >= MinimumExtendedCount)) {
            Gizmos.color = Color.green;
          } else {
            Gizmos.color = Color.red;
          }
          Gizmos.DrawWireSphere(finger.TipPosition.ToVector3(), finger.Width);
        }
      }
    }
    #endif
  }
  
  /** Defines the settings for comparing extended finger states */
  public enum PointingState{Extended, NotExtended, Either}
}
