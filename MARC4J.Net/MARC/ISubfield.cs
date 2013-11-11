using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    /// <summary>
    ///  Represents a subfield in a MARC record.
    ///  </summary>
    public interface ISubfield : ISerializable
    {

        /// <summary>
        ///  Identifier.
        ///  The purpose of this identifier is to provide an identifier for
        ///  persistency.
        ///  </summary>
        long Id { get; set; }

        char Code { get; set; }

        String Data { get; set; }

        /// <summary>
        ///  Returns true if the given regular expression matches a subsequence of the
        ///  data element.
        ///  
        ///  @param pattern
        ///             the regular expression
        ///  @return true if the pattern matches, false othewise
        ///  </summary>
        bool Find(String pattern);

    }
}