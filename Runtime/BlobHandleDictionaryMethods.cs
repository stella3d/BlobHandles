using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    public static unsafe class BlobHandleDictionaryMethods
    {
        /// <summary>
        /// Try to find the value associated with a given chunk of bytes
        /// </summary>
        /// <param name="self">The dictionary to look in</param>
        /// <param name="ptr">Pointer to the start of the bytes</param>
        /// <param name="byteCount">The number of bytes to read</param>
        /// <param name="value">The output value</param>
        /// <typeparam name="T">The dictionary value type</typeparam>
        /// <returns>True if the value was found, false otherwise</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueFromBytes<T>(this Dictionary<BlobHandle, T> self, byte* ptr, int byteCount, out T value)
        {
            return self.TryGetValue(new BlobHandle(ptr, byteCount), out value);
        }
        
        /// <summary>
        /// Try to find the value associated with a given chunk of bytes
        /// </summary>
        /// <param name="self">The dictionary to look in</param>
        /// <param name="ptr">Pointer to the start of the bytes</param>
        /// <param name="byteCount">The number of bytes to read</param>
        /// <param name="value">The output value</param>
        /// <typeparam name="T">The dictionary value type</typeparam>
        /// <returns>True if the value was found, false otherwise</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueFromBytes<T>(this Dictionary<BlobHandle, T> self, int* ptr, int byteCount, out T value)
        {
            return self.TryGetValue(new BlobHandle(ptr, byteCount), out value);
        }
    }
}