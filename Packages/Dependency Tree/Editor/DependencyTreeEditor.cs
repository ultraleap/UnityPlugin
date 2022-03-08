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
        private DependencyFolderNode _root;
        private DependencyFolderNode _specialResourcesNode;
        private const string SpecialResourcesName = "[All Resources]";

        private IEnumerable<DependencyFolderNode> Flatten(DependencyFolderNode node)
        {
            return new []{node}.Concat(node.Children.OfType<DependencyFolderNode>().SelectMany(Flatten));
        }

        public override void OnInspectorGUI()
        {
            var rootNode = ((DependencyTree)target).GetRootNode();
            if (_specialResourcesNode == null || _root != rootNode)
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
            EditorGUILayout.LabelField("Click the button to select an asset or folder");
            GUI.color = ModColor(Color.red);
            EditorGUILayout.LabelField("Red indicates that this item depends on the selection");
            GUI.color = ModColor(Color.green);
            EditorGUILayout.LabelField("Green indicates an asset that the selection depends on");
            GUI.color = ModColor(Color.yellow);
            EditorGUILayout.LabelField("Yellow indicates that the selection and current item depend on each other");
            GUI.color = Color.white;
            DrawElement(_specialResourcesNode, 0);
            foreach (var c in rootNode.Children)
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
            if (selected == null) return Color.white;
            if (node == selected) return Color.blue;
            var nDs = node.DependsOnCached(selected);
            var sDn = selected.DependsOnCached(node);
            if (nDs && sDn) return Color.yellow;
            if (nDs) return Color.red;
            if (sDn) return Color.green;
            return Color.white;
        }

        private Color ModColor(Color color)
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

        private DependencyNodeBase FromPath(string nodeP)
        {
            if (string.IsNullOrEmpty(nodeP)) return null;
            DependencyFolderNode current;
            const string specialResourcesPath = "//" + SpecialResourcesName;
            if (nodeP.StartsWith(specialResourcesPath)) {
                current = _specialResourcesNode;
                nodeP = nodeP.Substring(specialResourcesPath.Length);
            } else {
                current = ((DependencyTree)target).GetRootNode();
            }
            foreach (var t in nodeP.Split(new []{'/'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var c = current.Children.FirstOrDefault(o => o.Name == t);
                current = c as DependencyFolderNode;
                if (current == null) return c;
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