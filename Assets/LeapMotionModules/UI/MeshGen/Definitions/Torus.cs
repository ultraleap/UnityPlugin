using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  using MeshGenHelperExtensions;

  public static partial class MeshGen {

    private static class TorusSupport {

      public static void AddIndices(List<int> outIndices, int startingVertCount, int numMajorSegments, int numMinorSegments) {
        List<int> i = outIndices;
        int v0 = startingVertCount;

        int totalNumVerts = numMinorSegments * numMajorSegments;

        int ring0StartIdx = 0;
        int ring1StartIdx = numMinorSegments;
        for (int m = 0; m < numMajorSegments; m++) {
          for (int n = 0; n < numMinorSegments; n++) {
            int a = ring0StartIdx + n;
            int b = ring0StartIdx + ((n + 1) % numMinorSegments);
            int c = ring1StartIdx + n;
            int d = ring1StartIdx + ((n + 1) % numMinorSegments);
            i.AddTri(v0, a, c, b);
            i.AddTri(v0, b, c, d);
          }

          ring0StartIdx += numMinorSegments;
          ring1StartIdx += numMinorSegments;
          ring1StartIdx %= totalNumVerts;
        }
      }

      public static void AddVerts(List<Vector3> outVerts, List<Vector3> outNormals, float majorRadius, int numMajorSegments, float minorRadius, int numMinorSegments) {
        List<Vector3> v = outVerts;

        Vector3 majorNormal = Vector3.up;
        Vector3 majorRadialDir = Vector3.right;
        Vector3 minorNormal = Vector3.forward;
        float majorTheta = 360F / numMajorSegments;
        float minorTheta = 360F / numMinorSegments;

        Quaternion majorRotation = Quaternion.AngleAxis(majorTheta, majorNormal);
        for (int i = 0; i < numMajorSegments; i++) {
          for (int j = 0; j < numMinorSegments; j++) {
            Vector3 minorRadial = Quaternion.AngleAxis(minorTheta * j, minorNormal) * (majorRadialDir * minorRadius);
            v.Add((majorRadialDir * majorRadius) + minorRadial);
            outNormals.Add(minorRadial.normalized);
          }
          majorRadialDir = majorRotation * majorRadialDir;
          minorNormal = majorRotation * minorNormal;
        }
      }
    }

  }

}