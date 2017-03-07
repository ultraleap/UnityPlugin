using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiBakedRenderer))]
public class LeapGuiBakedRendererEditor : CustomEditorBase {

  protected override void OnEnable() {
    base.OnEnable();

    specifyConditionalDrawing("_createMeshRenderers", "_bakeLightmapUvs", "_lightmapUnwrapSettings");
    specifyConditionalDrawing("_bakeLightmapUvs", "_lightmapUnwrapSettings");
  }
}
