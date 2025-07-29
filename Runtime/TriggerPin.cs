using System.Collections.Generic;

namespace Trgen
{
    /// <summary>
    /// TriggerPin is the class that describe the Pin Mapping Configuration
    /// </summary>
    public static class TriggerPin
    {   
        public const int NS0 = 0;
        public const int NS1 = 1;
        public const int NS2 = 2;
        public const int NS3 = 3;
        public const int NS4 = 4;
        public const int NS5 = 5;
        public const int NS6 = 6;
        public const int NS7 = 7;

        public const int SA0 = 8;
        public const int SA1 = 9;
        public const int SA2 = 10;
        public const int SA3 = 11;
        public const int SA4 = 12;
        public const int SA5 = 13;
        public const int SA6 = 14;
        public const int SA7 = 15;

        public const int TMSO = 16;
        public const int TMSI = 17;

        public const int GPIO0 = 18;
        public const int GPIO1 = 19;
        public const int GPIO2 = 20;
        public const int GPIO3 = 21;
        public const int GPIO4 = 22;
        public const int GPIO5 = 23;
        public const int GPIO6 = 24;
        public const int GPIO7 = 25;

        /// <summary>
        /// AllNs is a helper that returns all the Neuroscan pins array
        /// </summary>
        public static readonly List<int> AllNs = new() { NS0, NS1, NS2, NS3, NS4, NS5, NS6, NS7 };

        /// <summary>
        /// AllNs is a helper that returns all the Sinamps pins array
        /// </summary>
        public static readonly List<int> AllSa = new() { SA0, SA1, SA2, SA3, SA4, SA5, SA6, SA7 };

        /// <summary>
        /// AllNs is a helper that returns all the GPIO pins array
        /// </summary>
        public static readonly List<int> AllGpio = new() { GPIO0, GPIO1, GPIO2, GPIO3, GPIO4, GPIO5, GPIO6, GPIO7 };
    }
}