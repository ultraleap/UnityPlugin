using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Space;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Text")]
  [AddComponentMenu("")]
  public class LeapTextRenderer : LeapRenderingMethod<LeapTextGraphic>,
  ISupportsFeature<LeapRuntimeTintFeature> {

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
    private const string CURVED_PARAMETERS = LeapGraphicRenderer.PROPERTY_PREFIX + "Curved_ElementParameters";
    private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
    private List<Matrix4x4> _curved_meshTransforms = new List<Matrix4x4>();
    private List<Vector4> _curved_graphicParameters = new List<Vector4>();

    public override SupportInfo GetSpaceSupportInfo(LeapSpace space) {
      return SupportInfo.FullSupport();
    }

    public void GetSupportInfo(List<LeapRuntimeTintFeature> features, List<SupportInfo> info) {
      SupportUtil.OnlySupportFirstFeature(features, info);
    }

    public override void OnEnableRenderer() {
      foreach (var graphic in group.graphics) {
        var textGraphic = graphic as LeapTextGraphic;
        _font.RequestCharactersInTexture(textGraphic.text);
      }

      generateMaterial();
    }

    public override void OnDisableRenderer() { }

    public override void OnUpdateRenderer() {
      for (int i = 0; i < group.graphics.Count; i++) {
        var graphic = group.graphics[i] as LeapTextGraphic;

        if (graphic.isRepresentationDirty) {
          generateTextMesh(i, graphic, _meshData[i]);
        }
      }

      if (renderer.space == null) {
        using (new ProfilerSample("Draw Meshes")) {
          for (int i = 0; i < group.graphics.Count; i++) {
            var graphic = group.graphics[i];
            Graphics.DrawMesh(_meshData[i], graphic.transform.localToWorldMatrix, _material, 0);
          }
        }
      } else if (renderer.space is LeapRadialSpace) {
        var curvedSpace = renderer.space as LeapRadialSpace;

        using (new ProfilerSample("Build Material Data")) {
          _curved_worldToAnchor.Clear();
          _curved_meshTransforms.Clear();
          _curved_graphicParameters.Clear();
          for (int i = 0; i < _meshData.Count; i++) {
            var graphic = group.graphics[i];
            var transformer = graphic.anchor.transformer;

            Vector3 localPos = renderer.transform.InverseTransformPoint(graphic.transform.position);

            Matrix4x4 mainTransform = transform.localToWorldMatrix * transformer.GetTransformationMatrix(localPos);
            Matrix4x4 deform = transform.worldToLocalMatrix * Matrix4x4.TRS(transform.position - graphic.transform.position, Quaternion.identity, Vector3.one) * graphic.transform.localToWorldMatrix;
            Matrix4x4 total = mainTransform * deform;

            _curved_graphicParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(graphic.transform));
            _curved_meshTransforms.Add(total);
            _curved_worldToAnchor.Add(mainTransform.inverse);
          }
        }

        using (new ProfilerSample("Upload Material Data")) {
          _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
          _material.SetMatrixArraySafe("_LeapGuiCurved_WorldToAnchor", _curved_worldToAnchor);
          _material.SetMatrix("_LeapGui_LocalToWorld", transform.localToWorldMatrix);
          _material.SetVectorArraySafe("_LeapGuiCurved_ElementParameters", _curved_graphicParameters);
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

      //Make sure we have enough meshes to render all our graphics
      _meshData.Clear();
      while (_meshData.Count < group.graphics.Count) {
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

      if (renderer.space != null) {
        if (renderer.space is LeapCylindricalSpace) {
          _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
        } else if (renderer.space is LeapSphericalSpace) {
          _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
        }
      }

      if (_useColor) {
        _material.EnableKeyword(LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_COLORS");
      }
    }

    private List<TextWrapper.Line> _tempLines = new List<TextWrapper.Line>();
    private List<Vector3> _verts = new List<Vector3>();
    private List<Vector4> _uvs = new List<Vector4>();
    private List<Color> _colors = new List<Color>();
    private List<int> _tris = new List<int>();
    private void generateTextMesh(int index, LeapTextGraphic graphic, Mesh mesh) {
      mesh.Clear();

      int scaledFontSize = Mathf.RoundToInt(graphic.fontSize * _dynamicPixelsPerUnit);

      _font.RequestCharactersInTexture(graphic.text,
                                       scaledFontSize,
                                       graphic.fontStyle);

      var textGraphic = graphic as LeapTextGraphic;
      var text = textGraphic.text;

      float _charScale = 0.1f / _dynamicPixelsPerUnit;
      float _scale = _charScale * graphic.fontSize / _font.fontSize;
      float lineHeight = _scale * textGraphic.lineSpacing * _font.lineHeight * _dynamicPixelsPerUnit;

      RectTransform rectTransform = textGraphic.transform as RectTransform;
      float maxWidth;
      if (rectTransform != null) {
        maxWidth = rectTransform.rect.width;
      } else {
        maxWidth = float.MaxValue;
      }

      TextWrapper.Wrap(text, textGraphic.tokens, _tempLines, c => {
        CharacterInfo info;
        _font.GetCharacterInfo(c, out info, scaledFontSize, graphic.fontStyle);
        return info.advance * _charScale;
      }, maxWidth);

      float textHeight = _tempLines.Count * lineHeight;

      Vector3 origin = Vector3.zero;
      origin.y -= _font.ascent * _scale * _dynamicPixelsPerUnit;

      if (rectTransform != null) {
        origin.y -= rectTransform.rect.y;

        switch (textGraphic.verticalAlignment) {
          case LeapTextGraphic.VerticalAlignment.Center:
            origin.y -= (rectTransform.rect.height - textHeight) / 2;
            break;
          case LeapTextGraphic.VerticalAlignment.Bottom:
            origin.y -= (rectTransform.rect.height - textHeight);
            break;
        }
      }

      foreach (var line in _tempLines) {

        if (rectTransform != null) {
          origin.x = rectTransform.rect.x;
          switch (textGraphic.horizontalAlignment) {
            case LeapTextGraphic.HorizontalAlignment.Center:
              origin.x += (rectTransform.rect.width - line.width) / 2;
              break;
            case LeapTextGraphic.HorizontalAlignment.Right:
              origin.x += (rectTransform.rect.width - line.width);
              break;
          }
        } else {
          switch (textGraphic.horizontalAlignment) {
            case LeapTextGraphic.HorizontalAlignment.Left:
              origin.x = 0;
              break;
            case LeapTextGraphic.HorizontalAlignment.Center:
              origin.x = -line.width / 2;
              break;
            case LeapTextGraphic.HorizontalAlignment.Right:
              origin.x = -line.width;
              break;
          }
        }

        for (int i = line.start; i < line.end; i++) {
          char c = text[i];

          CharacterInfo info;
          if (!_font.GetCharacterInfo(c, out info, scaledFontSize, graphic.fontStyle)) {
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
            _colors.Append(4, _globalTint * graphic.color);
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
}
