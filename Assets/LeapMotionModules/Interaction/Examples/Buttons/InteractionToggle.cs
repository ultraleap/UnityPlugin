using UnityEngine;
namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// A physics-enabled toggle. Toggling is triggered by physically pushing the toggle to its compressed position. 
  /// </summary>
  public class InteractionToggle : InteractionButton {

    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    public float ToggledRestingHeight = 0.5f;
    [Space]
    public bool Toggled = false;

    protected float originalRestingHeight;

    protected override void Start() {
      base.Start();
      originalRestingHeight = restingHeight;
    }

    protected override void Update() {
      base.Update();

      if (depressedThisFrame) {
        Toggled = !Toggled;
      }

      restingHeight = Toggled ? ToggledRestingHeight : originalRestingHeight;
    }
  }
}
