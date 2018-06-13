using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  [Serializable]
  public class SwitchTree {

    #region NodeRef & Node

    public class NodeRef {
      public Node node;

      public NodeRef(Node node) {
        this.node = node;
      }

      public NodeRef() { }
    }
    
    public struct Node {
      
      bool _isValid;

      /// <summary>
      /// The MonoBehaviour for this Node represents both its Transform (via its
      /// transform property) and its IPropertySwitch (because the MonoBehaviour itself
      /// must implement IPropertySwitch).
      /// </summary>
      private MonoBehaviour _switchBehaviour;
      public MonoBehaviour switchBehaviour {
        get { return _switchBehaviour; }
      }

      public Transform transform {
        get {
          if (!isValid) return null;
          return _switchBehaviour.transform;
        }
      }
      public IPropertySwitch objSwitch {
        get {
          if (!isValid) return null;
          return _switchBehaviour as IPropertySwitch;
        }
      }
      
      public NodeRef     parent;
      public List<Node>  children;

      public int treeDepth;

      /// <summary>
      /// The number of this node's children and grandchildren.
      /// </summary>
      public int numAllChildren;

      public int numChildren { get { return children.Count; } }

      public bool hasChildren { get { return children.Count > 0; } }

      public bool hasParent { get { return parent != null; } }

      public bool hasSibling { get { return hasParent && parent.node.numChildren > 1; } }

      public bool isValid {
        get { return _isValid; }
      }

      public bool isOn {
        get {
          if (objSwitch == null) return false;
          return objSwitch.GetIsOnOrTurningOn();
        }
      }

      public bool isParentOn {
        get {
          return hasParent && parent.node.isOn;
        }
      }

      public bool isOff {
        get {
          if (objSwitch == null) return false;
          return objSwitch.GetIsOffOrTurningOff();
        }
      }

      public bool hasNextSibling {
        get {
          return hasParent
              && parent.node.GetIndexOfChild(this) != parent.node.numChildren - 1;
        }
      }

      public Node nextSibling {
        get {
          return parent.node.GetChild(parent.node.GetIndexOfChild(this) + 1);
        }
      }

      public bool isNextSiblingOn {
        get {
          return hasNextSibling && nextSibling.isOn;
        }
      }

      public bool hasPrevSibling {
        get {
          return hasParent && parent.node.GetIndexOfChild(this) != 0;
        }
      }

      public Node prevSibling {
        get {
          return parent.node.GetChild(parent.node.GetIndexOfChild(this) - 1);
        }
      }

      public bool isPrevSiblingOn {
        get {
          return hasPrevSibling && prevSibling.isOn;
        }
      }

      public PrevSiblingEnumerator prevSiblings {
        get { return new PrevSiblingEnumerator(this); }
      }

      public struct PrevSiblingEnumerator : IEnumerator<Node> {
        Node node;
        int curIdx;

        public PrevSiblingEnumerator(Node node) {
          this.node = node;
          if (!node.hasParent) { curIdx = -1; }
          else { curIdx = node.parent.node.GetIndexOfChild(node); }
        }

        public Node Current { get { return node.parent.node.GetChild(curIdx); } }

        object IEnumerator.Current {
          get { return Current; }
        }
        public void Dispose() { }

        public bool MoveNext() {
          curIdx -= 1;
          return curIdx >= 0;
        }
        public PrevSiblingEnumerator GetEnumerator() { return this; }

        public bool TryGetNext(out Node t) {
          bool hasNext = MoveNext();
          if (!hasNext) { t = default(Node); return false; }
          else { t = Current; return true; }
        }

        public void Reset() { curIdx = node.parent.node.GetIndexOfChild(node); }

        public Query<Node> Query() {
          return (this as IEnumerator<Node>).Query();
        }
      }

      public Node(MonoBehaviour switchBehaviour,
                  NodeRef parent = null,
                  int treeDepth = 0) {
        this._switchBehaviour = switchBehaviour;

        this.parent = parent;
        this.treeDepth = treeDepth;

        _isValid = true;

        children = new List<Node>();
        numAllChildren = 0;
        constructChildren();
      }

      public int GetIndexOfChild(Node child) {
        return children.IndexOf(child);
      }

      public Node GetChild(int childIdx) {
        return children[childIdx];
      }

      private void constructChildren() {
        var stack = Pool<Stack<Transform>>.Spawn();
        stack.Clear();
        stack.Push(this.transform);
        try {
          while (stack.Count > 0) {
            var transform = stack.Pop();

            foreach (var child in transform.GetChildren()) {
              // Ignore SwitchTreeControllers, which will handle their own internal
              // hierarchy.
              if (child.GetComponent<SwitchTreeController>() != null) continue;

              // ObjectSwitches get priority, but any Switch will do.
              IPropertySwitch objSwitch = child.GetComponent<ObjectSwitch>();
              if (objSwitch == null) {
                objSwitch = child.GetComponent<IPropertySwitch>();
              }
              if (objSwitch != null) {
                // Each child with a switch component gets a node with the current node
                // as its parent.
                var newChild = new Node((objSwitch as MonoBehaviour),
                                        new NodeRef(this),
                                        this.treeDepth + 1);
                children.Add(newChild);
                this.numAllChildren += 1 + newChild.numAllChildren;
              }
              else {
                // This node will "inherit" any grand-children nodes whose parents are
                // not _themselves_ switches as direct "children" in the switch tree.
                stack.Push(child);
              }
            }
          }
        }
        finally {
          stack.Clear();
          Pool<Stack<Transform>>.Recycle(stack);
        }
      }

      public static bool operator ==(Node one, Node other) {
        return one.Equals(other);
      }
      public static bool operator !=(Node one, Node other) {
        return !one.Equals(other);
      }
      public override bool Equals(object obj) {
        if (!(obj is Node)) return false;
        return this.Equals((Node)obj);
      }
      public bool Equals(Node other) {
        return isValid && this._switchBehaviour == other._switchBehaviour;
      }
      public override int GetHashCode() {
        if (this._switchBehaviour == null) return 0;
        return this._switchBehaviour.GetHashCode();
      }

    }

    #endregion

    /// <summary>
    /// This is the sole object Unity serializes to serialize the switch tree; the rest
    /// is generated in OnAfterDeserialize.
    /// </summary>
    [SerializeField]
    public MonoBehaviour rootSwitchBehaviour;

    /// <summary>
    /// Unity also serializes the name of the currently active node; when the tree is
    /// initialized, this will be the node it attempts to switch to.
    /// </summary>
    [SerializeField]
    private string _curActiveNodeName = "";
    public string curActiveNodeName {
      get { return _curActiveNodeName; }
    }

    private Node _root;
    private bool _treeReady;

    public SwitchTree(Transform transform, string startingActiveNode = null) {
      var objSwitch = transform.GetComponent<IPropertySwitch>();
      if (objSwitch == null) {
        throw new System.InvalidOperationException("Cannot build a Switch Tree for "
                                                 + "a Transform that is not itself a "
                                                 + "switch.");
      }

      rootSwitchBehaviour = (objSwitch as MonoBehaviour);
      _treeReady = false;

      if (startingActiveNode == null) {
        _curActiveNodeName = rootSwitchBehaviour.name;
      }
      else {
        _curActiveNodeName = startingActiveNode;

        ensureTreeReady();

        SwitchTo(startingActiveNode);
      }
    }

    public int NodeCount {
      get {
        ensureTreeReady();
        return _root.numAllChildren + 1;
      }
    }

    private void ensureTreeReady() {
      if (!_treeReady) {
        initTree();
      }
    }

    private void initTree() {
      _root = new Node(rootSwitchBehaviour, null);
    }

    /// <summary>
    /// Traverses the tree, deactivating all switch pathways that do not lead to the
    /// switch node identified by this nodeName, then activating all switches along the
    /// path to the switch node identified by this nodeName, but no deeper. Switches that
    /// are children of the named node are also deactivated.
    /// 
    /// Returns true if the node identified by nodeName was found, false otherwise.
    /// </summary>
    public bool SwitchTo(string nodeName, bool immediately = false, bool toggle = false) {
      ensureTreeReady();

      // We have to traverse the whole tree because we don't know where the node matching
      // nodeName resides. This is fine, because the contract of the tree is to maintain
      // every node's state anyway, not just the ones that are currently activating or
      // deactivating.

      var activeNodeChain = Pool<Stack<Node>>.Spawn();
      var visitedNodes = Pool<HashSet<Node>>.Spawn();
      var tempNodeRef = Pool<NodeRef>.Spawn();
      var curNodeRef = tempNodeRef;
      curNodeRef.node = _root;

      // This dictionary allows us to reverse-breadth-first traverse all nodes once;
      // we build it during the depth-first traversal.
      var nodesAtDepthLevel = Pool<Dictionary<int, List<Node>>>.Spawn();
      int curDepth = 0, largestDepth = 0;

      Node activeNode = default(Node);
      try {
        // Depth-first traversal.
        while (curNodeRef != null) {
          var node = curNodeRef.node;
          
          // If visiting a new node, check if it's the desired active node.
          if (!visitedNodes.Contains(node)) {
            visitedNodes.Add(node);

            // We also construct a depth-first stack of all nodes, so we
            // can deactivate nodes in a predictable (reverse-depth-first) order
            // post-traversal.
            List<Node> nodes = null;
            if (!nodesAtDepthLevel.TryGetValue(curDepth, out nodes)) {
              nodesAtDepthLevel[curDepth] = nodes = Pool<List<Node>>.Spawn();
            }
            nodes.Add(node);

            if (node.transform.name.Equals(nodeName)) {
              // We've found the node we want active.
              activeNode = node;
              _curActiveNodeName = activeNode.switchBehaviour.name;
              buildNodeChain(activeNode, activeNodeChain);
            }
          }

          // Go deeper into children if there are any.
          bool goingDown = false;
          foreach (var child in node.children) {
            if (!visitedNodes.Contains(child)) {
              curNodeRef.node = child;
              goingDown = true;
              break;
            }
          }
          if (goingDown) {
            // Heading down into the unvisited child.
            curDepth += 1;
            if (curDepth > largestDepth) {
              largestDepth = curDepth;
            }
            continue;
          }
          else {
            // Head back up to the parent node.
            curDepth -= 1;
            curNodeRef = node.parent;
          }
        }

        // After traversal, we have a per-depth-level dictionary of all nodes and a
        // Stack of the node chain we desire to activate.
        //
        // Deeper nodes should deactivate before their parent nodes. Nodes in the chain
        // we desire active should not deactivate, and if they are not already active,
        // they should activate from the root down.
        if (nodesAtDepthLevel.Count != 0) {
          List<Node> depthNodes;
          for (int d = largestDepth; d >= 0; d--) {
            depthNodes = nodesAtDepthLevel[d];

            foreach (var node in depthNodes) {
              if (node.objSwitch.GetIsOnOrTurningOn()
                  && !activeNodeChain.Contains(node)) {
                turnOff(node, immediately);
              }
            }
          }
          while (activeNodeChain.Count > 0) {
            var node = activeNodeChain.Pop();
            if (toggle && activeNode.isOn && node == activeNode) {
              // The current node is the target "active" node, but if we're trying to
              // _toggle_ this node and the node is on, then we only desire to switch to
              // its parent, and have the node itself off.
              turnOff(node, immediately);

              if (!node.hasParent) {
                _curActiveNodeName = "";
              }
              else {
                _curActiveNodeName = node.parent.node.switchBehaviour.name;
              }

              continue;
            }
            else {
              turnOn(node, immediately);
            }

            // Furthermore, some nodes activate switches in their children as a part of
            // their normal switching functionality. However, when the tree desires to
            // switch to a specific node beneath one of these nodes, this behavior should
            // be overridden.
            // To do this, we need to deactivate all OTHER children when we still have a\
            // child node to activate, just after activating any node.
            if (activeNodeChain.Count > 0) {
              var childToActivate = activeNodeChain.Pop();
              foreach (var child in node.children) {
                if (childToActivate == child) continue;
                if (child.objSwitch.GetIsOnOrTurningOn()) {
                  turnOff(child, immediately);
                }
              }
              activeNodeChain.Push(childToActivate);
            }
          }
        }

        // Return true if we were able to find our target active node.
        return activeNode != default(Node);
      }
      finally {
        Pool<NodeRef>.Recycle(tempNodeRef);

        visitedNodes.Clear();
        Pool<HashSet<Node>>.Recycle(visitedNodes);

        foreach (var depthNodesPair in nodesAtDepthLevel) {
          var nodes = depthNodesPair.Value;
          nodes.Clear();
          Pool<List<Node>>.Recycle(nodes);
        }
        nodesAtDepthLevel.Clear();
        Pool<Dictionary<int, List<Node>>>.Recycle(nodesAtDepthLevel);

        activeNodeChain.Clear();
        Pool<Stack<Node>>.Recycle(activeNodeChain);
      }
    }

    /// <summary>
    /// If the specified state node is not active, the switch tree will switch to that
    /// node. If switch tree will switch to that node's parent, deactivating the
    /// specified node. (Calling this method will also deactivate any sibling nodes that
    /// might have been activated out of the context of the SwitchTree.)
    /// 
    /// Returns true if the node identified by nodeName was found in the tree,
    /// false otherwise.
    /// </summary>
    public bool ToggleState(string nodeName, bool immediately = false) {
      return SwitchTo(nodeName, immediately, toggle: true);
    }

    /// <summary>
    /// Fills the node stack with the node-parent chain such that
    /// the first Pop() produces the highest parent node and further Pops()
    /// produce children down the chain, ending with (and including) the
    /// starting node.
    /// </summary>
    private static void buildNodeChain(Node node, Stack<Node> nodeStack) {
      nodeStack.Clear();

      var curNode = node;
      while (true) {
        nodeStack.Push(curNode);

        if (curNode.hasParent) {
          curNode = curNode.parent.node;
        }
        else {
          break;
        }
      }
    }

    private static void turnOn(Node node, bool immediately) {
      if (Application.isPlaying && !immediately) {
        node.objSwitch.On();
      }
      else {
        node.objSwitch.OnNow();
      }
    }

    private static void turnOff(Node node, bool immediately) {
      if (Application.isPlaying && !immediately) {
        node.objSwitch.Off();
      }
      else {
        node.objSwitch.OffNow();
      }
    }

    /// <summary>
    /// Returns an enumerator that traverses the switch tree depth-first. You must
    /// provide a HashSet of SwitchTree.Node objects for the enumerator to track which
    /// nodes it has visited already without allocating. (You can also use this, as long
    /// as you don't modify it mid-traversal.)
    /// </summary>
    public DepthFirstEnumerator Traverse(HashSet<Node> visitedNodesCache) {
      return new DepthFirstEnumerator(this, visitedNodesCache);
    }

    #region Enumerators

    public struct DepthFirstEnumerator : IEnumerator<Node> {
      private Maybe<Node> maybeCurNode;
      private HashSet<Node> visitedNodes;
      private SwitchTree tree;

      public DepthFirstEnumerator(SwitchTree tree, HashSet<Node> useToTrackVisitedNodes) {
        useToTrackVisitedNodes.Clear();

        this.tree = tree;
        maybeCurNode = Maybe.None;
        visitedNodes = useToTrackVisitedNodes;
      }

      public DepthFirstEnumerator GetEnumerator() { return this; }
      public Node Current { get { return maybeCurNode.valueOrDefault; } }

      object IEnumerator.Current {
        get { return Current; }
      }
      public void Dispose() { }

      public bool MoveNext() {
        if (!maybeCurNode.hasValue) {
          maybeCurNode = Maybe.Some(tree._root);
          return true;
        }

        var node = maybeCurNode.valueOrDefault;
        visitedNodes.Add(node);

        bool goingDown = false;
        foreach (var child in node.children) {
          if (!visitedNodes.Contains(child)) {
            maybeCurNode = Maybe.Some(child);
            goingDown = true;
          }
        }
        if (goingDown) {
          // We've already set maybeCurNode with the child node.
          return true;
        }
        else if (node.hasParent) {
          maybeCurNode = Maybe.Some(node.parent.node);
          return MoveNext();
        }
        else {
          return false;
        }
      }

      public bool TryGetNext(out Node t) {
        bool hasNext = MoveNext();
        if (hasNext) {
          t = Current;
          return true;
        }
        else {
          t = default(Node);
          return false;
        }
      }

      public void Reset() {
        visitedNodes.Clear();
        maybeCurNode = Maybe.None;
      }
    }

    #endregion

  }

}