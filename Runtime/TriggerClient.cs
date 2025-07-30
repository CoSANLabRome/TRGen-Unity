using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Trgen
{
    /// <summary>
    /// Gestisce la connessione, la comunicazione e il controllo dei trigger hardware tramite il protocollo TrGEN.
    /// Permette di programmare, resettare e inviare segnali di trigger su diversi tipi di porte (NeuroScan, Synamaps, GPIO).
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

        /// <summary>
        /// Crea una nuova istanza di TriggerClient.
        /// </summary>
        /// <param name="ip">Indirizzo IP del dispositivo TrGEN.</param>
        /// <param name="port">Porta di comunicazione.</param>
        /// <param name="timeout">Timeout per la connessione in millisecondi.</param>
        public TriggerClient(string ip = "192.168.123.1", int port = 4242, int timeout = 2000)
        {
            this.ip = ip;
            this.port = port;
            this.timeout = timeout;
        }

        /// <summary>
        /// Crea un oggetto Trigger associato a un identificatore specifico.
        /// </summary>
        /// <param name="id">Identificatore del trigger.</param>
        /// <returns>Oggetto Trigger.</returns>
        public Trigger CreateTrigger(int id)
        {
            return new Trigger(id, _memoryLength);
        }

        /// <summary>
        /// Tenta di connettersi al dispositivo TrGEN e aggiorna la configurazione interna.
        /// </summary>
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

        /// <summary>
        /// Verifica se il server TrGEN è raggiungibile.
        /// </summary>
        /// <returns>True se disponibile, altrimenti False.</returns>
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

        /// <summary>
        /// Invia un pacchetto di dati al dispositivo TrGEN.
        /// </summary>
        /// <param name="packetId">Identificatore del pacchetto.</param>
        /// <param name="payload">Dati opzionali da inviare.</param>
        /// <returns>Risposta del dispositivo come stringa.</returns>
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

        public void ResetAllTMSO()
        {
            Trigger t = new Trigger(TriggerPin.TMSO);
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTriggerMemory(t);
        }

        public void ResetAllSA()
        {
            int[] sinampMap = [
                TriggerPin.SA0,
                TriggerPin.SA1,
                TriggerPin.SA2,
                TriggerPin.SA3,
                TriggerPin.SA4,
                TriggerPin.SA5,
                TriggerPin.SA6,
                TriggerPin.SA7,
            ];
            for (int i = 0; i < sinampMap.Length; i++)
            {
                Trigger sa = CreateTrigger(sinampMap[i]);
                sa.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    sa.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTriggerMemory(sa);
            }
        }

        public void ResetAllGPIO()
        {
            int[] gpioMap = [
                TriggerPin.GPIO0,
                TriggerPin.GPIO1,
                TriggerPin.GPIO2,
                TriggerPin.GPIO3,
                TriggerPin.GPIO4,
                TriggerPin.GPIO5,
                TriggerPin.GPIO6,
                TriggerPin.GPIO7,
            ];
            for (int i = 0; i < gpioMap.Length; i++)
            {
                Trigger gpio = CreateTrigger(gpioMap[i]);
                gpio.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    gpio.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTriggerMemory(gpio);
            }
        }
        public void ResetAllNS()
        {
            int[] neuroscanMap = [
                TriggerPin.NS0,
                TriggerPin.NS1,
                TriggerPin.NS2,
                TriggerPin.NS3,
                TriggerPin.NS4,
                TriggerPin.NS5,
                TriggerPin.NS6,
                TriggerPin.NS7,
            ];
            for (int i = 0; i < neuroscanMap.Length; i++)
            {
                Trigger ns = CreateTrigger(neuroscanMap[i]);
                ns.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    ns.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTriggerMemory(ns);
            }
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

        /// <summary>
        /// Invia un marker (segnale di trigger) su una o più porte (NeuroScan, Synamps, GPIO).
        /// </summary>
        /// <param name="markerNS">Valore marker per NeuroScan.</param>
        /// <param name="markerSA">Valore marker per Synamps.</param>
        /// <param name="markerGPIO">Valore marker per GPIO.</param>
        /// <param name="LSB">Se true, usa il bit meno significativo come primo pin.</param>
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, bool LSB = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null)
                return;

            var neuroscanMap = new TriggerPin[]
            {
        TriggerPin.NS0,
        TriggerPin.NS1,
        TriggerPin.NS2,
        TriggerPin.NS3,
        TriggerPin.NS4,
        TriggerPin.NS5,
        TriggerPin.NS6,
        TriggerPin.NS7
            };

            var synampsMap = new TriggerPin[]
            {
        TriggerPin.SA0,
        TriggerPin.SA1,
        TriggerPin.SA2,
        TriggerPin.SA3,
        TriggerPin.SA4,
        TriggerPin.SA5,
        TriggerPin.SA6,
        TriggerPin.SA7
            };

            var gpioMap = new TriggerPin[]
            {
        TriggerPin.GPIO0,
        TriggerPin.GPIO1,
        TriggerPin.GPIO2,
        TriggerPin.GPIO3,
        TriggerPin.GPIO4,
        TriggerPin.GPIO5,
        TriggerPin.GPIO6,
        TriggerPin.GPIO7
            };

            ResetAllNS();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllTMSO();

            if (markerNS != null)
            {
                var maskNS = Convert.ToString(markerNS.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskNS);
                for (int idx = 0; idx < maskNS.Length; idx++)
                {
                    if (maskNS[idx] == '1')
                    {
                        var nsx = CreateTrigger(neuroscanMap[idx]);
                        ProgramDefaultTrigger(nsx);
                    }
                }
            }

            if (markerSA != null)
            {
                var maskSA = Convert.ToString(markerSA.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskSA);
                for (int idx = 0; idx < maskSA.Length; idx++)
                {
                    if (maskSA[idx] == '1')
                    {
                        var sax = CreateTrigger(synampsMap[idx]);
                        ProgramDefaultTrigger(sax);
                    }
                }
            }

            if (markerGPIO != null)
            {
                var maskGPIO = Convert.ToString(markerGPIO.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskGPIO);
                for (int idx = 0; idx < maskGPIO.Length; idx++)
                {
                    if (maskGPIO[idx] == '1')
                    {
                        var gpx = CreateTrigger(gpioMap[idx]);
                        ProgramDefaultTrigger(gpx);
                    }
                }
            }

            // Avvio sequenza
            Start();
        }
        /// <summary>
        /// Ferma tutti i trigger attivi e resetta lo stato dei pin.
        /// </summary>
        public void StopTrigger()
        {
            Stop();
            ResetAllTMSO();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllNS();
        }
    }

    /// <summary>
    /// Rappresenta un trigger programmabile, con memoria interna per le istruzioni.
    /// </summary>
    public class Trigger
    {
        /// <summary>
        /// Identificatore del trigger.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Memoria delle istruzioni del trigger.
        /// </summary>
        public uint[] Memory { get; }

        /// <summary>
        /// Crea un nuovo trigger con un identificatore e una lunghezza di memoria specifica.
        /// </summary>
        /// <param name="id">Identificatore del trigger.</param>
        /// <param name="memoryLength">Numero di istruzioni programmabili.</param>
        public Trigger(int id, int memoryLength);

        /// <summary>
        /// Imposta una istruzione nella memoria del trigger.
        /// </summary>
        /// <param name="index">Indice della memoria.</param>
        /// <param name="instruction">Valore dell'istruzione.</param>
        public void SetInstruction(int index, uint instruction);
    }

    /// <summary>
    /// Rappresenta la configurazione e le capacità del dispositivo TrGEN.
    /// </summary>
    public class TrgenImplementation
    {
        /// <summary>
        /// Lunghezza della memoria programmabile per ciascun trigger.
        /// </summary>
        public int MemoryLength { get; }

        /// <summary>
        /// Crea una nuova istanza di TrgenImplementation a partire dal valore restituito dal dispositivo.
        /// </summary>
        /// <param name="packed">Valore packed ricevuto dal dispositivo.</param>
        public TrgenImplementation(int packed);
    }
}
