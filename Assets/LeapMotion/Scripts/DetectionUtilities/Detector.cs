using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Leap;

namespace Leap.Unity {

  public class Detector : MonoBehaviour {
    public bool IsActive{ get{ return _isActive;} private set { _isActive = value;}}
    private bool _isActive = false;
    [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
    public bool ShowGizmos = true;
    [Tooltip("Dispatched when condition is detected.")]
    public UnityEvent OnActivate;
    [Tooltip("Dispatched when condition is no longer detected.")]
    public UnityEvent OnDeactivate;

    /**
    * Invoked when this detector activates.
    * Subclasses must call this function when the detector's conditions become true.
    */
    public virtual void Activate(){
      if (!IsActive) {
        IsActive = true;
        OnActivate.Invoke();
      }
      IsActive = true;
    }

    /**
    * Invoked when this detector deactivates.
    * Subclasses must call this function when the detector's conditions change from true to false.
    */
    public virtual void Deactivate(){
      if (IsActive) {
        IsActive = false;
        OnDeactivate.Invoke();
      }
      IsActive = false;
    }
  }

  /** 
  * Settings for handling pointing conditions
  * - RelativeToCamera -- the target direction is defined relative to the camera's forward vector.
  * - RelativeToHorizon -- the target direction is defined relative to the camera's forward vector, 
  *                        except that it does not change with pitch.
  * - RelativeToWorld -- the target direction is defined as a global direction that does not change with camera movement.
  * - AtTarget -- a target object is used as the pointing direction.
  */
  public enum PointingType { RelativeToCamera, RelativeToHorizon, RelativeToWorld, AtTarget }

}
