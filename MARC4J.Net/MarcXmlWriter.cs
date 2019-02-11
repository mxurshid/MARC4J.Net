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
using System.Xml;

namespace MARC4J.Net
{
    /// <summary>
    /// Class for writing MARC record objects in MARCXML format.
    /// </summary>
    public class MarcXmlWriter : IMarcWriter
    {
        #region Fields
        protected const String CONTROL_FIELD = "controlfield";

        protected const String DATA_FIELD = "datafield";

        protected const String SUBFIELD = "subfield";

        protected const String COLLECTION = "collection";

        protected const String RECORD = "record";

        protected const String LEADER = "leader";

        private bool indent = false;

        private XmlWriter writer = null;

        private CharConverter converter = null;

        private bool normalize = false;

        private bool isClosed = false;
        #endregion

        #region Ctors
        /// <summary>
        /// Constructs an instance with the specified output stream.
        /// <para/>
        /// The default character encoding for UTF-8 is used.
        /// </summary>
        /// <param name="output"></param>
        public MarcXmlWriter(Stream output)
            : this(output, false)
        {
        }

        /// <summary>
        /// Constructs an instance with the specified output stream and indentation.
        /// <para/>
        /// The default character encoding for UTF-8 is used.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="indent"></param>
        public MarcXmlWriter(Stream output, bool indent)
            : this(output, "UTF-8", indent)
        {
        }

        /// <summary>
        /// Constructs an instance with the specified output stream and character
        /// encoding.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoding"></param>
        public MarcXmlWriter(Stream output, String encoding)
            : this(output, encoding, false)
        {
        }

        /// <summary>
        /// Constructs an instance with the specified output stream, character
        /// encoding and indentation.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoding"></param>
        /// <param name="indent"></param>
        public MarcXmlWriter(Stream output, String encoding, bool indent)
            : this(output, Encoding.GetEncoding(encoding), indent)
        {
        }

        /// <summary>
        /// Constructs an instance with the specified output stream, character
        /// encoding and indentation.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoding"></param>
        /// <param name="indent"></param>
        public MarcXmlWriter(Stream output, Encoding encoding, bool indent)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output", "null OutputStream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding", "null encoding");
            }
            try
            {
                this.indent = indent;
                writer = XmlWriter.Create(output, new XmlWriterSettings
                {
                    Encoding = encoding,
                    Indent = indent
                });
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
            WriteStartDocument();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Returns the character converter.
        /// </summary>
        public CharConverter Converter
        {
            get
            {
                return converter;
            }
            set
            {
                converter = value;
            }
        }


        /// <summary>
        /// If set to true this writer will perform Unicode normalization on data
        /// elements using normalization form C (NFC). The default is false.
        /// </summary>
        public bool UnicodeNormalization
        {
            get
            {
                return normalize;
            }
            set
            {
                normalize = value;
            }
        }

        /// <summary>
        /// Activates or deactivates indentation. Default value is false.
        /// </summary>
        public bool Indent
        {
            get
            {
                return indent;
            }
            set
            {
                indent = value;
            }
        } 
        #endregion

        #region Methods
        public void Close()
        {
            WriteEndDocument();
            try
            {
                writer.Close();
            }
            catch (IOException e)
            {
                throw new MarcException(e.Message, e);
            }
        }
        protected void WriteStartDocument()
        {
            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement(COLLECTION, Constants.MARCXML_NS_URI);
            }
            catch (Exception e)
            {
                throw new MarcException(
                        "Error occured while writing start document", e);
            }
        }

        protected void WriteEndDocument()
        {
            try
            {

                writer.WriteEndElement();
            }
            catch (Exception e)
            {
                throw new MarcException("Error occured while writing end document", e);
            }
        }

        /// <summary>
        /// Writes a Record object to the result.
        /// </summary>
        /// <param name="record"></param>
        public void Write(IRecord record)
        {
            try
            {
                ToXml(record);
            }
            catch (Exception e)
            {
                throw new MarcException("Error occured while writing record", e);
            }
        }



        protected void ToXml(IRecord record)
        {
            if (!MarcFactory.Instance.ValidateRecord(record))
            {
                throw new MarcException("Marc record didn't validate");
            }

            var elem = new XElement(XName.Get(RECORD, Constants.MARCXML_NS_URI));
            elem.Add(new XElement(XName.Get(LEADER, Constants.MARCXML_NS_URI)) { Value = record.Leader.ToString() });
            foreach (var field in record.GetControlFields())
            {
                elem.Add(new XElement(XName.Get(CONTROL_FIELD, Constants.MARCXML_NS_URI), new XAttribute("tag", field.Tag)) { Value = field.Data });
            }
            foreach (var field in record.GetDataFields())
            {
                var dataFieldElem = new XElement(XName.Get(DATA_FIELD, Constants.MARCXML_NS_URI),
                        new XAttribute("tag", field.Tag),
                        new XAttribute("ind1", field.Indicator1),
                        new XAttribute("ind2", field.Indicator2));

                foreach (var subfield in field.GetSubfields())
                {
                    dataFieldElem.Add(new XElement(XName.Get(SUBFIELD, Constants.MARCXML_NS_URI), new XAttribute("code", subfield.Code))
                    {
                        Value = GetDataElement(subfield.Data)
                    });
                }
                elem.Add(dataFieldElem);
            }

            elem.WriteTo(writer);
        }

        protected string GetDataElement(String data)
        {
            String dataElement = null;
            if (converter == null)
                dataElement = data;
            else
                dataElement = converter.Convert(data);
            if (normalize)
                dataElement = dataElement.Normalize(NormalizationForm.FormC);
            return dataElement;
        }

        public void Dispose()
        {
            if (!isClosed)
            {
                Close();
            }
            writer = null;
            converter = null;
        } 
        #endregion
    }
}