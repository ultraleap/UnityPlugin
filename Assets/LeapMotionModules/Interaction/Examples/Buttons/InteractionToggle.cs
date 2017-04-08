using System;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.GraphicalRenderer;
namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// A physics-enabled toggle. Toggling is triggered by physically pushing the toggle to its compressed position. 
  /// </summary>
  public class InteractionToggle : InteractionButton {

    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    ///<summary> The height that this toggle rests at when it is toggled. </summary>
    public float toggledRestingHeight = 0.25f;

    [Space]
    ///<summary> Whether or not this toggle is currently toggled. </summary>
    public bool toggled = false;

    public class BoolEvent : UnityEvent<bool> { }
    ///<summary> Triggered when this toggle is togggled. </summary>
    public BoolEvent toggleEvent = new BoolEvent();

    ///<summary> The minimum and maximum heights the button can exist at. </summary>
    private float _originalRestingHeight;

    protected override void Start() {
      base.Start();
      _originalRestingHeight = restingHeight;

      OnPress.AddListener(OnPressed);
    }

    void OnPressed() {
      toggled = !toggled;
      toggleEvent.Invoke(toggled);
      restingHeight = toggled ? toggledRestingHeight : _originalRestingHeight;
    }

    void OnDestroy() {
      OnPress.RemoveAllListeners();
    }
  }
}
