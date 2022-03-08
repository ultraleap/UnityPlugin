using System.Collections.Generic;

namespace Leap.Unity
{
    internal class DependencyRoot : DependencyFolderNode
    {
        public HashSet<KeyValuePair<DependencyNodeBase, DependencyNodeBase>> Dependencies =
            new HashSet<KeyValuePair<DependencyNodeBase, DependencyNodeBase>>();
        
        public void AddDependencies()
        {
            
        }
    }
}