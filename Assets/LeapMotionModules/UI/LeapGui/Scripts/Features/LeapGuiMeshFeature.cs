using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapGuiMeshFeature : LeapGuiFeature {

  [SerializeField]
  private bool _uv0;

  [SerializeField]
  private bool _uv1;

  [SerializeField]
  private bool _uv2;

  [SerializeField]
  private bool _uv3;

  [SerializeField]
  private bool _color;

  [SerializeField]
  private Color _tint;

  [SerializeField]
  private bool _normals;

  public override ScriptableObject CreateSettingsObject() {
    return ScriptableObject.CreateInstance<MeshSettings>();
  }

  public class MeshSettings : ElementSettings {

    [SerializeField]
    private Mesh _mesh;

    [SerializeField]
    private Color _color;
  }
}
