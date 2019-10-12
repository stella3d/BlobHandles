using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BlobHandles
{
    /// <summary>
    /// Represents a string as a fixed blob of bytes
    /// </summary>
    public struct BlobString : IDisposable, IEquatable<BlobString>
    {
        /// <summary>
        /// The encoding used to convert to & from strings.
        /// WARNING - Changing this after strings have been encoded will probably lead to errors!
        /// </summary>
        public static Encoding Encoding { get; set; } = Encoding.ASCII;
        
        /// <summary>Stores all of the bytes that represent this string</summary>
        readonly byte[] Bytes; 
        readonly GCHandle BytesGcHandle;
        
        public readonly BlobHandle Handle;

        public int Length => Bytes.Length;
        
        public BlobString(string source)
        {
            var bytes = Encoding.GetBytes(source);
            Bytes = bytes;
            // pin the address of our bytes for the lifetime of this string
            BytesGcHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Handle = new BlobHandle(BytesGcHandle.AddrOfPinnedObject(), bytes.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public bool Equals(BlobString other)
        {
            return Handle.Equals(other.Handle);
        }
        
        public override bool Equals(object obj)
        {
            return obj is BlobString other && Handle.Equals(other.Handle);
        }

        public static bool operator ==(BlobString l, BlobString r)
        {
            return l.Handle == r.Handle;
        }

        public static bool operator !=(BlobString l, BlobString r)
        {
            return l.Handle != r.Handle;
        }
        
        public override unsafe string ToString()
        {
            return Encoding.GetString(Handle.Pointer, Handle.Length);
        }
        
        public void Dispose()
        {
            if(BytesGcHandle.IsAllocated) BytesGcHandle.Free();
        }
    }
}
