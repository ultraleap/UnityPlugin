/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.IO;
using System.Collections;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class TestActualShaderOutput : GraphicRendererTestBase {
    public const string FOLDER_NAME = "ShaderOutputTestPrefabs";

    /// <summary>
    /// Test to verify that dynamic renderers correctly distort
    /// the vertices of a graphic in all situations.
    /// </summary>
    [UnityTest]
    public IEnumerator DoesCorrectlyRenderDynamicOutput([Values("OneDynamicGroup",
                                                                "OneCylindricalDynamicGroup",
                                                                "OneSphericalDynamicGroup",
                                                                "OneDynamicGroupWithBlendShapes",
                                                                "OneCylindricalDynamicGroupWithBlendShapes",
                                                                "OneSphericalDynamicGroupWithBlendShapes")]
                                                        string rendererPrefab) {
      InitTest(Path.Combine(FOLDER_NAME, rendererPrefab));
      yield return null;

      randomizeGraphicTransform();
      randomizeRendererTransform();
      initRendererAndGraphic();

      yield return null;

      renderer.BeginCollectingVertData();

      yield return null;

      assertVertsAreEqual();
    }

    /// <summary>
    /// Test to verify that baked renderers correctly distort
    /// the vertices of a graphic in all situations.
    /// </summary>
    [UnityTest]
    public IEnumerator DoesCorrectlyRenderStationaryBakedOutput([Values("OneBakedGroup",
                                                                        "OneCylindricalBakedGroup",
                                                                        "OneSphericalBakedGroup",
                                                                        "OneBakedGroupWithBlendShapes",
                                                                        "OneCylindricalBakedGroupWithBlendShapes",
                                                                        "OneSphericalBakedGroupWithBlendShapes")]
                                                                string rendererName) {
      LoadScene("StationaryBakedRendererShaderTestScene");
      yield return null;
      InitTest(rendererName);
      yield return null;

      randomizeRendererTransform();
      initRendererAndGraphic();

      yield return null;

      renderer.BeginCollectingVertData();

      yield return null;

      assertVertsAreEqual();
    }

    /// <summary>
    /// Test to verify that baked renderers correctly distort
    /// the vertices of a graphic in all situations.
    /// </summary>
    [UnityTest]
    public IEnumerator DoesCorrectlyRenderTranslationBakedOutput([Values("OneBakedGroup",
                                                                         "OneCylindricalBakedGroup",
                                                                         "OneSphericalBakedGroup",
                                                                         "OneBakedGroupWithBlendShapes",
                                                                         "OneCylindricalBakedGroupWithBlendShapes",
                                                                         "OneSphericalBakedGroupWithBlendShapes")]
                                                                 string rendererName) {
      LoadScene("TranslationBakedRendererShaderTestScene");
      yield return null;
      InitTest(rendererName);
      yield return null;

      randomizeGraphicPosition();
      randomizeRendererTransform();
      initRendererAndGraphic();

      yield return null;

      renderer.BeginCollectingVertData();

      yield return null;

      assertVertsAreEqual();
    }

    private Vector3[] verts;

    private void randomizeGraphicPosition() {
      oneGraphic.transform.localPosition = Random.onUnitSphere;
    }

    private void randomizeGraphicTransform() {
      oneGraphic.transform.localPosition = Random.onUnitSphere;
      oneGraphic.transform.localRotation = Quaternion.LookRotation(Random.onUnitSphere);
      oneGraphic.transform.localScale = Random.onUnitSphere;
    }

    private void randomizeRendererTransform() {
      renderer.transform.position = Random.onUnitSphere;
      renderer.transform.rotation = Quaternion.LookRotation(Random.onUnitSphere);
      renderer.transform.localScale = Random.onUnitSphere;
    }

    private void initRendererAndGraphic() {
      var oneMeshGraphic = oneGraphic as LeapMeshGraphicBase;
      oneMeshGraphic.RefreshMeshData();
      verts = oneMeshGraphic.mesh.vertices;

      Vector3[] deltaVerts = new Vector3[verts.Length];
      Vector3[] deltaNormals = new Vector3[verts.Length];
      Vector3[] deltaTangents = new Vector3[verts.Length];
      if (oneMeshGraphic.mesh.blendShapeCount > 0 && oneGraphic.featureData.Query().OfType<LeapBlendShapeData>().Any()) {
        oneMeshGraphic.mesh.GetBlendShapeFrameVertices(0, 0, deltaVerts, deltaNormals, deltaTangents);

        for (int i = 0; i < verts.Length; i++) {
          verts[i] += deltaVerts[i];
        }
      }
    }

    private void assertVertsAreEqual() {
      var renderedVerts = renderer.FinishCollectingVertData();

      for (int i = 0; i < verts.Length; i++) {
        Vector3 vert = verts[i];
        Vector3 rendererLocalVert = renderer.transform.InverseTransformPoint(oneGraphic.transform.TransformPoint(vert));
        Vector3 warpedLocalVert = oneGraphic.transformer.TransformPoint(rendererLocalVert);
        Vector3 warpedWorldVert = renderer.transform.TransformPoint(warpedLocalVert);

        Vector3 actualShaderVert = renderedVerts[i];

        Assert.That((warpedWorldVert - actualShaderVert).magnitude, Is.Zero.Within(0.0001f), actualShaderVert + " should be equal to " + warpedWorldVert);
      }
    }
  }
}
#endif
