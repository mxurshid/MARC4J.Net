using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using MARC4J.Net.Converter;
using MARC4J.Net.MARC;

namespace MARC4J.Net
{
    public class MarcTranslatedReader : IMarcReader
    {
        #region Fields
        
        readonly IMarcReader reader;
        CharConverter convert;
        NormalizationForm unicodeNormalize;
        IRecord currentRecord = null; 

        #endregion

        #region Ctors
        /// <summary>
        /// Initialize logging category
        /// </summary>
        /// <param name="r"></param>
        /// <param name="unicodeNormalizeBool"></param>
        public MarcTranslatedReader(IMarcReader r, bool unicodeNormalizeBool)
        {
            reader = r;
            convert = new AnselToUnicode();
            if (unicodeNormalizeBool) this.unicodeNormalize = NormalizationForm.FormC;
        }

        public MarcTranslatedReader(IMarcReader r, String unicodeNormalizeStr)
        {
            reader = r;
            convert = new AnselToUnicode();
            if (unicodeNormalizeStr.Equals("KC")) unicodeNormalize = NormalizationForm.FormKC;
            else if (unicodeNormalizeStr.Equals("KD")) unicodeNormalize = NormalizationForm.FormKD;
            else if (unicodeNormalizeStr.Equals("C")) unicodeNormalize = NormalizationForm.FormC;
            else if (unicodeNormalizeStr.Equals("D")) unicodeNormalize = NormalizationForm.FormD;
            else unicodeNormalize = NormalizationForm.FormC;
        }

        public MarcTranslatedReader(IMarcReader r, NormalizationForm unicodeNormalize)
        {
            reader = r;
            convert = new AnselToUnicode();
            this.unicodeNormalize = unicodeNormalize;
        } 

        #endregion

        #region Methods
        public IRecord Next()
        {
            IRecord rec = reader.Current;
            ILeader l = rec.Leader;
            bool is_utf_8 = false;
            if (l.CharCodingScheme == 'a') is_utf_8 = true;
            if (is_utf_8 && unicodeNormalize == NormalizationForm.FormC) return rec;
            var fields = rec.GetVariableFields();
            foreach (var f in fields)
            {
                if (!(f is IDataField)) continue;
                var field = (IDataField)f;
                var subfields = field.GetSubfields();
                foreach (var sf in subfields)
                {
                    String oldData = sf.Data;
                    String newData = oldData;
                    if (!is_utf_8) newData = convert.Convert(newData);
                    if (unicodeNormalize != NormalizationForm.FormC)
                    {
                        newData = newData.Normalize(unicodeNormalize);
                    }
                    if (!oldData.Equals(newData))
                    {
                        sf.Data = newData;
                    }
                }
            }
            l.CharCodingScheme = 'a';
            rec.Leader = l;
            return rec;
        } 
        #endregion

        #region IMarkReader implementation
        public IRecord Current
        {
            get { return currentRecord; }
        }

        public void Dispose()
        {
            currentRecord = null;
        }

        object IEnumerator.Current
        {
            get { return currentRecord; }
        }

        public bool MoveNext()
        {
            if (reader.MoveNext())
            {
                currentRecord = Next();
                return currentRecord != null;
            }
            return false;
        }

        public void Reset()
        {
            currentRecord = null;
            reader.Reset();
        }

        public IEnumerator<IRecord> GetEnumerator()
        {
            while (MoveNext())
                yield return currentRecord;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}