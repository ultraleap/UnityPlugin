using UnityEngine;
using System;
using Leap.Unity.Attributes;
using Procedural.DynamicPath;

namespace Procedural.DynamicMesh {

  public class PathDistortModifier : ModifierBehaviour<PathDistortMod>, IPreGenerate {

    [AutoFind(AutoFindLocations.Children)]
    [SerializeField]
    private PathBehaviourBase _pathBehaviour;

    public bool OnPreGenerate() {
      if (_pathBehaviour == null) {
        return false;
      }

      _modifier.path = _pathBehaviour.Path;
      _modifier.toPathSpace = _pathBehaviour.transform.worldToLocalMatrix * transform.localToWorldMatrix;
      return true;
    }
  }

  [Serializable]
  public struct PathDistortMod : IMeshMod {
    public IPath path;
    public Matrix4x4 toPathSpace;

    public void Modify(ref RawMesh input) {
      if (path == null) {
        return;
      }

      Matrix4x4 fromPathSpace = toPathSpace.inverse;
      for (int i = input.verts.Count; i-- != 0;) {
        Vector3 v = toPathSpace.MultiplyPoint(input.verts[i]);
        Vector3 offset = path.GetPosition(-v.x);
        v.x = 0;
        v += offset;

        input.verts[i] = fromPathSpace.MultiplyPoint(v);
      }

      input.normals = null;
    }
  }
}
