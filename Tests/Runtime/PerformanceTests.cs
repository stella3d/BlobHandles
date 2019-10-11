using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BlobHandles.Tests;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BlobHandles.Tests
{
    public class PerformanceTests
    {
        static readonly Stopwatch k_Stopwatch = new Stopwatch();
        
        public int StringCount = 1001;
        
        public int MinLength = 20;
        public int MaxLength = 100;
        
        string[] m_Strings;

        const string newlineChar = "\n";
        readonly byte[] m_NewLineBytes = Encoding.UTF8.GetBytes(newlineChar);

        public string RuntimeLog { get; set; }

        FileStream m_File;

        public void BeforeAll()
        {
            // init state to make sure that we always get the same test results
            Random.InitState(303);
            m_Strings = TestData.RandomStringsWithPrefix("/composition", StringCount, MinLength, MaxLength);
            m_File = new FileStream(RuntimeLog, FileMode.OpenOrCreate);
            WriteEnvironmentInfo();
        }

        // write out automatic compiler & environment info
        void WriteEnvironmentInfo()
        {
            m_File.Write(m_NewLineBytes, 0, 0);
            var versionBytes = Encoding.ASCII.GetBytes(Application.unityVersion);
            m_File.Write(versionBytes, 0, 0);
#if UNITY_EDITOR
            var editorBytes = Encoding.ASCII.GetBytes("Editor,");
            m_File.Write(editorBytes, 0, 0);
#endif
#if ENABLE_IL2CPP
            var runtimeBytes = Encoding.ASCII.GetBytes("IL2CPP");
#else
            var runtimeBytes = Encoding.ASCII.GetBytes(" Mono");
#endif
            m_File.Write(runtimeBytes, 0, 0);
            m_File.Write(m_NewLineBytes, 0, 0);
        }

        public void AfterAll()
        {
            m_File.Close();
        }
        
        void WriteLog(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            m_File.Write(bytes, 0, bytes.Length);
            m_File.Write(m_NewLineBytes, 0, m_NewLineBytes.Length);
        }

        public void StringEquals_ManagedIntString()
        {
            var searchForIndex = StringCount / 4;
            var searchString = m_Strings[searchForIndex];
            var searchIntString = new BlobString(searchString);
            
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);
            
            bool eql;
            // force the jit to compile the equals methods
            foreach (var str in m_Strings)
                eql = searchString == str;
            foreach (var intString in intStrings)
                eql = searchIntString == intString;
            
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

            WriteLog($"elements {searchForIndex} Equals(), str {strTicks}, intString  {intStrTicks}");
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        public void GetHashCode_ManagedIntString()
        {
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);

            // force jit to compile hashcode method            
            foreach (var intString in intStrings)
            {
                var hc = intString.GetHashCode();
            }
            
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

            WriteLog($"elements {m_Strings.Length} GetHashCode(), str {strTicks}, intString {intStrTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        public void DictionaryTryGetValue_ManagedIntString()
        {
            var intStrings = new BlobString[m_Strings.Length];
            for (int i = 0; i < m_Strings.Length; i++)
                intStrings[i] = new BlobString(m_Strings[i]);

            var strDict = new Dictionary<string, int>(m_Strings.Length);
            for (var i = 0; i < m_Strings.Length; i++)
                strDict.Add(m_Strings[i], i);

            var intStrDict = new Dictionary<BlobString, int>(intStrings.Length);
            for (var i = 0; i < intStrings.Length; i++)
                intStrDict.Add(intStrings[i], i);
            
            foreach (var intString in intStrings)
            {
                intStrDict.TryGetValue(intString, out var index);
            }
            
            k_Stopwatch.Restart();
            foreach (var str in m_Strings)
            {
                strDict.TryGetValue(str, out var index);
            }
            k_Stopwatch.Stop();
            var strTicks = k_Stopwatch.ElapsedTicks;

            k_Stopwatch.Restart();
            foreach (var intString in intStrings)
            {
                intStrDict.TryGetValue(intString, out var index);
            }
            k_Stopwatch.Stop();
            var intStrTicks = k_Stopwatch.ElapsedTicks;

            WriteLog($"elements {m_Strings.Length} Dictionary.TryGetValue, str {strTicks}, intString {intStrTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        public unsafe void DictionaryTryGetValue_BlobHandles()
        {
            var bHandles = new BlobHandle[m_Strings.Length];
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var b = Encoding.ASCII.GetBytes(m_Strings[i]);
                bytes[i] = b;

                fixed (byte* bPtr = b)
                {
                    bHandles[i] = new BlobHandle(bPtr, b.Length);
                }
            }

            var bDict = new Dictionary<BlobHandle, int>(m_Strings.Length);
            for (var i = 0; i < m_Strings.Length; i++)
            {
                bDict.Add(bHandles[i], i);
            }

            // force jit compilation
            bDict.TryGetValue(bHandles[0], out var bJitValue);
            
            k_Stopwatch.Restart();
            foreach (var bh in bHandles)
            {
                k_Stopwatch.Start();
                bDict.TryGetValue(bh, out var value);
                k_Stopwatch.Stop();
            }
            var bTicks = k_Stopwatch.ElapsedTicks;

            WriteLog($"{m_Strings.Length} count, Dictionary.TryGetValue() w/ BlobHandle key {bTicks}");
        }
        
        public unsafe void DictionaryExtension_TryGetValueFromBytes()
        {
            var bHandles = new BlobHandle[m_Strings.Length];
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var b = Encoding.ASCII.GetBytes(m_Strings[i]);
                bytes[i] = b;

                fixed (byte* bPtr = b)
                {
                    bHandles[i] = new BlobHandle(bPtr, b.Length);
                }
            }

            var bDict = new Dictionary<BlobHandle, int>(m_Strings.Length);
            for (var i = 0; i < m_Strings.Length; i++)
            {
                bDict.Add(bHandles[i], i);
            }

            // force jit compilation
            fixed (byte* bPtr = bytes[0])
            {
                bDict.TryGetValueFromBytes(bPtr, bytes[0].Length, out var bJitValue);
            }
            
            k_Stopwatch.Restart();
            foreach (var bh in bHandles)
            {
                k_Stopwatch.Start();
                bDict.TryGetValueFromBytes(bh.Pointer, bh.ByteLength, out var bValue);
                k_Stopwatch.Stop();
            }
            var bTicks = k_Stopwatch.ElapsedTicks;

            WriteLog($"{m_Strings.Length} count, Dictionary.TryGetValueFromBytes() w/ BlobHandle<byte*> {bTicks}");
        }
        
        public unsafe void GetAsciiStringFromBytes()
        {
            var jitAsciiStr = Encoding.ASCII.GetString(new byte[0]);
            var jitUtf8Str = Encoding.UTF8.GetString(new byte[0]);
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                bytes[i] = Encoding.ASCII.GetBytes(m_Strings[i]);
            }
            
            k_Stopwatch.Restart();
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var b = bytes[i];
                k_Stopwatch.Start();
                var str = Encoding.ASCII.GetString(b);
                k_Stopwatch.Stop();
            }
            
            WriteLog($"Encoding.ASCII.GetString(bytes), {k_Stopwatch.ElapsedTicks}");
            
            k_Stopwatch.Restart();
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var b = bytes[i];
                k_Stopwatch.Start();
                var str = Encoding.UTF8.GetString(b);
                k_Stopwatch.Stop();
            }
            
            WriteLog($"Encoding.UTF8.GetString(bytes), {k_Stopwatch.ElapsedTicks}");
        }
        
        public unsafe void IntStringLookup_TryGetValueFromBytes()
        {
            var intStrings = new BlobString[m_Strings.Length];
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var str = m_Strings[i];
                intStrings[i] = new BlobString(str);
                var b = Encoding.ASCII.GetBytes(str);
                bytes[i] = b;
            }
            
            var lookup = new BlobStringLookup<int>();
            for (int i = 0; i < intStrings.Length; i++)
                lookup.Add(intStrings[i], i);

            // force JIT compilation of the relevant methods
            fixed (byte* dummyPtr = bytes[0])
            {
                lookup.TryGetValueFromBytes(dummyPtr, bytes[0].Length, out var value);
                lookup.TryGetValueFromBytes((int*)dummyPtr, bytes[0].Length, out value);
            }

            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                fixed (byte* byteStrPtr = byteStr)
                {
                    k_Stopwatch.Start();

                    lookup.TryGetValueFromBytes(byteStrPtr, byteStr.Length, out var value);

                    k_Stopwatch.Stop();
                }
            }

            WriteLog($"TryGetValueFromBytes(byte* ) ticks: {k_Stopwatch.ElapsedTicks}");
            
            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                fixed (byte* byteStrPtr = byteStr)
                {
                    var iPtr = (int*) byteStrPtr;
                    k_Stopwatch.Start();

                    lookup.TryGetValueFromBytes(iPtr, byteStr.Length, out var value);

                    k_Stopwatch.Stop();
                }
            }
            
            WriteLog($"TryGetValueFromBytes(int* ) ticks: {k_Stopwatch.ElapsedTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
    }
}
