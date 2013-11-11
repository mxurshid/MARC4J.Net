using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public abstract class MarcFactory
    {
        #region Default Instance
        private static MarcFactory _instance;
        public static MarcFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance<DefaultMarcFactory>();
                }
                return _instance;
            }
        }

        #endregion

        #region Ctor
        protected MarcFactory()
        {
        }

        #endregion

        public abstract IControlField NewControlField();

        /// <summary>
        /// Creates a new control field with the given tag and returns the instance.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public abstract IControlField NewControlField(String tag);

        /// <summary>
        /// Creates a new control field with the given tag and data and returns the instance.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract IControlField NewControlField(String tag, String data);

        /// <summary>
        /// Returns a new data field instance.
        /// </summary>
        /// <returns></returns>
        public abstract IDataField NewDataField();

        /// <summary>
        /// Creates a new data field with the given tag and indicators and returns the
        /// instance.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="ind1"></param>
        /// <param name="ind2"></param>
        /// <returns></returns>
        public abstract IDataField NewDataField(String tag, char ind1, char ind2);

        /// <summary>
        /// Creates a new data field with the given tag and indicators and subfields and returns
        /// the instance.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="ind1"></param>
        /// <param name="ind2"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public abstract IDataField NewDataField(String tag, char ind1, char ind2, params String[] subfieldCodesAndData);

        /// <summary>
        /// Returns a new leader instance.
        /// </summary>
        /// <returns></returns>
        public abstract ILeader NewLeader();

        /// <summary>
        /// Creates a new leader with the given <c>String</c> object.
        /// </summary>
        /// <param name="ldr"></param>
        /// <returns></returns>
        public abstract ILeader NewLeader(String ldr);

        /// <summary>
        /// Returns a new record instance.
        /// </summary>
        /// <returns></returns>
        public abstract IRecord NewRecord();

        /// <summary>
        /// Returns a new record instance.
        /// </summary>
        /// <param name="leader"></param>
        /// <returns></returns>
        public abstract IRecord NewRecord(ILeader leader);

        /// <summary>
        /// Returns a new record instance.
        /// </summary>
        /// <param name="leader"></param>
        /// <returns></returns>
        public abstract IRecord NewRecord(String leader);

        /// <summary>
        /// Returns a new subfield instance.
        /// </summary>
        /// <returns></returns>
        public abstract ISubfield NewSubfield();

        /// <summary>
        /// Creates a new subfield with the given identifier.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public abstract ISubfield NewSubfield(char code);

        /// <summary>
        /// Creates a new subfield with the given identifier and data.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract ISubfield NewSubfield(char code, String data);

        public bool ValidateRecord(IRecord record)
        {
            if (record.Leader == null)
            {
                return false;
            }
            foreach (IControlField controlField in record.GetControlFields())
            {
                if (!ValidateControlField(controlField))
                {
                    return false;
                }
            }
            foreach (IDataField dataField in record.GetDataFields())
            {
                if (!ValidateDataField(dataField))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ValidateVariableField(IVariableField field)
        {
            return field.Tag != null;
        }

        public bool ValidateControlField(IControlField field)
        {
            return ValidateVariableField(field) && field.Data != null;
        }

        public bool ValidateDataField(IDataField field)
        {
            if (!ValidateVariableField(field))
            {
                return false;
            }
            if (field.Indicator1 == 0 || field.Indicator2 == 0)
            {
                return false;
            }
            foreach (ISubfield subfield in field.GetSubfields())
            {
                if (!ValidateSubField(subfield))
                {
                    return false;
                }
            }
            return true;
        }
        public bool ValidateSubField(ISubfield subfield)
        {
            return subfield.Code != 0 && subfield.Data != null;
        }

        ~MarcFactory()
        {
            if (_instance != null)
                _instance = null;
        }
    }
}