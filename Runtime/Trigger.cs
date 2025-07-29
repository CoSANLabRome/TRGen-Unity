using System;

namespace Trgen
{
    public class Trigger
    {
        public int Id { get; }
        public TriggerType Type { get; set; }
        public uint[] Memory { get; }

        public Trigger(int id, int memoryLength)
        {
            Id = id;
            Memory = new uint[memoryLength];
        }

        public void SetInstruction(int index, uint instruction)
        {
            if (index < 0 || index >= Memory.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            Memory[index] = instruction;
        }
    }

    public enum TriggerType
    {
        NS, SA, TMSI, TMSO, GPIO
    }
}