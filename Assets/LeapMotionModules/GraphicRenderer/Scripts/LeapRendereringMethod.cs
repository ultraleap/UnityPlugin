using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;

namespace Leap.Unity.GraphicalRenderer {

  public abstract class LeapRenderingMethod : LeapGraphicComponentBase<LeapGraphicRenderer> {
    public const string DATA_FOLDER_NAME = "_ElementData";

    [HideInInspector]
    public
#if UNITY_EDITOR
  new
#endif
  LeapGraphicRenderer renderer;

    [HideInInspector]
    public LeapGraphicGroup group;

    public abstract SupportInfo GetSpaceSupportInfo(LeapSpace space);

    protected override void OnValidate() {
      base.OnValidate();

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        if (renderer != null) {
          renderer.editor.ScheduleEditorUpdate();
        }
      }
#endif
    }

    protected bool isHeavyUpdate { get; private set; }

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
    public abstract void OnEnableRendererEditor();

    /// <summary>
    /// Called during edit time when this rendering method is destroyed.
    /// Use this for edit-time clean up.
    /// </summary>
    public abstract void OnDisableRendererEditor();

    /// <summary>
    /// Called during edit time to update the renderer status.  This is 
    /// called every time a change is performed, but it is
    /// not called all the time!
    /// </summary>
    public virtual void OnUpdateRendererEditor(bool isHeavyUpdate) {
      this.isHeavyUpdate = isHeavyUpdate;
    }
#endif

    public abstract bool IsValidGraphic<T>();
    public abstract bool IsValidGraphic(LeapGraphic graphic);

    public abstract LeapGraphic GetValidGraphicOnObject(GameObject obj);

    protected void CreateOrSave<T>(ref T t, string assetName) where T : SceneTiedAsset {
      T newT = t;
      if (SceneTiedAsset.CreateOrSave(gameObject, 
                                      ref newT,
                                      DATA_FOLDER_NAME,
                                      assetName)) {
        Undo.RecordObject(this, "Updated graphic data");
        t = newT;
        EditorUtility.SetDirty(this);
      }
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
