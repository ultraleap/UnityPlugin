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
        private SerializedProperty _knownBuiltinAssetFilePaths;

        private DependencyFolderNode _specialResourcesNode;
        private DependencyFolderNode _root; // Root is the project root folder, it has other project folders as children

        private const string SpecialResourcesName = "[All Resources]";

        private IEnumerable<DependencyFolderNode> Flatten(DependencyFolderNode node) =>
            new []{node}.Concat(node.Children.OfType<DependencyFolderNode>().SelectMany(Flatten));

        private void OnEnable()
        {
            _ignorePatterns = serializedObject.FindProperty(nameof(DependencyTree.GenerationIgnorePatterns));
            _knownBuiltinAssetFilePaths = serializedObject.FindProperty(nameof(DependencyTree.KnownBuiltinAssetFilePaths));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_ignorePatterns);
            EditorGUILayout.PropertyField(_knownBuiltinAssetFilePaths);
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
                        })
                );
            }

            Sort = (SortMode)EditorGUILayout.EnumPopup("Sort by", Sort);
            // GUI.color = ModColor(Color.blue);
            // EditorGUILayout.LabelField("Blue items are selected - click the button to select an asset or folder");
            // GUI.color = ModColor(Color.red);
            // EditorGUILayout.LabelField("Red indicates dependants - these items depends on the selection");
            // GUI.color = ModColor(Color.green);
            // EditorGUILayout.LabelField("Green indicates dependencies - these are items that the selection depends on");
            // GUI.color = ModColor(Color.yellow);
            // EditorGUILayout.LabelField("Yellow indicates circular dependencies");
            // GUI.color = Color.white;

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh (2-3 minutes)", GUILayout.MaxWidth(300f), GUILayout.Height(32f)))
            {
                ((DependencyTree)target).Refresh();
            }

            if (GUILayout.Button("Open Dependency Browser", GUILayout.MaxWidth(300f), GUILayout.Height(32f)))
            {
                DependencyBrowser.Open(this);
            }
            EditorUtility.SetDirty(this);
        }

        private static Color GetColor(DependencyNodeBase node, DependencyNodeBase selected)
        {
            if (selected == null) return Color.white; // Default color
            if (node == selected) return Color.blue; // Selected
            return GetStatus(node, selected) switch
            {
                (true, true) => Color.yellow, // Circular dependency
                (true, false) => Color.red, // Depends on selected
                (false, true) => Color.green,
                _ => Color.white
            };
        }

        private static (bool isDependency, bool isDependant) GetStatus(DependencyNodeBase node, DependencyNodeBase selected) =>
            (selected.DependsOnCached(node), node.DependsOnCached(selected));

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

        private static string GetPath(DependencyNodeBase node) =>
            node != null ? GetPath(node.Parents.FirstOrDefault()) + "/" + node.Name : "";

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

        private static Predicate<(DependencyNodeBase node, DependencyNodeBase selected)> GetPredicate(bool filterToDependencies, bool filterToDependants) =>
            ((DependencyNodeBase node, DependencyNodeBase selected) t) => GetStatus(t.node, t.selected) switch
            {
                (true, true) => filterToDependencies || filterToDependants,
                (true, false) => filterToDependencies,
                (false, true) => filterToDependants,
                _ => false
            };

        public void DrawSelectionTree(bool drawSelectionToggle, bool filterToDependencies, bool filterToDependants)
        {
            var predicate = filterToDependencies || filterToDependants // Don't filter if neither of these are true
                ? GetPredicate(filterToDependencies, filterToDependants)
                : null;
            DrawNode(_specialResourcesNode, 0, drawSelectionToggle, predicate);
            foreach (var child in _root?.Children ?? Enumerable.Empty<DependencyNodeBase>())
            {
                DrawNode(child, 0, drawSelectionToggle, predicate);
            }
        }

        private void DrawNode(DependencyNodeBase node, int indentLevel, bool drawSelectionToggle = false, Predicate<(DependencyNodeBase node, DependencyNodeBase selected)> filter = null)
        {
            var nodeP = GetPath(node);
            DependencyFolderNode drawChildren = null;

            EditorGUILayout.BeginHorizontal();
            if (drawSelectionToggle && EditorGUILayout.Toggle(Selected == nodeP, EditorStyles.radioButton,
                GUILayout.Width(20f)) && Selected != nodeP) {
                Selected = nodeP;
            }

            var selectedNode = FromPath(Selected);

            var filtered = filter?.Invoke((node, selectedNode)) ?? false;
            if (filtered)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            GUILayout.Space(4 + (12 * indentLevel));
            var nodeText = node.Name + " (" + (node.GetSize()/1000000).ToString("F1") + " MB)";

            if (node is DependencyFolderNode folderNode) {
                var wasFoldedOut = Expanded.Contains(nodeP);
                GUI.color = ModColor(GetColor(folderNode, selectedNode));
                var foldedOut = EditorGUILayout.Foldout(wasFoldedOut, nodeText);
                GUI.color = Color.white;
                if (foldedOut && !wasFoldedOut) {
                    Expanded.Add(nodeP);
                } else if(!foldedOut && wasFoldedOut) {
                    Expanded.Remove(nodeP);
                }
                if (foldedOut) {
                    drawChildren = folderNode;
                }
            } else {
                GUI.color = ModColor(GetColor(node, selectedNode));
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
                    DrawNode(c, indentLevel + 1, drawSelectionToggle);
                }
            }
        }
    }

    internal class DependencyBrowser : EditorWindow
    {
        private static DependencyBrowser window;
        private DependencyTreeEditor editor;

        private float resizerSpacing;
        private bool resizingHorizontally;
        private float currentHorizontalDistance;
        private Rect horizontalResizeInteractionRect;
        private bool resizingVertically;
        private float currentVerticalDistance;
        private Rect verticalResizeInteractionRect;

        private Texture2D resizerTexture;
        private Vector2 windowSize;
        private Vector2 selectionScrollViewPosition = Vector2.zero;
        private Vector2 dependenciesScrollViewPosition = Vector2.zero;
        private Vector2 dependantsScrollViewPosition = Vector2.zero;
        private bool isViewingCyclicDependencies;
        private event Action windowResized;
        private event Action resizeOccurred;

        public static void Open(DependencyTreeEditor dependencyTreeEditor)
        {
            if (window == null)
            {
                window = (DependencyBrowser)EditorWindow.GetWindow(typeof(DependencyBrowser),
                    true, "Dependency Browser", true);
            }
            window.editor = dependencyTreeEditor;
            window.Show();
        }

        private void OnEnable()
        {
            this.position = new Rect(200, 200, 800, 600);
            resizerSpacing = 4f;
            windowSize = new Vector2(800, 600);
            resizerTexture = new Texture2D(1, 1);
            resizerTexture.SetPixel(0, 0, Color.black);
            resizerTexture.Apply();
            currentHorizontalDistance = windowSize.x / 2f;
            currentVerticalDistance = windowSize.y / 2f;
            horizontalResizeInteractionRect = new Rect(currentHorizontalDistance, 0f, 8f, windowSize.y);
            verticalResizeInteractionRect = new Rect(currentHorizontalDistance + resizerSpacing, currentVerticalDistance, windowSize.x - currentHorizontalDistance - resizerSpacing, 8f);

            void UpdateResizeInteractionRects()
            {
                horizontalResizeInteractionRect.Set(currentHorizontalDistance, 0f, 8f, windowSize.y);
                verticalResizeInteractionRect.Set(currentHorizontalDistance + resizerSpacing, currentVerticalDistance, windowSize.x - currentHorizontalDistance - resizerSpacing, 8f);
            }

            resizeOccurred += UpdateResizeInteractionRects;
        }

        void OnGUI()
        {
            CheckForWindowResize();
            GUILayout.BeginHorizontal();
            DrawLeftColumn();
            DrawResizer(ref resizingHorizontally, ref currentHorizontalDistance, ref horizontalResizeInteractionRect, resizeOccurred, resizerSpacing,false, resizerTexture);
            DrawRightColumn();
            GUILayout.EndHorizontal();
            Repaint();
        }

        void DrawLeftColumn()
        {
            selectionScrollViewPosition = GUILayout.BeginScrollView(selectionScrollViewPosition, GUILayout.Height(this.position.height), GUILayout.Width(currentHorizontalDistance));
            GUILayout.Label("Select an asset or folder");
            editor.DrawSelectionTree(true, false, false);
            GUILayout.EndScrollView();
        }

        void DrawRightColumn()
        {
            GUILayout.BeginVertical();
            dependenciesScrollViewPosition = GUILayout.BeginScrollView(dependenciesScrollViewPosition, GUILayout.Height(currentVerticalDistance), GUILayout.Width(windowSize.x - currentHorizontalDistance - resizerSpacing));
            GUILayout.Label("Dependencies View");
            editor.DrawSelectionTree(false, true, false);
            GUILayout.EndScrollView();

            DrawResizer(ref resizingVertically, ref currentVerticalDistance, ref verticalResizeInteractionRect, resizeOccurred, resizerSpacing, true, resizerTexture);

            dependantsScrollViewPosition = GUILayout.BeginScrollView(dependantsScrollViewPosition, GUILayout.Height(this.position.height - currentVerticalDistance - resizerSpacing), GUILayout.Width(windowSize.x - currentHorizontalDistance - resizerSpacing));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Dependants View");
            GUILayout.FlexibleSpace();
            isViewingCyclicDependencies = GUILayout.Toggle(isViewingCyclicDependencies, new GUIContent("Filter to Cyclic Dependencies", "Show dependants that are also dependencies, these assets are tightly coupled to the selection."));
            GUILayout.EndHorizontal();

            if (isViewingCyclicDependencies)
            {
                editor.DrawSelectionTree(false, true, true);
            }
            else
            {
                editor.DrawSelectionTree(false, false, true);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void CheckForWindowResize()
        {
            var size = new Vector2(this.position.width, this.position.height);
            if (size == windowSize) return;
            windowSize = size;
            resizeOccurred?.Invoke();
        }

        private static void DrawResizer(ref bool resizing, ref float currentDistance, ref Rect cursorInteractionRect, Action resize, float resizerSpacing, bool isVerticalResizer, Texture2D resizerTexture)
        {
            var textureRect = new Rect(cursorInteractionRect);

            if (isVerticalResizer)
            {
                textureRect.height = cursorInteractionRect.height / 2f;
                textureRect.y += textureRect.height / 2f;
            }
            else
            {
                textureRect.width = cursorInteractionRect.width / 2f;
                textureRect.x += textureRect.width / 2f;
            }

            GUI.DrawTexture(textureRect, resizerTexture);
            EditorGUIUtility.AddCursorRect(cursorInteractionRect, isVerticalResizer ? MouseCursor.ResizeVertical : MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && cursorInteractionRect.Contains(Event.current.mousePosition))
                resizing = true;
            if (resizing)
            {
                if (isVerticalResizer)
                {
                    currentDistance = Event.current.mousePosition.y;
                    cursorInteractionRect.Set(cursorInteractionRect.x, currentDistance, cursorInteractionRect.width, cursorInteractionRect.height);
                }
                else
                {
                    currentDistance = Event.current.mousePosition.x;
                    cursorInteractionRect.Set(currentDistance, cursorInteractionRect.y, cursorInteractionRect.width, cursorInteractionRect.height);
                }
                resize?.Invoke();
            }
            if (Event.current.type == EventType.MouseUp)
                resizing = false;

            GUILayout.Space(resizerSpacing);
        }
    }
}