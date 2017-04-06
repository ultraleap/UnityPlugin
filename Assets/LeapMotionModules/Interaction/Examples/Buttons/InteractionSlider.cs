using UnityEngine;
using Leap.Unity.Attributes;
namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled slider. Sliding is triggered by physically pushing the slider to its compressed position. 
  *  Increasing the horizontal and vertical slide limits allows it to act as either a 1D or 2D slider
  */
  public class InteractionSlider : InteractionButton {
    [Space]
    [Space]

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

    //Reset the slider positions when the slider values are set
    public float HorizontalSliderValue {
      get { return _HorizontalSliderValue; }
      set {
        if (_HorizontalSlideLimits.x != _HorizontalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(HorizontalValueRange.x, HorizontalValueRange.y, value);
          _localPhysicsPosition.x = Mathf.Lerp(_InitialLocalPosition.x + _HorizontalSlideLimits.x, _InitialLocalPosition.x + _HorizontalSlideLimits.y, alpha);
          _HorizontalSliderValue = value;
        }
      }
    }
    public float VerticalSliderValue {
      get { return _VerticalSliderValue; }
      set {
        if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(VerticalValueRange.x, VerticalValueRange.y, value);
          _localPhysicsPosition.y = Mathf.Lerp(_InitialLocalPosition.y + _VerticalSlideLimits.x, _InitialLocalPosition.y + _VerticalSlideLimits.y, alpha);
          _VerticalSliderValue = value;
        }
      }
    }

    //Slider Values
    protected float _HorizontalSliderValue;
    protected float _VerticalSliderValue;

    //Slide limits normalized to local space
    protected Vector2 _HorizontalSlideLimits;
    protected Vector2 _VerticalSlideLimits;

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
        _HorizontalSliderValue = Mathf.InverseLerp(_InitialLocalPosition.x + _HorizontalSlideLimits.x, _InitialLocalPosition.x + _HorizontalSlideLimits.y, _localPhysicsPosition.x);
        _HorizontalSliderValue = Mathf.Lerp(HorizontalValueRange.x, HorizontalValueRange.y, _HorizontalSliderValue);
      }

      if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
        _VerticalSliderValue = Mathf.InverseLerp(_InitialLocalPosition.y + _VerticalSlideLimits.x, _InitialLocalPosition.y + _VerticalSlideLimits.y, _localPhysicsPosition.y);
        _VerticalSliderValue = Mathf.Lerp(VerticalValueRange.x, VerticalValueRange.y, _VerticalSliderValue);
      }
    }

    protected override Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(Mathf.Clamp((_localPhysicsPosition.x + desiredOffset.x * 0.25f), _InitialLocalPosition.x + _HorizontalSlideLimits.x, _InitialLocalPosition.x + _HorizontalSlideLimits.y),
                         Mathf.Clamp((_localPhysicsPosition.y + desiredOffset.y * 0.25f), _InitialLocalPosition.y + _VerticalSlideLimits.x, _InitialLocalPosition.y + _VerticalSlideLimits.y),
                                     (_localPhysicsPosition.z + desiredOffset.z));
    }
  }
}
