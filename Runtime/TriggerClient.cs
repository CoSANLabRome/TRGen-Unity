using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Trgen
{   
    /// <summary>
    /// TriggerClient is a class that manages 
    /// connectivity and command set for TrGEN
    /// </summary>
    public class TriggerClient
    {
        private readonly string ip;
        private readonly int port;
        private readonly int timeout;
        private TrgenImplementation _impl;
        private int _memoryLength = 32;
        private bool connected = false;
        public bool Connected => connected;

        public TriggerClient(string ip = "192.168.123.1", int port = 4242, int timeout = 2000)
        {
            this.ip = ip;
            this.port = port;
            this.timeout = timeout;
        }

        public Trigger CreateTrigger(int id)
        {
            return new Trigger(id, _memoryLength);
        }

        public void Connect()
        {
            try
            {
                int packed = RequestImplementation();
                _impl = new TrgenImplementation(packed);
                UnityEngine.Debug.Log(_impl.MemoryLength);
                _memoryLength = _impl.MemoryLength;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[TRGEN] Connect failed: {ex.Message}");
            }
        }

        public bool IsAvailable()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(ip, port, null, null);
                    return result.AsyncWaitHandle.WaitOne(timeout);
                }
            }
            catch { return false; }
        }

        public string SendPacket(int packetId, uint[] payload = null)
        {
            byte[] header = ToLittleEndian((uint)packetId);
            byte[] payloadBytes = payload != null ? BuildPayload(payload) : Array.Empty<byte>();

            byte[] raw = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, raw, 0, header.Length);
            if (payloadBytes.Length > 0)
                Buffer.BlockCopy(payloadBytes, 0, raw, header.Length, payloadBytes.Length);

            uint crc = Crc32.Compute(raw);
            byte[] crcBytes = ToLittleEndian(crc);

            byte[] packet = new byte[raw.Length + crcBytes.Length];
            Buffer.BlockCopy(raw, 0, packet, 0, raw.Length);
            Buffer.BlockCopy(crcBytes, 0, packet, raw.Length, 4);
            DebugPacket(packet, $"Sending packet 0x{packetId:X8}");
            using (var client = new TcpClient())
            {
                try
                {
                    client.Connect(ip, port); // sincrono
                    connected = true;

                    using var stream = client.GetStream();
                    stream.Write(packet, 0, packet.Length);

                    byte[] buffer = new byte[64];
                    int read = stream.Read(buffer, 0, buffer.Length);
                    return Encoding.ASCII.GetString(buffer, 0, read);
                }
                catch
                {
                    connected = false;
                    throw;
                }
            }
        }

        private byte[] ToLittleEndian(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        private byte[] BuildPayload(uint[] words)
        {
            List<byte> result = new();
            foreach (var word in words)
                result.AddRange(ToLittleEndian(word));
            return result.ToArray();
        }

        public int ParseAckValue(string ackStr, int expectedId)
        {
            if (!ackStr.StartsWith($"ACK{expectedId}"))
                throw new Exception($"Unexpected ACK: {ackStr}");
            var parts = ackStr.Split('.');
            if (parts.Length != 2)
                throw new Exception($"Malformed ACK: {ackStr}");
            return int.Parse(parts[1]);
        }

        private void DebugPacket(byte[] packet, string label = "Packet")
        {
            var hex = BitConverter.ToString(packet).Replace("-", " ");
            Console.WriteLine($"{label}: {hex}");
            UnityEngine.Debug.Log($"{label}: {hex}");
        }

        /*private byte[] BuildPayload(uint[] words)
        {
            byte[] payload = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                byte[] word = BitConverter.GetBytes(words[i]);
                Buffer.BlockCopy(word, 0, payload, i * 4, 4);
            }
            return payload;
        }*/

        // Commands
        public void Start() => SendPacket(0x02);
        public void Stop() => SendPacket(0x09);
        public void SetLevel(uint mask) => SendPacket(0x06, new uint[] { mask });
        public void SetGpio(uint mask) => SendPacket(0x03, new uint[] { mask });
        public int GetLevel() => ParseAckValue(SendPacket(0x08), 0x08);
        public int GetStatus() => ParseAckValue(SendPacket(0x05), 0x05);
        public int GetGpio() => ParseAckValue(SendPacket(0x07), 0x07);

        public void SendTriggerMemory(Trigger t)
        {
            int id = t.Id;
            int packetId = 0x01 | (id << 24);
            SendPacket(packetId, t.Memory);
        }

        public int RequestImplementation()
        {
            var ack = SendPacket(0x04);
            return ParseAckValue(ack, 0x04);
        }

        public void ResetTrigger(Trigger t)
        {
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTriggerMemory(t);
        }

        public void ProgramDefaultTrigger(Trigger t, uint us = 20)
        {
            t.SetInstruction(0, InstructionEncoder.ActiveForUs(us));
            t.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
            t.SetInstruction(2, InstructionEncoder.End());
            for (int i = 3; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTriggerMemory(t);
        }

        // Implement ResetAll per tipo pin
        public void ResetAll(List<int> ids)
        {
            foreach (var id in ids)
            {
                var tr = CreateTrigger(id);
                ResetTrigger(tr);
            }
        }

        public void StartTrigger(int triggerId)
        {
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);

            var tr = CreateTrigger(triggerId);
            ProgramDefaultTrigger(tr);
            Start();
        }

        public void StartTriggerList(List<int> triggerIds)
        {
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);

            foreach (var id in triggerIds)
            {
                var tr = CreateTrigger(id);
                ProgramDefaultTrigger(tr);
            }
            Start();
        }
    }
}
