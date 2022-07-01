/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.IO;
using UnityEditor;

namespace Leap.Unity
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    [CustomPropertyDrawer(typeof(StreamingFolder), useForChildren: true)]
    public class StreamingAssetPropertyDrawer : StreamingFolderPropertyDrawer
    {

        protected override string PromptUserForPath(string currentPath)
        {
            return EditorUtility.OpenFilePanel("Select File", currentPath, "");
        }

        protected override bool ValidatePath(string fullPath, string relativePath, out string errorMessage)
        {
            if (!File.Exists(fullPath))
            {
                errorMessage = "The specified file does not exist!";
                return false;
            }

            if ((File.GetAttributes(fullPath) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                errorMessage = "You must specify a file and not a directory!";
                return false;
            }

            bool isValid = base.ValidatePath(fullPath, relativePath, out errorMessage);
            if (!isValid)
            {
                errorMessage = "The specified file is not a streaming asset. Streaming assets must be inside project's Assets/StreamingAssets directory.";
            }
            return isValid;
        }
    }
}