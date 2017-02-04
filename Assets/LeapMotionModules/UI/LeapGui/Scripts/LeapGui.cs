using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Leap.Unity.Query;

[ExecuteInEditMode]
public class LeapGui : MonoBehaviour {
  public const string FEATURE_PREFIX = "LEAP_GUI_";
  public const string PROPERTY_PREFIX = "_LeapGui";

  public const string FEATURE_MOVEMENT_TRANSLATION = FEATURE_PREFIX + "MOVEMENT_TRANSLATION";
  public const string FEATURE_MOVEMENT_FULL = FEATURE_PREFIX + "MOVEMENT_FULL";

  public List<LeapGuiFeatureBase> features = new List<LeapGuiFeatureBase>();

  public LeapGuiSpace space;

#if UNITY_EDITOR
  public new LeapGuiRenderer renderer;
#else
  public LeapGuiRenderer renderer;
#endif

  [HideInInspector]
  public List<LeapGuiElement> elements;

  [HideInInspector]
  public List<AnchorOfConstantSize> anchors;

  [NonSerialized]
  public List<FeatureSupportInfo> supportInfo;

  void OnValidate() {
    for (int i = features.Count; i-- != 0;) {
      if (features[i] == null) {
        features.RemoveAt(i);
      }
    }

#if UNITY_EDITOR
    for (int i = features.Count; i-- != 0;) {
      var feature = features[i];
      if (feature.gameObject != gameObject) {
        LeapGuiFeatureBase movedFeature;
        if (InternalUtility.TryMoveComponent(feature, gameObject, out movedFeature)) {
          features[i] = movedFeature;
        } else {
          Debug.LogWarning("Could not move feature component " + feature + "!");
          InternalUtility.Destroy(feature);
          features.RemoveAt(i);
        }
      }
    }

    if (space != null && space.gameObject != gameObject) {
      LeapGuiSpace movedSpace;
      if (InternalUtility.TryMoveComponent(space, gameObject, out movedSpace)) {
        space = movedSpace;
      } else {
        Debug.LogWarning("Could not move space component " + space + "!");
        InternalUtility.Destroy(space);
      }
    }

    if (renderer != null && renderer.gameObject != gameObject) {
      LeapGuiRenderer movedRenderer;
      if (InternalUtility.TryMoveComponent(renderer, gameObject, out movedRenderer)) {
        renderer = movedRenderer;
      } else {
        Debug.LogWarning("Could not move renderer component " + renderer + "!");
        InternalUtility.Destroy(renderer);
      }
    }
#endif
  }

  void OnDestroy() {
    if (renderer != null) DestroyImmediate(renderer);
    if (space != null) DestroyImmediate(space);
    foreach (var feature in features) {
      if (feature != null) DestroyImmediate(feature);
    }
  }

  void Awake() {
    if (space != null) {
      space.gui = this;
    }
  }

  void OnEnable() {
    if (Application.isPlaying) {
      renderer.OnEnableRenderer();
      if (space != null) space.BuildElementData(transform);
    }
  }

  void OnDisable() {
    if (Application.isPlaying) {
      renderer.OnDisableRenderer();
    }
  }

  void LateUpdate() {
#if UNITY_EDITOR
    if (Application.isPlaying) {
      doLateUpdateRuntime();
    } else {
      doLateUpdateEditor();
    }
#else
    doLateUpdateRuntime();
#endif
  }

  public LeapGuiRenderer GetRenderer() {
    return renderer;
  }

#if UNITY_EDITOR
  public void SetSpace(Type spaceType) {
    if (Application.isPlaying) {
      throw new InvalidOperationException("Cannot change the space at runtime.");
    }

    UnityEditor.Undo.RecordObject(this, "Change Gui Space");
    UnityEditor.EditorUtility.SetDirty(this);

    if (space != null) {
      DestroyImmediate(space);
      space = null;
    }

    space = gameObject.AddComponent(spaceType) as LeapGuiSpace;

    if (space != null) {
      space.gui = this;
    }
  }

  public void AddFeature(Type featureType) {
    var feature = gameObject.AddComponent(featureType);
    features.Add(feature as LeapGuiFeatureBase);
  }

  public void SetRenderer(Type rendererType) {
    if (Application.isPlaying) {
      throw new InvalidOperationException("Cannot change renderer at runtime.");
    }

    UnityEditor.Undo.RecordObject(this, "Changed Gui Renderer");
    UnityEditor.EditorUtility.SetDirty(this);

    if (renderer != null) {
      renderer.OnDisableRendererEditor();
      DestroyImmediate(renderer);
      renderer = null;
    }

    renderer = gameObject.AddComponent(rendererType) as LeapGuiRenderer;

    if (renderer != null) {
      renderer.gui = this;
      renderer.OnEnableRendererEditor();
    }
  }
#endif

  public bool GetSupportedFeatures<T>(List<T> features) where T : LeapGuiFeatureBase {
    features.Clear();
    for (int i = 0; i < this.features.Count; i++) {
      var feature = this.features[i];
      if (!(feature is T)) continue;

      if (supportInfo[i].support != SupportType.Warning) {
        features.Add(feature as T);
      }
    }

    return features.Count != 0;
  }

