using System.Collections.Generic;

namespace Procedural.DynamicMesh {

  public interface IMeshOp {
    void Operate(List<RawMesh> input, out RawMesh output);
  }
}
