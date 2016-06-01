using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class PhysicMaterialReplacer {
    private Collider[] _colliders;
    private PhysicMaterial[] _originalMaterials;
    private PhysicMaterial[] _replacementMaterials;

    public PhysicMaterialReplacer(Transform anchor, InteractionMaterial material) {
      _colliders = anchor.GetComponentsInChildren<Collider>(true);
      _originalMaterials = _colliders.Select(c => c.sharedMaterial).ToArray();

      switch (material.PhysicMaterialMode) {
        case InteractionMaterial.PhysicMaterialModeEnum.NoAction:
          _replacementMaterials = null;
          break;
        case InteractionMaterial.PhysicMaterialModeEnum.DuplicateExisting:
          _replacementMaterials = _originalMaterials.Select(m => Object.Instantiate(m)).ToArray();
          break;
        case InteractionMaterial.PhysicMaterialModeEnum.Replace:
          _replacementMaterials = _originalMaterials.Select(m => material.ReplacementPhysicMaterial).ToArray();
          break;
      }
    }

    public void ReplaceMaterials() {
      if (_replacementMaterials != null) {
        for (int i = 0; i < _colliders.Length; i++) {
          _colliders[i].sharedMaterial = _replacementMaterials[i];
        }
      }
    }

    public void RevertMaterials() {
      if (_replacementMaterials != null) {
        for (int i = 0; i < _colliders.Length; i++) {
          _colliders[i].sharedMaterial = _originalMaterials[i];
        }
      }
    }
  }
}
