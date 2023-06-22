
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Speckle.ConnectorUnity.Components;
using Speckle.ConnectorUnity.Wrappers;
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
        public void ToNative_Passes(string stream)
        {
            Base testCase = Receive(stream);
            var results = sut.RecursivelyConvertToNative_Sync(testCase, null);
            Assert.That(results, Has.Count.GreaterThan(0));
            Assert.That(results, HasSomeComponent<Transform>());
            Assert.That(results, HasSomeComponent<MeshRenderer>());
            Assert.That(results, HasSomeComponent<SpeckleProperties>());
        }

        private static Constraint HasSomeComponent<T>() where T : Component
        {
            return Has.Some.Matches<ConversionResult>(
                x =>
                {
                    return x.WasSuccessful(out var success, out _) 
                        && success.GetComponent<T>();
                });
        }
    }
}
