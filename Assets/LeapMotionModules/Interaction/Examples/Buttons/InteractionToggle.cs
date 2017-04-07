using System;
using UnityEngine;
using UnityEngine.Events;
namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// A physics-enabled toggle. Toggling is triggered by physically pushing the toggle to its compressed position. 
  /// </summary>
  public class InteractionToggle : InteractionButton {

    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    ///<summary> The height that this toggle rests at when it is toggled. </summary>
    public float toggledRestingHeight = 0.5f;

    [Space]
    ///<summary> Whether or not this toggle is currently toggled. </summary>
    public bool toggled = false;
    ///<summary> Whether or not this toggle is currently toggled. </summary>
    [NonSerialized]
    public bool toggledOnThisFrame = false;
    ///<summary> Whether or not this toggle is currently toggled. </summary>
    [NonSerialized]
    public bool toggledOffThisFrame = false;

    public class BoolEvent : UnityEvent<bool> { }
    ///<summary> Triggered when this toggle is togggled. </summary>
    public BoolEvent toggleEvent = new BoolEvent();

    ///<summary> The minimum and maximum heights the button can exist at. </summary>
    private float _originalRestingHeight;

    protected override void Start() {
      base.Start();
      _originalRestingHeight = restingHeight;
    }

    protected override void Update() {
      base.Update();
      toggledOnThisFrame = false;
      toggledOffThisFrame = false;

      if (depressedThisFrame) {
        toggled = !toggled;
        toggleEvent.Invoke(toggled);
        if (toggled) {
          toggledOnThisFrame = true;
        } else {
          toggledOffThisFrame = false;
        }
      }

      restingHeight = toggled ? toggledRestingHeight : _originalRestingHeight;
    }
  }
}
