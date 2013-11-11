using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public class Verifier
    {
        private Verifier()
        {
        }
        public static bool IsControlField(String tag)
        {
            if (tag.Length == 3 && tag[0] == '0' && tag[1] == '0' && tag[2] >= '0' && tag[2] <= '9')// if (Integer.parseInt(tag) < 10)
                return true;
            return false;
        }

        public static bool IsControlNumberField(String tag)
        {
            if (tag.Equals("001"))
                return true;
            return false;
        }
        public static bool HasControlNumberField(ICollection<IControlField> col)
        {
            foreach (var field in col)
            {
                if (IsControlNumberField(field.Tag))
                    return true;
            }
            return false;
        }
    }
}