using System;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// Represents an ordered collection of objects of type BaseType.
  /// 
  /// Unlike normal List objects, when MultiTypedList is serialized
  /// it is able to support a certain amount of polymorphism.  To
  /// use MultiTypedList you must specify exactly which types could
  /// possibly added.  You must also pre-declare a non-generic version
  /// of the chosen class, much in the same style as UnityEvent.
  /// </summary>
  public abstract class MultiTypedList<BaseType> {
    public abstract int Count { get; }
    public abstract void Add(BaseType obj);
    public abstract void RemoveAt(int index);
    public abstract BaseType this[int index] { get; set; }

    [Serializable]
    public struct Key {
      public int id;
      public int index;
    }
  }

  [Serializable]
  public class MultiTypedList<BaseType, A, B> : MultiTypedList<BaseType>
    where A : BaseType
    where B : BaseType {

    public List<Key> table = new List<Key>();

    public List<A> a = new List<A>();
    public List<B> b = new List<B>();

    private Dictionary<int, IList> _idToList;

    public override int Count {
      get {
        return table.Count;
      }
    }

    public override void Add(BaseType obj) {
      table.Add(addInternal(obj));
    }

    public override void RemoveAt(int index) {
      var key = table[index];
      table.RemoveAt(index);

      getList(key.id).RemoveAt(key.index);
    }

    public override BaseType this[int index] {
      get {
        Key key = table[index];
        return (BaseType)getList(key.id)[key.index];
      }
      set {
        Key oldKey = table[index];
        getList(oldKey.id).RemoveAt(oldKey.index);

        Key newKey = addInternal(value);
        table[index] = newKey;
      }
    }

    protected Key addHelper(IList list, BaseType instance, int id) {
      Key key = new Key() {
        id = id,
        index = list.Count
      };
      list.Add(instance);
      return key;
    }

    protected virtual Key addInternal(BaseType obj) {
      if (obj is A) {
        return addHelper(a, obj, 0);
      } else if (obj is B) {
        return addHelper(b, obj, 1);
      } else {
        throw new Exception("This multi typed list does not support type " + obj.GetType().Name);
      }
    }

    protected virtual IList getList(int id) {
      if (id == 0) {
        return a;
      } else if (id == 1) {
        return b;
      } else {
        throw new Exception("This multi typed list does not have a list with id " + id);
      }
    }
  }

  public class MultiTypedList<BaseType, A, B, C> : MultiTypedList<BaseType, A, B>
    where A : BaseType
    where B : BaseType
    where C : BaseType {

    public List<C> c = new List<C>();

    protected override Key addInternal(BaseType obj) {
      return obj is C ? addHelper(c, obj, 2) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 2 ? c : base.getList(id);
    }
  }

  public class MultiTypedList<BaseType, A, B, C, D> : MultiTypedList<BaseType, A, B, C>
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType {

    public List<D> d = new List<D>();

    protected override Key addInternal(BaseType obj) {
      return obj is D ? addHelper(d, obj, 3) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 3 ? c : base.getList(id);
    }
  }

  public class MultiTypedList<BaseType, A, B, C, D, E> : MultiTypedList<BaseType, A, B, C, D>
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType
    where E : BaseType {

    public List<E> e = new List<E>();

    protected override Key addInternal(BaseType obj) {
      return obj is E ? addHelper(e, obj, 4) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 4 ? c : base.getList(id);
    }
  }

  public class MultiTypedList<BaseType, A, B, C, D, E, F> : MultiTypedList<BaseType, A, B, C, D, E>
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType
    where E : BaseType
    where F : BaseType {

    public List<F> f = new List<F>();

    protected override Key addInternal(BaseType obj) {
      return obj is F ? addHelper(f, obj, 5) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 5 ? c : base.getList(id);
    }
  }

  public class MultiTypedList<BaseType, A, B, C, D, E, F, G> : MultiTypedList<BaseType, A, B, C, D, E, F>
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType
    where E : BaseType
    where F : BaseType
    where G : BaseType {

    public List<G> g = new List<G>();

    protected override Key addInternal(BaseType obj) {
      return obj is G ? addHelper(g, obj, 6) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 6 ? c : base.getList(id);
    }
  }

  public class MultiTypedList<BaseType, A, B, C, D, E, F, G, H> : MultiTypedList<BaseType, A, B, C, D, E, F, G>
    where A : BaseType
    where B : BaseType
    where C : BaseType
    where D : BaseType
    where E : BaseType
    where F : BaseType
    where G : BaseType
    where H : BaseType {

    public List<H> h = new List<H>();

    protected override Key addInternal(BaseType obj) {
      return obj is H ? addHelper(h, obj, 7) : base.addInternal(obj);
    }

    protected override IList getList(int id) {
      return id == 7 ? c : base.getList(id);
    }
  }
}
