using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  public static partial class MeshGen {

    public static void GenerateRoundedRectPrism(Vector3 extents, float cornerRadius, int cornerDivisions, List<Vector3> outVerts, List<int> outIndices) {
      RoundedRectPrismSupport.AddFrontIndices(outIndices, outVerts.Count, cornerDivisions);
      RoundedRectPrismSupport.AddFrontVerts(outVerts, extents, cornerRadius, cornerDivisions);
      //RoundedRectPrism.AddFrontUVs(outVerts, cornerDivisions); // NYI

      RoundedRectPrismSupport.AddSideIndices(outIndices, outVerts.Count, cornerDivisions);
      RoundedRectPrismSupport.AddSideVerts(outVerts, extents, cornerRadius, cornerDivisions);
      //RoundedRectPrism.AddSideUVs(outVerts, cornerDivisions); // NYI
    }

  }

  #region Helper Extensions

  namespace MeshGenHelperExtensions {

    static class TriMeshExtensions {

      public static void AddTri(this List<int> indices, int idx1, int idx2, int idx3) {
        indices.Add(idx1); indices.Add(idx2); indices.Add(idx3);
      }
      public static void AddTri(this List<int> indices, int vertexOffset, int idx1, int idx2, int idx3) {
        indices.Add(vertexOffset + idx1); indices.Add(vertexOffset + idx2); indices.Add(vertexOffset + idx3);
      }

      public static void AddVert(this List<Vector3> verts, float x, float y, float z) {
        verts.Add(new Vector3(x, y, z));
      }

    }

  }

  #endregion

}