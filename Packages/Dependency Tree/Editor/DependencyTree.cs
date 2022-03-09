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

        // GUID format prefix label is slightly different in asmdef files
        private static readonly Regex GuidRegex = new Regex("(?:GUID:|guid: )([0-9a-f]{32})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static List<AssetReference> ParseDirectories(params string[] directories)
        {
            static void ParseFileContent(string content, List<string> references) =>
                references.AddRange(GuidRegex.Matches(content)
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value));

            // Adds all GUIDs referenced by the asset to the list.
            // The first added element is always its own GUID.
            static void ParseAssetFile(string path, List<string> references)
            {
                var metaFile = $"{path}.meta";
                if (File.Exists(metaFile)) {
                    // TODO: Warn about assets missing meta files?
                    ParseFileContent(File.ReadAllText(metaFile), references);
                }
                if (YamlFileExtensions.Contains(Path.GetExtension(path) ?? string.Empty)) {
                    ParseFileContent(File.ReadAllText(path), references);
                }
            }

            var intermediateGuidsList = new List<string>();
            var assetsByGuid = new Dictionary<string, AssetReference>();
            var allFilesPaths = AssetDatabase.FindAssets("", directories)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !Directory.Exists(p)) // Exclude folders for now, Unity treats them as Assets but we don't care about them
                .ToArray();
            for (int i = 0; i < allFilesPaths.Length; i++)
            {
                var filePath = allFilesPaths[i];
                if (filePath.Equals(DependencyTreePath)) continue; // Don't parse the dependency tree itself as it will be cyclic
                var ext = Path.GetExtension(filePath);
                if (ext.Equals(".meta", StringComparison.OrdinalIgnoreCase)) {
                    // Skip .meta files on their own; we'll find them later
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Parsing all files", filePath, (float)i / allFilesPaths.Length))
                {
                    return null;
                }
                intermediateGuidsList.Clear();

                ParseAssetFile(filePath, intermediateGuidsList);
                if (intermediateGuidsList.Count < 1) { continue; }

                // Add every new guid to the map
                foreach (var guid in intermediateGuidsList) {
                    if (!assetsByGuid.ContainsKey(guid)) {
                        assetsByGuid.Add(guid, new AssetReference{guid = guid});
                    }
                }

                // Get this asset to fill in the details
                // It might already be in the map if some other asset references it and was processed first
                var thisAsset = assetsByGuid[intermediateGuidsList[0]];
                thisAsset.path = filePath;
                thisAsset.references.AddRange(intermediateGuidsList.Skip(1).Distinct()); // TODO: Count references instead of throwing away count?
                thisAsset.size = (float)(new FileInfo(filePath).Length);
            }
            EditorUtility.ClearProgressBar();
            return assetsByGuid.Values.ToList();
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
                foreach (var guid in asset.references)
                {
                    DependencyNode nodeB;
                    if (guidMap.TryGetValue(guid, out nodeB)) {
                        nodeA.Dependencies.Add(nodeB);
                    }
                }
            }
            return root;
        }

        public string[] RootDirectories = new []{"Assets", "Packages"};
        public List<AssetReference> RawTree = new List<AssetReference>();
        [NonSerialized] public DependencyFolderNode RootNode;

        public void Refresh()
        {
            RawTree = ParseDirectories(RootDirectories) ?? RawTree;
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

        private static string DependencyTreePath = "Assets/DependencyTree.asset";

        [MenuItem("Assets/Generate Dependency Tree")]
        public static void Generate()
        {
            // TODO: Put the asset somewhere less annoying to users
            if (File.Exists(DependencyTreePath))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<DependencyTree>(DependencyTreePath);
            }
            else
            {
                var holder = CreateInstance<DependencyTree>();
                Selection.activeObject = holder;
                AssetDatabase.CreateAsset(holder, DependencyTreePath);
                holder.Refresh();
            }
        }
    }
}
