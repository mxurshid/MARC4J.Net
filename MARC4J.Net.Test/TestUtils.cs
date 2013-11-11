using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MARC4J.Net.MARC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MARC4J.Net.Test
{
    public class TestUtils
    {

        public static void ValidateKavalieAndClayRecord(IRecord record)
        {
            Assert.AreEqual("00759cam a2200229 a 4500", record.Leader.Marshal(), "leader");
            var it = record.GetVariableFields().GetEnumerator();
            it.MoveNext();
            AssertControlFieldInRecordEquals("001", "11939876", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("005", "20041229190604.0", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("008", "000313s2000    nyu           000 1 eng  ", it.Current);
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "020", ' ', ' ', "a", "0679450041 (acid-free paper)");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "040", ' ', ' ', "a", "DLC", "c", "DLC", "d", "DLC");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "100", '1', ' ', "a", "Chabon, Michael.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "245", '1', '4', "a", "The amazing adventures of Kavalier and Clay :", "b", "a novel /", "c", "Michael Chabon.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "260", ' ', ' ', "a", "New York :", "b", "Random House,", "c", "c2000.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "300", ' ', ' ', "a", "639 p. ;", "c", "25 cm.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Comic books, strips, etc.", "x", "Authorship", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Heroes in mass media", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Czech Americans", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "651", ' ', '0', "a", "New York (N.Y.)", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Young men", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Cartoonists", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "655", ' ', '7', "a", "Humorous stories.", "2", "gsafd");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "655", ' ', '7', "a", "Bildungsromane.", "2", "gsafd");
            Assert.IsFalse(it.MoveNext(), "too many fields");
        }


        public static void ValidateSummerlandRecord(IRecord record)
        {
            Assert.AreEqual("00714cam a2200205 a 4500", record.Leader.Marshal(), "leader");
            var it = record.GetVariableFields().GetEnumerator();
            it.MoveNext();
            AssertControlFieldInRecordEquals("001", "12883376", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("005", "20030616111422.0", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("008", "020805s2002    nyu    j      000 1 eng  ", it.Current);
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "020", ' ', ' ', "a", "0786808772");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "020", ' ', ' ', "a", "0786816155 (pbk.)");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "040", ' ', ' ', "a", "DLC", "c", "DLC", "d", "DLC");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "100", '1', ' ', "a", "Chabon, Michael.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "245", '1', '0', "a", "Summerland /", "c", "Michael Chabon.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "250", ' ', ' ', "a", "1st ed.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "260", ' ', ' ', "a", "New York :", "b", "Miramax Books/Hyperion Books for Children,", "c", "c2002.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "300", ' ', ' ', "a", "500 p. ;", "c", "22 cm.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "520", ' ', ' ', "a", "Ethan Feld, the worst baseball player in the history of the game, finds himself recruited by a 100-year-old scout to help a band of fairies triumph over an ancient enemy.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '1', "a", "Fantasy.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '1', "a", "Baseball", "v", "Fiction.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '1', "a", "Magic", "v", "Fiction.");
            Assert.IsFalse(it.MoveNext(), "too many fields");
        }

        public static void ValidateFreewheelingBobDylanRecord(IRecord record)
        {
            Assert.AreEqual("01471cjm a2200349 a 4500", record.Leader.Marshal(), "leader");
            var it = record.GetVariableFields().GetEnumerator();
            it.MoveNext();
            AssertControlFieldInRecordEquals("001", "5674874", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("005", "20030305110405.0", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("007", "sdubsmennmplu", it.Current);
            it.MoveNext();
            AssertControlFieldInRecordEquals("008", "930331s1963    nyuppn              eng d", it.Current);
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "035", ' ', ' ', "9", "(DLC)   93707283");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "906", ' ', ' ', "a", "7", "b", "cbc", "c", "copycat", "d", "4", "e", "ncip", "f", "19", "g", "y-soundrec");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "010", ' ', ' ', "a", "   93707283 ");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "028", '0', '2', "a", "CS 8786", "b", "Columbia");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "035", ' ', ' ', "a", "(OCoLC)13083787");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "040", ' ', ' ', "a", "OClU", "c", "DLC", "d", "DLC");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "041", '0', ' ', "d", "eng", "g", "eng");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "042", ' ', ' ', "a", "lccopycat");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "050", '0', '0', "a", "Columbia CS 8786");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "100", '1', ' ', "a", "Dylan, Bob,", "d", "1941-");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "245", '1', '4', "a", "The freewheelin' Bob Dylan", "h", "[sound recording].");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "260", ' ', ' ', "a", "[New York, N.Y.] :", "b", "Columbia,", "c", "[1963]");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "300", ' ', ' ', "a", "1 sound disc :", "b", "analog, 33 1/3 rpm, stereo. ;", "c", "12 in.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "500", ' ', ' ', "a", "Songs.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "511", '0', ' ', "a", "The composer accompanying himself on the guitar ; in part with instrumental ensemble.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "500", ' ', ' ', "a", "Program notes by Nat Hentoff on container.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "505", '0', ' ', "a", "Blowin' in the wind -- Girl from the north country -- Masters of war -- Down the highway -- Bob Dylan's blues -- A hard rain's a-gonna fall -- Don't think twice, it's all right -- Bob Dylan's dream -- Oxford town -- Talking World War III blues -- Corrina, Corrina -- Honey, just allow me one more chance -- I shall be free.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Popular music", "y", "1961-1970.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "650", ' ', '0', "a", "Blues (Music)", "y", "1961-1970.");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "856", '4', '1', "3", "Preservation copy (limited access)", "u", "http://hdl.loc.gov/loc.mbrsrs/lp0001.dyln");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "952", ' ', ' ', "a", "New");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "953", ' ', ' ', "a", "TA28");
            it.MoveNext();
            AssertDataFieldEquals(it.Current, "991", ' ', ' ', "b", "c-RecSound", "h", "Columbia CS 8786", "w", "MUSIC");
            Assert.IsFalse(it.MoveNext(), "too many fields");
        }

        static void AssertControlFieldInRecordEquals(String tag, String expected, IVariableField field)
        {
            IControlField tmp = (IControlField)field;
            Assert.AreEqual(expected, tmp.Data, "Control field mismatch for tag " + tag);
        }

        static void AssertDataFieldEquals(IVariableField vf, String tag, char ind1, char ind2, params string[] subfieldsAndValues)
        {
            var field = (IDataField)vf;
            Assert.AreEqual(tag, field.Tag, "tag");
            Assert.AreEqual(ind1, field.Indicator1, "Indicator 1");
            Assert.AreEqual(ind2, field.Indicator2, "Indicator 2");
            var subfields = field.GetSubfields();
            var it = subfields.GetEnumerator();
            for (int i = 0; i < subfieldsAndValues.Length; i++)
            {
                var expectedCode = subfieldsAndValues[i++];
                var expectedValue = subfieldsAndValues[i];
                if (!it.MoveNext())
                {
                    Assert.Fail("not enough subfields - expecting $" + expectedCode + " = " + expectedValue);
                }

                var sf = it.Current;
                Assert.AreEqual(sf.Code, expectedCode[0], "subfieldCode");
                Assert.AreEqual(expectedValue, sf.Data, "subfield value (" + tag + " $" + expectedCode + ") ");
            }
            if (it.MoveNext())
            {
                Assert.Fail("Too many subfields for " + tag + " - first unexpected is " + it.Current);
            }
        }

        public static void ValidateBytesAgainstFile(byte[] actual, String fileName)
        {
            var expected = File.ReadAllBytes(Environment.CurrentDirectory + fileName);
            String comparison = CompareFilesContentsLineByLine(BitConverter.ToString(expected), BitConverter.ToString(expected));
            if (comparison != null)
            {
                Assert.Fail("actual differs from expected as shown below:" + Environment.NewLine + comparison);
            }
        }

        public static void ValidateStringAgainstFile(String actual, String fileName)
        {
            var expected = File.ReadAllText(Environment.CurrentDirectory + fileName);
            var comparison = CompareFilesContentsLineByLine(expected, actual);
            if (comparison != null)
            {
                Assert.Fail("actual differs from expected as shown below:" + Environment.NewLine + comparison);
            }
        }

        public static String CompareFilesContentsLineByLine(String expected, String actual)
        {
            String[] expectedLines = Regex.Split(expected, "[\r]?\n");
            String[] actualLines = Regex.Split(actual, "[\r]?\n");
            String separator = Environment.NewLine;
            bool matches = true;
            int expectedIndex = 0, actualIndex = 0;
            var sb = new StringBuilder();
            while (expectedIndex < expectedLines.Length && actualIndex < actualLines.Length)
            {
                if (expectedLines[expectedIndex].Equals(actualLines[actualIndex]))
                {
                    sb.Append("  " + expectedLines[expectedIndex]).Append(separator);
                    expectedIndex++;
                    actualIndex++;
                }
                else if (actualIndex + 1 < actualLines.Length && expectedLines[expectedIndex].Equals(actualLines[actualIndex + 1]))
                {
                    sb.Append("+ " + actualLines[actualIndex]).Append(separator);
                    actualIndex++;
                    matches = false;
                }
                else if (expectedIndex + 1 < expectedLines.Length && expectedLines[expectedIndex + 1].Equals(actualLines[actualIndex]))
                {
                    sb.Append("- " + expectedLines[expectedIndex]).Append(separator);
                    expectedIndex++;
                    matches = false;
                }
                else
                {
                    sb.Append("+ " + actualLines[actualIndex]).Append(separator);
                    actualIndex++;
                    sb.Append("- " + expectedLines[expectedIndex]).Append(separator);
                    expectedIndex++;
                    matches = false;
                }
            }
            while (expectedIndex < expectedLines.Length || actualIndex < actualLines.Length)
            {
                if (actualIndex < actualLines.Length)
                {
                    sb.Append("+ " + actualLines[actualIndex]).Append(separator);
                    actualIndex++;
                    matches = false;
                }
                else if (expectedIndex < expectedLines.Length)
                {
                    sb.Append("- " + expectedLines[expectedIndex]).Append(separator);
                    expectedIndex++;
                    matches = false;
                }
            }

            if (matches) return null;
            return sb.ToString();
        }

    }
}