using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace MARC4J.Net.MARC
{
    public class DataField : VariableField, IDataField
    {
        #region Ctors
        
        public DataField()
        {
        }

        public DataField(String tag, char ind1, char ind2)
            : base(tag)
        {
            Indicator1 = ind1;
            Indicator2 = ind2;
        } 

        #endregion

        #region Properties & fields

        private IList<ISubfield> subfields = new List<ISubfield>();

        private char ind1 = ' ';
        public char Indicator1
        {
            get
            {
                return ind1;
            }
            set
            {
                ind1 = value;
            }
        }

        private char ind2 = ' ';
        public char Indicator2
        {
            get
            {
                return ind2;
            }
            set
            {
                ind2 = value;
            }
        }
        #endregion

        #region Methods

        public void AddSubfield(ISubfield subfield)
        {
            if (subfield is ISubfield)
                subfields.Add(subfield);
            else
                throw new IllegalAddException("Subfield");
        }

        public void AddSubfield(int index, ISubfield subfield)
        {
            subfields.Insert(index, subfield);
        }

        public void RemoveSubfield(ISubfield subfield)
        {
            subfields.Remove(subfield);
        }
        public IList<ISubfield> GetSubfields()
        {
            return subfields;
        }

        public IList<ISubfield> GetSubfields(char code)
        {
            return subfields.Where(a => a.Code == code)
                            .ToList();
        }

        public ISubfield GetSubfield(char code)
        {
            return subfields.FirstOrDefault(a => a.Code == code);
        }

        public bool Find(String pattern)
        {
            Regex reg = new Regex(pattern);
            return subfields.Any(a => reg.IsMatch(a.Data));
        }

        /// <summary>
        /// Returns a string representation of this data field.
        /// Example:
        ///    245 10$aSummerland /$cMichael Chabon.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(' ');
            sb.Append(Indicator1);
            sb.Append(Indicator2);
            sb.Append(string.Join(string.Empty, subfields.Select(a => a.ToString())));
            return sb.ToString();
        } 

        #endregion
    }
}