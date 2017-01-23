using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Gui.Space {

  [RequireComponent(typeof(LeapElement))]
  public class LeapPanel : ProceduralMeshSource {

    [MinValue(2)]
    [SerializeField]
    private int _resolutionX;

    [MinValue(2)]
    [SerializeField]
    private int _resolutionY;

    [SerializeField]
    private bool _isNineSliced = true;

    public override Mesh GetMesh() {
      throw new System.NotImplementedException();
    }

    public override bool DoesMeshHaveAtlasUvs(int uvChannel) {
      throw new System.NotImplementedException();
    }
  }
}
