using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Packaging {
  using Attributes;

  [CreateAssetMenu(fileName = "Create Me Pls")]
  public class BuildDefinition : DefinitionBase {
    private const string BUILD_EXPORT_FOLDER_KEY = "LeapBuildDefExportFolder";
    private const string DEFAULT_BUILD_NAME = "Build.asset";

    [SerializeField]
    private bool _trySuffixWithGitHash = false;

    [Tooltip("The options to enable for this build.")]
    [EnumFlags]
    [SerializeField]
    private BuildOptions _options;

    [Tooltip("The scenes that should be included in this build, " + "" +
             "in the order they should be included.")]
    [SerializeField]
    private SceneAsset[] _scenes;

    [Tooltip("The build targets to use for this build definition.")]
    [SerializeField]
    private BuildTarget[] _targets = { BuildTarget.StandaloneWindows64 };

    public static void Build(string guid) {
      var path = AssetDatabase.GUIDToAssetPath(guid);
      var def = AssetDatabase.LoadAssetAtPath<BuildDefinition>(path);
      def.Build();
    }

    public void Build() {
      return;

      var buildOptions = new BuildPlayerOptions();
      buildOptions.scenes = _scenes.Where(s => s != null).
                                    Select(s => AssetDatabase.GetAssetPath(s)).
                                    ToArray();
      buildOptions.options = _options;

      foreach (var target in _targets) {
        buildOptions.target = target;

        BuildPipeline.BuildPlayer(buildOptions);
      }
    }

    [MenuItem("Build/All Apps", priority = 1)]
    public static void BuildAll() {

    }
  }
}
