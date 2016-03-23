using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using System;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity{
  /* The ExecuteBefore and ExecuteAfter attributes can be used to cause a behavior to be executed after or before
   * another behavior.  This is a more robust way to specify requirements in ordering than using Unity's built in
   * ScriptExecutionOrder tab since it cannot be changed or invalidated accidentally by a user.
   * 
   * These attributes can be stacked and combined as much as one desires.  The effects of the ordering attributes
   * are not inherited by an extending class.  
   * 
   * If one defines a cycle (Script A executed before B executes before C executes before A) the update of the 
   * script order will fail, and alert you to the cycle so that you can resolve it.  
   * 
   * The application of the execution order occurs after every script reload, so there is no need to manually 
   * trigger the proccess, but if you DO need to, you can go to Assets->Apply Execution Order Attributes to
   * trigger the proccess manually
   */
  
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class ExecuteBeforeAttribute : Attribute {
    public readonly Type beforeType;
  
    public ExecuteBeforeAttribute(Type beforeType) {
      this.beforeType = beforeType;
    }
  }
  
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class ExecuteAfterAttribute : Attribute {
    public readonly Type afterType;
  
    public ExecuteAfterAttribute(Type afterType) {
      this.afterType = afterType;
    }
  }
  
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class ExecuteBeforeDefault : Attribute { }
  
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class ExecuteAfterDefault : Attribute { }
  
  #if UNITY_EDITOR
  public class ExecutionOrderSolver {
  
    private enum NodeType {
      RELATIVE_ORDERED = 0,   // The node has at least one ordering attribute, but no attribute has been violated
      RELATIVE_UNORDERED = 1, // The node has at least one ordering attribute, and at least one has been violated
      ANCHORED = 2,           // The node has no ordering attributes, and its index is allowed to change.  Relative ordering is NOT allowed to change.
      LOCKED = 3              // The node has no ordering attributes, and its index is NOT allowed to change.
    }
  
    /* Every node represents a grouping of behaviors that all can the same execution index.  Grouping them
     * both helps algorithmic complexity, as well as ensuring that scripts with the same sorting index do
     * not become seperated */
    private class Node {
      /* A set of all the behavior types associated with this Node */
      public List<Type> types = new List<Type>(1);
  
      //Types that this node executes before
      public List<Type> beforeTypes = new List<Type>();
  
      //Types that this node executes after
      public List<Type> afterTypes = new List<Type>();
  
      /* Used during the topological sort.  Represents the number of edges that travel to this node in the graph*/
      public int incomingEdgeCount = 0;
  
      /* Represents the execution index of this node.  Is initialized to the exising execution index, and 
       * eventually is solved to satisfy the ordering attributes */
      public int executionIndex = 0;
  
      public NodeType nodeType;
  
      public Node(Type type, int executionIndex, NodeType nodeType) {
        this.types.Add(type);
        this.executionIndex = executionIndex;
        this.nodeType = nodeType;
      }
  
      public bool isAnchoredOrLocked {
        get {
          return nodeType == NodeType.ANCHORED || nodeType == NodeType.LOCKED;
        }
      }
  
      /* Tries to combine another node into this one.  This method assumes that the other node is a direct
       * neighbor to this one in an ordering, as two nodes cannot be combined if they are not neighbors. */
      public bool tryCombineWith(Node other) {
        /* If both nodes are anchored or locked, but have difference execution indexes, we cannot combine them. */
        if (isAnchoredOrLocked && other.isAnchoredOrLocked && executionIndex != other.executionIndex) {
          return false;
        }
  
        /* If either node has an ordering conflict with the other, we cannot combine them. */
        if (other.doesHaveOrderingComparedTo(this)) {
          return false;
        }
  
        if (doesHaveOrderingComparedTo(other)) {
          return false;
        }
  
        /* This node and the other node can be combined! */
  
        types.AddRange(other.types);
        beforeTypes.AddRange(other.beforeTypes);
        afterTypes.AddRange(other.afterTypes);
  
        executionIndex = isAnchoredOrLocked ? executionIndex : other.executionIndex;
        nodeType = (NodeType)Mathf.Max((int)nodeType, (int)other.nodeType);
  
        return true;
      }
  
      private bool doesHaveOrderingComparedTo(Node other) {
        foreach (Type t in other.types) {
          if (beforeTypes.Contains(t)) {
            return true;
          }
          if (afterTypes.Contains(t)) {
            return true;
          }
        }
        return false;
      }
  
      public override string ToString() {
        string typeList = "";
        foreach (Type t in types) {
          if (typeList == "") {
            typeList = t.Name;
          } else {
            typeList += "+" + t.Name;
          }
        }
        return typeList;
      }
    }
  
    [MenuItem("Assets/Apply Execution Order Attributes")]
    [DidReloadScripts]
    public static void solveForExecutionOrders() {
      MonoScript[] monoscripts = MonoImporter.GetAllRuntimeMonoScripts();
  
      List<Node> nodes;
      Node defaultNode;
  
      constructLockedNodes(monoscripts, out nodes, out defaultNode);
  
      constructRelativeNodes(monoscripts, defaultNode, ref nodes);
  
      if (!nodes.Any(n => n.nodeType == NodeType.RELATIVE_UNORDERED)) {
        //If we don't have any unordered nodes, there is no work to do.
        return;
      }
  
      unanchorReferencedNodes(ref nodes);
  
      collapseAnchoredOrLockedNodes(ref nodes);
  
      Dictionary<Node, List<Node>> edges = new Dictionary<Node, List<Node>>();
  
      constructRelativeEdges(nodes, ref edges);
      if (checkForCycles(defaultNode, edges)) return;
  
      constructAnchoredEdges(nodes, ref edges);
      if (checkForCycles(defaultNode, edges)) return;
  
      if (!trySolveTopologicalOrdering(ref nodes, ref edges)) return;
  
      collapseNeighbors(ref nodes);
  
      if (!tryAssignExecutionIndexes(defaultNode, ref nodes)) return;
  
      applyExecutionIndexes(monoscripts, nodes);
    }
  
    /* Given all of the loaded monoscripts inside of the UnityEngine namespace, construct a locked Node
     * for every group of scripts with the same execution index.  The node with the index of 0 is assigned
     * to be the default node.
     */
    private static void constructLockedNodes(MonoScript[] monoscripts, out List<Node> nodes, out Node defaultNode) {
      Dictionary<int, Node> indexToNode = new Dictionary<int, Node>();
      nodes = new List<Node>();
  
      foreach (MonoScript script in monoscripts) {
        Type type = script.GetClass();
        if (type == null) continue;
        if (type.Namespace == null) continue;
        if (!type.Namespace.StartsWith("UnityEngine")) continue;
  
        int executionIndex = MonoImporter.GetExecutionOrder(script);
  
        Node node;
        if (!indexToNode.TryGetValue(executionIndex, out node)) {
          node = new Node(type, executionIndex, NodeType.LOCKED);
          nodes.Add(node);
          indexToNode[executionIndex] = node;
        } else {
          node.types.Add(type);
        }
      }
  
      defaultNode = indexToNode[0];
    }
  
    /* Given all of the loaded monoscripts outside of the UnityEngine namespace, construct a single Node for
     * each monoscript.
     * 
     * Monoscripts that have no ordering attributes are considered 'anchored', as their ordering has been defined by
     * their current index in the ScriptExecutionOrder.  All anchored nodes should not be moved relative to each
     * other, as this could be an important ordering that has been defined by the user or plugins.
     * 
     * Behaviors that do have ordering attributes are not considered anchored, even if they are currently in order.
     * If the behavior is out of order relative to the requirements of its ordering attributes, it is marked
     * as unordered.
     */
    private static void constructRelativeNodes(MonoScript[] monoscripts, Node defaultNode, ref List<Node> nodes) {
      Dictionary<Type, int> typeToIndex = new Dictionary<Type, int>();
      foreach (MonoScript script in monoscripts) {
        Type scriptType = script.GetClass();
        if (scriptType == null) continue;
  
        typeToIndex[scriptType] = MonoImporter.GetExecutionOrder(script);
      }
  
      foreach (MonoScript script in monoscripts) {
        Type scriptType = script.GetClass();
        if (scriptType == null) continue;
        if (scriptType.Namespace != null && scriptType.Namespace.StartsWith("UnityEngine")) continue;
  
        if (Attribute.IsDefined(scriptType, typeof(ExecuteAfterAttribute), false) ||
            Attribute.IsDefined(scriptType, typeof(ExecuteBeforeAttribute), false) ||
            Attribute.IsDefined(scriptType, typeof(ExecuteBeforeDefault), false) ||
            Attribute.IsDefined(scriptType, typeof(ExecuteAfterDefault), false)) {
  
          Node relativeNode = new Node(scriptType, typeToIndex[scriptType], NodeType.RELATIVE_ORDERED);
          nodes.Add(relativeNode);
  
          foreach (Attribute customAttribute in Attribute.GetCustomAttributes(scriptType, false)) {
  
            if (customAttribute is ExecuteAfterAttribute) {
              ExecuteAfterAttribute executeAfter = customAttribute as ExecuteAfterAttribute;
              setRelativeToType(typeToIndex, relativeNode, executeAfter.afterType, defaultNode, false);
            } else if (customAttribute is ExecuteBeforeAttribute) {
              ExecuteBeforeAttribute executeBefore = customAttribute as ExecuteBeforeAttribute;
              setRelativeToType(typeToIndex, relativeNode, executeBefore.beforeType, defaultNode, true);
            } else if (customAttribute is ExecuteAfterDefault) {
              setRelativeToDefault(relativeNode, defaultNode, false);
            } else if (customAttribute is ExecuteBeforeDefault) {
              setRelativeToDefault(relativeNode, defaultNode, true);
            }
  
          }
        } else {
          Node anchoredNode = new Node(scriptType, typeToIndex[scriptType], NodeType.ANCHORED);
          nodes.Add(anchoredNode);
        }
      }
    }
  
    private static void setRelativeToType(Dictionary<Type, int> typeToIndex, Node node, Type relativeType, Node defaultNode, bool isBefore) {
      string beforeAfter = isBefore ? "before" : "after";
  
      if (!typeof(Behaviour).IsAssignableFrom(relativeType)) {
        Debug.LogWarning(node + " can not execute " + beforeAfter + " " + relativeType.Name + " because " + relativeType + " is not a Behaviour");
        return;
      }
  
      int relativeIndex;
      if (!typeToIndex.TryGetValue(relativeType, out relativeIndex)) {
        setRelativeToDefault(node, defaultNode, isBefore);
        return;
      }
  
      if (isBefore != (node.executionIndex < relativeIndex) ||
          node.executionIndex == relativeIndex) {
        node.nodeType = NodeType.RELATIVE_UNORDERED;
      }
  
      if (isBefore) {
        node.beforeTypes.Add(relativeType);
      } else {
        node.afterTypes.Add(relativeType);
      }
    }
  
    private static void setRelativeToDefault(Node node, Node defaultNode, bool isBefore) {
      if (isBefore != (node.executionIndex < 0)) {
        node.nodeType = NodeType.RELATIVE_UNORDERED;
      }
  
      if (isBefore) {
        node.beforeTypes.AddRange(defaultNode.types);
      } else {
        node.afterTypes.AddRange(defaultNode.types);
      }
    }
  
    /* Any nodes that are referenced by other nodes in an ordering will be unanchored so that
     * they are not grouped, and so that they are completely free to move around to satisfy an
     * ordering.
     */
    private static void unanchorReferencedNodes(ref List<Node> nodes) {
      HashSet<Type> referencedTypes = new HashSet<Type>();
      foreach (Node node in nodes) {
        referencedTypes.UnionWith(node.beforeTypes);
        referencedTypes.UnionWith(node.afterTypes);
      }
  
      foreach (Node node in nodes.Where(n => n.nodeType == NodeType.ANCHORED)) {
        foreach (Type type in node.types) {
          if (referencedTypes.Contains(type)) {
            node.nodeType = NodeType.RELATIVE_UNORDERED;
            break;
          }
        }
      }
    }
  
    /* Collapses any anchored or locked nodes with the same index into the same node. */
    private static void collapseAnchoredOrLockedNodes(ref List<Node> nodes) {
      List<Node> newNodeList = new List<Node>();
  
      Dictionary<int, Node> _collapsedAnchoredNodes = new Dictionary<int, Node>();
  
      foreach (Node node in nodes) {
        //If the node is anchored, we can put it into the collapsed node
        //The EventSystem node should never be combined
        if (node.isAnchoredOrLocked) {
          Node anchorGroup;
          if (!_collapsedAnchoredNodes.TryGetValue(node.executionIndex, out anchorGroup)) {
            anchorGroup = node;
            _collapsedAnchoredNodes[anchorGroup.executionIndex] = anchorGroup;
            newNodeList.Add(anchorGroup);
          } else {
            anchorGroup.tryCombineWith(node);
          }
        } else {
          newNodeList.Add(node);
        }
      }
  
      nodes = newNodeList;
    }
  
    private static void constructAnchoredEdges(List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
      //Create a sorted list of all the execution indexes of all of the anchored nodes
      List<int> anchoredNodeIndexes = nodes.Where(n => n.isAnchoredOrLocked).Select(n => n.executionIndex).Distinct().ToList();
      anchoredNodeIndexes.Sort();
  
      //Map each index to a list of all the anchored nodes with that index
      Dictionary<int, List<Node>> _indexToAnchoredNodes = new Dictionary<int, List<Node>>();
      foreach (Node anchoredNode in nodes.Where(n => n.isAnchoredOrLocked)) {
        List<Node> list;
        if (!_indexToAnchoredNodes.TryGetValue(anchoredNode.executionIndex, out list)) {
          list = new List<Node>();
          _indexToAnchoredNodes[anchoredNode.executionIndex] = list;
        }
        list.Add(anchoredNode);
      }
  
      /* Each anchored node has an edge connecting it to every other anchored node with the next lowest index
       * We do not need to connect every combination of nodes, because of the communicative property of comparison
       * if A > B > C, we don't need to specify that A > C explicitly, creating an edge for A > B and B > C is 
       * enough */
      foreach (Node anchoredNode in nodes.Where(n => n.isAnchoredOrLocked)) {
        int offset = anchoredNodeIndexes.IndexOf(anchoredNode.executionIndex);
  
        if (offset != 0) {
          List<Node> lowerNodes = _indexToAnchoredNodes[anchoredNodeIndexes[offset - 1]];
          foreach (Node lowerNode in lowerNodes) {
            addEdge(edges, lowerNode, anchoredNode);
          }
        }
      }
    }
  
    private static void constructRelativeEdges(List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
      Dictionary<Type, Node> typeToNode = new Dictionary<Type, Node>();
      foreach (Node node in nodes) {
        foreach (Type type in node.types) {
          typeToNode[type] = node;
        }
      }
  
      /* Build edges for non-anchored nodes.  This is simpler than the edges for the anchored nodes, since
       * there is exactly one edge for every ordering attribute */
      foreach (Node relativeNode in nodes.Where(n => !n.isAnchoredOrLocked)) {
        foreach (Type beforeType in relativeNode.beforeTypes) {
          Node beforeNode = typeToNode[beforeType];
          addEdge(edges, relativeNode, beforeNode);
        }
  
        foreach (Type afterType in relativeNode.afterTypes) {
          Node afterNode = typeToNode[afterType];
          addEdge(edges, afterNode, relativeNode);
        }
      }
    }
  
    private static void addEdge(Dictionary<Node, List<Node>> edges, Node before, Node after) {
      List<Node> set;
      if (!edges.TryGetValue(before, out set)) {
        set = new List<Node>();
        edges[before] = set;
      }
  
      set.Add(after);
      after.incomingEdgeCount++;
    }
  
    /* Here we check to see if there are any cycles in the graph we have constructed.  Our
     * solving algorithm will tell us if there is no solution, but it doesn't give us any
     * useful information about why it is unsolvable.  We try to find a cycle here so that
     * we can output it so that the user can more easily find the cycle and correct it.
     */
    private static bool checkForCycles(Node defaultNode, Dictionary<Node, List<Node>> edges) {
      Stack<Node> cycle = new Stack<Node>();
  
      foreach (var edge in edges) {
        if (findCycle(edges, edge.Key, cycle)) {
  
          string cycleString = "";
          foreach (Node cycleNode in cycle.Reverse()) {
            string nodeString = cycleNode == defaultNode ? "[Default]" : cycleNode.ToString();
  
            if (cycleString == "") {
              cycleString = nodeString;
            } else {
              cycleString += " => " + nodeString;
            }
          }
  
          EditorUtility.DisplayDialog("Execution Order Cycle!", "A cycle was found while trying to apply the Execution Order attributes!  Execution order cannot be applied until the cycle is removed\n\n" + cycleString, "Ok");
          return true;
        }
      }
  
      return false;
    }
  
    private static bool findCycle(Dictionary<Node, List<Node>> edges, Node visitingNode, Stack<Node> visitedNodes) {
      if (visitedNodes.LastOrDefault() == visitingNode) {
        visitedNodes.Push(visitingNode);
        return true;
      }
  
      if (visitedNodes.Contains(visitingNode)) {
        return false;
      }
  
      visitedNodes.Push(visitingNode);
  
      List<Node> connections;
      if (edges.TryGetValue(visitingNode, out connections)) {
        foreach (Node nextNode in connections) {
          if (findCycle(edges, nextNode, visitedNodes)) {
            return true;
          }
        }
      }
  
      visitedNodes.Pop();
      return false;
    }
  
    /* Given a directed graph of nodes, returns an ordering of nodes such that a node
     * always falls before a node it points towards.  This modifies the graph in the proccess.
     * 
     * Direct implementation of https://en.wikipedia.org/wiki/Topological_sorting#Algorithms
     * 
     * This method destroys the graph during the solve.
     */
    private static bool trySolveTopologicalOrdering(ref List<Node> nodes, ref Dictionary<Node, List<Node>> edges) {
  
      //Empty list to contain sorted nodes
      List<Node> L = new List<Node>(nodes.Count);
  
      //Set of all nodes with no incoming edges
      Stack<Node> S = new Stack<Node>(nodes.Where(s => s.incomingEdgeCount == 0));
  
      while (S.Count != 0) {
        //Remove a node n from S
        Node n = S.Pop();
  
        //Append n to L
        L.Add(n);
  
        List<Node> edgeList;
        if (edges.TryGetValue(n, out edgeList)) {
  
          //For every Node m where n -> m
          foreach (Node m in edgeList) {
            //Cut the edge from n to m
            m.incomingEdgeCount--;
            if (m.incomingEdgeCount == 0) {
              S.Push(m);
            }
          }
  
          //remove the edges from the graph
          edgeList.Clear();
        }
      }
  
      nodes = L;
  
      /* The only time this if statement will be satisfied is if we failed to find a cycle that
       * existed before, or if the solving algorithm failed to find an existing solution.  Both
       * cases are an error and we cannot procede.  This is mostly here for sanity checking, and
       * should never be hit if everything is working as it should.
       */
      if (edges.Values.Any(l => l.Count != 0)) {
        Debug.LogError("Topological sort failed for unknown reason!  Execution Order cannot be enforced!");
        return false;
      }
  
      return true;
    }
  
    /* It is often the case that neighboring nodes can be combined into a single node. */
    private static void collapseNeighbors(ref List<Node> nodes) {
      List<Node> newNodeList = new List<Node>();
  
      Node current = nodes[0];
      newNodeList.Add(current);
  
      for (int i = 1; i < nodes.Count; i++) {
        Node node = nodes[i];
        if (!current.tryCombineWith(node)) {
          current = node;
          newNodeList.Add(current);
        }
      }
  
      nodes = newNodeList;
    }
  
    /* This method takes the Node ordering and assigns execution indexes to each of them.  The
     * default node is never moved, since that is the node that contains all of Unity's behaviours,
     * which we cannot change the ordering for. This method has the potential to 'push' an
     * anchored Node to a different index, but will throw an error if tries to push a locked 
     * node.
     * 
     * The only case where a locked node tries to be pushed is if there are 2 locked nodes with more 
     * scripts between them than there are indexes available.  Currently in Unity the only locked 
     * nodes are the Default node (index 0) and the EventSystem node (index -1000).  The odds 
     * of 1000 scripts that all execute in a dependant chain is fairly  small.
     */
    private static bool tryAssignExecutionIndexes(Node defaultNode, ref List<Node> nodes) {
      int indexOfDefault = nodes.IndexOf(defaultNode);
  
      /* Shift all nodes that come before the default node away from the default node */
      int minIndex = 0;
      for (int i = indexOfDefault - 1; i >= 0; i--) {
        minIndex--;
  
        Node node = nodes[i];
  
        switch (node.nodeType) {
          case NodeType.RELATIVE_UNORDERED:
            break;
          case NodeType.RELATIVE_ORDERED:
            minIndex = Mathf.Min(node.executionIndex, minIndex);
            break;
          case NodeType.ANCHORED:
            minIndex = Mathf.Min(node.executionIndex, minIndex);
            break;
          case NodeType.LOCKED:
            if (minIndex < node.executionIndex) {
              Debug.LogError("Note enough execution indexes to fit all of the scripts!");
              return false;
            }
            minIndex = node.executionIndex;
            break;
        }
  
        node.executionIndex = minIndex;
      }
  
      /* Shift all nodes that come after the default node away from the default node */
      int maxIndex = 0;
      for (int i = indexOfDefault + 1; i < nodes.Count; i++) {
        maxIndex++;
  
        Node node = nodes[i];
  
        switch (node.nodeType) {
          case NodeType.RELATIVE_UNORDERED:
            break;
          case NodeType.RELATIVE_ORDERED:
            maxIndex = Mathf.Max(node.executionIndex, maxIndex);
            break;
          case NodeType.ANCHORED:
            maxIndex = Mathf.Min(node.executionIndex, maxIndex);
            break;
          case NodeType.LOCKED:
            if (maxIndex > node.executionIndex) {
              Debug.LogError("Note enough execution indexes to fit all of the scripts!");
              return false;
            }
            maxIndex = node.executionIndex;
            break;
        }
  
        node.executionIndex = maxIndex;
      }
  
      return true;
    }
  
    /* Given the list of existing monoscripts and the Node ordering, apply the ordering to the
     * Unity ExecutionOrder using MonoImporter
     */
    private static void applyExecutionIndexes(MonoScript[] monoscripts, List<Node> nodes) {
      Dictionary<Type, MonoScript> typeToMonoScript = new Dictionary<Type, MonoScript>();
      foreach (MonoScript monoscript in monoscripts) {
        Type scriptType = monoscript.GetClass();
        if (scriptType == null) {
          continue;
        }
  
        typeToMonoScript[scriptType] = monoscript;
      }
  
      foreach (Node node in nodes) {
        foreach (Type type in node.types) {
          MonoScript monoscript = typeToMonoScript[type];
          if (MonoImporter.GetExecutionOrder(monoscript) != node.executionIndex) {
            MonoImporter.SetExecutionOrder(monoscript, node.executionIndex);
          }
        }
      }
    }
  }
  #endif
}
