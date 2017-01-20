using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  public abstract class GuiSpace : MonoBehaviour {
    public abstract Vector3 FromRect(Vector3 rectPos);
    public abstract Vector3 ToRect(Vector3 guiPos);
    public abstract void FromRect(Vector3 rectPos, Quaternion rectRot, out Vector3 guiPos, out Quaternion guiRot);
    public abstract void ToRect(Vector3 guiPos, Quaternion guiRot, out Vector3 rectPos, out Quaternion rectRot);

    protected abstract string ShaderVariantName { get; }
  }
}
