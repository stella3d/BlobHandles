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
            var managedIntString = new BlobString(input);
            Debug.Log($"input - {input}, managed int string output - {managedIntString}");
            Assert.AreEqual(input, managedIntString.ToString());
            managedIntString.Dispose();
        }
        
        
        [TestCase(TestData.StringConstants.EatTheRich)]
        [TestCase(TestData.StringConstants.M4A)]
        [TestCase(TestData.StringConstants.HealthJustice)]
        public void BlobString_SetFromBytes(string input)
        {
            var randomStr = TestData.RandomString(input.Length, input.Length);
            var managedIntString = new BlobString(randomStr);
            Debug.Log($"random string before byte set: {managedIntString}");
            
            var inputAsciiBytes = Encoding.ASCII.GetBytes(input);
            managedIntString.SetBytesUnchecked(inputAsciiBytes, 0, inputAsciiBytes.Length);
            Debug.Log($"input - {input}, managed int string output after byte set- {managedIntString}");
            Assert.AreEqual(input, managedIntString.ToString());
            managedIntString.Dispose();
        }
    }
}