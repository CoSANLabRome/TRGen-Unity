using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections;
using Trgen;
using System;

namespace Trgen.Tests
{
    public class TriggerClientTests
    {
        private TriggerClient client;

        [SetUp]
        public void Setup()
        {
            // Use a dummy IP and port to avoid real network calls in unit tests.
            client = new TriggerClient("127.0.0.1", 9999, 100);
        }

        [Test]
        public void CreateTrigger_ReturnsTriggerWithCorrectId()
        {
            var trigger = client.CreateTrigger(5);
            Assert.AreEqual(5, trigger.Id);
        }

        [Test]
        public void IsAvailable_ReturnsFalse_WhenNoServer()
        {
            Assert.IsFalse(client.IsAvailable());
        }

        [Test]
        public void ParseAckValue_ThrowsOnMalformedAck()
        {
            Assert.Throws<Exception>(() => client.ParseAckValue("ACK04", 4));
            Assert.Throws<Exception>(() => client.ParseAckValue("NACK04.123", 4));
        }

        [Test]
        public void ParseAckValue_ReturnsValue_WhenValid()
        {
            int val = client.ParseAckValue("ACK4.123", 4);
            Assert.AreEqual(123, val);
        }

        [Test]
        public void ToLittleEndian_ReturnsCorrectBytes()
        {
            var method = typeof(TriggerClient).GetMethod("ToLittleEndian", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            byte[] bytes = (byte[])method.Invoke(client, new object[] { (uint)0x12345678 });
            if (BitConverter.IsLittleEndian)
                Assert.AreEqual(new byte[] { 0x78, 0x56, 0x34, 0x12 }, bytes);
            else
                Assert.AreEqual(new byte[] { 0x12, 0x34, 0x56, 0x78 }, bytes);
        }

        [Test]
        public void BuildPayload_ReturnsCorrectLength()
        {
            var method = typeof(TriggerClient).GetMethod("BuildPayload", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            byte[] payload = (byte[])method.Invoke(client, new object[] { new uint[] { 1, 2, 3 } });
            Assert.AreEqual(12, payload.Length);
        }

        [UnityTest]
        public IEnumerator ResetAll_DoesNotThrow()
        {
            var ids = new System.Collections.Generic.List<int> { 1, 2, 3 };
            Assert.DoesNotThrow(() => client.ResetAll(ids));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProgramDefaultTrigger_DoesNotThrow()
        {
            var trigger = client.CreateTrigger(1);
            Assert.DoesNotThrow(() => client.ProgramDefaultTrigger(trigger, 10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartTriggerList_DoesNotThrow()
        {
            var ids = new System.Collections.Generic.List<int> { 1, 2 };
            Assert.DoesNotThrow(() => client.StartTriggerList(ids));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SendMarker_DoesNotThrow_WhenAllNull()
        {
            Assert.DoesNotThrow(() => client.SendMarker());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SendMarker_DoesNotThrow_WithMarkers()
        {
            Assert.DoesNotThrow(() => client.SendMarker(markerNS: 3, markerSA: 2, markerGPIO: 1, LSB: true));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopTrigger_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => client.StopTrigger());
            yield return null;
        }
    }
}