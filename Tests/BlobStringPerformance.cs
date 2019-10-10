using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Debug = UnityEngine.Debug;
using BlobHandles;

namespace BlobHandles.Tests
{
    public class PerformanceTests
    {
        /*
        public int StringCount = 1001;
        
        public int MinLength = 20;
        public int MaxLength = 200;
        
        string[] m_SmallerStrings;
        string[] m_Strings;

        
        NativeArray<int> m_Result;

        static readonly Stopwatch k_Stopwatch = new Stopwatch();

        [OneTimeSetUp]
        public void BeforeAll()
        {
            m_Result = new NativeArray<int>(1, Allocator.Persistent);
            m_SmallerStrings = TestData.RandomStrings(10, MinLength, MaxLength);
            
            m_Strings = TestData.RandomStringsWithPrefix("/composition", StringCount, MinLength, MaxLength);
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            m_Result.Dispose();

        }

        [Test]
        public void StringEquals_ManagedIntString()
        {
            var searchForIndex = StringCount / 4;
            var searchString = m_Strings[searchForIndex];
            var searchIntString = new BlobString(searchString);
            
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);

            bool eql;
            k_Stopwatch.Restart();
            foreach (var str in m_Strings)
            {
                eql = searchString == str;
            }
            k_Stopwatch.Stop();
            var strTicks = k_Stopwatch.ElapsedTicks;
            
            k_Stopwatch.Restart();
            foreach (var intString in intStrings)
            {
                eql = searchIntString == intString;
            }
            k_Stopwatch.Stop();
            var intStrTicks = k_Stopwatch.ElapsedTicks;

            Debug.Log($"elements {searchForIndex} Equals(), str {strTicks}, intString  {intStrTicks}");
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        [Test]
        public void GetHashCode_ManagedIntString()
        {
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);

            int hashCode;
            k_Stopwatch.Restart();
            foreach (var str in m_Strings)
            {
                hashCode = str.GetHashCode();
            }
            k_Stopwatch.Stop();
            var strTicks = k_Stopwatch.ElapsedTicks;
            
            k_Stopwatch.Restart();
            foreach (var intString in intStrings)
            {
                hashCode = intString.GetHashCode();
            }
            k_Stopwatch.Stop();
            var intStrTicks = k_Stopwatch.ElapsedTicks;

            Debug.Log($"elements {m_Strings.Length} GetHashCode(), str {strTicks}, intString {intStrTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        [Test]
        public void DictionaryTryGetValue_ManagedIntString()
        {
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);

            var strDict = new Dictionary<string, int>(m_Strings.Length);
            for (var i = 0; i < m_Strings.Length; i++)
                strDict.Add(m_Strings[i], i);

            k_Stopwatch.Restart();
            foreach (var str in m_Strings)
            {
                strDict.TryGetValue(str, out var index);
            }
            k_Stopwatch.Stop();
            var strTicks = k_Stopwatch.ElapsedTicks;
            
            var intStrDict = new Dictionary<BlobString, int>(intStrings.Length);
            for (var i = 0; i < intStrings.Length; i++)
                intStrDict.Add(intStrings[i], i);
            
            k_Stopwatch.Restart();
            foreach (var intString in intStrings)
            {
                intStrDict.TryGetValue(intString, out var index);
            }
            k_Stopwatch.Stop();
            var intStrTicks = k_Stopwatch.ElapsedTicks;

            Debug.Log($"elements {m_Strings.Length} Dictionary.TryGetValue, str {strTicks}, intString {intStrTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        [Test]
        public unsafe void ManagedIntString_SetFromBytes()
        {
            var intStrings = new BlobString[m_Strings.Length];
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var str = m_Strings[i];
                intStrings[i] = new BlobString(str);
                bytes[i] = Encoding.ASCII.GetBytes(str);
            }

            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                var intStr = intStrings[i];
                k_Stopwatch.Start();
                intStr.SetBytes(byteStr, 0, byteStr.Length);
                k_Stopwatch.Stop();
            }

            var checkedTicks = k_Stopwatch.ElapsedTicks;
            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                var intStr = intStrings[i];
                k_Stopwatch.Start();
                intStr.SetBytesUnchecked(byteStr, 0, byteStr.Length);
                k_Stopwatch.Stop();
            }

            var unCheckedTicks = k_Stopwatch.ElapsedTicks;
            
            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                var intStr = intStrings[i];
                fixed (byte* bsPtr = &byteStr[0])
                {
                    k_Stopwatch.Start();
                    intStr.SetBytesMemCpy(bsPtr, byteStr.Length);
                    k_Stopwatch.Stop();
                }
            }

            var memCpyTicks = k_Stopwatch.ElapsedTicks;
            
            Debug.Log($"count {m_Strings.Length}, SetBytes(), checked {checkedTicks}, unchecked {unCheckedTicks}, memcpy {memCpyTicks}");
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        [Test]
        public unsafe void IntStringLookup_TryGetValueFromBytes()
        {
            var intStrings = new BlobString[m_Strings.Length];
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var str = m_Strings[i];
                intStrings[i] = new BlobString(str);
                bytes[i] = Encoding.ASCII.GetBytes(str);
            }
            
            var lookup = new BlobStringLookup<int>();
            for (int i = 0; i < intStrings.Length; i++)
                lookup.Add(intStrings[i], i);

            k_Stopwatch.Reset();
            foreach (var byteStr in bytes)
            {
                fixed (byte* byteStrPtr = byteStr)
                {
                    k_Stopwatch.Start();

                    lookup.TryGetValueFromBytes(byteStrPtr, byteStr.Length, out var value);
                
                    k_Stopwatch.Stop();
                }
            }

            var ncTicks = k_Stopwatch.ElapsedTicks;
            Debug.Log($"count {m_Strings.Length}, TryGetValueFromBytes() time in ticks, {ncTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        */
    }
}