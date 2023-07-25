using System;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Readme
{
    [CreateAssetMenu(fileName = "Readme", menuName = "ScriptableObjects/Readme", order = 5)]
    public class SceneReadme : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        public SceneAsset scene;
        public Section[] sections;

        [Serializable]
        public class Section
        {
            public string heading, text, linkText, url, pingSceneElement, settingsPage, assetLinkText;
            public UnityEngine.Object assetLink;
            public bool assetLinkOpens;
            public Texture2D image;
            public int imageHeight;
        }
    }
}