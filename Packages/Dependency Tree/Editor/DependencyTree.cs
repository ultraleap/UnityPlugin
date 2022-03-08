namespace Leap.Unity
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;

    internal class DependencyTree : ScriptableObject
    {
        [Serializable]
        public class AssetReference
        {
            public string guid;
            public string path;
            public float size;
            public List<string> references = new List<string>();
        }

        private static readonly HashSet<string> YamlFileExtensions
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".asset",
                ".unity",
                ".prefab",
                ".mat",
                ".controller",
                ".anim",
                ".asmdef"
            };

        private static readonly Regex GuidRegex = new Regex("guid: ([0-9a-f]{32})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex AsmGuidRegex = new Regex("GUID:([0-9a-f]{32})");

        private static void ParseFileContent(string content, List<string> references)
        {
            foreach (Match match in GuidRegex.Matches(content)) {
                references.Add(match.Groups[1].Value);
            }
        }

        /// <summary>
        /// Adds all GUIDs referenced by the asset to the list.
        /// The first added element is always its own GUID.
        /// </summary>
        public static void ParseAssetFile(string path, List<string> references)
        {
            var metaFile = path + ".meta";
            if (File.Exists(metaFile)) {
                ParseFileContent(File.ReadAllText(metaFile), references);
            }
            if (YamlFileExtensions.Contains(Path.GetExtension(path) ?? "")) {
                ParseFileContent(File.ReadAllText(path), references);
            }
        }

        public static List<AssetReference> ParseDirectory(string dir = "Assets")
        {
            var tmpReferences = new List<string>();
            var refMap = new Dictionary<string, AssetReference>();
            var allFiles = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < allFiles.Length; i++)
            {
                var file = allFiles[i];
                var ext = Path.GetExtension(file);
                if (ext.Equals(".meta", StringComparison.OrdinalIgnoreCase)) {
                    // Skip .meta files on their own; we'll find them later
                    continue;
                }
                EditorUtility.DisplayProgressBar("Parsing tree", file, (float)i/allFiles.Length);
                tmpReferences.Clear();
                ParseAssetFile(file, tmpReferences);
                if (tmpReferences.Count < 1) { continue; }
                foreach (var guid in tmpReferences) {
                    if (!refMap.ContainsKey(guid)) {
                        refMap.Add(guid, new AssetReference{guid = guid});
                    }
                }
                var dest = refMap[tmpReferences[0]];
                dest.path = file;
                dest.references.Capacity += (tmpReferences.Count - 1);
                dest.size = (float)(new FileInfo(file).Length);
                for (int j = 1; j < tmpReferences.Count; j++) {
                    dest.references.Add(tmpReferences[j]);
                }
            }
            EditorUtility.ClearProgressBar();
            return refMap.Values.ToList();
        }

        public static DependencyFolderNode BuildFolderTree(List<AssetReference> rawTree)
        {
            var sep = new char[]{'\\', '/'};
            var root = new DependencyFolderNode();
            var guidMap = new Dictionary<string, DependencyNode>();

            // Build folder structure
            foreach (var asset in rawTree)
            {
                if (string.IsNullOrEmpty(asset.path)) {
                    // Built-in asset
                    continue;
                }
                var path = asset.path.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                var myRoot = root;
                for (int i = 0; i < (path.Length - 1); i++)
                {
                    var cRoot = myRoot.Children
                        .FirstOrDefault(c => c.Name == path[i]) as DependencyFolderNode;
                    if (cRoot == null) {
                        cRoot = new DependencyFolderNode();
                        cRoot.Name = path[i];
                        cRoot.Parents.Add(myRoot);
                        myRoot.Children.Add(cRoot);
                    }
                    myRoot = cRoot;
                }
                var leafNode = new DependencyNode();
                leafNode.Name = path[path.Length-1];
                leafNode.Guid = asset.guid;
                leafNode.Parents.Add(myRoot);
                leafNode.Size = asset.size;
                guidMap.Add(asset.guid, leafNode);
                myRoot.Children.Add(leafNode);
            }

            // Add dependencies
            foreach (var asset in rawTree)
            {
                if (string.IsNullOrEmpty(asset.path)) {
                    // Built-in asset
                    continue;
                }
                var nodeA = guidMap[asset.guid];
                foreach (var r in asset.references)
                {
                    DependencyNode nodeB;
                    if (guidMap.TryGetValue(r, out nodeB)) {
                        nodeA.Dependencies.Add(nodeB);
                    }
                }
            }
            return root;
        }

        public string RootDir = "Assets";
        public List<AssetReference> RawTree = new List<AssetReference>();
        [NonSerialized] public DependencyFolderNode RootNode;

        public void Refresh()
        {
            RawTree = ParseDirectory(RootDir);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            RootNode = null;
        }

        public DependencyFolderNode GetRootNode()
        {
            if (RootNode == null) {
                RootNode = BuildFolderTree(RawTree);
            }
            return RootNode;
        }

        [MenuItem("Assets/Generate Dependency Tree")]
        public static void Generate()
        {
            var path = "Assets/DependencyTree.asset";
            if (File.Exists(path))
            {
                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<DependencyTree>(path);
            } else {
                var holder = CreateInstance<DependencyTree>();
                Selection.activeObject = holder;
                AssetDatabase.CreateAsset(holder, path);
                holder.Refresh();
            }
        }
    }
}
