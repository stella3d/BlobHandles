using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BlobHandles
{
    /// <summary>
    /// Wraps an arbitrary chunk of bytes in memory, so it can be used as a hash key
    /// and compared against other instances of the same set of bytes 
    /// </summary>
    public unsafe struct BlobHandle : IEquatable<BlobHandle>
    {
        /// <summary>A pointer to the start of the blob</summary>
        public readonly byte* Pointer;
        /// <summary>The number of bytes in the blob</summary>
        public readonly int ByteLength;

        public BlobHandle(byte* pointer, int byteLength)
        {
            Pointer = pointer;
            ByteLength = byteLength;
        }
        
        public BlobHandle(IntPtr pointer, int byteLength)
        {
            Pointer = (byte*) pointer;
            ByteLength = byteLength;
        }
        
        /// <summary>
        /// Get a blob handle for a byte array. WARNING - the byte array must have its address pinned to work safely!
        /// If not pinned, it will work unless the the runtime decides to move the array.
        /// </summary>
        /// <param name="bytes">The bytes to get a handle to</param>
        public BlobHandle(byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                Pointer = ptr;
                ByteLength = bytes.Length;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlobHandle other)
        {
            return ByteLength == other.ByteLength && 
                   MemoryCompare(Pointer, other.Pointer, (UIntPtr) ByteLength) == 0;
        }
        
        public override bool Equals(object obj)
        {
            return obj is BlobHandle other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BlobHandle left, BlobHandle right)
        {
            return left.ByteLength == right.ByteLength && 
                   MemoryCompare(left.Pointer, right.Pointer, (UIntPtr) left.ByteLength) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BlobHandle left, BlobHandle right)
        {
            return left.ByteLength != right.ByteLength || 
                   MemoryCompare(left.Pointer, right.Pointer, (UIntPtr) left.ByteLength) != 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                var lastValueByte = *(Pointer + ByteLength - 1);
                return ByteLength * 397 ^ lastValueByte;
            }
        }
        
        public override string ToString()
        {
            return $"{ByteLength.ToString()} bytes @ {new IntPtr(Pointer).ToString()}";
        }
                
        // comparing bytes using memcmp has shown to be several times faster than any other method i've found
        [DllImport("msvcrt.dll", EntryPoint = "memcmp")]
        static extern int MemoryCompare(void* ptr1, void* ptr2, UIntPtr count);
    }
}

