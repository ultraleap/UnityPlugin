using UnityEngine;
using UnityEngine.Events;
namespace Leap.Unity.UI.Interaction {

  ///<summary>
  /// A physics-enabled button. Activation is triggered by physically pushing the button
  /// back to its compressed position.
  ///</summary>
  public class InteractionButton : InteractionBehaviour {

    [Header("Motion Configuration")]

    ///<summary> The minimum and maximum heights the button can exist at. </summary>
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 minMaxHeight = new Vector2(0f, 0.02f);

    ///<summary> The height that this button rests at; this value is a lerp in between the min and max height. </summary>
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    public float restingHeight = 0.5f;

    [Range(0, 1)]
    [SerializeField]
    private float _springForce = 0.1f;

    // State Events
    public UnityEvent OnPress = new UnityEvent();
    public UnityEvent OnUnpress = new UnityEvent();

    public float springForce {
      get {
        return _springForce;
      }
      set {
        _springForce = value;
      }
    }

    //Public State variables
    ///<summary> Gets whether the button is currently held down. </summary>
    public bool isDepressed { get; protected set; }

    ///<summary> Gets whether the button was pressed during this Update frame. </summary>
    public bool depressedThisFrame { get; protected set; }

    ///<summary> Gets whether the button was unpressed during this Update frame. </summary>
    public bool unDepressedThisFrame { get; protected set; }

    // Protected State Variables

    ///<summary> The initial position of this element in local space, stored upon Start() </summary>
    protected Vector3 initialLocalPosition;

    ///<summary> The physical position of this element in local space; may diverge from the graphical position. </summary>
    protected Vector3 localPhysicsPosition;

    private Rigidbody _lastDepressor;
    private Vector3 _localDepressorPosition;
    private Vector3 _physicsPosition = Vector3.zero;
    private Vector3 _physicsVelocity = Vector3.back;
    private bool _handIsLeft = false;
    private bool _physicsOccurred;

    protected override void Start() {
      // Initialize Positions
      initialLocalPosition = transform.localPosition;
      transform.localPosition = initialLocalPosition;
      localPhysicsPosition = initialLocalPosition;
      _physicsPosition = transform.position;
      rigidbody.position = _physicsPosition;

      // Initialize Limits
      minMaxHeight /= transform.parent.lossyScale.z;

      base.Start();
    }

    protected virtual void FixedUpdate() {
      if (!_physicsOccurred) {
        _physicsOccurred = true;

        if (!rigidbody.IsSleeping()) {
          //Sleep the rigidbody if it's not really moving...

          float localPhysicsDisplacementPercentage = Mathf.InverseLerp(minMaxHeight.x, minMaxHeight.y, initialLocalPosition.z - localPhysicsPosition.z);
          if (rigidbody.position == _physicsPosition && _physicsVelocity == Vector3.zero && Mathf.Abs(localPhysicsDisplacementPercentage - restingHeight) < 0.01f) {
            rigidbody.Sleep();
            //Else, reset the body's position to where it was last time PhysX looked at it...
          } else {
            rigidbody.position = _physicsPosition;
            rigidbody.velocity = _physicsVelocity;
          }
        }
      }
    }

    protected virtual void Update() {
      //Reset our convenience state variables...
      depressedThisFrame = false;
      unDepressedThisFrame = false;

      //Apply physical corrections only if PhysX has modified our positions
      if (_physicsOccurred) {
        _physicsOccurred = false;

        //Record and enforce the sliding state from the previous frame
        Vector2 localSlidePosition = new Vector2(localPhysicsPosition.x, localPhysicsPosition.y);
        localPhysicsPosition = transform.parent.InverseTransformPoint(rigidbody.position);
        localPhysicsPosition = new Vector3(localSlidePosition.x, localSlidePosition.y, localPhysicsPosition.z);

        // Calculate the physical kinematics of the button in local space
        Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(rigidbody.velocity);
        if (isDepressed && isPrimaryHovered && _lastDepressor != null) {
          Vector3 curLocalDepressorPos = transform.parent.InverseTransformPoint(_lastDepressor.position);
          Vector3 origLocalDepressorPos = transform.parent.InverseTransformPoint(transform.TransformPoint(_localDepressorPosition));
          localPhysicsVelocity = Vector3.back * 0.05f / transform.parent.lossyScale.z;
          localPhysicsPosition = GetDepressedConstrainedLocalPosition(curLocalDepressorPos - origLocalDepressorPos);
        } else {
          localPhysicsVelocity += _springForce * Vector3.forward * (initialLocalPosition.z - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight) - localPhysicsPosition.z) / Time.fixedDeltaTime;
        }

        // Transform the local physics back into world space
        _physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        _physicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);

        // Calculate the Depression State of the Button from its Physical Position
        // Set its Graphical Position to be Constrained Physically
        bool oldDepressed = isDepressed;

        // If the button is depressed past its limit...
        if (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x);
          if (isPrimaryHovered) {
            isDepressed = true;
          } else {
            _physicsPosition = transform.parent.TransformPoint(initialLocalPosition);
            _physicsVelocity = _physicsVelocity * 0.1f;
            isDepressed = false;
            _lastDepressor = null;
          }
          // Else if the button is extended past its limit...
        } else if (localPhysicsPosition.z < initialLocalPosition.z - minMaxHeight.y) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.y);
          _physicsPosition = transform.position;
          isDepressed = false;
          _lastDepressor = null;
        } else {
          // Else, just make the physical and graphical motion of the button match
          transform.localPosition = localPhysicsPosition;
          isDepressed = false;
          _lastDepressor = null;
        }

        // If our depression state has changed since last time...
        if (isDepressed && !oldDepressed) {
          OnPress.Invoke();
          depressedThisFrame = true;
          _handIsLeft = primaryHoveringHand.IsLeft;
          manager.GetInteractionHand(_handIsLeft).SetInteractionHoverOverride(true);
        } else if (!isDepressed && oldDepressed) {
          unDepressedThisFrame = true;
          OnUnpress.Invoke();
          manager.GetInteractionHand(_handIsLeft).SetInteractionHoverOverride(false);
        }
      }

      //Disable collision on this button if it is not the primary hover
      ignoreContact = !isPrimaryHovered;
    }

    // How the button should behave when it is depressed
    protected virtual Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, localPhysicsPosition.z + desiredOffset.z);
    }

    protected virtual void OnCollisionEnter(Collision collision) { trySetDepressor(collision); }
    protected virtual void OnCollisionStay(Collision collision) { trySetDepressor(collision); }

    // Try grabbing the offset between the fingertip and this object...
    private void trySetDepressor(Collision collision) {
      if (collision.rigidbody != null && _lastDepressor == null && isDepressed) {
        _lastDepressor = collision.rigidbody;
        _localDepressorPosition = transform.InverseTransformPoint(collision.rigidbody.position);
      }
    }

    public void setMinHeight(float minHeight) {
      minMaxHeight = new Vector2(minHeight / transform.parent.lossyScale.z, minMaxHeight.y);
    }
    public void setMaxHeight(float maxHeight) {
      minMaxHeight = new Vector2(minMaxHeight.x, maxHeight / transform.parent.lossyScale.z);
    }
  }
}
