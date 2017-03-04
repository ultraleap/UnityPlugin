using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  [ExecuteInEditMode]
  public class ProceduralGenerator : MonoBehaviour {

    [SerializeField]
    private UpdateMode _updateMode = UpdateMode.OnChange;

    [SerializeField]
    private bool _updateIngame = false;

    void Update() {
      if (_updateMode == UpdateMode.Always && (!Application.isPlaying || _updateIngame)) {
        UpdateMesh();
      }
    }

    [ContextMenu("Update")]
    public void UpdateMesh() {
      ProceduralMesh[] found = GetComponentsInChildren<ProceduralMesh>(includeInactive: true);
      List<ProceduralMesh> valid = new List<ProceduralMesh>();
      foreach (Transform child in transform) {
        ensureProceduralMeshesAreCorrect(child, valid);
      }

      for (int i = 0; i < found.Length; i++) {
        ProceduralMesh procMesh = found[i];
        if (!valid.Contains(procMesh)) {
          DestroyImmediate(procMesh);
        }
      }

      List<RawMesh> sources;
      List<ProceduralMesh> targets;

      generateRecursively(transform, out sources, out targets);

      Assert.AreEqual(sources.Count, targets.Count, "The number of sources " + sources.Count + " needs to be equal to the number of targets " + targets.Count);

      for (int i = 0; i < sources.Count; i++) {
        TransformMod modifier;
        modifier.matrix = transform.localToWorldMatrix * targets[i].transform.worldToLocalMatrix;

        RawMesh mesh = sources[i];
        modifier.Modify(ref mesh);

        targets[i].UpdateWithMesh(mesh);
      }
    }

    private void generateRecursively(Transform anchor, out List<RawMesh> sources, out List<ProceduralMesh> targets) {
      sources = new List<RawMesh>();
      targets = new List<ProceduralMesh>();

      foreach (Transform child in anchor) {
        List<RawMesh> childSources;
        List<ProceduralMesh> childTargets;
        generateRecursively(child, out childSources, out childTargets);
        sources.AddRange(childSources);
        targets.AddRange(childTargets);
      }

      IMeshBehaviour behaviour = anchor.GetComponent<IMeshBehaviour>();
      if (behaviour != null && ((Behaviour)behaviour).enabled) {
        if (!(behaviour is IPreGenerate) || ((IPreGenerate)behaviour).OnPreGenerate()) {
          RawMesh mesh = new RawMesh();
          behaviour.meshDefinition.Generate(mesh);
          sources.Add(mesh);
        }
      }

      IOperationBehaviour operation = anchor.GetComponent<IOperationBehaviour>();
      if (operation != null && ((Behaviour)operation).enabled) {
        if (!(operation is IPreGenerate) || ((IPreGenerate)operation).OnPreGenerate()) {
          RawMesh newMesh;
          operation.meshOperation.Operate(sources, out newMesh);
          sources.Clear();
          sources.Add(newMesh);
        }
      }

      IModifierBehaviour[] modifiers = anchor.GetComponents<IModifierBehaviour>();
      for (int i = 0; i < sources.Count; i++) {
        RawMesh mesh = sources[i];
        for (int j = 0; j < modifiers.Length; j++) {
          IModifierBehaviour modifier = modifiers[j];
          if (modifier is IPreGenerate) {
            if (!((IPreGenerate)modifier).OnPreGenerate()) {
              continue;
            }
          }

          if (!((Behaviour)modifier).enabled) {
            continue;
          }

          modifier.meshModifier.Modify(ref mesh);
        }
        sources[i] = mesh;
      }

      ProceduralMesh procMesh = anchor.GetComponent<ProceduralMesh>();
      if (procMesh != null) {
        targets.Add(procMesh);
      }

      applyLocalTransform(anchor, sources);
    }

    private void applyLocalTransform(Transform anchor, List<RawMesh> meshes) {
      if (anchor.localPosition == Vector3.zero &&
          anchor.localRotation == Quaternion.identity &&
          anchor.localScale == Vector3.one) {
        return;
      }

      TransformMod modifier;
      modifier.matrix = Matrix4x4.TRS(anchor.localPosition, anchor.localRotation, anchor.localScale);
      for (int i = 0; i < meshes.Count; i++) {
        RawMesh mesh = meshes[i];
        modifier.Modify(ref mesh);
      }
    }

    private void ensureProceduralMeshesAreCorrect(Transform anchor, List<ProceduralMesh> valid) {
      IMeshBehaviour[] meshBehaviours = anchor.GetComponents<IMeshBehaviour>();
      IOperationBehaviour[] meshOperations = anchor.GetComponents<IOperationBehaviour>();

      if ((meshOperations.Length + meshBehaviours.Length) > 1) {
        Debug.LogError("Should not have more than one mesh operation or mesh behaviour per gameObject.");
      }

      if (meshBehaviours.Length == 0 && meshOperations.Length == 0) {
        foreach (Transform child in anchor) {
          ensureProceduralMeshesAreCorrect(child, valid);
        }
      } else {
        ProceduralMesh procMesh = anchor.GetComponent<ProceduralMesh>();
        if (procMesh == null) {
          procMesh = anchor.gameObject.AddComponent<ProceduralMesh>();
        }
        valid.Add(procMesh);
        return;
      }
    }

    public enum UpdateMode {
      None,
      OnChange,
      Always
    }
  }
}
