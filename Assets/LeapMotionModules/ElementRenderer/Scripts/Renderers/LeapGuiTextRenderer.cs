using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Space;
using Leap.Unity.Query;

[LeapGuiTag("Text")]
[AddComponentMenu("")]
public class LeapGuiTextRenderer : LeapGuiRenderer<LeapGuiTextElement>,
  ISupportsFeature<LeapGuiTintFeature> {

  [Header("Text Settings")]
  [SerializeField]
  private Font _font;

  [SerializeField]
  private float _dynamicPixelsPerUnit = 1.0f;

  public bool _useColor = true;

  public Color _globalTint = Color.white;

  [Header("Rendering Settings")]
  [SerializeField]
  private Shader _shader;

  [SerializeField, HideInInspector]
  private RendererMeshData _meshData;

  private Material _material;

  //Curved space
  private const string CURVED_PARAMETERS = LeapGui.PROPERTY_PREFIX + "Curved_ElementParameters";
  private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
  private List<Matrix4x4> _curved_meshTransforms = new List<Matrix4x4>();
  private List<Vector4> _curved_elementParameters = new List<Vector4>();

  public override SupportInfo GetSpaceSupportInfo(LeapSpace space) {
    return SupportInfo.FullSupport();
  }

  public void GetSupportInfo(List<LeapGuiTintFeature> features, List<SupportInfo> info) {
    SupportUtil.OnlySupportFirstFeature(features, info);
  }

  public override void OnEnableRenderer() {
    foreach (var element in group.elements) {
      var textElement = element as LeapGuiTextElement;
      _font.RequestCharactersInTexture(textElement.text);
    }

    generateMaterial();
  }

  public override void OnDisableRenderer() { }

  public override void OnUpdateRenderer() {
    for (int i = 0; i < group.elements.Count; i++) {
      var element = group.elements[i] as LeapGuiTextElement;

      if (element.isRepresentationDirty) {
        generateTextMesh(i, element, _meshData[i]);
      }
    }

    if (gui.space == null) {
      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < group.elements.Count; i++) {
          var element = group.elements[i] as LeapGuiTextElement;
          Graphics.DrawMesh(_meshData[i], element.transform.localToWorldMatrix, _material, 0);
        }
      }
    } else if (gui.space is LeapRadialSpace) {
      var curvedSpace = gui.space as LeapRadialSpace;

      using (new ProfilerSample("Build Material Data")) {
        _curved_worldToAnchor.Clear();
        _curved_meshTransforms.Clear();
        _curved_elementParameters.Clear();
        for (int i = 0; i < _meshData.Count; i++) {
          var element = group.elements[i];
          var transformer = element.anchor.transformer;

          Vector3 guiLocalPos = gui.transform.InverseTransformPoint(element.transform.position);

          Matrix4x4 guiTransform = transform.localToWorldMatrix * transformer.GetTransformationMatrix(guiLocalPos);
          Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - element.transform.position, Quaternion.identity, Vector3.one) * element.transform.localToWorldMatrix;
          Matrix4x4 total = guiTransform * deform;

          _curved_elementParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(element.transform));
          _curved_meshTransforms.Add(total);
          _curved_worldToAnchor.Add(guiTransform.inverse);
        }
      }

      using (new ProfilerSample("Upload Material Data")) {
        _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
        _material.SetMatrixArraySafe("_LeapGuiCurved_WorldToAnchor", _curved_worldToAnchor);
        _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
        _material.SetVectorArraySafe("_LeapGuiCurved_ElementParameters", _curved_elementParameters);
      }

      using (new ProfilerSample("Draw Meshes")) {
        for (int i = 0; i < _meshData.Count; i++) {
          Graphics.DrawMesh(_meshData[i], _curved_meshTransforms[i], _material, 0);
        }
      }
    }
  }

  public override void OnEnableRendererEditor() { }

  public override void OnDisableRendererEditor() { }

  public override void OnUpdateRendererEditor(bool isHeavyUpdate) {
    base.OnUpdateRendererEditor(isHeavyUpdate);

    CreateOrSave(ref _meshData, "Text Mesh Data");

    //Make sure we have enough meshes to render all our elements
    _meshData.Clear();
    while (_meshData.Count < group.elements.Count) {
      _meshData.AddMesh(new Mesh());
    }

    generateMaterial();
  }

  private void generateMaterial() {
    _material = Instantiate(_font.material);
    _material.hideFlags = HideFlags.HideAndDontSave;
    _material.name = "Font material";
    _material.shader = _shader;

    foreach (var keyword in _material.shaderKeywords) {
      _material.DisableKeyword(keyword);
    }

    if (gui.space != null) {
      if (gui.space is LeapCylindricalSpace) {
        _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
      } else if (gui.space is LeapSphericalSpace) {
        _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
      }
    }

    if (_useColor) {
      _material.EnableKeyword(LeapGui.FEATURE_PREFIX + "VERTEX_COLORS");
    }
  }

  private List<TextWrapper.Line> _tempLines = new List<TextWrapper.Line>();
  private List<Vector3> _verts = new List<Vector3>();
  private List<Vector4> _uvs = new List<Vector4>();
  private List<Color> _colors = new List<Color>();
  private List<int> _tris = new List<int>();
  private void generateTextMesh(int index, LeapGuiTextElement element, Mesh mesh) {
    mesh.Clear();

    int scaledFontSize = Mathf.RoundToInt(element.fontSize * _dynamicPixelsPerUnit);

    _font.RequestCharactersInTexture(element.text,
                                     scaledFontSize,
                                     element.fontStyle);

    var textElement = element as LeapGuiTextElement;
    var text = textElement.text;

    float _charScale = 0.1f / _dynamicPixelsPerUnit;
    float _scale = _charScale * element.fontSize / _font.fontSize;
    float lineHeight = _scale * textElement.lineSpacing * _font.lineHeight * _dynamicPixelsPerUnit;

    RectTransform rectTransform = textElement.transform as RectTransform;
    float maxWidth;
    if (rectTransform != null) {
      maxWidth = rectTransform.rect.width;
    } else {
      maxWidth = float.MaxValue;
    }

    TextWrapper.Wrap(text, textElement.tokens, _tempLines, c => {
      CharacterInfo info;
      _font.GetCharacterInfo(c, out info, scaledFontSize, element.fontStyle);
      return info.advance * _charScale;
    }, maxWidth);

    float textHeight = _tempLines.Count * lineHeight;

    Vector3 origin = Vector3.zero;
    origin.y -= _font.ascent * _scale * _dynamicPixelsPerUnit;

    if (rectTransform != null) {
      origin.y -= rectTransform.rect.y;

      switch (textElement.verticalAlignment) {
        case LeapGuiTextElement.VerticalAlignment.Center:
          origin.y -= (rectTransform.rect.height - textHeight) / 2;
          break;
        case LeapGuiTextElement.VerticalAlignment.Bottom:
          origin.y -= (rectTransform.rect.height - textHeight);
          break;
      }
    }

    foreach (var line in _tempLines) {

      if (rectTransform != null) {
        origin.x = rectTransform.rect.x;
        switch (textElement.horizontalAlignment) {
          case LeapGuiTextElement.HorizontalAlignment.Center:
            origin.x += (rectTransform.rect.width - line.width) / 2;
            break;
          case LeapGuiTextElement.HorizontalAlignment.Right:
            origin.x += (rectTransform.rect.width - line.width);
            break;
        }
      } else {
        switch (textElement.horizontalAlignment) {
          case LeapGuiTextElement.HorizontalAlignment.Left:
            origin.x = 0;
            break;
          case LeapGuiTextElement.HorizontalAlignment.Center:
            origin.x = -line.width / 2;
            break;
          case LeapGuiTextElement.HorizontalAlignment.Right:
            origin.x = -line.width;
            break;
        }
      }

      for (int i = line.start; i < line.end; i++) {
        char c = text[i];

        CharacterInfo info;
        if (!_font.GetCharacterInfo(c, out info, scaledFontSize, element.fontStyle)) {
          continue;
        }

        int offset = _verts.Count;
        _tris.Add(offset + 0);
        _tris.Add(offset + 1);
        _tris.Add(offset + 2);

        _tris.Add(offset + 0);
        _tris.Add(offset + 2);
        _tris.Add(offset + 3);

        _verts.Add(_charScale * new Vector3(info.minX, info.maxY, 0) + origin);
        _verts.Add(_charScale * new Vector3(info.maxX, info.maxY, 0) + origin);
        _verts.Add(_charScale * new Vector3(info.maxX, info.minY, 0) + origin);
        _verts.Add(_charScale * new Vector3(info.minX, info.minY, 0) + origin);

        _uvs.Add(info.uvTopLeft);
        _uvs.Add(info.uvTopRight);
        _uvs.Add(info.uvBottomRight);
        _uvs.Add(info.uvBottomLeft);

        if (_useColor) {
          _colors.Append(4, _globalTint * element.color);
        }

        origin.x += info.advance * _charScale;
      }
      origin.y -= lineHeight;
    }

    for (int i = 0; i < _uvs.Count; i++) {
      Vector4 uv = _uvs[i];
      uv.w = index;
      _uvs[i] = uv;
    }

    mesh.SetVertices(_verts);
    mesh.SetTriangles(_tris, 0);
    mesh.SetUVs(0, _uvs);

    if (_useColor) {
      mesh.SetColors(_colors);
    }

    _verts.Clear();
    _uvs.Clear();
    _tris.Clear();
    _colors.Clear();
    _tempLines.Clear();
  }
}
