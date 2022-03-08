namespace Leap.Unity
{
    using UnityEngine;
    using System.Collections.Generic;

    internal class DependencyNode : DependencyNodeBase
    {
        public string Guid;

        public float Size;

        public List<DependencyNode> Dependencies = new List<DependencyNode>();

        public override float GetSize() { return Size; }

        public override bool DependsOn(DependencyNodeBase other)
        {
            foreach (var d in Dependencies) {
                if (d == this) {
                    Debug.Log($"Why does {Name} depend on itself?");
                    return false;
                }
                if (other == d || d.IsMyParent(other)) {
                    return true;
                }
            }
            foreach (var d in Dependencies) {
                if (d.DependsOnCached(other)) {
                    return true;
                }
            }
            return false;
        }
    }
}
