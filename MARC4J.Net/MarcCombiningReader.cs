using System;
using System.Collections;
using System.Collections.Generic;
using MARC4J.Net.MARC;

namespace MARC4J.Net
{
    /// <summary>
    /// Binary Marc records have a maximum size of 99999 bytes.  In the data dumps from 
    /// the Sirsi/Dynix Virgo system if a record with all of its holdings information 
    /// attached would be greater that that size, the records is written out multiple
    /// times with each subsequent record containing a subset of the total holdings information.
    /// This class reads ahead to determine when the next record in a Marc file is actually 
    /// a continuation of the same record.   When this occurs, the holdings information in the
    /// next record is appended to/merged with the in-memory Marc record representation already 
    /// read. 
    /// </summary>
    public class MarcCombiningReader : IMarcReader
    {
        #region Fields
        
        IRecord _currentRecord = null;
        IRecord _nextRecord = null;
        IMarcReader _reader;
        String _idsToMerge = null;
        String _leftControlField = null;
        String _rightControlField = null;
        ErrorHandler _nextErrors;
        ErrorHandler _currentErrors; 

        #endregion

        #region Ctors
        /// <summary>
        /// Constructor for a "combining" Marc reader, that looks ahead at the Marc file to determine 
        /// when the next record is a continuation of the currently read record.  
        /// </summary>
        /// <param name="reader">The Lower level MarcReader that returns Marc4J.Net Record objects that are read from a Marc file.</param>
        /// <param name="idsToMerge">string representing a regular expression matching those fields to be merged for continuation records.</param>
        /// <param name="leftControlField">string representing a control field in the current record to use for matching purposes (null to default to 001).</param>
        /// <param name="rightControlField">string representing a control field in the next record to use for matching purposes (null to default to 001).</param>
        public MarcCombiningReader(IMarcReader reader, String idsToMerge, String leftControlField, String rightControlField)
        {
            this._reader = reader;
            this._idsToMerge = idsToMerge;
            this._leftControlField = leftControlField;
            this._rightControlField = rightControlField;
            this._nextErrors = null;
            this._currentErrors = null;
        }

        /// <summary>
        /// Constructor for a "combining" Marc reader, that looks ahead at the Marc file to determine 
        /// when the next record is a continuation of the currently read record.  Because this reader 
        /// needs to have two records in memory to determine when the subsequent record is a continuation,
        /// if Error Handling is being performed, this constructor needs to be used, so that the errors 
        /// from the "next" record are not appended to the results for the "current" record.
        /// <para>Call this constructor in the following way:                                                          </para>
        /// <para>         ErrorHandler errors2 = errors;                                                              </para>
        /// <para>         errors = new ErrorHandler();                                                                </para>
        /// <para>         reader = new MarcCombiningReader(reader, errors, errors2, combineConsecutiveRecordsFields); </para>
        /// </summary>
        /// <param name="reader">The Lower level MarcReader that returns Marc4J.Net Record objects that are read from a Marc file.</param>
        /// <param name="currentErrors">ErrorHandler Object to use for attaching errors to a record.</param>
        /// <param name="nextErrors">ErrorHandler Object that was passed into the lower level IMarcReader</param>
        /// <param name="idsToMerge">string representing a regular expression matching those fields to be merged for continuation records.</param>
        /// <param name="leftControlField">string representing a control field in the current record to use for matching purposes (null to default to 001).</param>
        /// <param name="rightControlField">string representing a control field in the next record to use for matching purposes (null to default to 001).</param>
        public MarcCombiningReader(IMarcReader reader, ErrorHandler currentErrors, ErrorHandler nextErrors, String idsToMerge,
            String leftControlField, String rightControlField)
        {
            this._reader = reader;
            this._idsToMerge = idsToMerge;
            this._leftControlField = leftControlField;
            this._rightControlField = rightControlField;
            this._nextErrors = nextErrors;
            this._currentErrors = currentErrors;
        }
        #endregion

        #region IMarcReader implementation

        public IRecord Current
        {
            get { return _currentRecord; }
        }

        public void Dispose()
        {
            _currentRecord = null;
        }

        object IEnumerator.Current
        {
            get { return _currentRecord; }
        }

        public bool MoveNext()
        {
            if (_currentRecord == null)
            {
                _currentRecord = Next();
            }
            return _currentRecord != null;
        }

