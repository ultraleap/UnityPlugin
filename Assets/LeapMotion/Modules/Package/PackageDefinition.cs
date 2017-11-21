/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leap.Unity.Packaging {

  [CreateAssetMenu(fileName = "Package", menuName = "Package Definition", order = 202)]
  public class PackageDefinition : DefinitionBase {
    private const string DEFAULT_PACKAGE_NAME = "Package.asset";

    [Tooltip("All files within each folder will be included in this package when built.")]
    [SerializeField]
    protected string[] _dependantFolders = new string[0];

    [SerializeField]
    protected string[] _ignoredFolders = new string[0];

    [Tooltip("All files specified in this list will be included in this package when built.")]
    [SerializeField]
    protected string[] _dependantFiles = new string[0];

    [SerializeField]
    protected string[] _ignoredFiles = new string[0];

    [Tooltip("All files specified in each package will be included in this package when built.")]
    [SerializeField]
    protected PackageDefinition[] _dependantPackages;

    public PackageDefinition() {
      _definitionName = "Package";
    }

#if UNITY_EDITOR
    public static PackageDefinition[] FindAll() {
      return AssetDatabase.FindAssets("t:PackageDefinition").
             Select(guid => AssetDatabase.GUIDToAssetPath(guid)).
             Select(path => AssetDatabase.LoadAssetAtPath<PackageDefinition>(path)).
             OrderBy(def => def.DefinitionName).
             ToArray();
    }

    public static void BuildPackage(string packageGUID) {
      string assetPath = AssetDatabase.GUIDToAssetPath(packageGUID);
      var packageDef = AssetDatabase.LoadAssetAtPath<PackageDefinition>(assetPath);

      if (packageDef != null) {
        packageDef.BuildPackage(interactive: true);
      }
    }

    /// <summary>
    /// Builds the package defined by this definition.  This will include all dependant files, folders,
    /// and the dependancies of all dependant packages.  This will NOT include ANY package definition assets
    /// in the exported package.
    /// </summary>
    public void BuildPackage(bool interactive) {
      string exportFolder;
      if (!TryGetPackageExportFolder(out exportFolder, promptIfNotDefined: true)) {
        UnityEngine.Debug.LogWarning("Did not build package " + DefinitionName + " because no path was defined.");
        return;
      }

      string exportPath = Path.Combine(exportFolder, DefinitionName + ".unitypackage");

      HashSet<string> assets = new HashSet<string>();

      HashSet<PackageDefinition> totalPackages = new HashSet<PackageDefinition>();
      totalPackages.Add(this);
      buildPackageSet(totalPackages);

      foreach (var package in totalPackages) {

        //Check for missing files.  Any dependant file that is missing is an error and build cannot continue!
        var missingFiles = package._dependantFiles.Distinct().Where(path => !File.Exists(path));
        if (missingFiles.Any()) {
          string message = "Could not build package [" + package.DefinitionName + "] because the following dependant files were not found:\n";
          foreach (var missingFile in missingFiles) {
            message += "\n" + missingFile;
          }

          EditorUtility.DisplayDialog("Build Failed: Missing file", message, "Ok");
          return;
        }

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
      var packagePaths = new HashSet<string>(FindAll().Select(package => Path.GetFullPath(AssetDatabase.GetAssetPath(package))));

      //Filter paths to:
      // - paths that point to existing files
      // - paths that do not point to package definitions
      // - paths that do not point to meta files (let the exporter take care of that)
      // - paths that are not ignored
      // - paths that are not in an ignored folder
      var filteredAssets = assets.Where(path => File.Exists(path)).
                                  Where(path => !packagePaths.Contains(Path.GetFullPath(path))).
                                  Where(path => Path.GetExtension(path) != ".meta").
                                  Where(path => !_ignoredFiles.Select(Path.GetFullPath).Contains(Path.GetFullPath(path))).
                                  Where(path => _ignoredFolders.All(folder => !Path.GetFullPath(path).Contains(Path.GetFullPath(folder)))).
                                  ToArray();

      ExportPackageOptions options = ExportPackageOptions.Recurse;
      if (interactive) {
        options |= ExportPackageOptions.Interactive;
      }

      AssetDatabase.ExportPackage(filteredAssets, exportPath, options);
    }

    /// <summary>
    /// Builds this package in addition to all packages that depend on this package in some way.
    /// </summary>
    public void BuildAllChildPackages() {
      List<PackageDefinition> childPackages = GetChildPackages();
      childPackages.Add(this);

      buildPackages(childPackages.ToArray());
    }

    private void buildPackageSet(HashSet<PackageDefinition> packages) {
      if (_dependantPackages == null) return;

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
    public List<PackageDefinition> GetChildPackages() {
      List<PackageDefinition> children = new List<PackageDefinition>();
      var allPackages = FindAll();

      //Just search through all existing package definitions and check their dependancies
      HashSet<PackageDefinition> packages = new HashSet<PackageDefinition>();
      foreach (var package in allPackages) {
        package.buildPackageSet(packages);
        if (packages.Contains(this)) {
          children.Add(package);
        }
        packages.Clear();
      }

      return children;
    }

    private static void buildPackages(PackageDefinition[] packages) {
      string validPath = null;
      try {
        for (int i = 0; i < packages.Length; i++) {
          var package = packages[i];

          if (EditorUtility.DisplayCancelableProgressBar("Building Packages", "Building " + package.DefinitionName + "...", i / (float)packages.Length)) {
            break;
          }
          try {
            package.BuildPackage(interactive: false);
            package.TryGetPackageExportFolder(out validPath, promptIfNotDefined: false);
          } catch (Exception e) {
            UnityEngine.Debug.LogError("Exception thrown while trying to build package " + package.DefinitionName);
            UnityEngine.Debug.LogException(e);
          }
        }
      } finally {
        EditorUtility.ClearProgressBar();

        if (validPath != null) {
          Process.Start(validPath);
        }
      }
    }

    [MenuItem("Build/All Packages", priority = 0)]
    private static void buildAllPackages() {
      buildPackages(FindAll());
    }
#endif
  }
}
