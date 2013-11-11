using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public class Leader : ILeader
    {
        public Leader() { }

        public Leader(String ldr)
        {
            UnMarshal(ldr);
        }

        public void UnMarshal(String ldr)
        {
            try
            {
                String s;
                s = ldr.Substring(0, 5);
                if (IsInteger(s))
                    RecordLength = int.Parse(s);
                else
                    RecordLength = 0;
                RecordStatus = ldr[5];
                TypeOfRecord = ldr[6];
                ImplDefined1 = ldr.Substring(7, 2).ToCharArray();
                CharCodingScheme = ldr[9];
                s = ldr[10].ToString();
                if (IsInteger(s))
                    IndicatorCount = int.Parse(s);
                else
                    IndicatorCount = 2;
                s = ldr[10].ToString();
                if (IsInteger(s))
                    SubfieldCodeLength = int.Parse(s);
                else
                    SubfieldCodeLength = 2;
                s = ldr.Substring(12, 5);
                if (IsInteger(s))
                    BaseAddressOfData = int.Parse(s);
                else
                    BaseAddressOfData = 0;
                ImplDefined2 = ldr.Substring(17, 3).ToCharArray();
                EntryMap = ldr.Substring(20, 4).ToCharArray();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to parse leader", e);
            }
        }

        /// <summary>
        /// Returns a string representation of this leader.
        /// 
        /// Example: 00714cam a2200205 a 4500
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return RecordLength.ToString().PadLeft(5, '0') + 
                   RecordStatus + 
                   TypeOfRecord +
                   new String(ImplDefined1) +
                   CharCodingScheme +
                   IndicatorCount +
                   SubfieldCodeLength +
                   BaseAddressOfData.ToString().PadLeft(5, '0') +
                   new String(ImplDefined2) + 
                   new String(EntryMap);
        }

        private bool IsInteger(String value)
        {
            int len = value.Length;
            if (len == 0)
                return false;
            int i = 0;
            do
            {
                switch (value[i])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    default:
                        return false;
                }
            } while (++i < len);
            return true;
        }

        #region Properties & fields

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

        private int _recordLength;
        public int RecordLength
        {
            get
            {
                return _recordLength;
            }
            set
            {
                _recordLength = value;
            }
        }

        private char _recordStatus;
        public char RecordStatus
        {
            get
            {
                return _recordStatus;
            }
            set
            {
                _recordStatus = value;
            }
        }

        private char _typeOfRecord;
        public char TypeOfRecord
        {
            get
            {
                return _typeOfRecord;
            }
            set
            {
                _typeOfRecord = value;
            }
        }

        private char[] _implDefined1;
        public char[] ImplDefined1
        {
            get
            {
                return _implDefined1;
            }
            set
            {
                _implDefined1 = value;
            }
        }

        private char _charCodingScheme;
        public char CharCodingScheme
        {
            get
            {
                return _charCodingScheme;
            }
            set
            {
                _charCodingScheme = value;
            }
        }

        private int _indicatorCount;
        public int IndicatorCount
        {
            get
            {
                return _indicatorCount;
            }
            set
            {
                _indicatorCount = value;
            }
        }

        private int _subfieldCodeLength;
        public int SubfieldCodeLength
        {
            get
            {
                return _subfieldCodeLength;
            }
            set
            {
                _subfieldCodeLength = value;
            }
        }

        private int _baseAddressOfData;
        public int BaseAddressOfData
        {
            get
            {
                return _baseAddressOfData;
            }
            set
            {
                _baseAddressOfData = value;
            }
        }

        private char[] _implDefined2;
        public char[] ImplDefined2
        {
            get
            {
                return _implDefined2;
            }
            set
            {
                _implDefined2 = value;
            }
        }

        private char[] _entryMap;
        public char[] EntryMap
        {
            get
            {
                return _entryMap;
            }
            set
            {
                _entryMap = value;
            }
        } 

        #endregion

        public string Marshal()
        {
            return ToString();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}