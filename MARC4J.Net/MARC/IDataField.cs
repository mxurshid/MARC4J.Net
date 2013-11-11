using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public interface IDataField : IVariableField
    {
        char Indicator1 { get; set; }

        char Indicator2 { get; set; }

        IList<ISubfield> GetSubfields();

        IList<ISubfield> GetSubfields(char code);

        ISubfield GetSubfield(char code);

        void AddSubfield(ISubfield subfield);

        void AddSubfield(int index, ISubfield subfield);

        void RemoveSubfield(ISubfield subfield);

    }
}