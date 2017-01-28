using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public abstract class InteractionBehaviourBase : MonoBehaviour {

    #region Hover Callbacks

    public abstract void OnHoverBegin(Hand hand, HoverEventData data);

    public abstract void OnHoverStay(Hand hand, HoverEventData data);

    public abstract void OnHoverEnd(Hand hand, HoverEventData data);

    public abstract void OnPrimaryHoverBegin(Hand hand, HoverEventData data);

    public abstract void OnPrimaryHoverStay(Hand hand, HoverEventData data);

    public abstract void OnPrimaryHoverEnd(Hand hand, HoverEventData data);

    #endregion


    #region Touch Callbacks

    public abstract void OnTouchBegin(Hand hand, TouchEventData data);

    public abstract void OnTouchStay(Hand hand, TouchEventData data);

    public abstract void OnTouchEnd(Hand hand, TouchEventData data);

    #endregion


    #region Grab Callbacks

    public abstract void OnGrab(Hand hand, GrabEventData data);

    public abstract void OnHold(Hand hand, GrabEventData data);

    public abstract void OnRelease(Hand hand, GrabEventData data);

    public abstract void OnSuspend(Hand hand, GrabEventData data);

    public abstract void OnResume(Hand hand, GrabEventData data);

    #endregion

  }

}