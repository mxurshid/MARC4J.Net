using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;
using System.IO;
using MARC4J.Net.Converter;
using System.Text;
using System.Text.RegularExpressions;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class MarcXmlWriterTest
    {

        [TestMethod]
        public void TestMarcXmlWriter()
        {
            using (var ms = new MemoryStream())
            {
                MarcXmlWriter writer = new MarcXmlWriter(ms, true);
                foreach (var record in StaticTestRecords.summerland)
                {
                    writer.Write(record);
                }
                writer.Close();

                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                    TestUtils.ValidateStringAgainstFile(sr.ReadToEnd(), StaticTestRecords.RESOURCES_SUMMERLAND_XML);
            }
        }

        [TestMethod]
        public void TestMarcXmlWriterNormalized()
        {
            using (var ms = new MemoryStream())
            {
                using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
                {
                    Assert.IsNotNull(fs);
                    MarcXmlWriter writer = new MarcXmlWriter(fs, true);
                    writer.Converter = new AnselToUnicode();
                    MarcStreamReader reader = new MarcStreamReader(ms);
                    while (reader.MoveNext())
                    {
                        var record = reader.Current;
                        writer.Write(record);
                    }
                    using (StreamReader Testoutput = new StreamReader(fs, Encoding.UTF8))
                    {
                        String line;
                        while ((line = Testoutput.ReadLine()) != null)
                        {
                            if (Regex.IsMatch(line, "[ ]*<subfield code=\"a\">This is a Test of diacritics.*"))
                            {
                                String[] lineParts = line.Split(new string[] { ", " }, StringSplitOptions.None);
                                foreach (var linePart in lineParts)
                                {
                                    if (linePart.StartsWith("the tilde in "))
                                        Assert.IsTrue(linePart.Equals("the tilde in man\u0303ana"), "Incorrect value for tilde");
                                    else if (linePart.StartsWith("the grave accent in "))
                                        Assert.IsTrue(linePart.Equals("the grave accent in tre\u0300s"), "Incorrect value for grave");
                                    else if (linePart.StartsWith("the acute accent in "))
                                        Assert.IsTrue(linePart.Equals("the acute accent in de\u0301sire\u0301e"), "Incorrect value for acute");
                                    else if (linePart.StartsWith("the circumflex in "))
                                        Assert.IsTrue(linePart.Equals("the circumflex in co\u0302te"), "Incorrect value for macron");
                                    else if (linePart.StartsWith("the macron in "))
                                        Assert.IsTrue(linePart.Equals("the macron in To\u0304kyo"), "Incorrect value for macron");
                                    else if (linePart.StartsWith("the breve in "))
                                        Assert.IsTrue(linePart.Equals("the breve in russkii\u0306"), "Incorrect value for breve");
                                    else if (linePart.StartsWith("the dot above in "))
                                        Assert.IsTrue(linePart.Equals("the dot above in z\u0307aba"), "Incorrect value for dot above");
                                    else if (linePart.StartsWith("the dieresis (umlaut) in "))
                                        Assert.IsTrue(linePart.Equals("the dieresis (umlaut) in Lo\u0308wenbra\u0308u"), "Incorrect value for umlaut");
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestWriteOfRecordWithIndicatorlessSubfield()
        {
            var record = StaticTestRecords.GetSummerlandRecord();
            MarcFactory factory = StaticTestRecords.getFactory();
            var badField = factory.NewDataField();
            badField.Tag = "911";
            badField.AddSubfield(factory.NewSubfield('a', "HAZMARC - INDICATORLESS FIELD DETECTED - MOPP LEVEL 4"));
            record.AddVariableField(badField);

            using (var output = new MemoryStream())
            {
                using (MarcXmlWriter writer = new MarcXmlWriter(output, true))
                {
                    writer.Write(record);
                }
            }
        }

        [TestMethod]
        public void TestMarcXmlWriterConvertedToUTF8AndNormalized()
        {
            using (var output = new MemoryStream())
            {
                using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
                {
                    Assert.IsNotNull(fs);
                    MarcXmlWriter writer = new MarcXmlWriter(output, true);
                    writer.Converter = new AnselToUnicode();
                    writer.UnicodeNormalization = true;
                    MarcStreamReader reader = new MarcStreamReader(fs);
                    while (reader.MoveNext())
                    {
                        var record = reader.Current;
                        writer.Write(record);
                    }
                    using (StreamReader Testoutput = new StreamReader(fs, Encoding.UTF8))
                    {
                        String line;
                        while ((line = Testoutput.ReadLine()) != null)
                        {
                            if (Regex.IsMatch(line, "[ ]*<subfield code=\"a\">This is a Test of diacritics.*"))
                            {
                                String[] lineParts = line.Split(new string[] { ", " }, StringSplitOptions.None);
                                for (int i = 0; i < lineParts.Length; i++)
                                {
                                    if (lineParts[i].StartsWith("the tilde in "))
                                        Assert.IsTrue(lineParts[i].Equals("the tilde in ma\u00F1ana"), "Incorrect normalized value for tilde accent");
                                    else if (lineParts[i].StartsWith("the grave accent in "))
                                        Assert.IsTrue(lineParts[i].Equals("the grave accent in tr\u00E8s"), "Incorrect normalized value for grave accent");
                                    else if (lineParts[i].StartsWith("the acute accent in "))
                                        Assert.IsTrue(lineParts[i].Equals("the acute accent in d\u00E9sir\u00E9e"), "Incorrect normalized value for acute accent");
                                    else if (lineParts[i].StartsWith("the circumflex in "))
                                        Assert.IsTrue(lineParts[i].Equals("the circumflex in c\u00F4te"), "Incorrect normalized value for circumflex");
                                    else if (lineParts[i].StartsWith("the macron in "))
                                        Assert.IsTrue(lineParts[i].Equals("the macron in T\u014Dkyo"), "Incorrect normalized value for macron");
                                    else if (lineParts[i].StartsWith("the breve in "))
                                        Assert.IsTrue(lineParts[i].Equals("the breve in russki\u012D"), "Incorrect normalized value for breve");
                                    else if (lineParts[i].StartsWith("the dot above in "))
                                        Assert.IsTrue(lineParts[i].Equals("the dot above in \u017Caba"), "Incorrect normalized value for dot above");
                                    else if (lineParts[i].StartsWith("the dieresis (umlaut) in "))
                                        Assert.IsTrue(lineParts[i].Equals("the dieresis (umlaut) in L\u00F6wenbr\u00E4u"), "Incorrect normalized value for umlaut");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}