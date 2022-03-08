using System.Collections.Generic;

namespace Leap.Unity
{
    internal abstract class DependencyNodeBase
    {
        public string Name;
        public List<DependencyNodeBase> Parents = new List<DependencyNodeBase>();

        public abstract float GetSize();

        private Dictionary<DependencyNodeBase, bool> _dependsCache =
            new Dictionary<DependencyNodeBase, bool>();

        public bool DependsOnCached(DependencyNodeBase other)
        {
            if (other == null || other == this) return false;
            bool ret;
            if (!_dependsCache.TryGetValue(other, out ret)) {
                _dependsCache.Add(other, false);
                ret = DependsOn(other);
                _dependsCache[other] = ret;
            }
            return ret;
        }

        public abstract bool DependsOn(DependencyNodeBase other);

        public bool IsMyParent(DependencyNodeBase other)
        {
            foreach (var parent in Parents)
            {
                return parent != null && (parent == other || parent.IsMyParent(other));
            }
            return false;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}