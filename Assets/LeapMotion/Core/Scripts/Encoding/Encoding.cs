

namespace Leap.Unity.Encoding {

  public interface ICodec<DecodedType, EncodedType> {

    void Encode(EncodedType into, DecodedType from);
    void Decode(EncodedType from, DecodedType into);

  }

  public interface IByteCodec<T> {

    int numBytesRequired { get; }

    void ReadBytes(byte[] bytes, ref int offset, T into);
    void FillBytes(byte[] bytes, ref int offset, T from);

  }

  public interface IEncoding<T> {

    void Encode(T from);
    void Decode(T into);

  }

  public interface IByteEncoding {

    int numBytesRequired { get; }

    void ReadBytes(byte[] bytes, ref int offset);
    void FillBytes(byte[] bytes, ref int offset);

  }

  public static class EncodingExtensions {

    public static void ReadBytes<T>(this IByteCodec<T> byteCodec, byte[] bytes, T into) {
      int offset = 0;
      byteCodec.ReadBytes(bytes, ref offset, into);
    }

    public static void FillBytes<T>(this IByteCodec<T> byteCodec, byte[] bytes, T from) {
      int offset = 0;
      byteCodec.FillBytes(bytes, ref offset, from);
    }

  }

}
