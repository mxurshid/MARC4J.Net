using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public class IllegalAddException : ArgumentException
    {

        public IllegalAddException(String className)
            : base(string.Format("The addition of the object of type {0} is not allowed.", className))
        {
        }

        public IllegalAddException(String className, String reason)
            : base(string.Format("The addition of the object of type {0} is not allowed: {1}", className, reason))
        {
        }
    }
}