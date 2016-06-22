using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Packaging {

  public class PackageDef : ScriptableObject {
    private const string PACKAGE_EXPORT_FOLDER_KEY = "LeapPackageDefExportFolder";

    [SerializeField]
    protected string _packageName;

    [SerializeField]
    protected string[] _dependantFolders;

    [SerializeField]
    protected string[] _dependantFiles;

    [SerializeField]
    protected PackageDef[] _dependantPackages;

    [ContextMenu("Reset Export Folder")]
    public void ResetExportFolder() {
      EditorPrefs.DeleteKey(getExportFolderKey());
    }

    [ContextMenu("Reset Export Folder For All")]
    public void ResetAllExportFolders() {
      var allPackageDefs = Resources.FindObjectsOfTypeAll<PackageDef>();
      foreach (var package in allPackageDefs) {
        package.ResetExportFolder();
      }
    }

    public bool PrompUserToSetExportPath() {
      string chosenFolder = EditorUtility.SaveFilePanel("Select export path for " + _packageName, Application.dataPath, _packageName, "unitypackage");
      if (string.IsNullOrEmpty(chosenFolder)) {
        return false;
      }

      EditorPrefs.SetString(getExportFolderKey(), chosenFolder);
      return true;
    }

    public bool HasExportFolderBeenDefined() {
      string key = getExportFolderKey();
      return EditorPrefs.HasKey(key);
    }

    public bool TryGetPackageExportFolder(out string folder, bool prompIfNotDefined) {
      string key = getExportFolderKey();
      if (!EditorPrefs.HasKey(key)) {
        folder = null;
        return false;
      }

      folder = EditorPrefs.GetString(key);
      return true;
    }

    public void BuildPackage(ExportPackageOptions options) {
      string exportFolder;
      if (!TryGetPackageExportFolder(out exportFolder, prompIfNotDefined: true)) {
        Debug.LogWarning("Did not build package " + _packageName + " because no path was defined.");
        return;
      }

      HashSet<string> assets = new HashSet<string>();

      HashSet<PackageDef> totalPackages = new HashSet<PackageDef>();
      totalPackages.Add(this);
      buildPackageSet(totalPackages);

      foreach (var package in totalPackages) {
        assets.UnionWith(package._dependantFiles);
        assets.UnionWith(package._dependantFolders);
      }

      var filteredAssets = assets.Where(path => File.Exists(path) || Directory.Exists(path)).ToArray();

      AssetDatabase.ExportPackage(filteredAssets, exportFolder, options);
    }

    /// <summary>
    /// Builds this package in addition to all packages that depend on this package in some way.
    /// </summary>
    public void BuildAllParentPackages(ExportPackageOptions options) {
      List<PackageDef> parentPackages = findParentPackages();
      parentPackages.Add(this);

      foreach (var package in parentPackages) {
        package.BuildPackage(options);
      }
    }

    private void buildPackageSet(HashSet<PackageDef> packages) {
      for (int i = 0; i < _dependantPackages.Length; i++) {
        PackageDef package = _dependantPackages[i];
        if (package == null) {
          continue;
        }

        if (!packages.Contains(package)) {
          packages.Add(package);
          package.buildPackageSet(packages);
        }
      }
    }

    /// <summary>
    /// Finds all packages that depend on this package in some way.
    /// </summary>
    private List<PackageDef> findParentPackages() {
      List<PackageDef> parents = new List<PackageDef>();
      var allPackages = Resources.FindObjectsOfTypeAll<PackageDef>();

      HashSet<PackageDef> packages = new HashSet<PackageDef>();
      foreach (var package in allPackages) {
        package.buildPackageSet(packages);
        if (packages.Contains(this)) {
          parents.Add(package);
        }
        packages.Clear();
      }

      return parents;
    }

    private string getExportFolderKey() {
      return PACKAGE_EXPORT_FOLDER_KEY + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
    }

    [MenuItem("Menu/MakeOne")]
    public static void doTheTHing() {
      var instance = CreateInstance<PackageDef>();
      AssetDatabase.CreateAsset(instance, "Assets/packageDef.asset");
      AssetDatabase.SaveAssets();
    }
  }
}
