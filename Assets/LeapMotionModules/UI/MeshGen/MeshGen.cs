using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI {

  public static partial class MeshGen {

    public static void GenerateRoundedRectPrism(Vector3 extents, float cornerRadius, int cornerDivisions, List<Vector3> outVerts, List<int> outIndices, bool withBack = true) {
      RoundedRectPrismSupport.AddFrontIndices(outIndices, outVerts.Count, cornerDivisions);
      RoundedRectPrismSupport.AddFrontVerts(outVerts, extents, cornerRadius, cornerDivisions);
      //RoundedRectPrism.AddFrontUVs(); // NYI

      RoundedRectPrismSupport.AddSideIndices(outIndices, outVerts.Count, cornerDivisions);
      RoundedRectPrismSupport.AddSideVerts(outVerts, extents, cornerRadius, cornerDivisions);
      //RoundedRectPrism.AddSideUVs(); // NYI

      if (withBack) {
        Vector3 extentsForBack = new Vector3(extents.x, extents.y, 0F);
        RoundedRectPrismSupport.AddFrontIndices(outIndices, outVerts.Count, cornerDivisions, flipFacing: true);
        RoundedRectPrismSupport.AddFrontVerts(outVerts, extentsForBack, cornerRadius, cornerDivisions);
        //RoundedRectPrism.AddBackUVs(); // NYI
      }
    }

  }

  #region Helper Extensions

  namespace MeshGenHelperExtensions {

    static class TriMeshExtensions {

      public static void AddTri(this List<int> indices, int idx1, int idx2, int idx3, bool flipFacing=false) {
        if (flipFacing) {
          int temp = idx2;
          idx2 = idx3;
          idx3 = temp;
        }
        indices.Add(idx1); indices.Add(idx2); indices.Add(idx3);
      }
      public static void AddTri(this List<int> indices, int vertexOffset, int idx1, int idx2, int idx3, bool flipFacing=false) {
        indices.AddTri(vertexOffset + idx1, vertexOffset + idx2, vertexOffset + idx3, flipFacing);
      }

      public static void AddVert(this List<Vector3> verts, float x, float y, float z) {
        verts.Add(new Vector3(x, y, z));
      }

    }

  }

  #endregion

}