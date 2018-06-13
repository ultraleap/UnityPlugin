using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gestures {
  
  /// <summary>
  /// A thin layer of general abstraction for one-handed and two-handed gestures.
  /// </summary>
  public abstract class Gesture : MonoBehaviour, IGesture {

    public abstract bool wasActivated { get; }

    public abstract bool isActive { get; }

    public abstract bool wasDeactivated { get; }

    public abstract bool wasFinished { get; }

    public abstract bool wasCancelled { get; }

    /// <summary>
    /// Optionally override this property to specify to systems that take gestures as
    /// input whether or not the gesture "could be activated" given its current state.
    /// </summary>
    public virtual bool isEligible { get { return true; } }

    protected enum DeactivationReason {
      FinishedGesture,
      CancelledGesture,
    }

    #region TODO: Seriously consider removing Actions from the system.

    public Action OnGestureActivated = () => { };
    public Action OnGestureDeactivated = () => { };

    #endregion

  }

}