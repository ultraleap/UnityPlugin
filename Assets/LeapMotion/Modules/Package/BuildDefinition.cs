using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Packaging {
  using Attributes;

  [CreateAssetMenu(fileName = "Create Me Pls")]
  public class BuildDefinition : DefinitionBase {
    private const string BUILD_EXPORT_FOLDER_KEY = "LeapBuildDefExportFolder";
    private const string DEFAULT_BUILD_NAME = "Build.asset";

    [SerializeField]
    private bool _trySuffixWithGitHash = false;

#if UNITY_EDITOR
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
      string exportFolder;
      if (!TryGetPackageExportFolder(out exportFolder, promptIfNotDefined: true)) {
        UnityEngine.Debug.LogError("Could not build " + DefinitionName + " because no export folder was chosen.");
        return;
      }

      var buildOptions = new BuildPlayerOptions() {
        scenes = _scenes.Where(s => s != null).
                                    Select(s => AssetDatabase.GetAssetPath(s)).
                                    ToArray(),
        options = _options,
        locationPathName = exportFolder
      };

      foreach (var target in _targets) {
        buildOptions.target = target;

        BuildPipeline.BuildPlayer(buildOptions);
      }
    }

    private static bool tryGetGitCommitHash(out string hash) {
      try {
        Process process = new Process();

        ProcessStartInfo startInfo = new ProcessStartInfo() {
          WindowStyle = ProcessWindowStyle.Hidden,
          FileName = "cmd.exe",
          Arguments = "/C git log -1",
          WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
          RedirectStandardOutput = true,
          UseShellExecute = false
        };

        process.StartInfo = startInfo;
        process.Start();

        string result = process.StandardOutput.ReadToEnd();

        var match = Regex.Match(result, @"^commit (\w{40})");
        if (match.Success) {
          hash = match.Groups[1].Value;
          return true;
        } else {
          hash = "";
          return false;
        }
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
        hash = "";
        return false;
      }
    }

    [MenuItem("Build/All Apps", priority = 1)]
    public static void BuildAll() {

    }
#endif
  }
}
