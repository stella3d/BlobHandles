using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    /// <summary>
    /// Designed to allow efficient matching of strings received as bytes (such as from network or disk) to values.
    /// </summary>
    /// <typeparam name="T">The type to associate a string key with</typeparam>
    public sealed unsafe class BlobStringDictionary<T> : IDisposable
    {
        const int defaultSize = 16;
        
        readonly Dictionary<BlobHandle, T> Dictionary;

        // map from a managed string to its blob representation
        readonly Dictionary<string, BlobString> SourceMap;

        public BlobStringDictionary(int initialCapacity = defaultSize)
        {
            Dictionary = new Dictionary<BlobHandle, T>(initialCapacity);
            SourceMap = new Dictionary<string, BlobString>(initialCapacity);
        }
        
        /// <summary>Converts a string into a BlobString and adds it and the value to the dictionary</summary>
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
        
        /// <summary>Adds a BlobString and its associated value to the dictionary</summary>
        /// <param name="blobStr">The blob string to add</param>
        /// <param name="value">The value to associate with the key</param>
        [Il2CppSetOption(Option.NullChecks, false)]
        public void Add(BlobString blobStr, T value)
        {
            Dictionary.Add(blobStr.Handle, value);
        }
        
        /// <summary>Removes the value with the specified key</summary>
        /// <param name="str">The string to remove</param>
        /// <returns>true if the string was found and removed, false otherwise</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool Remove(string str)
        {
            if (!SourceMap.TryGetValue(str, out var blobStr)) 
                return false;

            SourceMap.Remove(str);
            var removed = Dictionary.Remove(blobStr.Handle);
            blobStr.Dispose();
            return removed;
        }

        /// <summary>Removes the value with the specified key</summary>
        /// <param name="blobStr">The blob string to remove</param>
        /// <returns>true if the string was found and removed, false otherwise</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool Remove(BlobString blobStr)
        {
            return Dictionary.Remove(blobStr.Handle);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        public bool TryGetValueFromBytes(byte* ptr, int byteCount, out T value)
        {
            var tempHandle = new BlobHandle(ptr, byteCount);
            return Dictionary.TryGetValue(tempHandle, out value);
        }

        [Il2CppSetOption(Option.NullChecks, false)]
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