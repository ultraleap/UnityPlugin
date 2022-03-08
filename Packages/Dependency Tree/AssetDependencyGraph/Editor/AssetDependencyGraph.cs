using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
#endif
using UnityEngine;

public class AssetGraphView : GraphView {
    public AssetGraphView() {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new FreehandSelector());

        VisualElement background = new VisualElement {
            style =
            {
                backgroundColor = new Color(0.17f, 0.17f, 0.17f, 1f)
            }
        };
        Insert(0, background);

        background.StretchToParentSize();
    }
}

public class AssetGroup {
    //public AssetGroup SharedGroup;
    public Group groupNode = new Group();
    public Node mainNode = new Node();
    public Rect MainNodeLastPosition = new Rect();
    public string assetPath = "";
    public List<GraphElement> m_AssetNodes = new List<GraphElement>();
    public List<GraphElement> m_AssetConnections = new List<GraphElement>();
    //public Dictionary<string, Node> m_GUIDNodeLookup = new Dictionary<string, Node>();
    public List<Node> m_DependenciesForPlacement = new List<Node>();
}

public class AssetDependencyGraph : EditorWindow {
    private const float kNodeWidth = 250.0f;

    Toggle codeToggle;
    Toggle textureToggle;
    Toggle shaderToggle;
    Toggle audioClipToggle;
    Toggle animationClipToggle;
    Toggle MaterialToggle;
    Toggle CustomToggle;  //for scriptable objects, change to list/searchfield containing custom filters or something
    Toggle AlignmentToggle;

    //Toggle SharedToggle;    //show shared assets in separate group

    private GraphView m_GraphView;

    //AssetGroup NewAssetGroup; //group for shared assets

    private readonly List<Object> SelectedObjects = new List<Object>();
    private readonly List<AssetGroup> AssetGroups = new List<AssetGroup>();

    //private readonly List<GraphElement> m_AssetElements = new List<GraphElement>();
    private readonly Dictionary<string, Node> m_GUIDNodeLookup = new Dictionary<string, Node>();
    //private readonly List<Node> m_DependenciesForPlacement = new List<Node>();


#if !UNITY_2019_1_OR_NEWER
    private VisualElement rootVisualElement;
#endif

    [MenuItem("Window/Analysis/Asset Dependency Graph")]
    public static void CreateTestGraphViewWindow() {
        var window = GetWindow<AssetDependencyGraph>();
        window.titleContent = new GUIContent("Asset Dependency Graph");
    }

    public void OnEnable() {
        CreateGraph();
    }

    public void OnDisable() {
        rootVisualElement.Remove(m_GraphView);
    }

    void CreateGraph() {
        m_GraphView = new AssetGraphView {
            name = "Asset Dependency Graph",
        };

        VisualElement toolbar = CreateToolbar();
        VisualElement toolbar2 = CreateFilterbar();

#if !UNITY_2019_1_OR_NEWER
        rootVisualElement = this.GetRootVisualContainer();
#endif
        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(toolbar2);
        rootVisualElement.Add(m_GraphView);
        m_GraphView.StretchToParentSize();
        toolbar.BringToFront();
        toolbar2.BringToFront();
    }

