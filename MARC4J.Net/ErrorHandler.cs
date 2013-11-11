using System;
using System.Collections;
using System.Collections.Generic;

namespace MARC4J.Net
{
    public class Error
    {
        /// <summary>
        /// FATAL is the most severe error, it is usually set in conjunction with throwing an
        /// exception, generally no record is returned when a FATAL error occurs.  Although in 
        /// some instances (a record with a field > 9999 bytes long) a record will be returned 
        /// that can be used, but it cannot be written back out without causing an error.
        ///  </summary>
        public const int FATAL = 4;

        /// <summary>
        /// MAJOR_ERROR indicates that a serious problem existed with the record, such as a 
        /// malformed directory or an invalid subfield tag, or an encoding error where missing 
        /// data had to be inferred through some heuristic process.  This indicates that 
        /// although a record is returned, you cannot be sure that the record is not corrupted.
        /// </summary>
        public const int MAJOR_ERROR = 3;

        /// <summary>
        /// MINOR_ERROR indicates that a less serious problem existed with the record, such as 
        /// a mismatch between the directory stated field sizes and the actual field sizes, 
        /// or an encoding error where extraneous data had to be discarded to correctly 
        /// interpret the data.  
        /// </summary>
        public const int MINOR_ERROR = 2;

        /// <summary>
        /// ERROR_TYPO indicates that an even less severe problem was found with the record,
        /// such as the record leader ends with characters other than "4500" or a field tag 
        /// contains non-numeric characters the record contains a html-style entity reference 
        /// such as &amp; or &quot; which was replaced with the unescaped version. 
        /// </summary>
        public const int ERROR_TYPO = 1;

        /// <summary>
        /// INFO is used to pass information about the record translation process.  It does 
        /// not indicate an error.  It usually will occur when a defaultEncoding value of "BESTGUESS"
        /// is passed in.  INFO statements are generated to indicate which character encoding was 
        /// determined to be the best fit for the data, and why.
        /// </summary>
        public const int INFO = 0;

        public String CurRecordID { get; internal set; }
        public String curField;
        public String curSubfield;
        public int severity;
        public String message;

        /// <summary>
        /// Error
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="field"></param>
        /// <param name="subfield"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        internal Error(String recordID, String field, String subfield, int severity, String message)
        {
            CurRecordID = recordID;
            curField = field;
            curSubfield = subfield;
            this.severity = severity;
            this.message = message;
        }

        /// <summary>
        /// Provides a descriptive string representation of the severity level.
        /// 
        /// <returns>
        /// a descriptive string representation of the severity level
        /// </returns>
        /// </summary>
        private String GetSeverityMsg(int severity)
        {
            switch (severity)
            {
                case FATAL: return ("FATAL       ");
                case MAJOR_ERROR: return ("Major Error ");
                case MINOR_ERROR: return ("Minor Error ");
                case ERROR_TYPO: return ("Typo        ");
                case INFO: return ("Info        ");
            }
            return null;
        }

        /// <summary>
        /// Formats the error message for display
        /// </summary>
        public override string ToString()
        {
            String severityMsg = GetSeverityMsg(severity);
            String ret = severityMsg + " : " + message + " --- [ " + curField + " : " + curSubfield + " ]";
            return ret;
        }
    }
    public class ErrorHandler
    {
        private ICollection<Error> errors;
        private String curRecordID;
        private String curField;
        private String curSubfield;
        private bool hasMissingID;
        private int maxSeverity;

        public ErrorHandler()
        {
            errors = null;
            hasMissingID = false;
            maxSeverity = Error.INFO;
        }

        /// <summary>
        /// Returns true if any errors (or warnings) were encountered in processing the 
        /// current record.  Note that if only INFO level messages are encountered for a 
        /// given record, this method will return false.
        /// 
        ///@return bool - The highest error severity level encountered for the current record.
        /// </summary>
        public bool HasErrors()
        {
            return (errors != null && errors.Count > 0 && maxSeverity > Error.INFO);
        }

