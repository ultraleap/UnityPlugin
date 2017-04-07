using UnityEngine;
using Leap.Unity.Attributes;
namespace Leap.Unity.UI.Interaction {

  ///<summary>
  /// A physics-enabled slider. Sliding is triggered by physically pushing the slider to its compressed position. 
  /// Increasing the horizontal and vertical slide limits allows it to act as either a 1D or 2D slider.
  ///</summary>
  public class InteractionSlider : InteractionButton {

    [Space, Space]
    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    public Vector2 HorizontalValueRange = new Vector2(0f, 1f);
    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    public Vector2 VerticalValueRange = new Vector2(0f, 1f);

    [Space]
    [Tooltip("The minimum and maximum horizontal extents that the slider can slide to in world space.")]
    [MinMax(-0.25f, 0.25f)]
    public Vector2 HorizontalSlideLimits = new Vector2(0f, 0f);
    [MinMax(-0.25f, 0.25f)]
    [Tooltip("The minimum and maximum vertical extents that the slider can slide to in world space.")]
    public Vector2 VerticalSlideLimits = new Vector2(0f, 0f);

    ///<summary> This slider's horizontal slider value, mapped between the values in the HorizontalValueRange. </summary>
    public float HorizontalSliderValue {
      get { return _HorizontalSliderValue; }
      set {
        if (_HorizontalSlideLimits.x != _HorizontalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(HorizontalValueRange.x, HorizontalValueRange.y, value);
          localPhysicsPosition.x = Mathf.Lerp(initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y, alpha);
          _HorizontalSliderValue = value;
        }
      }
    }

    ///<summary> This slider's current vertical slider value, mapped between the values in the VerticalValueRange. </summary>
    public float VerticalSliderValue {
      get { return _VerticalSliderValue; }
      set {
        if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(VerticalValueRange.x, VerticalValueRange.y, value);
          localPhysicsPosition.y = Mathf.Lerp(initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y, alpha);
          _VerticalSliderValue = value;
        }
      }
    }

    //Slide limits normalized to local space
    private Vector2 _HorizontalSlideLimits;
    private Vector2 _VerticalSlideLimits;

    //Internal Slider Values
    private float _HorizontalSliderValue;
    private float _VerticalSliderValue;

    protected override void Start() {
      base.Start();

      //Conversion of limits to local space
      _HorizontalSlideLimits = HorizontalSlideLimits / transform.parent.lossyScale.x;
      _VerticalSlideLimits = VerticalSlideLimits / transform.parent.lossyScale.y;
    }

    protected override void Update() {
      base.Update();

      //Calculate the Normalized Slider Values
      if (_HorizontalSlideLimits.x != _HorizontalSlideLimits.y) {
        _HorizontalSliderValue = Mathf.InverseLerp(initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y, localPhysicsPosition.x);
        _HorizontalSliderValue = Mathf.Lerp(HorizontalValueRange.x, HorizontalValueRange.y, _HorizontalSliderValue);
      }

      if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
        _VerticalSliderValue = Mathf.InverseLerp(initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y, localPhysicsPosition.y);
        _VerticalSliderValue = Mathf.Lerp(VerticalValueRange.x, VerticalValueRange.y, _VerticalSliderValue);
      }
    }

    protected override Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(Mathf.Clamp((localPhysicsPosition.x + desiredOffset.x * 0.25f), initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y),
                         Mathf.Clamp((localPhysicsPosition.y + desiredOffset.y * 0.25f), initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y),
                                     (localPhysicsPosition.z + desiredOffset.z));
    }
  }
}
