/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Packaging {

  public class DefinitionBase : ScriptableObject {
    private const string PACKAGE_EXPORT_FOLDER_KEY = "LeapPackageDefExportFolder";

    [SerializeField]
    [FormerlySerializedAs("_packageName")]
    protected string _definitionName;

    [FormerlySerializedAs("_generateBuildDropdown")]
    [SerializeField]
    protected bool _showInBuildMenu = false;

    public string DefinitionName {
      get {
        return _definitionName;
      }
    }

    public bool ShowInBuildMenu {
      get {
        return _showInBuildMenu;
      }
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Export Folder")]
    public void ResetExportFolder() {
      EditorPrefs.DeleteKey(getExportFolderKey());
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

      string chosenFolder = EditorUtility.OpenFolderPanel("Select export folder for " + DefinitionName, promptFolder, "Packages");
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

    private string getExportFolderKey() {
      //Tie the key to the guid of the asset, as it will never change for the duration of the asset's life and will be unique for
      //a given computer.
      return PACKAGE_EXPORT_FOLDER_KEY + "_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
    }
#endif
  }
}
