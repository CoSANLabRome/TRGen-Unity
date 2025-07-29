namespace Trgen
{
    public static class InstructionEncoder
    {
        private const uint INST_UNACTIVE = 0;
        private const uint INST_ACTIVE = 1;
        private const uint INST_WAITPE = 2;
        private const uint INST_WAITNE = 3;
        private const uint INST_REPEAT = 7;
        private const uint INST_END = 4;
        private const uint INST_NOT_ADMISSIBLE = 6;

        public static uint ActiveForUs(uint us) => (us << 3) | INST_ACTIVE;
        public static uint UnactiveForUs(uint us) => (us << 3) | INST_UNACTIVE;
        public static uint WaitPE(uint tr) => (tr << 3) | INST_WAITPE;
        public static uint WaitNE(uint tr) => (tr << 3) | INST_WAITNE;
        public static uint Repeat(uint addr, uint times) => (times << 8) | (addr << 3) | INST_REPEAT;
        public static uint End() => INST_END;
        public static uint NotAdmissible() => INST_NOT_ADMISSIBLE;
    }
}