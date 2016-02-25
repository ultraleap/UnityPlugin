using UnityEngine;

[ExecuteAfter(typeof(LeapPinchDetector))]
public class LeapRTS : MonoBehaviour {

  public enum RotationMethod {
    None,
    Single,
    Full
  }

  [SerializeField]
  private LeapPinchDetector _leftPinch;

  [SerializeField]
  private LeapPinchDetector _rightPinch;

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

  void Awake() {
    GameObject pinchControl = new GameObject("RTS Anchor");
    _anchor = pinchControl.transform;
    _anchor.transform.parent = transform.parent;
    transform.parent = _anchor;
  }

  void Update() {
    if (Input.GetKeyDown(_toggleGuiState)) {
      _showGUI = !_showGUI;
    }

    bool didUpdate = false;
    didUpdate |= _leftPinch.DidChangeFromLastFrame;
    didUpdate |= _rightPinch.DidChangeFromLastFrame;

    if (didUpdate) {
      transform.SetParent(null, true);
    }

    if (_leftPinch.IsPinching && _rightPinch.IsPinching) {
      transformDoubleAnchor();
    } else if (_leftPinch.IsPinching) {
      transformSingleAnchor(_leftPinch);
    } else if (_rightPinch.IsPinching) {
      transformSingleAnchor(_rightPinch);
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

  private void transformDoubleAnchor() {
    _anchor.position = (_leftPinch.Position + _rightPinch.Position) / 2.0f;

    switch (_twoHandedRotationMethod) {
      case RotationMethod.None:
        break;
      case RotationMethod.Single:
        Vector3 p = _leftPinch.Position;
        p.y = _anchor.position.y;
        _anchor.LookAt(p);
        break;
      case RotationMethod.Full:
        Quaternion pp = Quaternion.Lerp(_leftPinch.Rotation, _rightPinch.Rotation, 0.5f);
        Vector3 u = pp * Vector3.up;
        _anchor.LookAt(_leftPinch.Position, u);
        break;
    }

    if (_allowScale) {
      _anchor.localScale = Vector3.one * Vector3.Distance(_leftPinch.Position, _rightPinch.Position);
    }
  }

  private void transformSingleAnchor(LeapPinchDetector singlePinch) {
    _anchor.position = singlePinch.Position;

    switch (_oneHandedRotationMethod) {
      case RotationMethod.None:
        break;
      case RotationMethod.Single:
        Vector3 p = singlePinch.Rotation * Vector3.right;
        p.y = _anchor.position.y;
        _anchor.LookAt(p);
        break;
      case RotationMethod.Full:
        _anchor.rotation = singlePinch.Rotation;
        break;
    }

    _anchor.localScale = Vector3.one;
  }
}
