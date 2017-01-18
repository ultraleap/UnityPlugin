using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.Gui.Space {

  [ExecuteInEditMode]
  public abstract class GuiSpace : MonoBehaviour {
    public const string FEATURE_ALL = "GUI_SPACE_ALL";
    public const string FEATURE_CYLINDRICAL_CONST_WIDTH = "GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH";
    public const string FEATURE_CYLINDRICAL_ANGULAR = "GUI_SPACE_CYLINDRICAL_ANGULAR";
    public static string[] ALL_FEATURES = { FEATURE_ALL, FEATURE_CYLINDRICAL_CONST_WIDTH, FEATURE_CYLINDRICAL_ANGULAR };

    public const int GUI_SPACE_LIMIT = 32;
    private static GuiSpace[] _allGuiSpaces = new GuiSpace[GUI_SPACE_LIMIT];

    private static GuiSpaceMatrixProperty _guiTransforms = new GuiSpaceMatrixProperty("_WorldToGuiSpace", GUI_SPACE_LIMIT);
    private static GuiSpaceMatrixProperty _guiInverseTransforms = new GuiSpaceMatrixProperty("_GuiToWorldSpace", GUI_SPACE_LIMIT);
    private static GuiSpaceVectorProperty _guiParams0 = new GuiSpaceVectorProperty("_GuiSpaceParams0", GUI_SPACE_LIMIT);

    private static MaterialPropertyBlock _propertyBlock;

    private int _index;

    public abstract Vector3 FromRect(Vector3 rectPos);

    public abstract Vector3 ToRect(Vector3 guiPos);

    public abstract void FromRect(Vector3 rectPos, Quaternion rectRot, out Vector3 guiPos, out Quaternion guiRot);

    public abstract void ToRect(Vector3 guiPos, Quaternion guiRot, out Vector3 rectPos, out Quaternion rectRot);

    /// <summary>
    /// Update the entire space and all elements that are inside it.  Use this for large changes that require
    /// a complete re-traversal.
    /// </summary>
    public void UpdateSpace() {
      foreach (var modifier in GetComponentsInChildren<GuiSpaceModifier>()) {
        modifier.UpdateSpace();
      }
    }

    public void UpdateRenderer(Renderer renderer) {
#if UNITY_EDITOR
      _propertyBlock = _propertyBlock ?? new MaterialPropertyBlock();
      updateRendererEditor(renderer);
#else
      updateRendererRuntime(renderer);
#endif
    }

    private void updateRendererEditor(Renderer renderer) {
      foreach (var material in renderer.sharedMaterials) {
        material.EnableKeyword(FEATURE_ALL);
      }

      renderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetFloat("_GuiSpaceSelection", SelectionIndexForAllVariant);
      _propertyBlock.SetFloat("_GuiSpaceIndex", _index);
      UpdatePropertyBlock(_propertyBlock);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    private void updateRendererRuntime(Renderer renderer) {
      foreach (var material in renderer.sharedMaterials) {
        material.EnableKeyword(ShaderVariantName);
      }

      renderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetFloat("_GuiSpaceIndex", _index);
      UpdatePropertyBlock(_propertyBlock);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    public static void ResetRenderer(Renderer renderer) {
#if UNITY_EDITOR
      _propertyBlock = _propertyBlock ?? new MaterialPropertyBlock();
      resetRendererEditor(renderer);
#else
      resetRendererRuntime(renderer);
#endif
    }

    private static void resetRendererEditor(Renderer renderer) {
      renderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetFloat("_GuiSpaceSelection", 0);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    private static void resetRendererRuntime(Renderer renderer) {
      foreach (var material in renderer.sharedMaterials) {
        foreach (var feature in ALL_FEATURES) {
          material.DisableKeyword(feature);
        }
      }
    }

    protected virtual void OnValidate() { }

    void Awake() {
      if (_propertyBlock == null) {
        _propertyBlock = new MaterialPropertyBlock();
      }
    }

    void OnEnable() {
      _index = -1;
      for (int i = 0; i < GUI_SPACE_LIMIT; i++) {
        if (_allGuiSpaces[i] == null) {
          _index = i;
          break;
        }
      }

      if (_index == -1) {
        Debug.LogError("Tried to create more gui spaces than the maximum amount!");
        enabled = false;
        return;
      }

      _allGuiSpaces[_index] = this;

      UpdateSpace();
    }

    void OnDisable() {
      _allGuiSpaces[_index] = null;
      _index = -1;

      UpdateSpace();
    }

    protected virtual void Update() {

    }

    protected virtual void LateUpdate() {
      var guiSpace = GetGuiSpace();

      _guiTransforms[_index] = guiSpace;
      _guiInverseTransforms[_index] = guiSpace.inverse;

      //TODO: this might upload more than once per frame, how to fix?
      _guiTransforms.UploadIfDirty();
      _guiInverseTransforms.UploadIfDirty();
      _guiParams0.UploadIfDirty();
    }

    protected void SetGenericGuiParams(Vector4 genericParams) {
      if (_index == -1) return;
      _guiParams0[_index] = genericParams;
    }

    protected abstract string ShaderVariantName { get; }
    protected abstract int SelectionIndexForAllVariant { get; }
    protected abstract void UpdatePropertyBlock(MaterialPropertyBlock block);
    protected abstract Matrix4x4 GetGuiSpace();
  }
}
