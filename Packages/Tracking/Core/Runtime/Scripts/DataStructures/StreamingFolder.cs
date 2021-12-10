/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{

    [Serializable]
    public class StreamingFolder : AssetFolder, ISerializationCallbackReceiver
    {

        [SerializeField]
        private string _relativePath;

        /// <summary>
        /// Gets the full path to the streaming folder.  This operation is safe to be
        /// called from within a build or from within the editor, and will always return
        /// the correct full path to the streaming folder.  Setting the path via code
        /// is not supported.
        /// </summary>
        public override string Path
        {
            get
            {
                if (_relativePath == null) { _relativePath = ""; }
                return System.IO.Path.Combine(Application.streamingAssetsPath, _relativePath);
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public void OnAfterDeserialize() { }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(_assetFolder);
            if (string.IsNullOrEmpty(assetPath))
            {
                _relativePath = null;
            }
            else
            {
                string fullFolder = System.IO.Path.GetFullPath(assetPath);
                _relativePath = Utils.MakeRelativePath(Application.streamingAssetsPath, fullFolder);
                _relativePath = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(),
                                            _relativePath.Split(System.IO.Path.DirectorySeparatorChar).Skip(1).ToArray());
            }
#endif
        }
    }
}