    VisualElement CreateToolbar() { 
    
        var toolbar = new VisualElement {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 0,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f)
            }
        };

        var options = new VisualElement {
            style = { alignContent = Align.Center }
        };

        toolbar.Add(options);
        toolbar.Add(new Button(ExploreSelectedRecursive) {
            text = "Explore Assets (Recursive)",
        });
        toolbar.Add(new Button(ExploreAsset) {
            text = "Explore Asset",
        });
        toolbar.Add(new Button(ClearGraph) {
            text = "Clear"
        });
        toolbar.Add(new Button(ResetGroups) {
            text = "Reset Groups"
        });
        toolbar.Add(new Button(ResetAllNodes) {
            text = "Reset Nodes"
        });

        var ts = new ToolbarSearchField();
        ts.RegisterValueChangedCallback(x => {
            if (string.IsNullOrEmpty(x.newValue)) {
                m_GraphView.FrameAll();
                return;
            }

            m_GraphView.ClearSelection();
            // m_GraphView.graphElements.ForEach(y => { // BROKEN, Case 1268337
            m_GraphView.graphElements.ToList().ForEach(y => {
                if (y is Node node && y.title.IndexOf(x.newValue, System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    m_GraphView.AddToSelection(node);
                }
            });

            m_GraphView.FrameSelection();
        });
        toolbar.Add(ts);

        AlignmentToggle = new Toggle();
        AlignmentToggle.text = "Horizontal Layout";
        AlignmentToggle.value = false;
        AlignmentToggle.RegisterValueChangedCallback(x => {
            ResetAllNodes();
        });
        toolbar.Add(AlignmentToggle);

        //SharedToggle = new Toggle();
        //SharedToggle.text = "Show shared groups (WIP)";
        //SharedToggle.value = false;
        //SharedToggle.RegisterValueChangedCallback(x => {
        //    ResetAllNodes();
        //});
        //toolbar.Add(SharedToggle);

        return toolbar;
    }

    VisualElement CreateFilterbar() {

        var toolbar = new VisualElement {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 0,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f)
            }
        };

        var options = new VisualElement {
            style = { alignContent = Align.Center }
        };

        toolbar.Add(options);

        toolbar.Add(new Label("Filters: "));

        codeToggle = new Toggle();
        codeToggle.text = "Hide Scripts";
        codeToggle.value = true;
        codeToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(codeToggle);

        MaterialToggle = new Toggle();
        MaterialToggle.text = "Hide Materials";
        MaterialToggle.value = false;
        MaterialToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(MaterialToggle);

        textureToggle = new Toggle();
        textureToggle.text = "Hide Textures";
        textureToggle.value = true;
        textureToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(textureToggle);

        shaderToggle = new Toggle();
        shaderToggle.text = "Hide Shaders";
        shaderToggle.value = true;
        shaderToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(shaderToggle);

        audioClipToggle = new Toggle();
        audioClipToggle.text = "Hide Audioclips";
        audioClipToggle.value = false;
        audioClipToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(audioClipToggle);

        animationClipToggle = new Toggle();
        animationClipToggle.text = "Hide Animationclips";
        animationClipToggle.value = false;
        animationClipToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(animationClipToggle);

        CustomToggle = new Toggle();
        CustomToggle.text = "Hide Custom";
        CustomToggle.value = true;
        CustomToggle.RegisterValueChangedCallback(x => {
            FilterAssetGroups();
        });
        toolbar.Add(CustomToggle);

        return toolbar;
    }

    private void ExploreSelectedRecursive()
    {
        void RecursivelyAdd(string path)
        {
            // assetPath will be empty if obj is null or isn't an asset (a scene object)
            if (path == null || string.IsNullOrEmpty(path)) return;
            
            var isDirectory = Directory.Exists(path);
            if (isDirectory)
            {
                foreach (var assetPath in Directory.GetFileSystemEntries(path))
                {
                    RecursivelyAdd(assetPath);
                }

                return;
            }

            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null) return;
            if (SelectedObjects.Contains(obj)) return;
            SelectedObjects.Add(obj);
            var assetGroup = new AssetGroup
            {
                assetPath = path,
                groupNode = new Group { title = obj.name }
            };
            AssetGroups.Add(assetGroup);
            
            PopulateGroup(assetGroup, new Rect(0, 0, 0, 0));
            ResetNodes(assetGroup);
        }
        
        foreach (var obj in Selection.objects)
        {
            RecursivelyAdd(AssetDatabase.GetAssetPath(obj));
        }
    }

    private void ExploreAsset() {
        //ClearGraph();

        //Object obj = Selection.activeObject;
        Object[] objs = Selection.objects;

        foreach (var obj in objs) {
            //Prevent readding same object
            if (SelectedObjects.Contains(obj)) {
                Debug.Log("Object already loaded");
                return;
            }
            SelectedObjects.Add(obj);

            AssetGroup AssetGroup = new AssetGroup();
            AssetGroups.Add(AssetGroup);
            AssetGroup.assetPath = AssetDatabase.GetAssetPath(obj);

            // assetPath will be empty if obj is null or isn't an asset (a scene object)
            if (obj == null || string.IsNullOrEmpty(AssetGroup.assetPath))
                return;

            //Group groupNode = new Group { title = obj.name };
            //AssetGroup.groupName = obj.name;
            AssetGroup.groupNode = new Group { title = obj.name };
            
            PopulateGroup(AssetGroup, new Rect(0, 0, 0, 0));
        }
    }

    void PopulateGroup(AssetGroup AssetGroup, Rect position) {
        Object mainObject = AssetDatabase.LoadMainAssetAtPath(AssetGroup.assetPath);

        if (mainObject == null) {
            Debug.Log("Object doesn't exist anymore");
            return;
        }

        string[] dependencies = AssetDatabase.GetDependencies(AssetGroup.assetPath, false);

        AssetGroup.mainNode = CreateNode(AssetGroup, mainObject, AssetGroup.assetPath, true, dependencies.Length);
        AssetGroup.mainNode.userData = 0;

        AssetGroup.mainNode.SetPosition(position);

        if (!m_GraphView.Contains(AssetGroup.groupNode)) {
            m_GraphView.AddElement(AssetGroup.groupNode);
        }

        m_GraphView.AddElement(AssetGroup.mainNode);

        AssetGroup.groupNode.AddElement(AssetGroup.mainNode);

        CreateDependencyNodes(AssetGroup, dependencies, AssetGroup.mainNode, AssetGroup.groupNode, 1);

        AssetGroup.m_AssetNodes.Add(AssetGroup.mainNode);
        //AssetGroup.m_AssetNodes.Add(AssetGroup.groupNode);
        AssetGroup.groupNode.capabilities &= ~Capabilities.Deletable;

        AssetGroup.groupNode.Focus();

        AssetGroup.mainNode.RegisterCallback<GeometryChangedEvent, AssetGroup>(
            UpdateGroupDependencyNodePlacement, AssetGroup
        );
    }

    //Recreate the groups but use the already created groups instead of new ones
    void FilterAssetGroups() {

        //first collect the main node's position and then clear the graph
        foreach (var AssetGroup in AssetGroups) {
            AssetGroup.MainNodeLastPosition = AssetGroup.mainNode.GetPosition();
        }

        m_GUIDNodeLookup.Clear();

        foreach (var AssetGroup in AssetGroups) {
            //clear the nodes and dependencies after getting the position of the main node 
            CleanGroup(AssetGroup);

            PopulateGroup(AssetGroup, AssetGroup.MainNodeLastPosition);
        }
    }

    void CleanGroup(AssetGroup assetGroup) {
        if (assetGroup.m_AssetConnections.Count > 0) {
            foreach (var edge in assetGroup.m_AssetConnections) {
                m_GraphView.RemoveElement(edge);
            }
        }
        assetGroup.m_AssetConnections.Clear();

        foreach (var node in assetGroup.m_AssetNodes) {
            m_GraphView.RemoveElement(node);
        }
        assetGroup.m_AssetNodes.Clear();

        assetGroup.m_DependenciesForPlacement.Clear();


        //if (assetGroup.SharedGroup != null) {
        //    CleanGroup(assetGroup.SharedGroup);
        //}
    }

    private void CreateDependencyNodes(AssetGroup assetGroup, string[] dependencies, Node parentNode, Group groupNode, int depth) {
        //Debug.Log(depth);

        foreach (string dependencyString in dependencies) {
            Object dependencyAsset = AssetDatabase.LoadMainAssetAtPath(dependencyString);
            string[] deeperDependencies = AssetDatabase.GetDependencies(dependencyString, false);

            var typeName = dependencyAsset.GetType().Name;

            //filter out selected asset types
            if (FilterType(typeName)) {
                continue;
            }
            
            Node dependencyNode = CreateNode(assetGroup, dependencyAsset, AssetDatabase.GetAssetPath(dependencyAsset),
                false, deeperDependencies.Length);

            if (!assetGroup.m_AssetNodes.Contains(dependencyNode)) {
                dependencyNode.userData = depth;
            }            

            CreateDependencyNodes(assetGroup, deeperDependencies, dependencyNode, groupNode, depth + 1);

            //if the node doesnt exists yet, put it in the group
            if (!m_GraphView.Contains(dependencyNode)) {
                m_GraphView.AddElement(dependencyNode);

                assetGroup.m_DependenciesForPlacement.Add(dependencyNode);
                groupNode.AddElement(dependencyNode);
            }
            else {
                //TODO: if it already exists, put it in a separate group for shared assets
                //Check if the dependencyNode is in the same group or not
                //if it's a different group move it to a new shared group
                /*
                if (SharedToggle.value) {
                    if (!assetGroup.m_AssetNodes.Contains(dependencyNode)) {
                        if (assetGroup.SharedGroup == null) {
                            assetGroup.SharedGroup = new AssetGroup();

                            AssetGroups.Add(assetGroup.SharedGroup);
                            assetGroup.SharedGroup.assetPath = assetGroup.assetPath;

                            assetGroup.SharedGroup.groupNode = new Group { title = "Shared Group" };

                            assetGroup.SharedGroup.mainNode = dependencyNode;
                            assetGroup.SharedGroup.mainNode.userData = 0;
                        }

                        if (!m_GraphView.Contains(assetGroup.SharedGroup.groupNode)) {
                            m_GraphView.AddElement(assetGroup.SharedGroup.groupNode);
                        }

                        //add the node to the group and remove it from the previous group
                        assetGroup.m_AssetNodes.Remove(dependencyNode);
                        //assetGroup.groupNode.RemoveElement(dependencyNode);
                        assetGroup.m_DependenciesForPlacement.Remove(dependencyNode);

                        assetGroup.SharedGroup.m_DependenciesForPlacement.Add(dependencyNode);

                        if (!assetGroup.SharedGroup.groupNode.ContainsElement(dependencyNode)) {
                            assetGroup.SharedGroup.groupNode.AddElement(dependencyNode);
                        }

                        assetGroup.SharedGroup.m_AssetNodes.Add(dependencyNode);
                    }
                }*/
            }

            Edge edge = CreateEdge(dependencyNode, parentNode);

            assetGroup.m_AssetConnections.Add(edge);
            assetGroup.m_AssetNodes.Add(dependencyNode);
        }
    }

    Edge CreateEdge(Node dependencyNode, Node parentNode) {
        Edge edge = new Edge {
            input = dependencyNode.inputContainer[0] as Port,
            output = parentNode.outputContainer[0] as Port,
        };
        edge.input?.Connect(edge);
        edge.output?.Connect(edge);

        dependencyNode.RefreshPorts();

        m_GraphView.AddElement(edge);

        edge.capabilities &= ~Capabilities.Deletable;

        return edge;
    }

    private Node CreateNode(AssetGroup assetGroup, Object obj, string assetPath, bool isMainNode, int dependencyAmount) {
        Node resultNode;
        string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
        if (m_GUIDNodeLookup.TryGetValue(assetGUID, out resultNode)) {

            //----not sure what this is, the more dependencies the further removed on the chart?
            //int currentDepth = (int)resultNode.userData;
            //resultNode.userData = currentDepth + 1;
            return resultNode;
        }

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var assetGuid, out long _)) {
            var objNode = new Node {
                title = obj.name,
                style =
                {
                    width = kNodeWidth
                }
            };

            objNode.extensionContainer.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);

            #region Select button
            objNode.titleContainer.Add(new Button(() => {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }) {
                style =
            {
                        height = 16.0f,
                        alignSelf = Align.Center,
                        alignItems = Align.Center
                    },
                text = "Select"
            });
            #endregion

            #region Padding
            var infoContainer = new VisualElement {
                style =
                    {
                    paddingBottom = 4.0f,
                    paddingTop = 4.0f,
                    paddingLeft = 4.0f,
                    paddingRight = 4.0f
                }
            };
            #endregion

            #region Asset Path, removed to improve visibility with large amount of assets
            //            infoContainer.Add(new Label {
            //                text = assetPath,
            //#if UNITY_2019_1_OR_NEWER
            //                style = { whiteSpace = WhiteSpace.Normal }
            //#else
            //                style = { wordWrap = true }
            //#endif
            //            });
            #endregion

            #region Asset type
            var typeName = obj.GetType().Name;
            if (isMainNode) {
                var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                if (prefabType != PrefabAssetType.NotAPrefab)
                    typeName = $"{prefabType} Prefab";
            }

            var typeLabel = new Label {
                text = $"Type: {typeName}",
            };
            infoContainer.Add(typeLabel);

            objNode.extensionContainer.Add(infoContainer);
            #endregion

            var typeContainer = new VisualElement {
                style =
                    {
                    paddingBottom = 4.0f,
                    paddingTop = 4.0f,
                    paddingLeft = 4.0f,
                    paddingRight = 4.0f,
                    backgroundColor = GetColorByAssetType(obj)
        }
            };

            objNode.extensionContainer.Add(typeContainer);

            #region Node Icon, replaced with color 
            //Texture assetTexture = AssetPreview.GetAssetPreview(obj);
            //if (!assetTexture)
            //    assetTexture = AssetPreview.GetMiniThumbnail(obj);

            //if (assetTexture)
            //{
            //    AddDivider(objNode);

            //    objNode.extensionContainer.Add(new Image
            //    {
            //        image = assetTexture,
            //        scaleMode = ScaleMode.ScaleToFit,
            //        style =
            //        {
            //            paddingBottom = 4.0f,
            //            paddingTop = 4.0f,
            //            paddingLeft = 4.0f,
            //            paddingRight = 4.0f
            //        }
            //    });
            //} 
            #endregion

            // Ports
            //if (!isMainNode) {
                Port realPort = objNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Object));
                realPort.portName = "Dependent";
                objNode.inputContainer.Add(realPort);
            //}

            if (dependencyAmount > 0) {
#if UNITY_2018_1
                Port port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, typeof(Object));
