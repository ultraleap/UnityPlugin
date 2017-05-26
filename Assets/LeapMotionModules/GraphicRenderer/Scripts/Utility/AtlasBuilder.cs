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
using UnityEngine.Rendering;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  /// <summary>
  /// A class that contains mapping information that specifies how a texture
  /// is packed into an atlas.
  /// </summary>
  [Serializable]
  public class AtlasUvs {

    [Serializable]
    public class TextureToRect : SerializableDictionary<UnityEngine.Object, Rect> { }

    [SerializeField]
    private TextureToRect _channel0 = new TextureToRect();
    [SerializeField]
    private TextureToRect _channel1 = new TextureToRect();
    [SerializeField]
    private TextureToRect _channel2 = new TextureToRect();
    [SerializeField]
    private TextureToRect _channel3 = new TextureToRect();

    [SerializeField]
    private Rect[] _nullRects = new Rect[4];

    /// <summary>
    /// Given a texture object and a uv channel, return the rect that
    /// this texture occupies within the atlas.  If the key is not
    /// present in the atlas, the default rect is returned.  If a null
    /// texture is passed in, the rect for the empty texture (which is valid!)
    /// is returned.
    /// </summary>
    public Rect GetRect(int channel, UnityEngine.Object key) {
      if (key == null) {
        return _nullRects[channel];
      } else {
        Rect r;
        getChannel(channel).TryGetValue(key, out r);
        return r;
      }
    }

    /// <summary>
    /// Given a texture object and a uv channel, store into this data
    /// structure the rect that the texture takes up inside of the atlas.
    /// A null texture object is valid, and represents the empty texture.
    /// </summary>
    public void SetRect(int channel, UnityEngine.Object key, Rect rect) {
      if (key == null) {
        _nullRects[channel] = rect;
      } else {
        getChannel(channel)[key] = rect;
      }
    }

    private TextureToRect getChannel(int channel) {
      switch (channel) {
        case 0:
          return _channel0;
        case 1:
          return _channel1;
        case 2:
          return _channel2;
        case 3:
          return _channel3;
        default:
          throw new Exception();
      }
    }
  }

  [Serializable]
  public class AtlasBuilder {

    [Tooltip("When non-zero, extends each texture by a certain number of pixels, filling in the space with texture data based on the texture wrap mode.")]
    [MinValue(0)]
    [EditTimeOnly, SerializeField]
    private int _border = 0;

    [Tooltip("When non-zero, adds an amount of empty space between each texture.")]
    [MinValue(0)]
    [EditTimeOnly, SerializeField]
    private int _padding = 0;

    [Tooltip("Should the atlas have mip maps?")]
    [EditTimeOnly, SerializeField]
    private bool _mipMap = true;

    [Tooltip("The filter mode that should be used for the atlas.")]
    [EditTimeOnly, SerializeField]
    private FilterMode _filterMode = FilterMode.Bilinear;

    [Tooltip("The texture format that should be used for the atlas.")]
    [EditTimeOnly, SerializeField]
    private TextureFormat _format = TextureFormat.ARGB32;

    [Tooltip("The maximum atlas size in pixels.")]
    [MinValue(16)]
    [MaxValue(8192)]
    [EditTimeOnly, SerializeField]
    private int _maxAtlasSize = 4096;

    [Tooltip("Add textures to this array to ensure that they are always present in the atlas.")]
    [SerializeField]
    private TextureReference[] _extraTextures;

    /// <summary>
    /// Returns whether or not the results built by this atlas have become invalid.
    /// </summary>
    public bool isDirty {
      get {
        return _currHash != _atlasHash;
      }
    }

    private static Material _cachedBlitMaterial = null;
    private static void enableBlitPass(Texture tex) {
      if (_cachedBlitMaterial == null) {
        _cachedBlitMaterial = new Material(Shader.Find("Hidden/Leap Motion/Graphic Renderer/InternalPack"));
        _cachedBlitMaterial.hideFlags = HideFlags.HideAndDontSave;
      }
      _cachedBlitMaterial.mainTexture = tex;
      _cachedBlitMaterial.SetPass(0);
    }

    private List<LeapTextureFeature> _features = new List<LeapTextureFeature>();
    private Hash _atlasHash = 1;
    private Hash _currHash = 0;

    /// <summary>
    /// Updates the internal list of textures given some texture features to build an atlas for.
    /// This method does not do any atlas work, but must be called before RebuildAtlas is called.
    /// Once this method is called, you can check the isDirty flag to see if a rebuild is needed.
    /// </summary>
    public void UpdateTextureList(List<LeapTextureFeature> textureFeatures) {
      _features.Clear();
      _features.AddRange(textureFeatures);

      _currHash = new Hash() {
        _border,
        _padding,
        _mipMap,
        _filterMode,
        _format,
        _maxAtlasSize
      };

      if (_extraTextures == null) {
        _extraTextures = new TextureReference[0];
      }

      for (int i = 0; i < _extraTextures.Length; i++) {
        switch (_extraTextures[i].channel) {
          case UVChannelFlags.UV0:
          case UVChannelFlags.UV1:
          case UVChannelFlags.UV2:
          case UVChannelFlags.UV3:
            break;
          default:
            _extraTextures[i].channel = UVChannelFlags.UV0;
            break;
        }
      }

      foreach (var extra in _extraTextures) {
        _currHash.Add(extra.texture);
        _currHash.Add(extra.channel);
      }

      foreach (var feature in _features) {
        _currHash.Add(feature.channel);
        foreach (var dataObj in feature.featureData) {
          _currHash.Add(dataObj.texture);
        }
      }
    }

    /// <summary>
    /// Actually perform the build for the atlas.  This method outputs the atlas textures, and the atlas
    /// uvs that map textures into the atlas.  This method takes in a progress bar so that the atlas 
    /// process can be tracked visually, since it can take quite a bit of time when there are a lot of
    /// textures to pack.
    /// </summary>
    public void RebuildAtlas(ProgressBar progress, out Texture2D[] packedTextures, out AtlasUvs channelMapping) {
      if (!Utils.IsCompressible(_format)) {
        Debug.LogWarning("Format " + _format + " is not compressible!  Using ARGB32 instead.");
        _format = TextureFormat.ARGB32;
      }

      _atlasHash = _currHash;

      packedTextures = new Texture2D[_features.Count];
      channelMapping = new AtlasUvs();

      mainProgressLoop(progress, packedTextures, channelMapping);

      //Clear cache
      foreach (var texture in _cachedProcessedTextures.Values) {
        UnityEngine.Object.DestroyImmediate(texture);
      }
      _cachedProcessedTextures.Clear();

      foreach (var texture in _cachedDefaultTextures.Values) {
        UnityEngine.Object.DestroyImmediate(texture);
      }
      _cachedDefaultTextures.Clear();
    }

    private void mainProgressLoop(ProgressBar progress, Texture2D[] packedTextures, AtlasUvs channelMapping) {
      progress.Begin(5, "", "", () => {
        foreach (var channel in MeshUtil.allUvChannels) {
          progress.Begin(1, "", channel + ": ", () => {
            doPerChannelPack(progress, channel, packedTextures, channelMapping);
          });
        }

        finalizeTextures(progress, packedTextures);
      });
    }

    private void doPerChannelPack(ProgressBar progress, UVChannelFlags channel, Texture2D[] packedTextures, AtlasUvs channelMapping) {
      var mainTextureFeature = _features.Query().FirstOrDefault(f => f.channel == channel);
      if (mainTextureFeature == null) return;

      Texture2D defaultTexture, packedTexture;
      Texture2D[] rawTextureArray, processedTextureArray;

      progress.Step("Prepare " + channel);
      prepareForPacking(mainTextureFeature, out defaultTexture,
                                            out packedTexture,
                                            out rawTextureArray,
                                            out processedTextureArray);

      progress.Step("Pack " + channel);
      var packedRects = packedTexture.PackTextures(processedTextureArray,
                                                    _padding,
                                                    _maxAtlasSize,
                                                    makeNoLongerReadable: false);

      packedTexture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
      packedTextures[_features.IndexOf(mainTextureFeature)] = packedTexture;

      packSecondaryTextures(progress, channel, mainTextureFeature, packedTexture, packedRects, packedTextures);

      //Correct uvs to account for the added border
      for (int i = 0; i < packedRects.Length; i++) {
        float dx = 1.0f / packedTexture.width;
        float dy = 1.0f / packedTexture.height;
        Rect r = packedRects[i];

        if (processedTextureArray[i] != defaultTexture) {
          dx *= _border;
          dy *= _border;
        }

        r.x += dx;
        r.y += dy;
        r.width -= dx * 2;
        r.height -= dy * 2;
        packedRects[i] = r;
      }

      for (int i = 0; i < rawTextureArray.Length; i++) {
        channelMapping.SetRect(channel.Index(), rawTextureArray[i], packedRects[i]);
      }
    }

    private void packSecondaryTextures(ProgressBar progress, UVChannelFlags channel, LeapTextureFeature mainFeature, Texture2D packedTexture, Rect[] packedRects, Texture2D[] packedTextures) {
      //All texture features that are NOT the main texture do not get their own atlas step
      //They are simply copied into a new texture
      var nonMainFeatures = _features.Query().Where(f => f.channel == channel).Skip(1).ToList();

      progress.Begin(nonMainFeatures.Count, "", "Copying secondary textures: ", () => {
        foreach (var secondaryFeature in nonMainFeatures) {

          RenderTexture secondaryRT = new RenderTexture(packedTexture.width, packedTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

          RenderTexture.active = secondaryRT;
          GL.Clear(clearDepth: false, clearColor: true, backgroundColor: Color.black);
          GL.LoadPixelMatrix(0, 1, 0, 1);

          progress.Begin(secondaryFeature.featureData.Count, "", secondaryFeature.propertyName, () => {
            for (int i = 0; i < secondaryFeature.featureData.Count; i++) {
              var mainTexture = mainFeature.featureData[i].texture;
              if (mainTexture == null) {
                progress.Step();
                continue;
              }

              var secondaryTexture = secondaryFeature.featureData[i].texture;
              if (secondaryTexture == null) {
                progress.Step();
                continue;
              }

              progress.Step(secondaryTexture.name);

              Rect rect = packedRects[i];

              //Use mainTexture instead of secondaryTexture here to calculate correct border to line up with main texture
              float borderDX = _border / (float)mainTexture.width;
              float borderDY = _border / (float)mainTexture.height;

              drawTexture(secondaryTexture, secondaryRT, rect, borderDX, borderDY);
            }
          });

          packedTextures[_features.IndexOf(secondaryFeature)] = convertToTexture2D(secondaryRT, mipmap: false);
        }
      });
    }

    private void finalizeTextures(ProgressBar progress, Texture2D[] packedTextures) {
      progress.Begin(packedTextures.Length, "", "Finalizing ", () => {
        for (int i = 0; i < packedTextures.Length; i++) {
          progress.Begin(2, "", _features[i].propertyName + ": ", () => {
            Texture2D tex = packedTextures[i];
            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            GL.LoadPixelMatrix(0, 1, 0, 1);
            drawTexture(tex, rt, new Rect(0, 0, 1, 1), 0, 0);

            progress.Step("Copying Texture");
            tex = convertToTexture2D(rt, _mipMap);

            progress.Step("Compressing Texture");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.CompressTexture(tex, _format, TextureCompressionQuality.Best);
#endif
            tex.filterMode = _filterMode;

            progress.Step("Updating Texture");
            //keep the texture as readable because the user might want to do things with the texture!
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);

            packedTextures[i] = tex;
          });
        }
      });
    }

    private void prepareForPacking(LeapTextureFeature feature,
                                      out Texture2D defaultTexture,
                                      out Texture2D packedTexture,
                                      out Texture2D[] rawTextureArray,
                                      out Texture2D[] processedTextureArray) {
      if (_extraTextures == null) {
        _extraTextures = new TextureReference[0];
      }

      rawTextureArray = feature.featureData.Query().
                                            Select(dataObj => dataObj.texture).
                                            Concat(_extraTextures.Query().
                                                                  Where(p => p.channel == feature.channel).
                                                                  Select(p => p.texture)).
                                            ToArray();

      processedTextureArray = rawTextureArray.Query().
                                              Select(t => processTexture(t)).
                                              ToArray();

      defaultTexture = getDefaultTexture(Color.white); //TODO, pull color from feature data
      for (int i = 0; i < processedTextureArray.Length; i++) {
        if (processedTextureArray[i] == null) {
          processedTextureArray[i] = defaultTexture;
        }
      }

      packedTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipmap: false, linear: true);
      packedTexture.filterMode = _filterMode;
    }

    private Dictionary<Texture2D, Texture2D> _cachedProcessedTextures = new Dictionary<Texture2D, Texture2D>();
    private Texture2D processTexture(Texture2D source) {
      using (new ProfilerSample("Process Texture")) {
        if (source == null) {
          return null;
        }

        Texture2D processed;
        if (_cachedProcessedTextures.TryGetValue(source, out processed)) {
          return processed;
        }

        RenderTexture destRT = new RenderTexture(source.width + _border * 2, source.height + _border * 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        GL.LoadPixelMatrix(0, 1, 0, 1);
        drawTexture(source, destRT, new Rect(0, 0, 1, 1), _border / (float)source.width, _border / (float)source.height);

        processed = convertToTexture2D(destRT, mipmap: false);
        _cachedProcessedTextures[source] = processed;
        return processed;
      }
    }

    private void drawTexture(Texture2D source, RenderTexture dst, Rect rect, float borderDX, float borderDY) {
      enableBlitPass(source);
      RenderTexture.active = dst;

      GL.Begin(GL.QUADS);
      GL.TexCoord(new Vector2(0 - borderDX, 0 - borderDY));
      GL.Vertex(rect.Corner00());
      GL.TexCoord(new Vector2(1 + borderDX, 0 - borderDY));
      GL.Vertex(rect.Corner10());
      GL.TexCoord(new Vector2(1 + borderDX, 1 + borderDY));
      GL.Vertex(rect.Corner11());
      GL.TexCoord(new Vector2(0 - borderDX, 1 + borderDY));
      GL.Vertex(rect.Corner01());
      GL.End();
    }

    private Texture2D convertToTexture2D(RenderTexture source, bool mipmap) {
      Texture2D tex = new Texture2D(source.width, source.height, TextureFormat.ARGB32, mipmap, linear: true);

      RenderTexture.active = source;
      tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
      tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
      RenderTexture.active = null;

      source.Release();
      UnityEngine.Object.DestroyImmediate(source);

      return tex;
    }

    private Dictionary<Color, Texture2D> _cachedDefaultTextures = new Dictionary<Color, Texture2D>();
    private Texture2D getDefaultTexture(Color color) {
      Texture2D texture;
      if (!_cachedDefaultTextures.TryGetValue(color, out texture)) {
        texture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false);
        texture.SetPixels(new Color[3 * 3].Fill(color));
        _cachedDefaultTextures[color] = texture;
      }
      return texture;
    }

    [Serializable]
    public class TextureReference {
      public Texture2D texture;
      public UVChannelFlags channel = UVChannelFlags.UV0;
    }
  }
}
