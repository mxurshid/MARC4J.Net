using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MARC4J.Net.Converter;
using MARC4J.Net.MARC;

namespace MARC4J.Net
{
    public class MarcXmlReader : IEnumerable<IRecord>
    {
        private IControlField controlField;

        private IDataField dataField;

        private IRecord record;

        /** Constants representing each valid tag type */
        private const string COLLECTION = "collection";

        private const string LEADER = "leader";

        private const string RECORD = "record";

        private const string CONTROLFIELD = "controlfield";

        private const string DATAFIELD = "datafield";

        private const string SUBFIELD = "subfield";

        /** The tag attribute name string */
        private const String TAG_ATTR = "tag";

        /** The code attribute name string */
        private const String CODE_ATTR = "code";

        /** The first indicator attribute name string */
        private const String IND_1_ATTR = "ind1";

        /** The second indicator attribute name string */
        private const String IND_2_ATTR = "ind2";

        /** Dictionary for mapping of element strings to constants (int) */

        private readonly MarcFactory factory = null;

        private readonly Stream stream = null;

        public MarcXmlReader()
        {
            factory = MarcFactory.Instance;
        }

        public MarcXmlReader(Stream stream) : this()
        {
            this.stream = stream;
        }

        public IEnumerable<IRecord> Read(Stream stream)
        {
            var collection = XElement.Load(stream);
            foreach (var recordElem in collection.Descendants(XName.Get(RECORD, Constants.MARCXML_NS_URI)))
            {
                record = factory.NewRecord();

                LoadRecord(recordElem, ref record);

                yield return record;
            }
            yield break;
        }

        private void LoadRecord(XElement recordElement, ref IRecord rec)
        {
            foreach (var elem in recordElement.Elements())
            {
                switch (elem.Name.LocalName)
                {
                    case LEADER:
                        rec.Leader = factory.NewLeader(elem.Value);
                        break;
                    case CONTROLFIELD:
                        rec.AddVariableField(
                            factory.NewControlField(
                                elem.Attribute(TAG_ATTR).Value, elem.Value));
                        break;
                    case DATAFIELD:
                        var ind1 = elem.Attribute(IND_1_ATTR) != null ? elem.Attribute(IND_1_ATTR).Value : " ";
                        var ind2 = elem.Attribute(IND_2_ATTR) != null ? elem.Attribute(IND_2_ATTR).Value : " ";
                        dataField = factory.NewDataField(
                            elem.Attribute(TAG_ATTR).Value,
                            string.IsNullOrEmpty(ind1) ? ' ' : ind1[0],
                            string.IsNullOrEmpty(ind2) ? ' ' : ind2[0]);

                        foreach (var subField in elem.Elements(XName.Get(SUBFIELD, Constants.MARCXML_NS_URI)).Select(a =>
                                                            factory.NewSubfield(
                                                                a.Attribute(CODE_ATTR).Value[0],
                                                                a.Value)))
                        {
                            dataField.AddSubfield(subField);
                        }
                        rec.AddVariableField(dataField);
                        break;
                    default:
                        break;
                }
            }
        }

        public IEnumerator<IRecord> GetEnumerator()
        {
            return Read(stream).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}