        public void Reset()
        {
            _currentRecord = null;
            _nextRecord = null;
        }
        public IEnumerator<IRecord> GetEnumerator()
        {
            while (MoveNext())
                yield return _currentRecord;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private methods
        private IRecord Next()
        {
            if (_currentRecord != null)
            {
                IRecord tmp = _currentRecord;
                _currentRecord = null;
                return tmp;
            }
            else
                if (_currentRecord == null)
                {
                    if (_nextRecord != null)
                    {
                        _currentRecord = _nextRecord;
                        CopyErrors(_currentErrors, _nextErrors);
                        _nextRecord = null;
                    }
                    if (!_reader.MoveNext())
                    {
                        return (_currentRecord != null) ? Next() : null;
                    }

                    try
                    {
                        _nextRecord = _reader.Current;
                    }
                    catch (Exception e)
                    {
                        if (_currentRecord != null)
                        {
                            String recCntlNum = _currentRecord.GetControlNumber();
                            throw new MarcException("Couldn't get next record after " + (recCntlNum != null ? recCntlNum : "") + " -- " + e.ToString());
                        }
                        else
                            throw new MarcException("Marc record couldn't be read -- " + e.ToString());
                    }


                    while (RecordsMatch(_currentRecord, _nextRecord))
                    {
                        _currentRecord = CombineRecords(_currentRecord, _nextRecord, _idsToMerge);
                        MergeErrors(_currentErrors, _nextErrors);
                        if (_reader.MoveNext())
                        {
                            try
                            {
                                _nextRecord = _reader.Current;
                            }
                            catch (Exception e)
                            {
                                var recCntlNum = _currentRecord.GetControlNumber();
                                throw new MarcException("Couldn't get next record after " + (recCntlNum != null ? recCntlNum : "") + " -- " + e.ToString());
                            }
                        }
                        else
                        {
                            _nextRecord = null;
                        }
                    }
                    return Next();
                }
            return null;
        }

        /// <summary>
        /// Support method to find a specific control field within a record and return
        /// its contents as a string.
        /// </summary>
        /// <param name="record">record to search</param>
        /// <param name="tag">tag number to search for</param>
        /// <returns></returns>
        private String FindControlField(IRecord record, String tag)
        {
            var tagstart = tag.Substring(0, 3);
            IList<IVariableField> fields = record.GetVariableFields(tagstart);
            foreach (var field in fields)
            {
                if (field is IControlField)
                {
                    var cf = (IControlField)field;
                    if (cf.Tag.StartsWith(tagstart))
                    {
                        return cf.Data;
                    }
                }
                else if (field is IDataField)
                {
                    var df = (IDataField)field;
                    if (df.Tag.StartsWith(tagstart))
                    {
                        char subfieldtag = 'a';
                        if (tag.Length > 3) subfieldtag = tag[4];
                        var sf = df.GetSubfield(subfieldtag);
                        if (sf != null) return (sf.Data);
                    }
                }
            }
            return null;
        }

        /// <summary>
        ///  Support method to detect if two records match.
        /// </summary>
        /// <param name="left">left side of the comparison (current record)</param>
        /// <param name="right">right side of the comparison (next record)</param>
        /// <returns></returns>
        private bool RecordsMatch(IRecord left, IRecord right)
        {
            // Records can't match if they don't exist!
            if (left == null || right == null)
            {
                return false;
            }

            // Initialize match strings extracted from records:
            String leftStr = null;
            String rightStr = null;

            // For both sides of the match (left and right), check to see if the user
            // provided a control field setting.  If no preference was provided, we'll
            // match using the record ID.  If a preference exists, we need to look up
            // the specified control field in the record.
            if (_leftControlField == null)
            {
                leftStr = left.GetControlNumber();
            }
            else
            {
                leftStr = FindControlField(left, _leftControlField);
            }
            if (_rightControlField == null)
            {
                rightStr = right.GetControlNumber();
            }
            else
            {
                rightStr = FindControlField(right, _rightControlField);
            }

            // Check for a match and return an appropriate status:
            if (leftStr != null && rightStr != null && leftStr.Equals(rightStr))
            {
                return true;
            }
            return false;
        }

        private void CopyErrors(ErrorHandler currentErr, ErrorHandler nextErr)
        {
            if (currentErr != null && nextErr != null)
            {
                currentErr.Reset();
                MergeErrors(currentErr, nextErr);
            }
        }

        private void MergeErrors(ErrorHandler currentErr, ErrorHandler nextErr)
        {
            if (currentErr != null && nextErr != null)
            {
                currentErr.AddErrors(nextErr.GetErrors());
            }
        } 
        #endregion

        #region Static methods
        static public IRecord CombineRecords(IRecord currentRecord, IRecord nextRecord, String idsToMerge)
        {
            var fields = nextRecord.GetVariableFields();
            foreach (var field in fields)
            {
                if (field.Tag.Contains(idsToMerge))
                {
                    currentRecord.AddVariableField(field);
                }
            }
            return currentRecord;
        }

        static public IRecord CombineRecords(IRecord currentRecord, IRecord nextRecord, String idsToMerge, String fieldInsertBefore)
        {
            var existingFields = currentRecord.GetVariableFields();
            var fieldsToMove = new List<IVariableField>();
            // temporarily remove some existing fields
            foreach (var field in existingFields)
            {
                if (field.Tag.Contains(fieldInsertBefore))
                {
                    fieldsToMove.Add(field);
                    currentRecord.RemoveVariableField(field);
                }
            }
            var fields = nextRecord.GetVariableFields();
            foreach (var field in fields)
            {
                if (field.Tag.Contains(idsToMerge))
                {
                    currentRecord.AddVariableField(field);
                }
            }
            // now add back the temporarily removed fields
            foreach (var field in fieldsToMove)
            {
                currentRecord.AddVariableField(field);
            }
            return currentRecord;
        } 
        #endregion
    }
}