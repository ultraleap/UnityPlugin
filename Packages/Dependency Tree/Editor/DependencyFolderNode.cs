using System.Collections.Generic;

namespace Leap.Unity
{
    internal class DependencyFolderNode : DependencyNodeBase
    {
        public List<DependencyNodeBase> Children = new List<DependencyNodeBase>();

        public override float GetSize()
        {
            var size = 0f;
            foreach (var c in Children) {
                size += c.GetSize();
            }
            return size;
        }

        public override bool DependsOn(DependencyNodeBase other)
        {
            foreach (var c in Children) {
                if (c.DependsOnCached(other)) {
                    return true;
                }
            }
            return false;
        }
    }
}