        /// <summary>
        /// Returns the highest error severity level encountered in processing the current record.
        /// 
        ///@return int - The highest error severity level encountered for the current record.
        /// </summary>
        public int GetMaxSeverity()
        {
            return (maxSeverity);
        }

        /// <summary>
        /// Returns a list of all of the errors encountered in processing the current record.
        /// 
        ///@return List - A list of all of the errors encountered for the current record.
        /// </summary>
        public ICollection<Error> GetErrors()
        {
            if (errors == null || errors.Count == 0) return null;
            return (errors);
        }

        /// <summary>
        /// Resets the list of errors to empty. This should be called at the beginning of 
        /// processing of each record.
        /// </summary>
        public void Reset()
        {
            errors = null;
            maxSeverity = Error.INFO;
        }

        /// <summary>
        /// Logs an error message using the stated severity level.  Uses the values passed  
        /// in id, field, and subfield to note the location of the error.
        ///
        ///@param id - the record ID of the record currently being processed
        ///@param field - the tag of the field currently being processed
        ///@param subfield - the subfield tag of the subfield currently being processed
        ///@param severity - An indication of the relative severity of the error that was 
        ///						encountered.
        ///@param message - A descriptive message about the error that was encountered.
        /// </summary>
        public void AddError(String id, String field, String subfield, int severity, String message)
        {
            if (errors == null)
            {
                errors = new LinkedList<Error>();
                hasMissingID = false;
            }
            if (id != null && id.Equals("unknown")) hasMissingID = true;
            else if (hasMissingID)
            {
                SetRecordIDForAll(id);
            }
            errors.Add(new Error(id, field, subfield, severity, message));
            if (severity > maxSeverity) maxSeverity = severity;
        }

        /// <summary>
        /// Logs an error message using the stated severity level.  Uses the values stored 
        /// in curRecordID, curField, and curSubfield to note the location of the error.
        ///
        ///@param severity - An indication of the relative severity of the error that was 
        ///                     encountered.
        ///@param message - A descriptive message about the error that was encountered.
        /// </summary>
        public void AddError(int severity, String message)
        {
            AddError(curRecordID, curField, curSubfield, severity, message);
        }

        /// <summary>
        /// Copys a List of errors into the current error handler
        ///
        ///@param newErrors - A list of Errors.
        ///@param message - A descriptive message about the error that was encountered.
        /// </summary>
        public void AddErrors(ICollection<Error> newErrors)
        {
            if (newErrors == null || newErrors.Count == 0) return;
            if (errors == null)
            {
                errors = new LinkedList<Error>();
                hasMissingID = false;
            }
            foreach (var err in newErrors)
            {
                errors.Add(err);
                if (err.severity > maxSeverity) maxSeverity = err.severity;
            }
        }

        private void SetRecordIDForAll(String id)
        {
            if (id != null)
            {
                var iter = errors.GetEnumerator();
                while (iter.MoveNext())
                {
                    var err = iter.Current;
                    if (err.CurRecordID == null || err.CurRecordID.Equals("unknown"))
                    {
                        err.CurRecordID = id;
                    }
                }
                hasMissingID = false;
            }
        }

        /// <summary>
        /// Sets the record ID to be stored for subsequent error messages that are logged
        /// If any previous messages are stored for the current record that don't have a 
        /// stored record ID, set the value for those entries to this value also.
        ///
        /// @param recordID - the record ID of the record currently being processed
        /// </summary>
        public String CurRecordID
        {
            get { return curRecordID; }
            set
            {
                curRecordID = value;
                if (hasMissingID && errors != null)
                    SetRecordIDForAll(curRecordID);
            }
        }

        /// <summary>
        /// Sets the field tag to be stored for subsequent error messages that are logged
        ///
        /// @param curField - the tag of the field currently being processed
        ///
        public String CurField
        {
            get { return curField; }
            set { curField = value; }
        }

        /// <summary>
        /// Sets the subfield tag to be stored for subsequent error messages that are logged
        ///
        /// @param curSubfield - the subfield tag of the subfield currently being processed
        /// </summary>
        public String CurSubfield
        {
            get { return curSubfield; }
            set { curSubfield = value; }
        }
    }
}