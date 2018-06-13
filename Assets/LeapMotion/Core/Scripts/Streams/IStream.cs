using System;

namespace Leap.Unity {
  
  public interface IStream<T> {

    event Action OnOpen;

    event Action<T> OnSend;

    event Action OnClose;

  }

}
