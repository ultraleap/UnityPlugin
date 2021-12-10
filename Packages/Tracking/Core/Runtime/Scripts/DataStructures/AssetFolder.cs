/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{

    /// <summary>
    /// A convenient serializable representation of an asset folder.
    /// Only useful for editor scripts since asset folder structure
    /// is not preserved for builds.  The asset folder struct itself is
    /// still available at runtime for serialization ease, but the Path
    /// property will not be available.
    /// </summary>
    [Serializable]
    public class AssetFolder
    {

        [SerializeField]
        protected UnityObject _assetFolder;

        public AssetFolder() { }

        public AssetFolder(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets or sets the folder path.  This path will always be a path
        /// relative to the asset folder, and matches the format expected and
        /// returned by AssetDatabase.  This operation cannot be performed
        /// from inside of a build due to Assets no longer existing.
        /// </summary>
        public virtual string Path
        {
            get
            {
#if UNITY_EDITOR
                if (_assetFolder != null)
                {
                    return AssetDatabase.GetAssetPath(_assetFolder);
                }
                else
                {
                    return null;
                }
#else
                throw new InvalidOperationException("Cannot access the Path of an Asset Folder in a build.");
#endif
            }
            set
            {
#if UNITY_EDITOR
                _assetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
#else
                throw new InvalidOperationException("Cannot set the Path of an Asset Folder in a build.");
#endif
            }
        }
    }
}