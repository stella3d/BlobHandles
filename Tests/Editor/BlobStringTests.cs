using System.Text;
using NUnit.Framework;
using UnityEngine;
using BlobHandles;

namespace BlobHandles.Tests
{
    public class BlobStringTests
    {
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public void BlobString_ToString_OutputIsIdentical(string input)
        {
            var blobString = new BlobString(input);
            Debug.Log($"input - {input}, managed int string output - {blobString}");
            Assert.AreEqual(input, blobString.ToString());
            blobString.Dispose();
        }
        
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public void BlobString_SetFromBytes(string input)
        {
            var randomStr = TestData.RandomString(input.Length, input.Length);
            var blobString = new BlobString(randomStr);
            Debug.Log($"random string before byte set: {blobString}");
            
            var inputAsciiBytes = Encoding.ASCII.GetBytes(input);
            blobString.SetBytesUnchecked(inputAsciiBytes, 0, inputAsciiBytes.Length);
            Debug.Log($"input - {input}, managed int string output after byte set- {blobString}");
            Assert.AreEqual(input, blobString.ToString());
            blobString.Dispose();
        }
        
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public void BlobString_GetHashCode_OutputSameAcrossCalls(string input)
        {
            var blobString = new BlobString(input);
            var firstHashCode = blobString.GetHashCode();

            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(firstHashCode, blobString.GetHashCode());
            }

            blobString.Dispose();
        }
        
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public void BlobString_GetHashCode_OutputSameAcrossInstances(string input)
        {
            var blobString1 = new BlobString(input);
            var blobString2 = new BlobString(input);
            
            var hashCode1 = blobString1.GetHashCode();
            var hashCode2 = blobString2.GetHashCode();
            blobString1.Dispose();
            blobString2.Dispose();
            
            Assert.AreEqual(hashCode1, hashCode2);
        }
        
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public unsafe void BlobString_GetHashCode_OutputSameAfterMutating(string input)
        {
            var randomStr = TestData.RandomString(input.Length, input.Length);
            var randomBlobString = new BlobString(randomStr);
            Debug.Log($"random string before byte set: {randomBlobString}");
            
            var blobString = new BlobString(input);
            var inputBytes = BlobString.Encoding.GetBytes(input);
            var inputHash = blobString.GetHashCode();

            fixed (byte* bPtr = &inputBytes[0])
            {
                randomBlobString.SetPointer(bPtr, inputBytes.Length);
                var randHashMutated = randomBlobString.GetHashCode();
                if (randHashMutated != inputHash)
                {
                    Debug.Log($"debug here - diff index: {blobString.FirstByteDifferenceIndex(randomBlobString)}");
                    
                }

                Assert.AreEqual(inputHash, randHashMutated);
            }
        }
    }
}