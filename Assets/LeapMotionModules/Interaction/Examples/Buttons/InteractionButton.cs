using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled button. Activation is triggered by physically pushing the button back to its compressed position.
   */
  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractionButton : MonoBehaviour {
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 MinMaxHeight = new Vector2(0f, 0.02f);
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f,1f)]
    public float RestingHeight = 0.5f;

    //State Events
    [HideInInspector]
    public UnityEvent OnPress = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnUnpress = new UnityEvent();

    //Public State variables
    public bool IsDepressed { get; protected set; }
    public bool DepressedThisFrame { get; protected set; }
    public bool UnDepressedThisFrame { get; protected set; }

    //Protected State Variables
    protected InteractionBehaviour _behaviour;
    protected LeapGraphic _element;
    protected Rigidbody _body;
    protected Rigidbody _lastDepressor;
    protected Vector3 _localDepressor;
    protected Vector3 _InitialLocalPosition;
    protected Vector3 _PhysicsPosition = Vector3.zero;
    protected Vector3 _PhysicsVelocity = Vector3.back;
    protected Vector3 _localPhysicsPosition;
    protected bool _physicsOccurred = false;
    protected bool _handChirality = false;

    protected virtual void Start() {
      //Initialize Elements
      _behaviour = GetComponent<InteractionBehaviour>();
      _element = GetComponent<LeapGraphic>();
      _body = _behaviour.rigidbody;

      //Initialize Positions
      _InitialLocalPosition = transform.localPosition;
      transform.localPosition = _InitialLocalPosition;
      _localPhysicsPosition = _InitialLocalPosition;
      _PhysicsPosition = transform.position;
      _body.position = _PhysicsPosition;

      //Initialize Limits
      MinMaxHeight /= transform.parent.lossyScale.z;
    }

    protected virtual void FixedUpdate() {
      if (!_physicsOccurred) {
        _physicsOccurred = true;

        if (!_body.IsSleeping()) {
          //Sleep the rigidbody if it's not really moving...
          if (_body.position == _PhysicsPosition && _PhysicsVelocity == Vector3.zero) {
            _body.Sleep();
          //Else, reset the body's position to where it was last time PhysX looked at it...
          } else {
            _body.position = _PhysicsPosition;
            _body.velocity = _PhysicsVelocity;
          }
        }
      }
    }

    protected virtual void Update() {
      //Reset our convenience state variables...
      DepressedThisFrame = false;
      UnDepressedThisFrame = false;

      //Apply physical corrections only if PhysX has modified our positions
      if (_physicsOccurred) {
        _physicsOccurred = false;

        //Record and enforce the sliding state from the previous frame
        Vector2 localSlidePosition = new Vector2(_localPhysicsPosition.x, _localPhysicsPosition.y);
        _localPhysicsPosition = transform.parent.InverseTransformPoint(_body.position);
        _localPhysicsPosition = new Vector3(localSlidePosition.x, localSlidePosition.y, _localPhysicsPosition.z);

        //Calculate the physical kinematics of the button in local space
        Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(_body.velocity);
        if (IsDepressed && _behaviour.isPrimaryHovered && _lastDepressor != null) {
          Vector3 curLocalDepressor = transform.parent.InverseTransformPoint(_lastDepressor.position);
          Vector3 origLocalDepressor = transform.parent.InverseTransformPoint(transform.TransformPoint(_localDepressor));
          localPhysicsVelocity = Vector3.back*5f;
          _localPhysicsPosition = GetDepressedConstrainedLocalPosition(curLocalDepressor - origLocalDepressor);
        } else {
          localPhysicsVelocity += Vector3.forward * Mathf.Clamp(((_InitialLocalPosition.z - Mathf.Lerp(MinMaxHeight.x, MinMaxHeight.y, RestingHeight) - _localPhysicsPosition.z) / transform.parent.lossyScale.z), -5f, 5f);
        }

        //Transform the local physics back into world space
        _PhysicsPosition = transform.parent.TransformPoint(_localPhysicsPosition);
        _PhysicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);

        //Calculate the Depression State of the Button from its Physical Position
        //Set its Graphical Position to be Constrained Physically
        bool oldDepressed = IsDepressed;

        //If the button is depressed past its limit...
        if (_localPhysicsPosition.z > _InitialLocalPosition.z - MinMaxHeight.x) {
          transform.localPosition = new Vector3(_localPhysicsPosition.x, _localPhysicsPosition.y, _InitialLocalPosition.z - MinMaxHeight.x);
          if (_behaviour.isPrimaryHovered) {
            IsDepressed = true;
          } else {
            _PhysicsPosition = transform.parent.TransformPoint(_InitialLocalPosition);
            _PhysicsVelocity = _PhysicsVelocity * 0.1f;
            IsDepressed = false;
            _lastDepressor = null;
          }
        //Else if the button is extended past its limit...
        } else if (_localPhysicsPosition.z < _InitialLocalPosition.z - MinMaxHeight.y) {
          transform.localPosition = new Vector3(_localPhysicsPosition.x, _localPhysicsPosition.y, _InitialLocalPosition.z - MinMaxHeight.y);
          _PhysicsPosition = transform.position;
          IsDepressed = false;
          _lastDepressor = null;
        } else {
        //Else, just make the physical and graphical motion of the button match
          transform.localPosition = _localPhysicsPosition;
          IsDepressed = false;
          _lastDepressor = null;
        }

        //If our depression state has changed since last time...
        if (IsDepressed && !oldDepressed) {
          DepressedThisFrame = true;
          OnPress.Invoke();
          _handChirality = _behaviour.primaryHoveringHand.IsLeft;
          _behaviour.manager.GetInteractionHand(_handChirality).SetInteractionHoverOverride(true);
        } else if (!IsDepressed && oldDepressed) {
          UnDepressedThisFrame = true;
          OnUnpress.Invoke();
          _behaviour.manager.GetInteractionHand(_handChirality).SetInteractionHoverOverride(false);
        }
      }

      //Disable collision on this button if it is not the primary hover
      if (_element != null) {
        if (_behaviour.isPrimaryHovered) {
          _behaviour.ignoreContact = false;
        } else {
          _behaviour.ignoreContact = true;
        }
      }
    }

    //How the button should behave when it is depressed
    protected virtual Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(_localPhysicsPosition.x, _localPhysicsPosition.y, _localPhysicsPosition.z + desiredOffset.z);
    }

    //Try grabbing the offset between the fingertip and this object...
    void trySetDepressor(Collision collision) {
      if (collision.rigidbody != null && _lastDepressor == null && IsDepressed) {
        _lastDepressor = collision.rigidbody;
        _localDepressor = transform.InverseTransformPoint(collision.rigidbody.position);
      }
    }

    void OnCollisionEnter(Collision collision) { trySetDepressor(collision); }
    void OnCollisionStay(Collision collision) { trySetDepressor(collision); }
  }
}