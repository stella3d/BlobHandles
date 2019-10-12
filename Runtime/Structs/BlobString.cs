using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BlobHandles
{
    /// <summary>
    /// Store a string as a series of 32-bit integers.
    /// Essentially, a trick to allow using chunks of bytes as fast dictionary keys in place of strings
    /// </summary>
    public struct BlobString : IDisposable, IEquatable<BlobString>
    {
        const int intSize = 4;
        
        /// <summary>
        /// The encoding used to convert to & from strings.
        /// WARNING - Changing this after startup may lead to errors in string conversions!
        /// </summary>
        public static Encoding Encoding { get; set; } = Encoding.ASCII;
        
        /// <summary>Stores all of the bytes that represent this string</summary>
        readonly int[] Bytes; 
        readonly GCHandle BytesGcHandle;
        
        public readonly BlobHandle Handle;

        public int Length => Handle.ByteLength;
        
        public BlobString(string source)
        {
            var bytes = Encoding.GetBytes(source);
            var alignedByteCount = (bytes.Length + 3) & ~3;
            Bytes = new int[alignedByteCount / intSize];
            Buffer.BlockCopy(bytes, 0, Bytes, 0, bytes.Length);
            // pin the address of our bytes for the lifetime of this string
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle(BytesGcHandle.AddrOfPinnedObject(), bytes.Length);
        }
        
        public BlobString(byte[] bytes)
        {
            var alignedByteLength = (bytes.Length + 3) & ~3;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(bytes, 0, Bytes, 0, bytes.Length);
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle(BytesGcHandle.AddrOfPinnedObject(), bytes.Length);
        }
        
        public BlobString(byte[] bytes, int byteLength, int offset = 0)
        {
            var end = offset + byteLength - 1;
            if (end >= bytes.Length)
            {
                throw new ArgumentOutOfRangeException
                    ($"Offset + length - 1 = {end} is beyond byte[] length {bytes.Length}");
            }

            var alignedByteLength = (byteLength + 3) & ~3;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(bytes, offset, Bytes, 0, byteLength);
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle(BytesGcHandle.AddrOfPinnedObject(), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlobString other)
        {
            return Handle.Equals(other.Handle);
        }
        
        public override bool Equals(object obj)
        {
            return obj is BlobString other && Handle.Equals(other.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BlobString l, BlobString r)
        {
            return l.Handle == r.Handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BlobString l, BlobString r)
        {
            return l.Handle != r.Handle;
        }
        
        public override unsafe string ToString()
        {
            return Encoding.GetString(Handle.Pointer, Handle.ByteLength);
        }
        
        public void Dispose()
        {
            if(BytesGcHandle.IsAllocated) BytesGcHandle.Free();
        }
    }
}
