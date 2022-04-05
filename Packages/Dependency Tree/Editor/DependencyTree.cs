using System.Collections;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

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
            public string packageName;
            public string asmdefName;
        }

        [Serializable]
        public class IgnorePatterns
        {
            public string[] AssetPathRegex;
            public string[] PackageNameRegex;
            public string FindAssetsFilter;
        }

        [Serializable]
        public class StaticAnalysisResults
        {
            public List<string> assetsWithoutGuids = new List<string>();
            public List<string> duplicatedGuids = new List<string>();
            public List<string> missingGuidReferences = new List<string>();
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
            RegexOptions.CultureInvariant);

        private static (bool Success, List<AssetReference> AssetReferences, StaticAnalysisResults AnalysisResults) GenerateTree(PackageCollection packages, IgnorePatterns ignorePatterns)
        {
            bool CheckPatternsDontMatch(string p, IEnumerable<string> patterns) => patterns.All(pattern => !Regex.IsMatch(p, pattern, RegexOptions.CultureInvariant));
            bool PassesAssetPathIgnorePatterns(string p) => CheckPatternsDontMatch(p, ignorePatterns.AssetPathRegex);
            bool PassesPkgNameIgnorePatterns(UnityEditor.PackageManager.PackageInfo pkg) => CheckPatternsDontMatch(pkg.name, ignorePatterns.PackageNameRegex);

            var intermediateGuidsList = new List<string>();
            var filesMissingMetas = new List<string>();
            var assetsByGuid = new Dictionary<string, AssetReference>();
            var analysis = new StaticAnalysisResults();

            var filteredPackages = packages.Where(PassesPkgNameIgnorePatterns).ToArray();

            static void UpdateAssetReferenceFromFilePath(AssetReference assetReference, string filePath)
            {
                assetReference.path = filePath;
                try
                {
                    assetReference.size = (float)(new FileInfo(filePath).Length);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                assetReference.packageName = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(filePath)?.name;
                assetReference.asmdefName = CompilationPipeline.GetAssemblyNameFromScriptPath(filePath);
            }

            void ParseDirectory(string directoryPath)
            {
                // Adds all GUIDs referenced by the asset to the list.
                // The first added element is always its own GUID.
                // Returns whether or not the GUID of this asset was found and is first in the references list
                static bool ParseAssetFile(string path, List<string> references, ICollection<string> missingMetaFiles)
                {
                    // Add GUIDs in a file to the references list
                    static void ParseFileContent(string content, List<string> references) =>
                        references.AddRange(GuidRegex.Matches(content)
                            .Cast<Match>()
                            .Select(match => match.Groups[1].Value));

                    var metaFile = $"{path}.meta";
                    var guidFound = true;
                    if (File.Exists(metaFile)) {
                        ParseFileContent(File.ReadAllText(metaFile), references);
                    }
                    else
                    {
                        missingMetaFiles.Add(path);
                        guidFound = false;
                        // We don't return here on this error case, if it's a Yaml file there is still useful data within
                    }
                    if (YamlFileExtensions.Contains(Path.GetExtension(path) ?? string.Empty)) {
                        ParseFileContent(File.ReadAllText(path), references);
                    }

                    return guidFound;
                }

                var directoryAssets = AssetDatabase.FindAssets(ignorePatterns.FindAssetsFilter, new []{directoryPath})
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => !Directory.Exists(p)) // Exclude folders for now, Unity treats them as Assets but we don't care about them
                    .Where(PassesAssetPathIgnorePatterns)
                    .ToArray();

                foreach (var filePath in directoryAssets)
                {
                    if (filePath.Equals(DependencyTreePath)) continue; // Don't parse the dependency tree itself as it will be cyclic
                    var ext = Path.GetExtension(filePath);
                    if (ext.Equals(".meta", StringComparison.OrdinalIgnoreCase)) {
                        // Skip .meta files on their own; we'll find them later
                        continue;
                    }

                    intermediateGuidsList.Clear();

                    string thisAssetsGuid = ParseAssetFile(filePath, intermediateGuidsList, filesMissingMetas)
                        ? intermediateGuidsList[0]
                        : null;

                    // Add every new guid to the map
                    foreach (var guid in intermediateGuidsList) {
                        if (!assetsByGuid.ContainsKey(guid)) {
                            assetsByGuid.Add(guid, new AssetReference{guid = guid, packageName = "<unknown>"});
                        }
                    }

                    // Get this asset to fill in the details
                    // It might already be in the map if some other asset references it and was processed first
                    AssetReference thisAsset;
                    if (thisAssetsGuid == null)
                    {
                        // If there's no discovered guid it won't be in the map at all, so add it to the list of assets without guids
                        thisAsset = new AssetReference { guid = string.Empty };
                        analysis.assetsWithoutGuids.Add(filePath);
                    }
                    else
                    {
                        thisAsset = assetsByGuid[thisAssetsGuid];
                    }
                    if (!string.IsNullOrEmpty(thisAsset.path) && !thisAsset.path.Equals(filePath)) analysis.duplicatedGuids.Add(thisAsset.guid);
                    thisAsset.references.AddRange(intermediateGuidsList.Skip(1).Distinct()); // TODO: Count references instead of throwing away count?
                    UpdateAssetReferenceFromFilePath(thisAsset, filePath);
                }
            }

            var directoryCount = filteredPackages.Length + 1;

            if (EditorUtility.DisplayCancelableProgressBar("Parsing Assets directory", $"(1/{directoryCount}): Assets", (float)0 / directoryCount))
            {
                EditorUtility.ClearProgressBar();
                return default;
            }

            ParseDirectory("Assets");

            for (var i = 0; i < filteredPackages.Length; i++)
            {
                var directoryIdx = i + 2; // +1 for indexing from 0, +1 for including assets directory
                var package = filteredPackages[i];
                if (EditorUtility.DisplayCancelableProgressBar("Parsing all packages", $"({directoryIdx}/{directoryCount}): {package.name}", (float)i+1.0f / directoryCount))
                {
                    EditorUtility.ClearProgressBar();
                    return default;
                }

                ParseDirectory(package.assetPath);
            }

            EditorUtility.ClearProgressBar();

            // Fill in details for assets that were not in the directories we scanned
            foreach (var tuple in assetsByGuid)
            {
                var (guid, assetRef) = (tuple.Key, tuple.Value);
                if (!string.IsNullOrEmpty(assetRef.path)) continue;
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) analysis.missingGuidReferences.Add(guid);
                else UpdateAssetReferenceFromFilePath(assetRef, assetPath);
            }

            return (true, assetsByGuid.Values.ToList(), analysis);
        }

        public static DependencyFolderNode BuildFolderTree(List<AssetReference> rawTree)
        {
            var sep = new char[]{'\\', '/'};
            var root = new DependencyFolderNode();
            var guidMap = new Dictionary<string, DependencyNode>();

            if (rawTree == null) return null;
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

        public IgnorePatterns GenerationIgnorePatterns;
        public List<AssetReference> RawTree = new List<AssetReference>();
        public StaticAnalysisResults AnalysisResults;
        [NonSerialized] public DependencyFolderNode RootNode;

        public void Refresh()
        {
            // TODO: Only rebuild what's changed instead of everything
            var listRequest = UnityEditor.PackageManager.Client.List();
            EditorApplication.update += OnComplete;

            void OnComplete()
            {
                if (!listRequest.IsCompleted) return;
                EditorApplication.update -= OnComplete;

                if (listRequest.Status == StatusCode.Failure)
                {
                    Debug.Log(listRequest.Error.message);
                    return;
                }

                var packages = listRequest.Result;
                (_, RawTree, AnalysisResults) = GenerateTree(packages, GenerationIgnorePatterns);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                RootNode = null;
            }
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
