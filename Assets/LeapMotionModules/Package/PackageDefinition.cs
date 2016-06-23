using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.Packaging {

  public class PackageDefinition : ScriptableObject {
    private const string PACKAGE_EXPORT_FOLDER_KEY = "LeapPackageDefExportFolder";
    private const string DEFAULT_PACKAGE_NAME = "Package.asset";

    [Tooltip("The name of the package.  Used to define the name of the export package file.")]
    [SerializeField]
    protected string _packageName = "New Package";

    [SerializeField]
    protected string[] _dependantFolders;

    [SerializeField]
    protected string[] _dependantFiles;

    [SerializeField]
    protected PackageDefinition[] _dependantPackages;

    [ContextMenu("Reset Export Folder")]
    public void ResetExportFolder() {
      EditorPrefs.DeleteKey(getExportFolderKey());
    }

    [ContextMenu("Reset Export Folder For All")]
    public void ResetAllExportFolders() {
      var allPackageDefs = Resources.FindObjectsOfTypeAll<PackageDefinition>();
      foreach (var package in allPackageDefs) {
        package.ResetExportFolder();
      }
    }

    /// <summary>
    /// Forces a save prompt for the user to select the export path.  Returns whether or not
    /// the path was updated.
    /// </summary>
    public bool PrompUserToSetExportPath() {
      string promptFolder;
      if (!TryGetPackageExportFolder(out promptFolder, promptIfNotDefined: false)) {
        promptFolder = Application.dataPath;
      }

      string chosenFolder = EditorUtility.OpenFolderPanel("Select export folder for " + _packageName, promptFolder, "Packages");
      if (string.IsNullOrEmpty(chosenFolder)) {
        return false;
      }

      EditorPrefs.SetString(getExportFolderKey(), chosenFolder);
      return true;
    }

    /// <summary>
    /// Returns whether or not the export folder has been defined for this user. 
    /// </summary>
    public bool HasExportFolderBeenDefined() {
      string key = getExportFolderKey();
      return EditorPrefs.HasKey(key);
    }

    /// <summary>
    /// Tries to get the package export folder.  This method can be configured to auto-promp
    /// the user for the export folder if it is not yet defined.  Returns whether or not this
    /// method returned a valid export folder.
    /// </summary>
    public bool TryGetPackageExportFolder(out string folder, bool promptIfNotDefined) {
      string key = getExportFolderKey();
      if (!EditorPrefs.HasKey(key)) {
        if (!promptIfNotDefined || !PrompUserToSetExportPath()) {
          folder = null;
          return false;
        }
      }

      folder = EditorPrefs.GetString(key);
      return true;
    }

    /// <summary>
    /// Builds the package defined by this definition.  This will include all dependant files, folders,
    /// and the dependancies of all dependant packages.  This will NOT include ANY package definition assets
    /// in the exported package.
    /// </summary>
    public void BuildPackage(ExportPackageOptions options) {
      string exportFolder;
      if (!TryGetPackageExportFolder(out exportFolder, promptIfNotDefined: true)) {
        Debug.LogWarning("Did not build package " + _packageName + " because no path was defined.");
        return;
      }

      string exportPath = Path.Combine(exportFolder, _packageName + ".unitypackage");

      HashSet<string> assets = new HashSet<string>();

      HashSet<PackageDefinition> totalPackages = new HashSet<PackageDefinition>();
      totalPackages.Add(this);
      buildPackageSet(totalPackages);

      foreach (var package in totalPackages) {
        assets.UnionWith(package._dependantFiles);

        //package exporter expands directories, we do it manually so that we can filter later
        //on a file-by-file basis
        foreach (var folder in package._dependantFolders) {
          foreach (var subFile in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) {
            assets.Add(subFile);
          }
        }
      }

      //Build a set of paths to package definitions
      //We want to be able to exclude paths from the export that are paths to package definitions
      var packagePaths = new HashSet<string>(Resources.FindObjectsOfTypeAll<PackageDefinition>().Select(package => Path.GetFullPath(AssetDatabase.GetAssetPath(package))));

      //Filter paths to:
      // - paths that point to existing files
      // - paths that do not point to package definitions
      // - paths that do not point to meta files (let the exporter take care of that)
      var filteredAssets = assets.Where(path => File.Exists(path)).
                                  Where(path => !packagePaths.Contains(Path.GetFullPath(path))).
                                  Where(path => Path.GetExtension(path) != ".meta").
                                  ToArray();

      AssetDatabase.ExportPackage(filteredAssets, exportPath, options);
    }

    /// <summary>
    /// Builds this package in addition to all packages that depend on this package in some way.
    /// </summary>
    public void BuildAllParentPackages(ExportPackageOptions options) {
      List<PackageDefinition> parentPackages = findParentPackages();
      parentPackages.Add(this);

      foreach (var package in parentPackages) {
        package.BuildPackage(options);
      }
    }

    private void buildPackageSet(HashSet<PackageDefinition> packages) {
      for (int i = 0; i < _dependantPackages.Length; i++) {
        PackageDefinition package = _dependantPackages[i];
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
    private List<PackageDefinition> findParentPackages() {
      List<PackageDefinition> parents = new List<PackageDefinition>();
      var allPackages = Resources.FindObjectsOfTypeAll<PackageDefinition>();

      //Just search through all existing package definitions and check their dependancies
      HashSet<PackageDefinition> packages = new HashSet<PackageDefinition>();
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
      //Tie the key to the guid of the asset, as it will never change for the duration of the asset's life and will be unique for
      //a given computer.
      return PACKAGE_EXPORT_FOLDER_KEY + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
    }

    [MenuItem("Assets/Create/Package Definition", priority = 201)]
    private static void createNewPackageDef() {
      string path = "Assets";

      foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
        path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
          path = Path.GetDirectoryName(path);
          break;
        }
      }

      path = Path.Combine(path, DEFAULT_PACKAGE_NAME);
      path = AssetDatabase.GenerateUniqueAssetPath(path);

      PackageDefinition package = CreateInstance<PackageDefinition>();
      AssetDatabase.CreateAsset(package, path);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Selection.activeObject = package;
    }
  }
}