  public void rebuildElementList(Transform root, AnchorOfConstantSize currAnchor) {
    int count = root.childCount;
    for (int i = 0; i < count; i++) {
      Transform child = root.GetChild(i);
      if (!child.gameObject.activeSelf) continue;

      var childAnchor = currAnchor;

      var anchor = child.GetComponent<AnchorOfConstantSize>();
      if (anchor != null && anchor.enabled) {
        childAnchor = anchor;
        anchors.Add(anchor);
      }

      var element = child.GetComponent<LeapGuiElement>();
      if (element != null && element.enabled) {
        element.anchor = childAnchor;
        element.elementId = elements.Count;
        elements.Add(element);
      }

      rebuildElementList(child, childAnchor);
    }
  }

#if UNITY_EDITOR
  private void doLateUpdateEditor() {
    elements.Clear();
    anchors.Clear();

    Profiler.BeginSample("Rebuild Element List");
    rebuildElementList(transform, null);
    Profiler.EndSample();

    Profiler.BeginSample("Rebuild Feature Data");
    rebuildFeatureData();
    Profiler.EndSample();

    Profiler.BeginSample("Rebuild Support Info");
    rebuildFeatureSupportInfo();
    Profiler.EndSample();

    if (space != null) {
      Profiler.BeginSample("Build Element Data");
      space.BuildElementData(transform);
      Profiler.EndSample();

      Profiler.BeginSample("Rebuild Picking Meshes");
      rebuildPickingMeshes();
      Profiler.EndSample();
    }

    if (renderer != null) {
      Profiler.BeginSample("Update Renderer");
      renderer.OnUpdateRendererEditor();
      Profiler.EndSample();
    }
  }
#endif

  private void doLateUpdateRuntime() {
    if (renderer != null) {
      renderer.OnUpdateRenderer();
      foreach (var feature in features) {
        feature.isDirty = false;
      }
    }
  }

  private void rebuildFeatureData() {
    foreach (var feature in features) {
      feature.ClearDataObjectReferences();
    }

    for (int i = 0; i < elements.Count; i++) {
      var element = elements[i];

      List<LeapGuiElementData> dataList = new List<LeapGuiElementData>();
      foreach (var feature in features) {
        var dataObj = element.data.Query().OfType(feature.GetDataObjectType()).FirstOrDefault();
        if (dataObj != null) {
          element.data.Remove(dataObj);
        } else {
          dataObj = feature.CreateDataObject(element);
        }
        feature.AddDataObjectReference(dataObj);
        dataList.Add(dataObj);
      }

      foreach (var dataObj in element.data) {
        DestroyImmediate(dataObj);
      }

      element.data = dataList;
    }
  }

  private void rebuildFeatureSupportInfo() {
    var typeToFeatures = new Dictionary<Type, List<LeapGuiFeatureBase>>();
    foreach (var feature in features) {
      Type featureType = feature.GetType();
      List<LeapGuiFeatureBase> list;
      if (!typeToFeatures.TryGetValue(featureType, out list)) {
        list = new List<LeapGuiFeatureBase>();
        typeToFeatures[featureType] = list;
      }

      list.Add(feature);
    }


    var featureToInfo = new Dictionary<LeapGuiFeatureBase, FeatureSupportInfo>();

    if (renderer != null) {
      foreach (var pair in typeToFeatures) {
        var featureType = pair.Key;
        var featureList = pair.Value;
        var infoList = new List<FeatureSupportInfo>().FillEach(featureList.Count, () => FeatureSupportInfo.FullSupport());

        var castList = Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType)) as IList;
        foreach (var feature in featureList) {
          castList.Add(feature);
        }

        try {
          var interfaceType = typeof(ISupportsFeature<>).MakeGenericType(featureType);
          if (!interfaceType.IsAssignableFrom(renderer.GetType())) {
            infoList.FillEach(() => FeatureSupportInfo.Error("This renderer does not support this feature."));
            continue;
          }

          var supportDelegate = interfaceType.GetMethod("GetSupportInfo");

          if (supportDelegate == null) {
            Debug.LogError("Could not find support delegate.");
            continue;
          }

          supportDelegate.Invoke(renderer, new object[] { castList, infoList });
        } finally {
          for (int i = 0; i < featureList.Count; i++) {
            featureToInfo[featureList[i]] = infoList[i];
          }
        }
      }
    }

    supportInfo = new List<FeatureSupportInfo>();
    foreach (var feature in features) {
      supportInfo.Add(feature.GetSupportInfo(this).OrWorse(featureToInfo[feature]));
    }
  }

#if UNITY_EDITOR
  private void rebuildPickingMeshes() {
    List<Vector3> pickingVerts = new List<Vector3>();
    List<int> pickingTris = new List<int>();

    foreach (var element in elements) {
      pickingVerts.Clear();
      pickingTris.Clear();

      Mesh pickingMesh = element.pickingMesh;
      if (pickingMesh == null) {
        pickingMesh = new Mesh();
        pickingMesh.MarkDynamic();
        pickingMesh.hideFlags = HideFlags.HideAndDontSave;
        pickingMesh.name = "Gui Element Picking Mesh";
        element.pickingMesh = pickingMesh;
      }
      pickingMesh.Clear();

      foreach (var dataObj in element.data) {
        if (dataObj is LeapGuiMeshData) {
          var meshData = dataObj as LeapGuiMeshData;
          if (meshData.mesh == null) continue;

          var topology = MeshCache.GetTopology(meshData.mesh);
          for (int i = 0; i < topology.tris.Length; i++) {
            pickingTris.Add(topology.tris[i] + pickingVerts.Count);
          }

          for (int i = 0; i < topology.verts.Length; i++) {
            Vector3 localRectVert = transform.InverseTransformPoint(element.transform.TransformPoint(topology.verts[i]));
            pickingVerts.Add(space.TransformPoint(element, localRectVert));
          }
        }
      }

      pickingMesh.SetVertices(pickingVerts);
      pickingMesh.SetTriangles(pickingTris, 0, calculateBounds: true);
      pickingMesh.RecalculateNormals();
    }
  }
#endif
}
