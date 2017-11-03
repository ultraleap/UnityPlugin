/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public partial class LeapGraphic {

    /// <summary>
    /// Helper method to set the blend shape amount for a blend shape
    /// data object attached to this graphic.  This method will throw
    /// an exception if there is no blend shape data obj attached to
    /// this graphic.
    /// </summary>
    public void SetBlendShapeAmount(float amount) {
      getFeatureDataOrThrow<LeapBlendShapeData>().amount = amount;
    }

    /// <summary>
    /// Helper method to get the blend shape amount for a blend shape
    /// data object attached to this graphic.  This method will throw
    /// an exception if there is no blend shape data obj attached to
    /// this graphic.
    /// </summary>
    public float GetBlendShapeAmount() {
      return getFeatureDataOrThrow<LeapBlendShapeData>().amount;
    }
  }

  [LeapGraphicTag("Blend Shape")]
  [Serializable]
  public class LeapBlendShapeData : LeapFeatureData {

    [Range(0, 1)]
    [SerializeField]
    private float _amount = 0;

    [EditTimeOnly]
    [SerializeField]
    private BlendShapeType _type = BlendShapeType.Scale;

    [EditTimeOnly]
    [MinValue(0)]
    [SerializeField]
    private float _scale = 1.1f;

    [EditTimeOnly]
    [SerializeField]
    private Vector3 _translation = new Vector3(0, 0, 0.1f);

    [EditTimeOnly]
    [SerializeField]
    private Vector3 _rotation = new Vector3(0, 0, 5);

    [EditTimeOnly]
    [SerializeField]
    private Transform _transform;

    /// <summary>
    /// Gets or sets the amount of strength for this blend shape.
    /// The value should be in the 0-1 range, where 0 is no strength,
    /// and 1 is complete strength.
    /// </summary>
    public float amount {
      get {
        return _amount;
      }
      set {
        if (value != _amount) {
          MarkFeatureDirty();
          _amount = value;
        }
      }
    }

    /// <summary>
    /// Returns the blended vertices based on the current mesh 
    /// this blend shape is attached to.  If you want to ensure
    /// this blend shape data is up-to-date, make sure to call
    /// RefreshMeshData on the graphic itself first.
    /// </summary>
    public bool TryGetBlendShape(List<Vector3> blendVerts) {
      if (!(graphic is LeapMeshGraphicBase)) {
        return false;
      }

      var meshGraphic = graphic as LeapMeshGraphicBase;

      var mesh = meshGraphic.mesh;

      if (mesh == null) {
        return false;
      }

      if (_type == BlendShapeType.Mesh) {
        if (mesh.blendShapeCount <= 0) {
          return false;
        }

        Vector3[] deltaVerts = new Vector3[mesh.vertexCount];
        Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
        Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
        mesh.GetBlendShapeFrameVertices(0, 0, deltaVerts, deltaNormals, deltaTangents);

        mesh.vertices.Query().Zip(deltaVerts.Query(), (a, b) => a + b).FillList(blendVerts);
        return true;
      } else {
        Matrix4x4 transformation;

        switch (_type) {
          case BlendShapeType.Translation:
            transformation = Matrix4x4.TRS(_translation, Quaternion.identity, Vector3.one);
            break;
          case BlendShapeType.Rotation:
            transformation = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(_rotation), Vector3.one);
            break;
          case BlendShapeType.Scale:
            transformation = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * _scale);
            break;
          case BlendShapeType.Transform:
            if (_transform == null) {
              return false;
            }
            transformation = graphic.transform.worldToLocalMatrix * _transform.localToWorldMatrix;
            break;
          default:
            throw new InvalidOperationException();
        }

        mesh.vertices.Query().Select(transformation.MultiplyPoint).FillList(blendVerts);
        return true;
      }
    }

    public enum BlendShapeType {
      Translation,
      Rotation,
      Scale,
      Transform,
      Mesh
    }
  }
}
