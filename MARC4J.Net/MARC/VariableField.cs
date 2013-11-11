using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public abstract class VariableField : IVariableField
    {
        #region Ctors
        public VariableField()
        {
        }

        public VariableField(String tag)
        {
            if (tag == null)
                throw new MarcException("Attempt to create field with null tag");
            Tag = tag;
        }

        #endregion

        #region Properties
        public string Tag { get; set; }

        private long _id;
        public long Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        } 

        #endregion

        #region Methods
        public virtual bool Find(string pattern)
        {
            throw new NotImplementedException();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public virtual int CompareTo(IVariableField other)
        {
            return Tag.CompareTo(other.Tag);
        }
        public override string ToString()
        {
            return Tag;
        } 

        #endregion
    }
}