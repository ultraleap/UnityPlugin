using UnityEngine;

namespace Leap.Unity.Interaction.Tests {

  [AddComponentMenu("")]
  public class TestInteractionStateColor : MonoBehaviour {

    public InteractionBehaviour intObj;

    private Material _mat;

    void Start() {
      if (intObj == null) {
        intObj = GetComponent<InteractionBehaviour>();
      }

      _mat = intObj.GetComponent<Renderer>().material;
    }

    void Update() {
      if (_mat != null && intObj != null) {
        Color color = Color.white;

        if (intObj.isGrasped) {
          color = Color.green;
        }
        else if (intObj.isPrimaryHovered) {
          color = Color.blue;
        }
        else if (intObj.isHovered) {
          color = Color.cyan;
        }
        else if (intObj.isSuspended) {
          color = Color.red;
        }

        _mat.color = color;
      }
    }

  }

}
