using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BlobHandles.Tests;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BlobHandles
{
    public class PerfTestRunner : MonoBehaviour
    {
        RuntimePerformanceTests m_Tests;

        int m_StartFrame;

        string logFile = "perflog.txt";

        public void Start()
        {
            m_Tests = new RuntimePerformanceTests();
            m_Tests.RuntimeLog = Path.Combine(Application.dataPath, logFile);
            m_Tests.BeforeAll();
            m_StartFrame = Time.frameCount;
        }
        
        void Update()
        {
            var frame = Time.frameCount - m_StartFrame;

            switch (frame)
            {
                case 20:
                    m_Tests.StringEquals_ManagedIntString();
                    break;
                case 40:
                    m_Tests.GetHashCode_ManagedIntString();
                    break;
                case 60:
                    m_Tests.DictionaryTryGetValue_ManagedIntString();
                    break;
                case 80:
                    m_Tests.ManagedIntString_SetFromBytes();
                    break;
                case 100:
                    m_Tests.IntStringLookup_TryGetValueFromBytes();
                    break;
                case 120:
                    m_Tests.IntStringLookup_SetPointerWrapper();
                    break;
                case 140:
                    m_Tests.ManagedString_GetBytes();
                    break;
                case 145:
                    m_Tests.AfterAll();
                    enabled = false;
                    break;
            }
        }
    }
}

namespace BlobHandles.Tests
{
    public class RuntimePerformanceTests
    {
        static readonly Stopwatch k_Stopwatch = new Stopwatch();
        
        public int StringCount = 1001;
        
        public int MinLength = 20;
        public int MaxLength = 100;
        
        string[] m_Strings;

        GCHandle[] m_GcHandles;
        
        const string newlineChar = "\n";
        readonly byte[] m_NewLineBytes = Encoding.UTF8.GetBytes(newlineChar);

        public string RuntimeLog { get; set; }

        FileStream m_File;

        public void BeforeAll()
        {
            // init state to make sure that we always get the same test results
            Random.InitState(303);
            m_Strings = RandomStringsWithPrefix("/composition", StringCount, MinLength, MaxLength);
            m_File = new FileStream(RuntimeLog, FileMode.Create);
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

        public void AfterEach()
        {
            if (m_GcHandles != null)
            {
                foreach (var handle in m_GcHandles)
                {
                    if(handle.IsAllocated)
                        handle.Free();
                }
            }
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
            
            WriteLog($"count {m_Strings.Length}, SetBytes(), checked {checkedTicks}, unchecked {unCheckedTicks}");
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        public unsafe void ManagedString_GetBytes()
        {
            var bytes = new byte[m_Strings.Length][];
            for (int i = 0; i < m_Strings.Length; i++)
            {
                var str = m_Strings[i];
                k_Stopwatch.Start();
                bytes[i] = Encoding.ASCII.GetBytes(str);
                k_Stopwatch.Stop();
            }
            
            WriteLog($"count {m_Strings.Length}, encoding.ascii.getBytes(), {k_Stopwatch.ElapsedTicks}");
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
        
        public unsafe void IntStringLookup_SetPointerWrapper()
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

            // force jit compilation
            fixed (byte* dummyPtr = bytes[0])
            {
                lookup.SetPointerWrapper(dummyPtr, bytes[0].Length);
                var iPtr = (int*) dummyPtr;
                lookup.SetPointerWrapper(iPtr, bytes[0].Length);
            }
            
            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                fixed (byte* byteStrPtr = byteStr)
                {
                    k_Stopwatch.Start();

                    lookup.SetPointerWrapper(byteStrPtr, byteStr.Length);

                    k_Stopwatch.Stop();
                }
            }

            WriteLog($"count {m_Strings.Length}, lookup.SetPointerWrapper() time in ticks, {k_Stopwatch.ElapsedTicks}");
            
            k_Stopwatch.Reset();
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteStr = bytes[i];
                fixed (byte* byteStrPtr = byteStr)
                {
                    var iPtr = (int*) byteStrPtr;
                    k_Stopwatch.Start();

                    lookup.SetPointerWrapper(iPtr, byteStr.Length);

                    k_Stopwatch.Stop();
                }
            }
            
            WriteLog($"count {m_Strings.Length}, lookup.SetIntPointerWrapper() time in ticks, {k_Stopwatch.ElapsedTicks}");
            
            foreach (var t in intStrings)
                t.Dispose();
        }
        
        // helps test performance impact of strings that share a common beginning
        public static string[] RandomStringsWithPrefix(string prefix, int count, int stringLengthMin, int stringLengthMax)
        {
            var strings = new string[count];
            for (int i = 0; i < strings.Length; i++)
                strings[i] = prefix + RandomString(stringLengthMin, stringLengthMax);

            return strings;
        }

        public static string RandomString(int minLength, int maxLength)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ/0123456789";
            
            var length = Random.Range(minLength, maxLength);
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Range(0, s.Length)]).ToArray());
        }
    }
}
