using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class LeapGuiBlendShapeExtensions {
  public static LeapGuiBlendShapeData BlendShape(this LeapGuiElement element) {
    return element.data.Query().FirstOrDefault(d => d is LeapGuiBlendShapeData) as LeapGuiBlendShapeData;
  }
}

public class LeapGuiBlendShapeData : LeapGuiElementData {

  [Range(0, 1)]
  [SerializeField]
  private float _amount = 0;

  [SerializeField]
  private BlendShapeType _type = BlendShapeType.Scale;

  [MinValue(0)]
  [SerializeField]
  private float _scale = 1.1f;

  [SerializeField]
  private Vector3 _translation = new Vector3(0, 0, 0.1f);

  [SerializeField]
  private Vector3 _rotation = new Vector3(0, 0, 5);

  [SerializeField]
  private Mesh _mesh;

  public float amount {
    get {
      return _amount;
    }
    set {
      feature.isDirty = true;
      _amount = value;
    }
  }

  private Mesh _cachedBlendShape;
  public Mesh blendShape {
    get {
      var meshData = element.Mesh();
      if (meshData == null) {
        return null;
      }

      var mesh = meshData.mesh;
      if (mesh == null) {
        return null;
      }

      if (_type == BlendShapeType.Mesh) {
        //If the vert count is different, we are in trouble
        if (_mesh == null || _mesh.vertexCount != mesh.vertexCount) {
          return null;
        }

        return _mesh;
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
          default:
            throw new InvalidOperationException();
        }

        if (_cachedBlendShape == null) {
          _cachedBlendShape = Instantiate(mesh);
          _cachedBlendShape.hideFlags = HideFlags.HideAndDontSave;
          _cachedBlendShape.name = "Generated Blend Shape Mesh";
        }

        _cachedBlendShape.vertices = mesh.vertices.Query().
                                     Select(v => transformation.MultiplyPoint3x4(v)).
                                     ToArray();
        return _cachedBlendShape;
      }
    }
  }

  public enum BlendShapeType {
    Translation,
    Rotation,
    Scale,
    Mesh
  }
}
