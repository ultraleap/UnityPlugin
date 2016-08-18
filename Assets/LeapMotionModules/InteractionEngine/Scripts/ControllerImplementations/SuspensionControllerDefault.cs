using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.Interaction {

  /**
  * The SuspensionControllerDefault class turns off rendering of suspended objects
  * and restores rendering when the suspension times out or the interaction 
  * simulation resumes.
  * @since 4.1.4
  */
  public class SuspensionControllerDefault : ISuspensionController {
    [SerializeField]
    private float _maxSuspensionTime = 4;

    private Renderer[] _renderers;

    protected override void Init(InteractionBehaviour obj) {
      base.Init(obj);

      _renderers = obj.GetComponentsInChildren<Renderer>();
    }

    /** The timeout period. */
    public override float MaxSuspensionTime {
      get {
        return _maxSuspensionTime;
      }
    }

    /** Resumes rendering of the object. */
    public override void Resume() {
      setRendererState(true);
    }

    /** Suspends rendering of the object and sets the IsKinematic property of its rigid body to true. */
    public override void Suspend() {
      _obj.rigidbody.isKinematic = true;
      setRendererState(false);
    }

    /** Resumes rendering of the object. */
    public override void Timeout() {
      setRendererState(true);
    }

    private void setRendererState(bool visible) {
      for (int i = 0; i < _renderers.Length; i++) {
        _renderers[i].enabled = visible;
      }
    }

    /** Validates that the object remains kinematic when it is suspended. */
    public override void Validate() {
      base.Validate();

      if (_obj.UntrackedHandCount != 0) {
        Assert.IsTrue(_obj.rigidbody.isKinematic,
                      "Object must be kinematic when suspended.");
      }
    }
  }
}
