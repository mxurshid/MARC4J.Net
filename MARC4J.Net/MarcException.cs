using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net
{
    public class MarcException : Exception
    {

        /**
         * Create a new <code>MarcException</code> with no detail mesage.
         */
        public MarcException()
        {
        }

        /**
         * Create a new <code>MarcException</code> with the <code>String</code>
         * specified as an error message.
         * 
         * @param message
         *          information about the cause of the exception
         */
        public MarcException(String message)
            : base(message)
        {
        }

        /**
         * Create a new <code>MarcException</code> with the given
         * <code>Exception</code> base cause and detail message.
         * 
         * @param message
         *          information about the cause of the exception
         * @param ex
         *          the nested exception that caused this exception
         */
        public MarcException(String message, Exception ex)
            : base(message, ex)
        {
        }
    }
}