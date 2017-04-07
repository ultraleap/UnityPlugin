using UnityEngine;
using UnityEngine.Events;
namespace Leap.Unity.UI.Interaction {

  ///<summary>
  /// A physics-enabled button. Activation is triggered by physically pushing the button
  /// back to its compressed position.
  ///</summary>
  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractionButton : MonoBehaviour {

    [Header("Motion Configuration")]

    ///<summary> The minimum and maximum heights the button can exist at. </summary>
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 minMaxHeight = new Vector2(0f, 0.02f);

    ///<summary> The height that this button rests at; this value is a lerp in between the min and max height. </summary>
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    public float restingHeight = 0.5f;

    // State Events
    [HideInInspector]
    public UnityEvent OnPress = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnUnpress = new UnityEvent();

    //Public State variables
    ///<summary> Gets whether the button is currently held down. </summary>
    public bool isDepressed { get; protected set; }

    // Protected State Variables

    ///<summary> The interaction object driving interactions for this UI element. </summary>
    protected InteractionBehaviour behaviour;

    ///<summary> The initial position of this element in local space, stored upon Start() </summary>
    protected Vector3 initialLocalPosition;

    ///<summary> The physical position of this element in local space; may diverge from the graphical position. </summary>
    protected Vector3 localPhysicsPosition;

    private Rigidbody _body;
    private Rigidbody _lastDepressor;
    private Vector3 _localDepressorPosition;
    private Vector3 _physicsPosition = Vector3.zero;
    private Vector3 _physicsVelocity = Vector3.back;
    private bool _handIsLeft = false;

    protected virtual void Start() {
      // Initialize Elements
      behaviour = GetComponent<InteractionBehaviour>();
      _body = behaviour.rigidbody;

      // Initialize Positions
      initialLocalPosition = transform.localPosition;
      transform.localPosition = initialLocalPosition;
      localPhysicsPosition = initialLocalPosition;
      _physicsPosition = transform.position;
      _body.position = _physicsPosition;

      // Initialize Limits
      minMaxHeight /= transform.parent.lossyScale.z;

      PhysicsCallbacks.OnPrePhysics += OnPrePhysics;
      PhysicsCallbacks.OnPostPhysics += OnPostPhysics;
    }

    private void OnPrePhysics() {
      // Disable collision on this button if it is not the primary hover
      if (behaviour.isPrimaryHovered) {
        behaviour.ignoreContact = false;
      } else {
        behaviour.ignoreContact = true;
      }

      if (!_body.IsSleeping()) {
        // Sleep the rigidbody if it's not really moving...
        if (_body.position == _physicsPosition && _physicsVelocity == Vector3.zero) {
          _body.Sleep();
          // Else, reset the body's position to where it was last time PhysX looked at it...
        } else {
          _body.position = _physicsPosition;
          _body.velocity = _physicsVelocity;
        }
      }
    }

    private void OnPostPhysics() {
      //Record and enforce the sliding state from the previous frame
      Vector2 localSlidePosition = new Vector2(localPhysicsPosition.x, localPhysicsPosition.y);
      localPhysicsPosition = transform.parent.InverseTransformPoint(_body.position);
      localPhysicsPosition = new Vector3(localSlidePosition.x, localSlidePosition.y, localPhysicsPosition.z);

      // Calculate the physical kinematics of the button in local space
      Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(_body.velocity);
      if (isDepressed && behaviour.isPrimaryHovered && _lastDepressor != null) {
        Vector3 curLocalDepressorPos = transform.parent.InverseTransformPoint(_lastDepressor.position);
        Vector3 origLocalDepressorPos = transform.parent.InverseTransformPoint(transform.TransformPoint(_localDepressorPosition));
        localPhysicsVelocity = Vector3.back * 5f;
        localPhysicsPosition = GetDepressedConstrainedLocalPosition(curLocalDepressorPos - origLocalDepressorPos);
      } else {
        localPhysicsVelocity += Vector3.forward * Mathf.Clamp(((initialLocalPosition.z - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight) - localPhysicsPosition.z) / transform.parent.lossyScale.z), -5f, 5f);
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
        if (behaviour.isPrimaryHovered) {
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
        _handIsLeft = behaviour.primaryHoveringHand.IsLeft;
        behaviour.manager.GetInteractionHand(_handIsLeft).SetInteractionHoverOverride(true);
      } else if (!isDepressed && oldDepressed) {
        OnUnpress.Invoke();
        behaviour.manager.GetInteractionHand(_handIsLeft).SetInteractionHoverOverride(false);
      }
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
  }
}
