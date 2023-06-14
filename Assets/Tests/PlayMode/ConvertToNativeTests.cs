
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Api;
using Speckle.Core.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Speckle.ConnectorUnity.Tests
{
    [TestFixture, TestOf(typeof(RecursiveConverter))]
    public class ConvertToNativeTests : ComponentTest<RecursiveConverter>
    {
        private static IEnumerable<string> TestCases()
        {
            yield return @"https://latest.speckle.dev/streams/c1faab5c62/commits/704984e22d";
        }

        private static Base Receive(string stream)
        {
            return Task.Run(async () => await Helpers.Receive(stream)).Result;
        }
        
        [Test, TestCaseSource(nameof(TestCases))]
        public void ToNative_Sync_Passes(string stream)
        {
            Base testCase = Receive(stream);
            var resuts = sut.RecursivelyConvertToNative_Sync(testCase, null);
            Assert.That(resuts, Has.Count.GreaterThan(0));
            Assert.That(resuts, Has.Some.Matches<ConversionResult>(x => x.WasSuccessful())));
            AssertChildren();
        }
        
        [UnityTest, TestCaseSource(nameof(TestCases))]
        public IEnumerator ToNative_Coroutine_Passes(string stream)
        {
            Base testCase = Receive(stream);
            GameObject parent = new("parent");
            
            yield return sut.RecursivelyConvertToNative_Coroutine(testCase, parent);
            AssertChildren(parent);
        }

        private void AssertChildren(IEnumerable<GameObject> children)
        {
            foreach (var res in children)
            {
                res 
            }
        }
    }
}
