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
using UnityEngine.Assertions;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public interface ILeapInternalRenderingMethod {
    LeapGraphicRenderer renderer { set; }
    LeapGraphicGroup group { set; }
  }

  [Serializable]
  public abstract class LeapRenderingMethod : ILeapInternalRenderingMethod {
    public const string DATA_FOLDER_NAME = "_ElementData";

    [NonSerialized]
    private LeapGraphicRenderer _renderer;

    [NonSerialized]
    private LeapGraphicGroup _group;

    /// <summary>
    /// Gets the renderer this rendering method is attached to.
    /// </summary>
    public LeapGraphicRenderer renderer {
      get {
        return _renderer;
      }
    }

    /// <summary>
    /// Gets the group this rendering method is attached to.
    /// </summary>
    public LeapGraphicGroup group {
      get {
        return _group;
      }
    }

    /// <summary>
    /// Sets the renderer this rendering method is attached to.
    /// </summary>
    LeapGraphicRenderer ILeapInternalRenderingMethod.renderer {
      set {
        _renderer = value;
      }
    }

    /// <summary>
    /// Sets the group this rendering methid is attached to.
    /// </summary>
    LeapGraphicGroup ILeapInternalRenderingMethod.group {
      set {
        _group = value;
      }
    }

    public abstract SupportInfo GetSpaceSupportInfo(LeapSpace space);

    /// <summary>
    /// Called when the renderer is enabled at runtime.
    /// </summary>
    public abstract void OnEnableRenderer();

    /// <summary>
    /// Called when the renderer is disabled at runtime.
    /// </summary>
    public abstract void OnDisableRenderer();

    /// <summary>
    /// Called from LateUpdate during runtime.  Use this to update the
    /// renderer using any changes made to during this frame.
    /// </summary>
    public abstract void OnUpdateRenderer();

#if UNITY_EDITOR
    /// <summary>
    /// Called curing edit time when this renderering method is created.  
    /// Use this for any edit-time construction you need.
    /// </summary>
    public virtual void OnEnableRendererEditor() {
    }

    /// <summary>
    /// Called during edit time when this rendering method is destroyed.
    /// Use this for edit-time clean up.
    /// </summary>
    public virtual void OnDisableRendererEditor() {
    }

    /// <summary>
    /// Called during edit time to update the renderer status.  This is 
    /// called every time a change is performed, but it is
    /// not called all the time!
    /// </summary>
    public virtual void OnUpdateRendererEditor() { }
#endif

    public abstract bool IsValidGraphic<T>();
    public abstract bool IsValidGraphic(LeapGraphic graphic);

    public abstract LeapGraphic GetValidGraphicOnObject(GameObject obj);

    private static Dictionary<UnityObject, object> _assetToOwner = new Dictionary<UnityObject, object>();
    public void PreventDuplication<T>(ref T t) where T : UnityObject {
      Assert.IsNotNull(t);

      object owner;
      if (_assetToOwner.TryGetValue(t, out owner)) {
        if (owner.Equals(this)) {
          return;
        }

        if (t is Texture2D) {
          Texture2D tex = t as Texture2D;

          RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
          Graphics.Blit(tex, rt);

          Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, tex.mipmapCount > 1, true);
          RenderTexture.active = rt;
          newTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
          newTex.Apply();
          RenderTexture.active = null;
          rt.Release();
          UnityObject.DestroyImmediate(rt);

          t = newTex as T;
        } else {
          t = UnityObject.Instantiate(t);
        }
      }
      _assetToOwner[t] = this;
    }
  }

  public abstract class LeapRenderingMethod<GraphicType> : LeapRenderingMethod
    where GraphicType : LeapGraphic {
    public const string ASSET_PATH = "Assets/Generated/RendererData/";

    public override bool IsValidGraphic<T>() {
      Type t = typeof(T);
      Type graphicType = typeof(GraphicType);

      return t == graphicType || (t.IsSubclassOf(graphicType));
    }

    public override bool IsValidGraphic(LeapGraphic graphic) {
      return graphic is GraphicType;
    }

    public override LeapGraphic GetValidGraphicOnObject(GameObject obj) {
      return obj.GetComponent<GraphicType>();
    }
  }
}
