using UnityEngine;
namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled toggle. Toggling is triggered by physically pushing the toggle to its compressed position. 
  */
  public class InteractionToggle : InteractionButton {
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    public float ToggledRestingHeight = 0.5f;
    [Space]
    public bool Toggled = false;

    protected float _originalRestingHeight;

    protected override void Start() {
      base.Start();
      _originalRestingHeight = RestingHeight;
    }

    protected override void Update() {
      base.Update();

      if (DepressedThisFrame) {
        Toggled = !Toggled;
      }

      RestingHeight = Toggled ? ToggledRestingHeight : _originalRestingHeight;
    }
  }
}
