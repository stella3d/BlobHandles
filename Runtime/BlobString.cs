using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    /// <summary>
    /// Store a string as a series of 32-bit integers.
    /// Essentially, a trick to allow using chunks of bytes as fast dictionary keys in place of strings
    /// </summary>
    public unsafe struct BlobString : IDisposable, IEquatable<BlobString>
    {
        const int intSize = 4;
        
        /// <summary>
        /// The encoding used to convert to & from strings.
        /// WARNING - Changing this after startup may lead to errors in string conversions!
        /// </summary>
        public static Encoding Encoding { get; set; } = Encoding.ASCII;
        
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
            // pin the address of our bytes for the lifetime of this object
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle((int*) BytesGcHandle.AddrOfPinnedObject(), bytes.Length);
        }
        
        public BlobString(BlobString copySource, int sourceByteOffset = 0)
        {
            var handle = copySource.Handle;
            var alignedByteLength = (handle.ByteLength + 3) & ~3;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(copySource.Bytes, sourceByteOffset, Bytes, 0, handle.ByteLength);
            // pin the address of our bytes for the lifetime of this object
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle((int*) BytesGcHandle.AddrOfPinnedObject(), handle.ByteLength);
        }
        
        public BlobString(byte[] bytes)
        {
            this = new BlobString(bytes, bytes.Length);
        }
        
        public BlobString(byte[] bytes, int byteLength, int offset = 0)
        {
            var alignedByteLength = (byteLength + 3) & ~3;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(bytes, offset, Bytes, 0, byteLength);
            // pin the address of our bytes for the lifetime of this object
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle((int*) BytesGcHandle.AddrOfPinnedObject(), byteLength);
        }

        public override string ToString()
        {
            return Encoding.GetString(Handle.Pointer, Handle.ByteLength);
        }

        public void Dispose()
        {
            if(BytesGcHandle.IsAllocated) BytesGcHandle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlobString other)
        {
            return Handle.Equals(other.Handle);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && Equals((BlobString) obj);
        }

        public static bool operator ==(BlobString l, BlobString r)
        {
            return l.Handle == r.Handle;
        }

        public static bool operator !=(BlobString l, BlobString r)
        {
            return l.Handle != r.Handle;
        }

    }
}

