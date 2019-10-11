using System;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    public sealed unsafe class BlobStringLookup<T> : IDisposable
    {
        const int defaultSize = 16;
        
        readonly Dictionary<BlobHandle, T> Dictionary;

        readonly Dictionary<string, BlobString> SourceMap;

        public BlobStringLookup(int initialCapacity = defaultSize)
        {
            Dictionary = new Dictionary<BlobHandle, T>(initialCapacity);
            SourceMap = new Dictionary<string, BlobString>(initialCapacity);
        }
        
        /// <summary>Convert a managed string into a BlobString and add it to the lookup</summary>
        /// <param name="str">The string to add</param>
        /// <param name="value">The value to associate with the key</param>
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Add(string str, T value)
        {
            if (SourceMap.ContainsKey(str)) 
                return;
            
            var blobStr = new BlobString(str);
            Dictionary.Add(blobStr.Handle, value);
            SourceMap.Add(str, blobStr);
        }
        
        /// <summary>Add an already-created BlobString to the lookup</summary>
        /// <param name="blobStr">The blob string to add</param>
        /// <param name="value">The value to associate with the key</param>
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Add(BlobString blobStr, T value)
        {
            Dictionary.Add(blobStr.Handle, value);
        }
        
        /// <summary>Remove a string from the lookup</summary>
        /// <param name="str">The string to remove</param>
        /// <returns>true if the string was found and removed, false otherwise</returns>
        public bool Remove(string str)
        {
            if (!SourceMap.TryGetValue(str, out var blobStr)) 
                return false;

            SourceMap.Remove(str);
            var removed = Dictionary.Remove(blobStr.Handle);
            blobStr.Dispose();
            return removed;
        }

        /// <summary>Remove a blob string from the lookup</summary>
        /// <param name="blobStr">The blob string to remove</param>
        /// <returns>true if the string was found and removed, false otherwise</returns>
        public bool Remove(BlobString blobStr)
        {
            return Dictionary.Remove(blobStr.Handle);
        }
        
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetValueFromBytes(byte* ptr, int byteCount, out T value)
        {
            var tempHandle = new BlobHandle(ptr, byteCount);
            return Dictionary.TryGetValue(tempHandle, out value);
        }
        
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetValueFromBytes(int* ptr, int byteCount, out T value)
        {
            var tempHandle = new BlobHandle(ptr, byteCount);
            return Dictionary.TryGetValue(tempHandle, out value);
        }

        public void Clear()
        {
            Dictionary.Clear();
            SourceMap.Clear();
        }

        public void Dispose()
        {
            foreach (var kvp in SourceMap)
                kvp.Value.Dispose();
        }
    }
}