using NUnit.Framework;
using UnityEngine;

namespace Speckle.ConnectorUnity.Tests
{
    public abstract class ComponentTest<T> where T : Component
    {
        protected T sut;

        [SetUp]
        public void Setup()
        {
            GameObject go = new();
            sut = go.AddComponent<T>();
            Assert.That(sut, Is.Not.Null);
        }
    }
}
