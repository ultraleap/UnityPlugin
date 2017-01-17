using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Gui.Space {

  [ExecuteInEditMode]
  public abstract class GuiSpace : MonoBehaviour {
    public const string FEATURE_ALL = "GUI_SPACE_ALL";
    public const string FEATURE_CYLINDRICAL_CONST_WIDTH = "GUI_SPACE_CYLINDRICAL_CONSTANT_WIDTH";
    public const string FEATURE_CYLINDRICAL_ANGULAR = "GUI_SPACE_CYLINDRICAL_ANGULAR";
    public static string[] ALL_FEATURES = { FEATURE_ALL, FEATURE_CYLINDRICAL_CONST_WIDTH, FEATURE_CYLINDRICAL_ANGULAR };

    private MaterialPropertyBlock _propertyBlock;

    public abstract Vector3 FromRect(Vector3 rectPos);

    public abstract Vector3 ToRect(Vector3 guiPos);

    public abstract void FromRect(Vector3 rectPos, Quaternion rectRot, out Vector3 guiPos, out Quaternion guiRot);

    public abstract void ToRect(Vector3 guiPos, Quaternion guiRot, out Vector3 rectPos, out Quaternion rectRot);

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
      UpdatePropertyBlock(_propertyBlock);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    private void updateRendererRuntime(Renderer renderer) {
      foreach (var material in renderer.sharedMaterials) {
        material.EnableKeyword(ShaderVariantName);
      }

      renderer.GetPropertyBlock(_propertyBlock);
      UpdatePropertyBlock(_propertyBlock);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    public void ResetRenderer(Renderer renderer) {
#if UNITY_EDITOR
      _propertyBlock = _propertyBlock ?? new MaterialPropertyBlock();
      resetRendererEditor(renderer);
#else
      resetRendererRuntime(renderer);
#endif
    }

    private void resetRendererEditor(Renderer renderer) {
      renderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetFloat("_GuiSpaceSelection", 0);
      renderer.SetPropertyBlock(_propertyBlock);
    }

    private void resetRendererRuntime(Renderer renderer) {
      foreach (var material in renderer.sharedMaterials) {
        foreach (var feature in ALL_FEATURES) {
          material.DisableKeyword(feature);
        }
      }
    }

    void Awake() {
      _propertyBlock = new MaterialPropertyBlock();
    }

    void OnEnable() {

    }

    void OnDisable() {

    }

#if UNITY_EDITOR
    void Update() {

    }
#endif

    protected abstract string ShaderVariantName { get; }
    protected abstract int SelectionIndexForAllVariant { get; }
    protected abstract void UpdatePropertyBlock(MaterialPropertyBlock block);
  }
}
