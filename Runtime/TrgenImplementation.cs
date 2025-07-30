namespace Trgen
{
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
        public TrgenImplementation(int packed)
        {
            NsNum =  (packed >> 0)  & 0x1F;
            SaNum =  (packed >> 5)  & 0x1F;
            TmsoNum = (packed >> 10) & 0x07;
            TmsiNum = (packed >> 13) & 0x07;
            GpioNum = (packed >> 16) & 0x1F;
            Mtml =    (packed >> 26) & 0x3F;
        }

        public int NsNum { get; }
        public int SaNum { get; }
        public int TmsoNum { get; }
        public int TmsiNum { get; }
        public int GpioNum { get; }
        public int Mtml { get; }
    }

    /// <summary>
    /// Fornisce metodi statici per codificare le istruzioni dei trigger.
    /// </summary>
    public static class InstructionEncoder
    {
        /// <summary>
        /// Restituisce il codice per terminare la sequenza di istruzioni.
        /// </summary>
        public static uint End();

        /// <summary>
        /// Restituisce il codice per una istruzione non ammessa.
        /// </summary>
        public static uint NotAdmissible();

        /// <summary>
        /// Restituisce il codice per attivare il trigger per un certo numero di microsecondi.
        /// </summary>
        /// <param name="us">Microsecondi di attivazione.</param>
        public static uint ActiveForUs(uint us);

        /// <summary>
        /// Restituisce il codice per disattivare il trigger per un certo numero di microsecondi.
        /// </summary>
        /// <param name="us">Microsecondi di disattivazione.</param>
        public static uint UnactiveForUs(uint us);
    }
}