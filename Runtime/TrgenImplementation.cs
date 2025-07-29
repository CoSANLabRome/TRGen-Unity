namespace Trgen
{
    public class TrgenImplementation
    {
        public int NsNum { get; }
        public int SaNum { get; }
        public int TmsoNum { get; }
        public int TmsiNum { get; }
        public int GpioNum { get; }
        public int Mtml { get; }
        public int MemoryLength => 1 << Mtml;

        public TrgenImplementation(int packed)
        {
            NsNum =  (packed >> 0)  & 0x1F;
            SaNum =  (packed >> 5)  & 0x1F;
            TmsoNum = (packed >> 10) & 0x07;
            TmsiNum = (packed >> 13) & 0x07;
            GpioNum = (packed >> 16) & 0x1F;
            Mtml =    (packed >> 26) & 0x3F;
        }
    }
}