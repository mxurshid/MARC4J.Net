using System;

namespace MARC4J.Net
{
    public class Constants
    {
        private Constants() { }

        /** RECORD TERMINATOR */
        public const int RT = 0x001D;

        /** FIELD TERMINATOR */
        public const int FT = 0x001E;

        /** SUBFIELD DELIMITER */
        public const int US = 0x001F;

        /** BLANK */
        public const int BLANK = 0x0020;

        /** NS URI */
        public const String MARCXML_NS_URI = "http://www.loc.gov/MARC21/slim";

        /** MARC-8 ANSEL ENCODING **/
        public const String MARC_8_ENCODING = "MARC8";

        /** ISO5426 ENCODING **/
        public const String ISO5426_ENCODING = "ISO5426";

        /** ISO6937 ENCODING **/
        public const String ISO6937_ENCODING = "ISO6937";

    }
}