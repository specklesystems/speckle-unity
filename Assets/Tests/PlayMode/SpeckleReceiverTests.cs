#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using Speckle.ConnectorUnity.Components;
using Speckle.Core.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Speckle.ConnectorUnity.Tests
{
    [TestFixture, TestOf(typeof(SpeckleReceiver))]
    public sealed class SpeckleReceiverTests : ComponentTest<SpeckleReceiver>
    {

        [UnityTest]
        public IEnumerator ReceiveAsync_Succeeds()
        {
            yield return null;
            
            var task = new Utils.Utils.WaitForTask<Base>(async () => await sut.ReceiveAsync(default));
            yield return task;
            Base myBase = task.Result;
            Assert.That(myBase, Is.Not.Null);
        }
        
        [UnityTest]
        public IEnumerator ReceiveAndConvert_Async_Succeeds()
        {
            Transform expectedParent = new GameObject("parent").transform;
            yield return null;

            bool wasSuccessful = false;
            Transform? actualParent = null;
            
            sut.OnComplete.AddListener(t =>
            {
                wasSuccessful = true;
                actualParent = t;
            });
            sut.OnErrorAction.AddListener((_, ex) => throw new Exception("Failed", ex));

            sut.ReceiveAndConvert_Async(expectedParent);

            yield return new WaitUntil(() => wasSuccessful);
            
            Assert.That(actualParent, Is.EqualTo(expectedParent));
        }
        
        [UnityTest]
        public IEnumerator ReceiveAndConvert_Routine_Succeeds()
        {
            Transform expectedParent = new GameObject("parent").transform;
            yield return null;

            bool wasSuccessful = false;
            Transform? actualParent = null;
            
            sut.OnComplete.AddListener(t =>
            {
                wasSuccessful = true;
                actualParent = t;
            });
            sut.OnErrorAction.AddListener((_, ex) => throw new Exception("Failed", ex));

            yield return sut.ReceiveAndConvert_Routine(expectedParent);

            yield return new WaitUntil(() => wasSuccessful);
            
            Assert.That(actualParent, Is.EqualTo(expectedParent));
        }
    }
}
