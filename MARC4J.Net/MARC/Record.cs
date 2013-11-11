using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace MARC4J.Net.MARC
{
    public class Record : IRecord
    {
        #region Ctor
        public Record()
        {
            _controlFields = new List<IControlField>();
            _dataFields = new List<IDataField>();
        }
        #endregion

        #region Properties & fields

        private long _id;
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private ILeader _leader;
        public ILeader Leader
        {
            get { return _leader; }
            set { _leader = value; }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        protected List<IControlField> _controlFields;
        protected List<IControlField> ControlFields
        {
            get
            {
                return _controlFields;
            }
        }

        protected List<IDataField> _dataFields;
        protected List<IDataField> DataFields
        {
            get
            {
                return _dataFields;
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Adds a <code>VariableField</code> being a <code>ControlField</code>
        /// or <code>DataField</code>.
        /// If the <code>VariableField</code> is a control number field (001) and
        /// the record already has a control number field, the field is replaced with
        /// the new instance.
        /// </summary>
        /// <param name="field"></param>
        public virtual void AddVariableField(IVariableField field)
        {
            var tag = field.Tag;
            if (field is IControlField)
            {
                var controlField = (IControlField)field;
                if (Verifier.IsControlNumberField(tag))
                {
                    if (Verifier.HasControlNumberField(_controlFields))
                        _controlFields[0] = controlField;
                    else
                        _controlFields.Insert(0, controlField);
                }
                else
                {
                    _controlFields.Add(controlField);
                }
            }
            else
            {
                _dataFields.Add((IDataField)field);
            }

        }

        public virtual void RemoveVariableField(IVariableField field)
        {
            var tag = field.Tag;
            if (Verifier.IsControlField(tag))
                _controlFields.Remove(field as IControlField);
            else
                _dataFields.Remove(field as IDataField);
        }

        /// <summary>
        /// Returns the control number field or <code>null</code> if no control
        /// number field is available.
        /// </summary>
        /// <returns></returns>
        public virtual IControlField GetControlNumberField()
        {
            if (Verifier.HasControlNumberField(_controlFields))
                return _controlFields[0];
            else
                return null;
        }

        public virtual IList<IControlField> GetControlFields()
        {
            return _controlFields;
        }

        public virtual IList<IDataField> GetDataFields()
        {
            return _dataFields;
        }

        public virtual IVariableField GetVariableField(string tag)
        {
            IEnumerator<IVariableField> i;
            if (Verifier.IsControlField(tag))
                i = _controlFields.GetEnumerator();
            else
                i = _dataFields.GetEnumerator();
            while (i.MoveNext())
            {
                var field = i.Current;
                if (field.Tag.Equals(tag))
                    return field;
            }
            return null;
        }

        public virtual IList<IVariableField> GetVariableFields(string tag)
        {
            IList<IVariableField> fields = new List<IVariableField>();
            IEnumerator<IVariableField> i;
            if (Verifier.IsControlField(tag))
                i = _controlFields.GetEnumerator();
            else
                i = _dataFields.GetEnumerator();
            while (i.MoveNext())
            {
                var field = i.Current;
                if (field.Tag.Equals(tag))
                    fields.Add(field);
            }
            return fields;
        }

        public virtual IList<IVariableField> GetVariableFields()
        {
            IList<IVariableField> fields = new List<IVariableField>();
            IEnumerator<IVariableField> i;
            i = _controlFields.GetEnumerator();
            while (i.MoveNext())
                fields.Add(i.Current);
            i = _dataFields.GetEnumerator();
            while (i.MoveNext())
                fields.Add(i.Current);
            return fields;
        }

        public virtual string GetControlNumber()
        {
            var f = GetControlNumberField();

            if (f == null || f.Data == null)
                return null;
            else
                return f.Data;
        }

        public IList<IVariableField> GetVariableFields(String[] tags)
        {
            var list = new List<IVariableField>();
            for (int i = 0; i < tags.Length; i++)
            {
                String tag = tags[i];
                var fields = GetVariableFields(tag);
                if (fields.Count > 0)
                    list.AddRange(fields);
            }

            return list;
        }

        /// <summary>
        /// Returns a string representation of this record.
        /// Example:
        /// 
        /// <para>LEADER 00714cam a2200205 a 4500                                       </para>
        /// <para>001 12883376                                                          </para>
        /// <para>005 20030616111422.0                                                  </para>
        /// <para>008 020805s2002 nyu j 000 1 eng                                       </para>
        /// <para>020   $a0786808772                                                    </para>
        /// <para>020   $a0786816155 (pbk.)                                             </para>
        /// <para>040   $aDLC$cDLC$dDLC                                                 </para>
        /// <para>100 1 $aChabon, Michael.                                              </para>
        /// <para>245 10$aSummerland /$cMichael Chabon.                                 </para>
        /// <para>250   $a1st ed.                                                       </para>
        /// <para>260   $aNew York :$bMiramax Books/Hyperion Books for Children,$cc2002.</para>
        /// <para>300   $a500 p. ;$c22 cm.                                              </para>
        /// <para>650  1$aFantasy.                                                      </para>
        /// <para>650  1$aBaseball$vFiction.                                            </para>
        /// <para>650  1$aMagic$vFiction.                                               </para>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("LEADER ");
            sb.Append(Leader.ToString());
            sb.Append('\n');
            foreach (var field in GetVariableFields())
            {
                sb.Append(field.ToString());
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public virtual IList<IVariableField> Find(string pattern)
        {
            var result = new List<IVariableField>();
            IEnumerator<IVariableField> i = _controlFields.GetEnumerator();
            while (i.MoveNext())
            {
                var field = i.Current;
                if (field.Find(pattern))
                    result.Add(field);
            }
            i = _dataFields.GetEnumerator();
            while (i.MoveNext())
            {
                var field = i.Current;
                if (field.Find(pattern))
                    result.Add(field);
            }
            return result;
        }

        public virtual IList<IVariableField> Find(String tag, String pattern)
        {
            var result = new List<IVariableField>();
            foreach (var field in GetVariableFields(tag))
            {
                if (field.Find(pattern))
                    result.Add(field);
            }
            return result;
        }

        public virtual IList<IVariableField> Find(String[] tag, String pattern)
        {
            var result = new List<IVariableField>();
            foreach (var field in GetVariableFields(tag))
            {
                if (field.Find(pattern))
                    result.Add(field);
            }
            return result;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}