using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  public class TorusGen : MeshGenBehaviour {

    [MinValue(0)]
    public float majorRadius = 1F;
    [MinValue(3)]
    public int majorNumSegments = 16;
    [MinValue(0)]
    public float minorRadius = 0.25F;
    [MinValue(3)]
    public int minorNumSegments = 16;

    public override void GenerateMeshInto(List<Vector3> vertCache, List<int> indexCache, List<Vector3> normalCache) {
      MeshGen.GenerateTorus(majorRadius, majorNumSegments, minorRadius, minorNumSegments, vertCache, indexCache, normalCache);
    }
  }

}