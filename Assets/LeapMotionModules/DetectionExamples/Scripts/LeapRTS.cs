using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Use this component on a Game Object to allow it to be manipulated by a pinch gesture.  The component
  /// allows rotation, translation, and scale of the object (RTS).
  /// </summary>
  public class LeapRTS : MonoBehaviour {

    public enum RotationMethod {
      None,
      Single,
      Full
    }

    [SerializeField]
    private RotationMethod _oneHandedRotationMethod;

    [SerializeField]
    private RotationMethod _twoHandedRotationMethod;

    [SerializeField]
    private bool _allowScale = true;

    [Header("GUI Options")]
    [SerializeField]
    private KeyCode _toggleGuiState = KeyCode.None;

    [SerializeField]
    private bool _showGUI = true;

    private Transform _anchor;

    private float _defaultNearClip;

    void Start() {
      GameObject pinchControl = new GameObject("RTS Anchor");
      _anchor = pinchControl.transform;
      _anchor.transform.parent = transform.parent;
      transform.parent = _anchor;
    }

    private Hand _lHand;
    private Hand _rHand;
    private bool _lHandPinchingLastFrame = false;
    private bool _rHandPinchingLastFrame = false;

    void Update() {
      if (Input.GetKeyDown(_toggleGuiState)) {
        _showGUI = !_showGUI;
      }

      _lHand = Hands.Left;
      _rHand = Hands.Right;

      bool didUpdate = false;
      bool lHandIsPinching = _lHand != null && _lHand.IsPinching();
      bool rHandIsPinching = _rHand != null && _rHand.IsPinching();
      if (lHandIsPinching ^ _lHandPinchingLastFrame) {
        didUpdate = true;
      }
      if (rHandIsPinching ^ _rHandPinchingLastFrame) {
        didUpdate = true;
      }
      if (didUpdate) {
        transform.SetParent(null, true);
      }

      _lHandPinchingLastFrame = _lHand != null && _lHand.IsPinching();
      _rHandPinchingLastFrame = _rHand != null && _rHand.IsPinching();

      if (lHandIsPinching && rHandIsPinching) {
        transformDoubleAnchor(_lHand, _rHand);
      }
      else if (lHandIsPinching) {
        transformSingleAnchor(_lHand);
      }
      else if (rHandIsPinching) {
        transformSingleAnchor(_rHand);
      }

      if (didUpdate) {
        transform.SetParent(_anchor, true);
      }
    }

    void OnGUI() {
      if (_showGUI) {
        GUILayout.Label("One Handed Settings");
        doRotationMethodGUI(ref _oneHandedRotationMethod);
        GUILayout.Label("Two Handed Settings");
        doRotationMethodGUI(ref _twoHandedRotationMethod);
        _allowScale = GUILayout.Toggle(_allowScale, "Allow Two Handed Scale");
      }
    }

    private void doRotationMethodGUI(ref RotationMethod rotationMethod) {
      GUILayout.BeginHorizontal();

      GUI.color = rotationMethod == RotationMethod.None ? Color.green : Color.white;
      if (GUILayout.Button("No Rotation")) {
        rotationMethod = RotationMethod.None;
      }

      GUI.color = rotationMethod == RotationMethod.Single ? Color.green : Color.white;
      if (GUILayout.Button("Single Axis")) {
        rotationMethod = RotationMethod.Single;
      }

      GUI.color = rotationMethod == RotationMethod.Full ? Color.green : Color.white;
      if (GUILayout.Button("Full Rotation")) {
        rotationMethod = RotationMethod.Full;
      }

      GUI.color = Color.white;

      GUILayout.EndHorizontal();
    }

    private void transformDoubleAnchor(Hand left, Hand right) {
      _anchor.position = (left.GetPinchPosition() + right.GetPinchPosition()) / 2.0f;

      switch (_twoHandedRotationMethod) {
        case RotationMethod.None:
          break;
        case RotationMethod.Single:
          Vector3 p = left.GetPinchPosition();
          p.y = _anchor.position.y;
          _anchor.LookAt(p);
          break;
        case RotationMethod.Full:
          Quaternion pp = Quaternion.Lerp(left.Rotation.ToQuaternion(), right.Rotation.ToQuaternion(), 0.5f);
          Vector3 u = pp * Vector3.up;
          _anchor.LookAt(left.GetPinchPosition(), u);
          break;
      }

      if (_allowScale) {
        _anchor.localScale = Vector3.one * Vector3.Distance(left.GetPinchPosition(), right.GetPinchPosition());
      }
    }

    private void transformSingleAnchor(Hand hand) {
      _anchor.position = hand.GetPinchPosition();

      switch (_oneHandedRotationMethod) {
        case RotationMethod.None:
          break;
        case RotationMethod.Single:
          Vector3 p = hand.Rotation.ToQuaternion() * Vector3.right;
          p.y = _anchor.position.y;
          _anchor.LookAt(p);
          break;
        case RotationMethod.Full:
          _anchor.rotation = hand.Rotation.ToQuaternion();
          break;
      }

      _anchor.localScale = Vector3.one;
    }

  }

}