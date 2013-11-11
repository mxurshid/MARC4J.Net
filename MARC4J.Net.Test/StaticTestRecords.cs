using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Test
{
    public class StaticTestRecords
    {
        public const String RESOURCES_ILLEGAL_MARC_IN_JSON_JSON = "\\Resources\\illegal-marc-in-json.json";
        public const String RESOURCES_MARC_IN_JSON_JSON = "\\Resources\\marc-in-json.json";
        public const String RESOURCES_MARC_JSON_JSON = "\\Resources\\marc-json.json";
        public const String RESOURCES_LEGAL_JSON_MARC_IN_JSON_JSON = "\\Resources\\legal-json-marc-in-json.json";
        public const String RESOURCES_BAD_LEADERS_10_11_MRC = "\\Resources\\bad_leaders_10_11.mrc";
        public const String RESOURCES_BAD_TOO_LONG_PLUS_2_MRC = "\\Resources\\bad_too_long_plus_2.mrc";
        public const String RESOURCES_SUMMERLAND_MRC = "\\Resources\\summerland.mrc";
        public const String RESOURCES_SUMMERLAND_XML = "\\Resources\\summerland.xml";
        public const String RESOURCES_SUMMERLAND_MARC_IN_JSON_JSON = "\\Resources\\summerland-marc-in-json.json";
        public const String RESOURCES_SUMMERLAND_MARC_IN_JSON_INDENTED_JSON = "\\Resources\\summerland-marc-in-json-indented.json";
        public const String RESOURCES_SUMMERLAND_MARC_JSON_JSON = "\\Resources\\summerland-marc-json.json";
        public const String RESOURCES_SUMMERLAND_INDENTED_MARC_JSON_JSON = "\\Resources\\summerland-indented-marc-json.json";
        public const String RESOURCES_BRKRTEST_MRC = "\\Resources\\brkrtest.mrc";
        public const String RESOURCES_CHABON_MRC = "\\Resources\\chabon.mrc";
        public const String RESOURCES_CHABON_XML = "\\Resources\\chabon.xml";

        public static IRecord[] chabon = new Record[2];
        public static IRecord[] summerland = new Record[1];
        private static MarcFactory factory;

        static StaticTestRecords()
        {
            factory = MarcFactory.Instance;
            chabon[0] = factory.NewRecord("00759cam a2200229 a 4500");
            chabon[0].AddVariableField(factory.NewControlField("001", "11939876"));
            chabon[0].AddVariableField(factory.NewControlField("005", "20041229190604.0"));
            chabon[0].AddVariableField(factory.NewControlField("008", "000313s2000    nyu           000 1 eng  "));
            chabon[0].AddVariableField(factory.NewDataField("020", ' ', ' ', "a", "0679450041 (acid-free paper)"));
            chabon[0].AddVariableField(factory.NewDataField("040", ' ', ' ', "a", "DLC", "c", "DLC", "d", "DLC"));
            chabon[0].AddVariableField(factory.NewDataField("100", '1', ' ', "a", "Chabon, Michael."));
            chabon[0].AddVariableField(factory.NewDataField("245", '1', '4', "a", "The amazing adventures of Kavalier and Clay :", "b", "a novel /", "c", "Michael Chabon."));
            chabon[0].AddVariableField(factory.NewDataField("260", ' ', ' ', "a", "New York :", "b", "Random House,", "c", "c2000."));
            chabon[0].AddVariableField(factory.NewDataField("300", ' ', ' ', "a", "639 p. ;", "c", "25 cm."));
            chabon[0].AddVariableField(factory.NewDataField("650", ' ', '0', "a", "Comic books, strips, etc.", "x", "Authorship", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("650", ' ', '0', "a", "Heroes in mass media", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("650", ' ', '0', "a", "Czech Americans", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("651", ' ', '0', "a", "New York (N.Y.)", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("650", ' ', '0', "a", "Young men", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("650", ' ', '0', "a", "Cartoonists", "v", "Fiction."));
            chabon[0].AddVariableField(factory.NewDataField("655", ' ', '7', "a", "Humorous stories.", "2", "gsafd"));
            chabon[0].AddVariableField(factory.NewDataField("655", ' ', '7', "a", "Bildungsromane.", "2", "gsafd"));

            chabon[1] = factory.NewRecord("00714cam a2200205 a 4500");
            chabon[1].AddVariableField(factory.NewControlField("001", "12883376"));
            chabon[1].AddVariableField(factory.NewControlField("005", "20030616111422.0"));
            chabon[1].AddVariableField(factory.NewControlField("008", "020805s2002    nyu    j      000 1 eng  "));
            chabon[1].AddVariableField(factory.NewDataField("020", ' ', ' ', "a", "0786808772"));
            chabon[1].AddVariableField(factory.NewDataField("020", ' ', ' ', "a", "0786816155 (pbk.)"));
            chabon[1].AddVariableField(factory.NewDataField("040", ' ', ' ', "a", "DLC", "c", "DLC", "d", "DLC"));
            chabon[1].AddVariableField(factory.NewDataField("100", '1', ' ', "a", "Chabon, Michael."));
            chabon[1].AddVariableField(factory.NewDataField("245", '1', '0', "a", "Summerland /", "c", "Michael Chabon."));
            chabon[1].AddVariableField(factory.NewDataField("250", ' ', ' ', "a", "1st ed."));
            chabon[1].AddVariableField(factory.NewDataField("260", ' ', ' ', "a", "New York :", "b", "Miramax Books/Hyperion Books for Children,", "c", "c2002."));
            chabon[1].AddVariableField(factory.NewDataField("300", ' ', ' ', "a", "500 p. ;", "c", "22 cm."));
            chabon[1].AddVariableField(factory.NewDataField("520", ' ', ' ', "a", "Ethan Feld, the worst baseball player in the history of the game, finds himself recruited by a 100-year-old scout to help a band of fairies triumph over an ancient enemy."));
            chabon[1].AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Fantasy."));
            chabon[1].AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Baseball", "v", "Fiction."));
            chabon[1].AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Magic", "v", "Fiction."));

            summerland[0] = MakeSummerlandRecord();
        }

        private static IRecord MakeSummerlandRecord()
        {
            var sumland = factory.NewRecord("00714cam a2200205 a 4500");
            sumland.AddVariableField(factory.NewControlField("001", "12883376"));
            sumland.AddVariableField(factory.NewControlField("005", "20030616111422.0"));
            sumland.AddVariableField(factory.NewControlField("008", "020805s2002    nyu    j      000 1 eng  "));
            sumland.AddVariableField(factory.NewDataField("020", ' ', ' ', "a", "0786808772"));
            sumland.AddVariableField(factory.NewDataField("020", ' ', ' ', "a", "0786816155 (pbk.)"));
            sumland.AddVariableField(factory.NewDataField("040", ' ', ' ', "a", "DLC", "c", "DLC", "d", "DLC"));
            sumland.AddVariableField(factory.NewDataField("100", '1', ' ', "a", "Chabon, Michael."));
            sumland.AddVariableField(factory.NewDataField("245", '1', '0', "a", "Summerland /", "c", "Michael Chabon."));
            sumland.AddVariableField(factory.NewDataField("250", ' ', ' ', "a", "1st ed."));
            sumland.AddVariableField(factory.NewDataField("260", ' ', ' ', "a", "New York :", "b", "Miramax Books/Hyperion Books for Children,", "c", "c2002."));
            sumland.AddVariableField(factory.NewDataField("300", ' ', ' ', "a", "500 p. ;", "c", "22 cm."));
            sumland.AddVariableField(factory.NewDataField("520", ' ', ' ', "a", "Ethan Feld, the worst baseball player in the history of the game, finds himself recruited by a 100-year-old scout to help a band of fairies triumph over an ancient enemy."));
            sumland.AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Fantasy."));
            sumland.AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Baseball", "v", "Fiction."));
            sumland.AddVariableField(factory.NewDataField("650", ' ', '1', "a", "Magic", "v", "Fiction."));
            return sumland;
        }


        public static IRecord GetSummerlandRecord()
        {
            return MakeSummerlandRecord();
        }

        public static MarcFactory getFactory()
        {
            return factory;
        }
    }
}
