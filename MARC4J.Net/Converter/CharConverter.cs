using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MARC4J.Net.Converter
{
    /// <summary>
    /// Extend this class to create a character converter.
    /// </summary>
    public abstract class CharConverter
    {
        /// <summary>
        /// The method that needs to be implemented in a subclass to create a CharConverter.
        /// Receives a data element extracted from a record as a array of characters, and 
        /// converts that data and returns the result as a <c>String</c> object.
        /// </summary>
        /// <param name="dataElement">the data to convert</param>
        /// <returns></returns>
        public abstract string Convert(char[] dataElement);

        /// <summary>
        /// Alternate method for performing a character conversion.  Receives the incoming
        /// as a byte array, converts the bytes to characters, and calls the above convert method
        /// which must be implemented in the subclass.
        /// </summary>
        /// <param name="dataElement">the data to convert</param>
        /// <returns></returns>
        public String Convert(byte[] dataElement)
        {
            char[] cData = new char[dataElement.Length];
            for (int i = 0; i < dataElement.Length; i++)
            {
                byte b = dataElement[i];
                cData[i] = (char)(b >= 0 ? b : 256 + b);
            }
            return Convert(cData);
        }

        /// <summary>
        ///  Alternate method for performing a character conversion.  Receives the incoming
        ///  as a String, converts the String to a character array, and calls the above convert
        ///  method which must be implemented in the subclass.
        /// </summary>
        /// <param name="dataElement">the data to convert</param>
        /// <returns></returns>
        public String Convert(String dataElement)
        {
            char[] data = null;
            data = dataElement.ToCharArray();
            return Convert(data);
        }

        /// <summary>
        /// Should return true if the CharConverter outputs Unicode encoded characters
        /// </summary>
        /// <returns></returns>
        public virtual bool OutputsUnicode()
        {
            return false;
        }
    }
}