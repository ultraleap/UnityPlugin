/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Attributes
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public enum FileDialogType { Open, Save, Folder };

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class ReadFileChooserAttribute : FileChooserAttribute
    {
        public ReadFileChooserAttribute(bool preserveExistingFileName = false,
          string extension = null) : base(FileDialogType.Open,
            preserveExistingFileName, extension)
        { }
    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class WriteFileChooserAttribute : FileChooserAttribute
    {
        public WriteFileChooserAttribute(bool preserveExistingFileName = false,
          string extension = null) : base(FileDialogType.Save,
            preserveExistingFileName, extension)
        { }
    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class FolderChooserAttribute : FileChooserAttribute
    {
        public FolderChooserAttribute(bool preserveExistingFileName = false,
          string extension = null) : base(FileDialogType.Folder,
            preserveExistingFileName, extension)
        { }
    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public class FileChooserAttribute : CombinablePropertyAttribute,
      IAfterFieldAdditiveDrawer
    {

        public FileDialogType dialogType;
        public bool preserveExistingFileName = false;
        /// <summary> Expected file extension .</summary>
        public string extension = null;

        public FileChooserAttribute(FileDialogType dialogType,
          bool preserveExistingFileName = false,
          string extension = null)
        {
            this.dialogType = dialogType;
            this.preserveExistingFileName = preserveExistingFileName;
            this.extension = extension;
        }

#if UNITY_EDITOR

        public void Draw(Rect rect, SerializedProperty property)
        {
            var existingValue = property.stringValue;
            var pipeSyntaxPath = PipeFileSyntax.Parse(property.stringValue);
            existingValue = pipeSyntaxPath.path;

            string currentDir = null;
            if (!string.IsNullOrEmpty(existingValue))
            {
                currentDir = Path.GetDirectoryName(existingValue);
            }

            string chosenFile = null;
            if (GUI.Button(rect, "..."))
            {
                if (dialogType == FileDialogType.Folder)
                {
                    chosenFile = EditorUtility.OpenFolderPanel("Choose Folder", currentDir, null);
                    if (!string.IsNullOrEmpty(chosenFile))
                    {
                        chosenFile += Path.DirectorySeparatorChar;
                        if (!string.IsNullOrEmpty(existingValue) && preserveExistingFileName)
                        {
                            var existingName = Path.GetFileName(existingValue);
                            chosenFile = Path.Combine(chosenFile, existingName);
                        }
                    }
                }
                else if (dialogType == FileDialogType.Open)
                {
                    chosenFile = EditorUtility.OpenFilePanel("Choose File", currentDir,
                      null);
                }
                else
                { // dialogType == FileDialogType.Save
                    chosenFile = EditorUtility.SaveFilePanel("Output File", currentDir,
                      "", null);
                }
            }

            if (!string.IsNullOrEmpty(chosenFile))
            {
                property.stringValue = pipeSyntaxPath.ChangePath(chosenFile).ToString();
            }
        }

#endif

        public float GetWidth()
        {
            return 24;
        }
    }

}