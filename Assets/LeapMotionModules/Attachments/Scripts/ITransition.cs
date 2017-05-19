/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Attachments {
  /**
  * Defines the interface used by AttachmentController to play transitions
  * when it activates and deactivates attachments. This interface is implemented
  * by the Transition class.
  * 
  * When implementing your own transition class, TransitionIn() and TransitionOut()
  * must invoke the OnStart event when the transition begins, the OnAnimationStep
  * event as the transition proceeds, and the OnComplete event when the transition
  * is finished.  
  * @since 4.1.4
  */
  public abstract class ITransition : MonoBehaviour {
    public abstract void TransitionIn();
    public abstract void TransitionOut();
    public abstract UnityEvent OnStart { get; set; }
    public abstract AnimationStepEvent OnAnimationStep { get; set; }
    public abstract UnityEvent OnComplete { get; set; }
  }

  /**
  * An event class that is dispatched by a Transition for each animation
  * step during a transition. The event occurs once per frame for the duration of
  * a transition.
  * The event parameter provides the current interpolation value between -1 and 0 for an
  * in transition and between 0 and +1 for an out transition..
  * @since 4.1.4
  */
  [System.Serializable]
  public class AnimationStepEvent : UnityEvent<float> { }
}