#else
                Port port = objNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Object));
#endif
                port.portName = dependencyAmount + " Dependencies";
                objNode.outputContainer.Add(port);
                objNode.RefreshPorts();
            }

            resultNode = objNode;

            resultNode.RefreshExpandedState();
            resultNode.RefreshPorts();
            resultNode.capabilities &= ~Capabilities.Deletable;
            resultNode.capabilities |= Capabilities.Collapsible;
        }
        //Debug.Log(assetGUID);
        m_GUIDNodeLookup[assetGUID] = resultNode;
        return resultNode;
    }

    bool FilterType(string type) {
        switch (type) {
            case "MonoScript":
                return codeToggle.value;
            case "Material":
                return MaterialToggle.value;
            case "Texture2D":
                return textureToggle.value;
            case "Shader":
                return shaderToggle.value;
            case "ComputeShader":
                return shaderToggle.value;
            case "AudioClip":
                return audioClipToggle.value;
            case "AnimationClip":
                return animationClipToggle.value;
            default:
                break;
        }

        return CustomFilter(type);
    }
    
    //Add custom asset types here like scriptable objects f.e.
    bool CustomFilter(string type) {
        switch (type) {
            case "GearObject":
                return CustomToggle.value;
            case "TalentObject":
                return CustomToggle.value;
            case "AbilityInfo":
                return CustomToggle.value;
            case "HealthSO":
                return CustomToggle.value;
            default:
                break;
        }

        return false;
    }

    StyleColor GetColorByAssetType(Object obj) {
        var typeName = obj.GetType().Name;
        //Debug.Log(obj.GetType());
        switch (typeName) {
            case "MonoScript":
                return Color.black;
            case "Material":
                return new Color(0.1f, 0.5f, 0.1f);   //green
            case "Texture2D":
                return new Color(0.5f, 0.1f, 0.1f); //red
            case "RenderTexture":
                return new Color(0.8f, 0.1f, 0.1f); //red
            case "Shader":
                return new Color(0.1f, 0.1f, 0.5f); //dark blue
            case "ComputeShader":
                return new Color(0.1f, 0.1f, 0.5f); //dark blue
            case "GameObject":
                return new Color(0f, 0.8f, 0.7f); //light blue
            case "AnimationClip":
                return new Color(1, 0.7f, 1); //pink
            case "AnimatorController":
                return new Color(1, 0.7f, 0.8f); //pink
            case "AudioClip":
                return new Color(1, 0.8f, 0); //orange
            case "AudioMixerController":
                return new Color(1, 0.8f, 0); //orange
            case "Font":
                return new Color(0.9f, 1, 0.9f); //light green
            case "TMP_FontAsset":
                return new Color(0.9f, 1, 0.9f); //light green
            case "Mesh":
                return new Color(0.5f, 0, 0.5f); //purple
            case "TerrainLayer":
                return new Color(0.5f, 0.8f, 0f);   //green
            default:
                break;
        }

        return CustomColor(typeName);
        //return new Color(0.24f, 0.24f, 0.24f, 0.8f);
    }

    //Add custom assets here 
    StyleColor CustomColor(string assetType) {
        switch (assetType) {
            case "GearObject":
                return new Color(0.9f, 0, 0.9f); //pink
            case "TalentObject":
                return new Color(0.9f, 0, 0.9f); //
            case "AbilityInfo":
                return new Color(0.9f, 0, 0.9f); //
            case "HealthSO":
                return new Color(0.9f, 0, 0.9f); //
            default:
                break;
        }

        //standard color
        return new Color(0.24f, 0.24f, 0.24f, 0.8f);
    }

    private static void AddDivider(Node objNode) {
        var divider = new VisualElement { name = "divider" };
        divider.AddToClassList("horizontal");
        objNode.extensionContainer.Add(divider);
    }
    
    private void ClearGraph() {
        SelectedObjects.Clear();

        foreach (var assetGroup in AssetGroups) {
            EmptyGroup(assetGroup);
        }

        m_GUIDNodeLookup.Clear();

        AssetGroups.Clear();
    }

    void EmptyGroup(AssetGroup assetGroup) {
        if (assetGroup.m_AssetConnections.Count > 0) {
            foreach (var edge in assetGroup.m_AssetConnections) {
                m_GraphView.RemoveElement(edge);
            }
        }
        assetGroup.m_AssetConnections.Clear();

        foreach (var node in assetGroup.m_AssetNodes) {
            m_GraphView.RemoveElement(node);
        }
        assetGroup.m_AssetNodes.Clear();
        
        assetGroup.m_DependenciesForPlacement.Clear();

        //if (assetGroup.SharedGroup != null) {
        //    EmptyGroup(assetGroup.SharedGroup);
        //}

        m_GraphView.RemoveElement(assetGroup.groupNode);

        assetGroup.groupNode = null;
    }

    private void UpdateGroupDependencyNodePlacement(GeometryChangedEvent e, AssetGroup assetGroup) {
        assetGroup.mainNode.UnregisterCallback<GeometryChangedEvent, AssetGroup>(
            UpdateGroupDependencyNodePlacement
        );

        ResetNodes(assetGroup);
    }

    void ResetAllNodes() {
        foreach (var assetGroup in AssetGroups) {
            ResetNodes(assetGroup);
        }
    }

    //Reset the node positions of the given group
    void ResetNodes(AssetGroup assetGroup) {
        // The current y offset in per depth
        var depthOffset = new Dictionary<int, float>();

        foreach (var node in assetGroup.m_DependenciesForPlacement) {
            int depth = (int)node.userData;

            if (!depthOffset.ContainsKey(depth))
                depthOffset.Add(depth, 0.0f);

            if (AlignmentToggle.value) {
                depthOffset[depth] += node.layout.height;
            }
            else {
                depthOffset[depth] += node.layout.width;
            }
        }

        // Move half of the node into negative y space so they're on either size of the main node in y axis
        var depths = new List<int>(depthOffset.Keys);
        foreach (int depth in depths) {
            if (depth == 0)
                continue;

            float offset = depthOffset[depth];
            depthOffset[depth] = (0f - offset / 2.0f);
        }

        Rect mainNodeRect = assetGroup.mainNode.GetPosition();

        foreach (var node in assetGroup.m_DependenciesForPlacement) {
            int depth = (int)node.userData;
            //Debug.Log(node.layout);
            if (AlignmentToggle.value) {
                //node.SetPosition(new Rect(mainNodeRect.x + kNodeWidth * 1.5f * depth, mainNodeRect.y + depthOffset[depth], 0, 0));
                node.SetPosition(new Rect(mainNodeRect.x + node.layout.width * 1.5f * depth, mainNodeRect.y + depthOffset[depth], 0, 0));
            }
            else {
                node.SetPosition(new Rect(mainNodeRect.x + depthOffset[depth], mainNodeRect.y + node.layout.height * 1.5f * depth, 0, 0));
                //node.SetPosition(new Rect(mainNodeRect.x + depthOffset[depth], mainNodeRect.y + kNodeWidth * 1.5f * depth, 0, 0));
            }

            if (AlignmentToggle.value) {
                depthOffset[depth] += node.layout.height;
            }
            else {
                depthOffset[depth] += node.layout.width;
            }
        }        
    }

    //fix the position of the groups so they dont overlap
    void ResetGroups() {
        float y = 0;
        float x = 0;

        foreach (var assetGroup in AssetGroups) {
            //Debug.Log(assetGroup.groupNode.GetPosition());

            if (AlignmentToggle.value) {
                Rect pos = assetGroup.groupNode.GetPosition();
                pos.x = x;
                assetGroup.groupNode.SetPosition(pos);
                x += assetGroup.groupNode.GetPosition().width;
            }
            else {
                Rect pos = assetGroup.groupNode.GetPosition();
                pos.y = y;
                assetGroup.groupNode.SetPosition(pos);
                y += assetGroup.groupNode.GetPosition().height;
            }
        }
    }
}