using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    public sealed unsafe class BlobStringLookup<T> : IDisposable
    {
        const int defaultSize = 16;
        
        public readonly Dictionary<BlobString, T> Dictionary;
        readonly Dictionary<string, IntPtr> SourceMapping;

        int m_MaxByteLengthOfAdded;
        BlobString m_KeyBuffer;
        
        public BlobStringLookup(int initialCapacity = defaultSize)
        {
            Dictionary = new Dictionary<BlobString, T>(initialCapacity);
            SourceMapping = new Dictionary<string, IntPtr>(initialCapacity);
            m_KeyBuffer = new BlobString();
        }
        
        public void Add(string str, T value)
        {
            if (SourceMapping.ContainsKey(str)) return;

            var iStr = new BlobString(str);
            Add(iStr, value);
            SourceMapping.Add(str, new IntPtr(iStr.OriginalPtr));
        }
        
        public void Remove(string str)
        {
            if (!SourceMapping.TryGetValue(str, out var byteIntPtr)) return;

            var searchStrPtr = (int*) byteIntPtr;
            BlobString toRemove = default;
            foreach (var intStr in Dictionary.Keys)
            {
                if (intStr.OriginalPtr == searchStrPtr)
                {
                    toRemove = intStr;
                    break;
                }
            }

            SourceMapping.Remove(str);
            if (toRemove != default)
            {
                Dictionary.Remove(toRemove);
                toRemove.Dispose();
            }
        }

        public void Add(BlobString intStr, T value)
        {
            Dictionary.Add(intStr, value);
            
            if (m_MaxByteLengthOfAdded < intStr.ByteCount)
            {
                m_MaxByteLengthOfAdded = intStr.ByteCount;
                m_KeyBuffer.Dispose();
                var alignedByteCount = (intStr.ByteCount + 3) & ~3;
                m_KeyBuffer = new BlobString(alignedByteCount / 4);
            }
        }
        
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetValueFromBytes(byte* ptr, int byteCount, out T value)
        {
            var kb = m_KeyBuffer;
            // override the pointer & count, which causes all operating to work against the new data
            kb.Ptr = (int*) ptr;
            kb.ByteCount = byteCount;
            // set the hashcode base to the number of 32-bit integers needed to contain the bytes.
            // this performs better as a hashcode than using ByteCount
            kb.HashBase = ((byteCount + 3) & ~3) / 4;
            return Dictionary.TryGetValue(kb, out value);
        }
        
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetValueFromBytes(int* ptr, int byteCount, out T value)
        {
            // override the pointer & count, which causes all operating to work against the new data
            var kb = m_KeyBuffer;
            kb.Ptr = ptr;
            kb.ByteCount = byteCount;
            // set the hashcode base to the number of 32-bit integers needed to contain the bytes.
            // this performs better as a hashcode than using ByteCount
            kb.HashBase = ((byteCount + 3) & ~3) / 4;
            return Dictionary.TryGetValue(kb, out value);
        }

        public void Clear()
        {
            Dictionary.Clear();
            SourceMapping.Clear();
        }

        public void Dispose()
        {
            foreach (var kvp in Dictionary)
                kvp.Key.Dispose();
        }
    }
}