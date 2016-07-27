using UnityEditor;
using System.Collections.Generic;

namespace Leap.Unity.Interaction.Testing {

  [CustomEditor(typeof(SadisticTestManager))]
  public class SadisticTestManagerEditor : CustomEditorBase {

    private HashSet<string> _importantProperties = new HashSet<string>();

    protected override void OnEnable() {
      base.OnEnable();

      _importantProperties.UnionWith(new string[] {
        "_recordings",
        "_callbacks",
        "_actions",
        "_expectedCallbacks",
        "_forbiddenCallbacks",
        "_actionDelay",
        "_spawnObjectTime",
        "_spawnObjectDelay",
      });
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      bool wasAnyImportantPropertyModified = false;
      foreach (var modifiedProperty in _modifiedProperties) {
        if (_importantProperties.Contains(modifiedProperty.name)) {
          wasAnyImportantPropertyModified = true;
          break;
        }
      }

      if (wasAnyImportantPropertyModified) {
        (target as SadisticTestManager).UpdateChildrenTests();
      }
    }

  }
}
