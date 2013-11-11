using System;
using System.Linq;
using System.IO;
using MARC4J.Net.MARC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class RecordTestingUtils
    {
        /**
         * assert two Record objects are equal by comparing them as strings
         */
        public static void AssertEquals(IRecord expected, IRecord actual)
        {
            String actualId = actual.GetControlNumber();
            String errmsg = "Record " + actualId + " wasn't as expected";

            if (actualId.Equals(expected.GetControlNumber()))
                Assert.IsTrue(expected.ToString().Equals(actual.ToString()), errmsg);
            else
                Assert.Fail(errmsg);
        }

        /**
         * assert two Record objects aren't equal by comparing them as strings
         */
        public static void assertNotEqual(IRecord expected, IRecord actual)
        {
            String actualId = actual.GetControlNumber();
            if (!actualId.Equals(expected.GetControlNumber()))
                return;

            Assert.IsFalse(expected.ToString().Equals(actual.ToString()), "Records unexpectedly the same: " + actualId);
        }

        /**
         * assert two Record objects are equal by comparing them as strings, skipping over the leader
         */
        public static void AssertEqualsIgnoreLeader(IRecord expected, IRecord actual)
        {
            String actualId = actual.GetControlNumber();
            String errmsg = "Record " + actualId + " wasn't as expected";

            if (actualId.Equals(expected.GetControlNumber()))
                Assert.IsTrue(expected.ToString().Substring(24).Equals(actual.ToString().Substring(24)), errmsg);
            else
                Assert.Fail(errmsg);
        }

        /**
         * assert two Record objects are not equal by comparing them as strings, skipping over the leader
         */
        public static void AssertNotEqualIgnoreLeader(IRecord expected, IRecord actual)
        {
            String actualId = actual.GetControlNumber();
            if (!actualId.Equals(expected.GetControlNumber()))
                return;

            Assert.IsFalse(expected.ToString().Substring(24).Equals(actual.ToString().Substring(24)), "Records unexpectedly the same: " + actualId);
        }

        /**
         * compare two marc records;  the expected result is represented as
         *  an array of strings.  The leaders don't match; not sure why or if it
         *  matters.
         * @param expected
         * @param actual
         */
        public static void AssertEqualsIgnoreLeader(String[] expected, IRecord actual)
        {
            String actualAsStr = actual.ToString();
            // removing leader is removing "LEADER " and the 24 char leader and the newline
            String actualAsStrWithoutLdr = actualAsStr.Substring(32);

            StringBuilder buf = new StringBuilder();
            for (int i = 1; i < expected.Length; i++)
            {
                buf.Append(expected[i] + "\n");
            }

            Assert.AreEqual(buf.ToString(), actualAsStrWithoutLdr, "Records weren't equal");
        }


        /**
         * Given an expected marc record as an Array of strings corresponding to 
         *  the lines in the output of MarcPrinter and given the actual marc record as an InputStream,
         *  assert they are equal
         */
        public static void AssertMarcRecsEqual(String[] expectedAsLines, Stream actualAsInputStream)
        {
            StreamReader actualAsBuffRdr = null;
            try
            {
                actualAsBuffRdr = new StreamReader(actualAsInputStream, Encoding.UTF8);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine(e.StackTrace);
                Assert.Fail("couldn't read record to be tested from InputStream");
            }

            int numExpectedLines = expectedAsLines.Length;

            try
            {
                int lineCnt = 0;
                String actualLine = null;
                while ((actualLine = actualAsBuffRdr.ReadLine()) != null)
                {
                    if (actualLine.Length == 0)
                    {
                        // do nothing;
                    }
                    else if (numExpectedLines > 0 && lineCnt < numExpectedLines)
                    {
                        if (actualLine.Equals("Flushing results...") || actualLine.Equals("Flushing results done") || actualLine.StartsWith("Cobertura:"))
                            continue;   // skip this line and don't even count it.  I don't know where these "Flushing Results..." lines are coming from.

                        String expectedLine = expectedAsLines[lineCnt];
                        Assert.AreEqual(expectedLine, actualLine, "output line [" + actualLine + "]  doesn't match expected [" + expectedLine + "]");
                    }
                    lineCnt++;
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.StackTrace);
                Assert.Fail("couldn't compare records");
            }
        }

        /**
         * Assert that each instance of the subfield is in the expected values
         *  and that the number of instances match.
         */
        public static void AssertSubfieldHasExpectedValues(Record record, String fieldTag, char subfieldCode, ICollection<String> expectedVals)
        {
            ICollection<String> resultSet = new List<String>();
            foreach (var vf in record.GetVariableFields(fieldTag))
            {
                var df = (IDataField)vf;
                var sfList = df.GetSubfields(subfieldCode);
                foreach (var sf in sfList)
                {
                    String val = sf.Data;
                    resultSet.Add(val);
                    Assert.IsTrue(expectedVals.Contains(val), "Got unexpected value " + val);
                }
            }
            Assert.AreEqual(expectedVals.Count, resultSet.Count, "Number of values doesn't match");
        }

        /**
          * Assign id of record to be the ckey. Our ckeys are in 001 subfield a. 
          * Marc4j is unhappy with subfields in a control field so this is a kludge 
          * work around.
          */
        public static String GetRecordIdFrom001(IRecord record)
        {
            String id = null;
            var fld = (IControlField)record.GetVariableField("001");
            if (fld != null && fld.Data != null)
            {
                String rawVal = fld.Data;
                // 'u' is for testing
                if (rawVal.StartsWith("a") || rawVal.StartsWith("u"))
                    id = rawVal.Substring(1);
            }
            return id;
        }

    }
}