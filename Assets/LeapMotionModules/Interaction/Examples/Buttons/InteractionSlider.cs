using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled button. Activation is triggered by physically pushing the button back to its compressed position. 
   *  Increasing the horizontal and vertical slide limits allows it to act as a 1D or 2D slider
   */
  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractionSlider : MonoBehaviour {
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 MinMaxHeight = new Vector2(0f, 0.02f);
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    public float RestingHeight = 0.5f;
    [Tooltip("The minimum and maximum horizontal extents that the slider can slide to.")]
    public Vector2 HorizontalSlideLimits = new Vector2(-0.05f, 0.05f);
    [Tooltip("The minimum and maximum vertical extents that the slider can slide to.")]
    public Vector2 VerticalSlideLimits = new Vector2(0f, 0f);

    //State Events
    public UnityEvent OnPress = new UnityEvent();
    public UnityEvent OnUnpress = new UnityEvent();

    //Public State variables
    public bool isDepressed { get; protected set; }
    public bool depressedThisFrame { get; protected set; }
    public bool unDepressedThisFrame { get; protected set; }

    //Private State Variables
    private InteractionBehaviour behaviour;
    private LeapGuiElement element;
    private Rigidbody body;
    private Rigidbody lastDepressor;
    private Vector3 localDepressor;
    private Vector3 InitialLocalPosition;
    private Vector3 PhysicsPosition = Vector3.zero;
    private Vector3 PhysicsVelocity = Vector3.back;
    private Vector3 localPhysicsPosition;
    private bool physicsOccurred = false;

    void Start() {
      //Initialize Elements
      behaviour = GetComponent<InteractionBehaviour>();
      element = GetComponent<LeapGuiElement>();
      body = behaviour.rigidbody;

      //Initialize Positions
      InitialLocalPosition = transform.localPosition;
      transform.localPosition = InitialLocalPosition;
      localPhysicsPosition = InitialLocalPosition;
      PhysicsPosition = transform.position;
      body.position = PhysicsPosition;

      //Initialize Limits
      MinMaxHeight /= transform.parent.lossyScale.z;
      HorizontalSlideLimits /= transform.parent.lossyScale.x;
      VerticalSlideLimits /= transform.parent.lossyScale.y;
    }

    void FixedUpdate() {
      if (!physicsOccurred) {
        physicsOccurred = true;

        if (!body.IsSleeping()) {
          //Sleep the rigidbody if it's not really moving...
          if (body.position == PhysicsPosition && PhysicsVelocity == Vector3.zero) {
            body.Sleep();
          //Else, reset the body's position to where it was last time PhysX looked at it...
          } else {
            body.position = PhysicsPosition;
            body.velocity = PhysicsVelocity;
          }
        }
      }
    }

    void Update() {
      //Reset our convenience state variables...
      depressedThisFrame = false;
      unDepressedThisFrame = false;

      //Apply physical corrections only if PhysX has modified our positions
      if (physicsOccurred) {
        physicsOccurred = false;

        //Record and enforce the sliding state from the previous frame
        Vector2 localSlidePosition = new Vector2(localPhysicsPosition.x, localPhysicsPosition.y);
        localPhysicsPosition = transform.parent.InverseTransformPoint(body.position);
        localPhysicsPosition = new Vector3(localSlidePosition.x, localSlidePosition.y, localPhysicsPosition.z);

        //Calculate the physical kinematics of the button in local space
        Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(body.velocity);
        if (isDepressed && behaviour.isPrimaryHovered && lastDepressor != null) {
          Vector3 curLocalDepressor = transform.InverseTransformPoint(lastDepressor.position);
          localPhysicsVelocity = new Vector3(0f, 0f, (curLocalDepressor - localDepressor).z) / Time.fixedDeltaTime;
          localPhysicsPosition = new Vector3(Mathf.Clamp((localPhysicsPosition.x + (curLocalDepressor - localDepressor).x * 0.1f), InitialLocalPosition.x + HorizontalSlideLimits.x, InitialLocalPosition.x + HorizontalSlideLimits.y),
                                             Mathf.Clamp((localPhysicsPosition.y + (curLocalDepressor - localDepressor).y * 0.1f), InitialLocalPosition.y + VerticalSlideLimits.x, InitialLocalPosition.y + VerticalSlideLimits.y),
                                             (localPhysicsPosition.z + (curLocalDepressor - localDepressor).z * 0.1f));
        } else {
          localPhysicsVelocity += Vector3.forward * Mathf.Clamp(((InitialLocalPosition.z - Mathf.Lerp(MinMaxHeight.x, MinMaxHeight.y, RestingHeight) - localPhysicsPosition.z) / transform.parent.lossyScale.z), -5f, 5f);
        }

        //Transform the local physics back into world space
        PhysicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        PhysicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);

        //Calculate the Depression State of the Button from its Physical Position
        //Set its Graphical Position to be Constrained Physically
        bool oldDepressed = isDepressed;

        //If the button is depressed past its limit...
        if (localPhysicsPosition.z > InitialLocalPosition.z - MinMaxHeight.x) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, InitialLocalPosition.z - MinMaxHeight.x);
          if (behaviour.isPrimaryHovered) {
            isDepressed = true;
          } else {
            PhysicsPosition = transform.parent.TransformPoint(InitialLocalPosition);
            PhysicsVelocity = PhysicsVelocity * 0.1f;
            isDepressed = false;
            lastDepressor = null;
          }
        //Else if the button is extended past its limit...
        } else if (localPhysicsPosition.z < InitialLocalPosition.z - MinMaxHeight.y) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, InitialLocalPosition.z - MinMaxHeight.y);
          PhysicsPosition = transform.position;
          isDepressed = false;
          lastDepressor = null;
        } else {
        //Else, just make the physical and graphical motion of the button match
          transform.localPosition = localPhysicsPosition;
          isDepressed = false;
          lastDepressor = null;
        }

        //If our depression state has changed since last time...
        if (isDepressed && !oldDepressed) {
          depressedThisFrame = true;
          OnPress.Invoke();
          behaviour.interactionManager.GetInteractionHand(behaviour.primaryHoveringHand).SetInteractionHoverOverride(true);
        } else if (!isDepressed && oldDepressed) {
          unDepressedThisFrame = true;
          OnUnpress.Invoke();
          behaviour.interactionManager.GetInteractionHand(behaviour.primaryHoveringHand).SetInteractionHoverOverride(false);
        }
      }

      //Disable collision on this button if it is not the primary hover
      if (element != null) {
        if (behaviour.isPrimaryHovered) {
          element.Tint().tint = isDepressed ? Color.red : Color.Lerp(Color.white, Color.red, 0.5f);
          behaviour.ignoreContact = false;
        } else {
          element.Tint().tint = Color.white;
          behaviour.ignoreContact = true;
        }
      }
    }

    //Try grabbing the offset between the fingertip and this object...
    void trySetDepressor(Collision collision) {
      if (collision.rigidbody != null && lastDepressor == null && isDepressed) {
        lastDepressor = collision.rigidbody;
        localDepressor = transform.InverseTransformPoint(collision.rigidbody.position);
      }
    }

    void OnCollisionEnter(Collision collision) { trySetDepressor(collision); }
    void OnCollisionStay(Collision collision) { trySetDepressor(collision); }
  }
}