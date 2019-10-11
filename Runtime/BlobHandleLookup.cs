using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace BlobHandles
{
    public static unsafe class BlobLookupMethods
    {
        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueFromBytes<T>(this Dictionary<BlobHandle, T> self, byte* ptr, int byteCount, out T value)
        {
            return self.TryGetValue(new BlobHandle(ptr, byteCount), out value);
        }
        
        [Il2CppSetOption(Option.NullChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueFromBytes<T>(this Dictionary<BlobHandle, T> self, int* ptr, int byteCount, out T value)
        {
            return self.TryGetValue(new BlobHandle(ptr, byteCount), out value);
        }
    }
}