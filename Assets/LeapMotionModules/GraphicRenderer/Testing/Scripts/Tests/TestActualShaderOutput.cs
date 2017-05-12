using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.IO;
using System.Collections;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class TestActualShaderOutput : GraphicRendererTestBase {
    public const string FOLDER_NAME = "ShaderOutputTestPrefabs";

    /// <summary>
    /// Just a basic test to verify that the system for reading back
    /// shader values from the GPU actually works.  Applies a small
    /// translation of the graphic itself. 
    /// </summary>
    /// <returns></returns>
    [UnityTest]
    public IEnumerator DoesCorrectlyRenderOutput([Values("OneDynamicGroup",
                                                         "OneCylindricalDynamicGroup",
                                                         "OneSphericalDynamicGroup",
                                                         "OneDynamicGroupWithBlendShapes",
                                                         "OneCylindricalDynamicGroupWithBlendShapes",
                                                         "OneSphericalDynamicGroupWithBlendShapes")]
                                                 string rendererPrefab) {
      InitTest(Path.Combine(FOLDER_NAME, rendererPrefab));
      yield return null;

      //To get around gpu values holding over from previous tests
      Vector3 randomOffset = Random.onUnitSphere;

      oneGraphic.transform.localPosition = Random.onUnitSphere;
      oneGraphic.transform.localRotation = Quaternion.LookRotation(Random.onUnitSphere);
      oneGraphic.transform.localScale = Random.onUnitSphere;

      renderer.transform.position = Random.onUnitSphere;
      renderer.transform.rotation = Quaternion.LookRotation(Random.onUnitSphere);
      renderer.transform.localScale = Random.onUnitSphere;

      var oneMeshGraphic = oneGraphic as LeapMeshGraphicBase;
      oneMeshGraphic.RefreshMeshData();
      var verts = oneMeshGraphic.mesh.vertices;

      Vector3[] deltaVerts = new Vector3[verts.Length];
      Vector3[] deltaNormals = new Vector3[verts.Length];
      Vector3[] deltaTangents = new Vector3[verts.Length];
      if (oneMeshGraphic.mesh.blendShapeCount > 0) {
        oneMeshGraphic.mesh.GetBlendShapeFrameVertices(0, 0, deltaVerts, deltaNormals, deltaTangents);
      }

      yield return null;

      renderer.BeginCollectingVertData();

      yield return null;

      var renderedVerts = renderer.FinishCollectingVertData();

      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i] + deltaVerts[i];
        Vector3 rendererLocalVert = renderer.transform.InverseTransformPoint(oneGraphic.transform.TransformPoint(vert));
        Vector3 warpedLocalVert = oneGraphic.transformer.TransformPoint(rendererLocalVert);
        Vector3 warpedWorldVert = renderer.transform.TransformPoint(warpedLocalVert);

        Vector3 actualShaderVert = renderedVerts[i];

        Assert.That((warpedWorldVert - actualShaderVert).magnitude, Is.Zero.Within(0.0001f), actualShaderVert + " should be equal to " + warpedWorldVert);
      }
    }
  }
}
