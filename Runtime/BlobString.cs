using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

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
        public static Encoding Encoding { get; private set; } = Encoding.ASCII;
        
        internal readonly int[] Bytes;
        internal int ByteCount;
        internal int HashBase;

        GCHandle m_BytesHandle;
        
        /// <summary>Always points to Bytes - used to restore to original state & as a lookup key</summary>
        public readonly int* OriginalPtr;
        /// <summary>The active pointer used for all operations</summary>
        internal int* Ptr;
        
        public BlobString(string source)
        {
            var bytes = Encoding.GetBytes(source);
            var alignedByteCount = (bytes.Length + 3) & ~3;
            ByteCount = bytes.Length;
            Bytes = new int[alignedByteCount / intSize];
            Buffer.BlockCopy(bytes, 0, Bytes, 0, bytes.Length);
            // pin the address of our bytes for the lifetime of this object
            m_BytesHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            OriginalPtr = (int*) m_BytesHandle.AddrOfPinnedObject();
            Ptr = OriginalPtr;
            HashBase = Bytes.Length;
        }
        
        public BlobString(BlobString copySource, int sourceByteOffset = 0)
        {
            var alignedByteLength = (copySource.ByteCount + 3) & ~3;
            Bytes = new int[alignedByteLength / intSize];
            ByteCount = copySource.ByteCount;
            Buffer.BlockCopy(copySource.Bytes, sourceByteOffset, Bytes, 0, copySource.ByteCount);
            // pin the address of our bytes for the lifetime of this object
            m_BytesHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            OriginalPtr = (int*) m_BytesHandle.AddrOfPinnedObject();
            Ptr = OriginalPtr;
            HashBase = Bytes.Length;
        }
        
        public BlobString(byte[] bytes)
        {
            var alignedByteLength = (bytes.Length + 3) & ~3;
            ByteCount = bytes.Length;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(bytes, 0, Bytes, 0, bytes.Length);
            // pin the address of our bytes for the lifetime of this object
            m_BytesHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            OriginalPtr = (int*) m_BytesHandle.AddrOfPinnedObject();
            Ptr = OriginalPtr;
            HashBase = Bytes.Length;
        }
        
        public BlobString(byte[] bytes, int byteLength, int offset = 0)
        {
            var alignedByteLength = (byteLength + 3) & ~3;
            ByteCount = bytes.Length;
            Bytes = new int[alignedByteLength / intSize];
            Buffer.BlockCopy(bytes, offset, Bytes, 0, byteLength);
            // pin the address of our bytes for the lifetime of this object
            m_BytesHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            OriginalPtr = (int*) m_BytesHandle.AddrOfPinnedObject();
            Ptr = OriginalPtr;
            HashBase = Bytes.Length;
        }
        
        internal BlobString(int intCapacity)
        {
            ByteCount = 0;
            Bytes = new int[intCapacity];
            m_BytesHandle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            OriginalPtr = (int*) m_BytesHandle.AddrOfPinnedObject();
            Ptr = OriginalPtr;
            HashBase = Bytes.Length;
        }
        
        public BlobString(byte* pointer, int byteLength)
        {
            Bytes = null;
            ByteCount = byteLength;
            HashBase = ((byteLength + 3) & ~3) / 4;
            Ptr = (int*) pointer;
            OriginalPtr = Ptr;
        }
        
        public BlobString(int* pointer, int byteLength)
        {
            Bytes = null;
            ByteCount = byteLength;
            HashBase = ((byteLength + 3) & ~3) / 4;
            Ptr = pointer;
            OriginalPtr = Ptr;
        }

        public override string ToString()
        {
            return Encoding.GetString((byte*) Ptr, ByteCount);
        }

        public void SetBytes(byte[] bytes, int offset, int byteLength)
        {
            var alignedByteCount = (byteLength + 3) & ~3;
            if (alignedByteCount / intSize != Bytes.Length)
            {
                Debug.LogError("Tried to set managed int string from bytes, " + 
                               $"but byte length of {byteLength} does not match int length {Bytes.Length}");
                return;
            }

            // clear trailing bytes
            if (ByteCount != byteLength)
                Bytes[Bytes.Length - 1] = 0;  
            
            ByteCount = byteLength;
            Buffer.BlockCopy(bytes, offset, Bytes, 0, byteLength);
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public void SetBytesUnchecked(byte[] bytes, int offset, int byteLength)
        {
            // clear trailing bytes
            if (ByteCount < byteLength)
                Bytes[Bytes.Length - 1] = 0;  
            
            ByteCount = byteLength;
            Buffer.BlockCopy(bytes, offset, Bytes, 0, byteLength);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public void SetPointer(byte* bytes, int byteLength)
        {
            // if we have more trailing bytes after setting, that means we'd be left with junk.
            // since trailing bytes are always in the last int, just set it to 0 to clear that part before we copy.
            HashBase = ((byteLength + 3) & ~3) / 4;
            ByteCount = byteLength;
            Ptr = (int*) bytes;
        }
        
        public void Dispose()
        {
            if(m_BytesHandle.IsAllocated) m_BytesHandle.Free();
        }

        /// <summary>Restore this blob's original data, in case of any mutation since creation</summary>
        public void Reset()
        {
            Ptr = OriginalPtr;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                // there may be non-zero values beyond the ending byte & we're reading as an int*,
                // so we need to ignore the last 3 bytes
                var lastValueByte = *(Ptr + HashBase - 1) & 0x00FFFFFF;
                return HashBase ^ 397 + lastValueByte;
            }
        }
        
        /// <summary>
        /// DO NOT CALL if instances have already been created! Changes the encoding used for byte representations. 
        /// Changing the encoding after instances have already been encoded will likely produce unexpected behavior.
        /// </summary>
        /// <param name="newEncoding">The new encoding to use</param>
        public static void SetEncoding(Encoding newEncoding)
        {
            Encoding = newEncoding;
        }
        
        // comparing bytes using memcmp has shown to be several times faster than any other method i've found
        [DllImport("msvcrt.dll", EntryPoint = "memcmp")]
        static extern int MemoryCompare(void* ptr1, void* ptr2, UIntPtr count);

        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BlobString other)
        {
            if (other.ByteCount != ByteCount) return false;
            return MemoryCompare(Ptr, other.Ptr, (UIntPtr) ByteCount) == 0;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && Equals((BlobString) obj);
        }

        public static bool operator ==(BlobString l, BlobString r)
        {
            var countAsUintPtr = (UIntPtr) r.ByteCount;
            return l.ByteCount == r.ByteCount && MemoryCompare(l.Ptr, r.Ptr, countAsUintPtr) == 0;
        }

        public static bool operator !=(BlobString l, BlobString r)
        {
            return l.ByteCount != r.ByteCount || MemoryCompare(l.Ptr, r.Ptr, (UIntPtr) r.ByteCount) != 0;
        }
        
        // fallback for equality checks if we can't use memcmp
        bool FallbackEquals(BlobString other)
        {
            if (other.ByteCount != ByteCount) return false;
            
            if (Bytes.Length % 2 == 0)
            {
                fixed (int* otherPtr = other.Bytes)
                    for (int i = 0; i < Bytes.Length; i += 2)
                        if (*(ulong*) (Ptr + i) != *(ulong*) (otherPtr + i)) 
                            return false;
            }
            else
            {
                fixed (int* otherPtr = other.Bytes)
                    for (int i = 0; i < Bytes.Length; i++)
                        if (*(Ptr + i) != *(otherPtr + i)) 
                            return false;
            }

            return true;
        }

        // this is for debugging
        public int FirstByteDifferenceIndex(BlobString other)
        {
            if (other.ByteCount != ByteCount) return -1;

            var selfBytePtr = (byte*) Ptr;
            var otherBytePtr = (byte*) other.Ptr;
            
            for (int i = 0; i < ByteCount; i++)
            {
                var selfByte = *(selfBytePtr + i);
                var otherByte = *(otherBytePtr + i);
                
                if (selfByte != otherByte)
                    return i;
            }

            return -1;
        }

    }
}

