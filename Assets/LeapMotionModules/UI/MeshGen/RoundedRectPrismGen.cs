using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  public class RoundedRectPrismGen : MeshGenBehaviour {

    [MinValue(0)]
    public Vector3 extents = new Vector3(1F, 1F, 0.5F);
    [MinValue(0)]
    public float cornerRadius = 0.2F;
    [MinValue(0)]
    public int cornerDivisions = 5;
    public bool withBack = true;

    public override void GenerateMeshInto(List<Vector3> vertCache, List<int> indexCache, List<Vector3> normalCache) {
      MeshGen.GenerateRoundedRectPrism(extents, cornerRadius, cornerDivisions, vertCache, indexCache, normalCache, withBack);
    }
  }

}


