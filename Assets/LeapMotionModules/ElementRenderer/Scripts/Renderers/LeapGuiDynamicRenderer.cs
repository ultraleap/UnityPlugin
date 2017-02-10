using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
[LeapGuiTag("Dynamic")]
public class LeapGuiDynamicRenderer : LeapGuiMesherBase,
  ISupportsAddRemove {

  #region PRIVATE VARIABLES

  //Curved space
  private const string CURVED_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Curved_ElementParameters";
  private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
  private List<Matrix4x4> _curved_meshTransforms = new List<Matrix4x4>();
  private List<Vector4> _curved_elementParameters = new List<Vector4>();
  #endregion

  public void OnAddElements(List<LeapGuiElement> elements, List<int> indexes) {
    //TODO, this is super slow and sad
    OnUpdateRendererEditor();
  }

  public void OnRemoveElements(List<int> toRemove) {
    //TODO, this is super slow and sad
    OnUpdateRendererEditor();
  }

  public override SupportInfo GetSpaceSupportInfo(LeapGuiSpace space) {
    if (space is LeapGuiRectSpace ||
        space is LeapGuiCylindricalSpace ||
        space is LeapGuiSphericalSpace) {
      return SupportInfo.FullSupport();
    } else {
      return SupportInfo.Error("Dynamic Renderer does not support " + space.GetType().Name);
    }
  }

  public override void OnUpdateRenderer() {
    base.OnUpdateRenderer();

    if (gui.space is LeapGuiRectSpace) {
      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < gui.elements.Count; i++) {
          Graphics.DrawMesh(_meshes[i], gui.elements[i].transform.localToWorldMatrix, _material, 0);
        }
      }
    } else if (gui.space is LeapGuiRadialSpace) {
      var curvedSpace = gui.space as LeapGuiRadialSpace;

      using (new ProfilerSample("Build Material Data")) {
        _curved_worldToAnchor.Clear();
        _curved_meshTransforms.Clear();
        _curved_elementParameters.Clear();
        for (int i = 0; i < _meshes.Count; i++) {
          var element = gui.elements[i];
          var transformer = curvedSpace.GetTransformer(element.anchor);

          Vector3 guiLocalPos = gui.transform.InverseTransformPoint(element.transform.position);

          Matrix4x4 guiTransform = transformer.GetTransformationMatrix(guiLocalPos);
          Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - element.transform.position, Quaternion.identity, Vector3.one) * element.transform.localToWorldMatrix;
          Matrix4x4 total = guiTransform * deform;

          _curved_elementParameters.Add((transformer as LeapGuiRadialSpace.IRadialTransformer).GetVectorRepresentation(element));
          _curved_meshTransforms.Add(total);
          _curved_worldToAnchor.Add(guiTransform.inverse);
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetFloat(LeapGuiRadialSpace.RADIUS_PROPERTY, curvedSpace.radius);
        _material.SetMatrixArray("_LeapGuiCurved_WorldToAnchor", _curved_worldToAnchor);
        _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
        _material.SetVectorArray("_LeapGuiCurved_ElementParameters", _curved_elementParameters);
      }

      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < _meshes.Count; i++) {
          Graphics.DrawMesh(_meshes[i], _curved_meshTransforms[i], _material, 0);
        }
      }
    }
  }

  protected override void setupForBuilding() {
    if (_shader == null) {
      _shader = Shader.Find("LeapGui/Defaults/Dynamic");
    }

    base.setupForBuilding();

    if (gui.space is LeapGuiCylindricalSpace) {
      _material.EnableKeyword(LeapGuiCylindricalSpace.FEATURE_NAME);
    } else if (gui.space is LeapGuiSphericalSpace) {
      _material.EnableKeyword(LeapGuiSphericalSpace.FEATURE_NAME);
    }
  }

  protected override void buildElement() {
    //Always start a new mesh for each element
    finishMesh();
    beginMesh();

    base.buildElement();
  }

  protected override bool doesRequireSpecialUv3() {
    return true;
  }

  protected override Vector3 elementVertToMeshVert(Vector3 vertex) {
    return vertex;
  }

  protected override Vector3 blendShapeDelta(Vector3 shapeVert, Vector3 originalVert) {
    //TODO, optimize this, i'm sure it could be optimized.
    Vector3 worldVert = _currElement.transform.TransformPoint(shapeVert);
    Vector3 guiVert = gui.transform.InverseTransformPoint(worldVert);
    shapeVert = guiVert - gui.transform.InverseTransformPoint(_currElement.transform.position);

    Vector3 worldVert2 = _currElement.transform.TransformPoint(originalVert);
    Vector3 guiVert2 = gui.transform.InverseTransformPoint(worldVert2);
    originalVert = guiVert2 - gui.transform.InverseTransformPoint(_currElement.transform.position);

    return shapeVert - originalVert;
  }
}
