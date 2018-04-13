/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public static class AccessShaderOutputExtensions {

    private static ComputeBuffer _buffer;

    public static void BeginCollectingVertData(this LeapGraphicRenderer renderer) {
      Camera targetCamera = renderer.GetComponentInChildren<Camera>();
      var renderingMethod = renderer.groups[0].renderingMethod as LeapMesherBase;
      var material = renderingMethod.material;

      var graphic = renderer.groups[0].graphics[0] as LeapMeshGraphicBase;
      graphic.RefreshMeshData();
      var mesh = graphic.mesh;

      _buffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3);
      material.SetBuffer("_FinalVertexPositions", _buffer);

      Graphics.ClearRandomWriteTargets();
      Graphics.SetRandomWriteTarget(1, _buffer);

      targetCamera.allowMSAA = false;
      targetCamera.enabled = true;
    }

    public static Vector3[] FinishCollectingVertData(this LeapGraphicRenderer renderer) {
      Camera targetCamera = renderer.GetComponentInChildren<Camera>();
      var renderingMethod = renderer.groups[0].renderingMethod as LeapMesherBase;
      var material = renderingMethod.material;

      var graphic = renderer.groups[0].graphics[0] as LeapMeshGraphicBase;
      graphic.RefreshMeshData();
      var mesh = graphic.mesh;

      Vector3[] array = new Vector3[mesh.vertexCount];
      _buffer.GetData(array);

      Graphics.ClearRandomWriteTargets();

      _buffer.Release();
      targetCamera.enabled = false;

      return array;
    }
  }
}
#endif
