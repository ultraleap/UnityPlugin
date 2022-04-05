namespace Leap.Unity
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    [CustomEditor(typeof(DependencyTree))]
    internal class DependencyTreeEditor : Editor
    {
        public enum SortMode
        {
            Directory,
            Name,
            Size
        }

        public SortMode Sort = SortMode.Directory;
        public List<string> Expanded = new List<string>();
        public string Selected;

        private SerializedProperty _ignorePatterns;
        private DependencyFolderNode _root;
        private DependencyFolderNode _specialResourcesNode;

        private const string SpecialResourcesName = "[All Resources]";

        private IEnumerable<DependencyFolderNode> Flatten(DependencyFolderNode node)
        {
            return new []{node}.Concat(node.Children.OfType<DependencyFolderNode>().SelectMany(Flatten));
        }

        private void OnEnable()
        {
            _ignorePatterns = serializedObject.FindProperty(nameof(DependencyTree.GenerationIgnorePatterns));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_ignorePatterns);
            serializedObject.ApplyModifiedProperties();

            var rootNode = ((DependencyTree)target).GetRootNode();
            if (_specialResourcesNode == null || _root != rootNode && rootNode != null)
            {
                _root = rootNode;
                _specialResourcesNode = new DependencyFolderNode();
                _specialResourcesNode.Parents.Add(_root);
                _specialResourcesNode.Name = SpecialResourcesName;
                _specialResourcesNode.Children.AddRange(
                    Flatten(rootNode)
                    .Where(f => f.Name == "Resources")
                    .Select(n => {
                        var newN = new DependencyFolderNode{
                            Name = GetPath(n).Replace("//Assets/", "").Replace("/", " \\ ")
                        };
                        newN.Parents.Add(_specialResourcesNode);
                        newN.Children.AddRange(n.Children);
                        foreach (var c in n.Children) {
                            c.Parents.Add(newN);
                        }
                        return newN;
                        }).Cast<DependencyNodeBase>()
                );
            }

            Sort = (SortMode)EditorGUILayout.EnumPopup("Sort by", Sort);
            GUI.color = ModColor(Color.blue);
            EditorGUILayout.LabelField("Blue items are selected - click the button to select an asset or folder");
            GUI.color = ModColor(Color.red);
            EditorGUILayout.LabelField("Red indicates dependants - these items depends on the selection");
            GUI.color = ModColor(Color.green);
            EditorGUILayout.LabelField("Green indicates dependencies - these are items that the selection depends on");
            GUI.color = ModColor(Color.yellow);
            EditorGUILayout.LabelField("Yellow indicates circular dependencies");
            GUI.color = Color.white;
            DrawElement(_specialResourcesNode, 0);
            foreach (var c in rootNode?.Children ?? Enumerable.Empty<DependencyNodeBase>())
            {
                DrawElement(c, 0);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh (2-3 minutes)",
                GUILayout.MaxWidth(150f), GUILayout.Height(32f)))
            {
                ((DependencyTree)target).Refresh();
            }
            EditorUtility.SetDirty(this);
        }

        private Color GetColor(DependencyNodeBase node)
        {
            var selected = FromPath(Selected);
            if (selected == null) return Color.white; // Default color
            if (node == selected) return Color.blue; // Selected
            var dependsOnSelected = node.DependsOnCached(selected);
            var selectedDependsOn = selected.DependsOnCached(node);
            return (dependsOnSelected, selectedDependsOn) switch
            {
                (true, true) => Color.yellow, // Circular dependency
                (true, false) => Color.red, // Depends on selected
                (false, true) => Color.green,
                _ => Color.white
            };
        }

        private static Color ModColor(Color color)
        {
            const float tintLow = 0.6f;
            return new Color(
                Mathf.Lerp(tintLow, 1f, color.r),
                Mathf.Lerp(tintLow, 1f, color.g),
                Mathf.Lerp(tintLow, 1f, color.b),
                1f
            );
        }

        private string GetPath(DependencyNodeBase node)
        {
            return node != null ? GetPath(node.Parents.FirstOrDefault()) + "/" + node.Name : "";
        }

        private DependencyNodeBase FromPath(string nodePath)
        {
            if (string.IsNullOrEmpty(nodePath)) return null;
            DependencyFolderNode current;
            const string specialResourcesPath = "//" + SpecialResourcesName;
            if (nodePath.StartsWith(specialResourcesPath)) {
                current = _specialResourcesNode;
                nodePath = nodePath.Substring(specialResourcesPath.Length);
            } else {
                current = ((DependencyTree)target).GetRootNode();
            }

            foreach (var t in nodePath.Split(new []{'/'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var child = current.Children.FirstOrDefault(o => o.Name == t);
                current = child as DependencyFolderNode;
                if (current == null) return child;
            }
            return current;
        }

        private void DrawElement(DependencyNodeBase node, int indentLevel)
        {
            var nodeP = GetPath(node);
            DependencyFolderNode drawChildren = null;
            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.Toggle(Selected == nodeP, EditorStyles.radioButton,
                GUILayout.Width(20f)) && Selected != nodeP) {
                Selected = nodeP;
            }
            GUILayout.Space(4 + (12 * indentLevel));
            var nodeText = node.Name + " (" + (node.GetSize()/1000000).ToString("F1") + " MB)";
            if(node is DependencyFolderNode) {
                var wasFoldedOut = Expanded.Contains(nodeP);
                GUI.color = ModColor(GetColor(node));
                var foldedOut = EditorGUILayout.Foldout(wasFoldedOut, nodeText);
                GUI.color = Color.white;
                if (foldedOut && !wasFoldedOut) {
                    Expanded.Add(nodeP);
                } else if(!foldedOut && wasFoldedOut) {
                    Expanded.Remove(nodeP);
                }
                if (foldedOut) {
                    drawChildren = (DependencyFolderNode)node;
                }
            } else {
                GUI.color = ModColor(GetColor(node));
                EditorGUILayout.LabelField(nodeText);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            if (drawChildren != null)
            {
                IEnumerable<DependencyNodeBase> children = drawChildren.Children;
                switch (Sort) {
                    case SortMode.Name:
                        children = children.OrderBy(c => c.Name);
                        break;
                    case SortMode.Size:
                        children = children.OrderByDescending(c => c.GetSize());
                        break;
                }
                foreach (var c in children)
                {
                    DrawElement(c, indentLevel + 1);
                }
            }
        }
    }
}