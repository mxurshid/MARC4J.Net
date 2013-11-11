using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MARC4J.Net.MARC
{
    public class SortedRecord : Record
    {
        public SortedRecord()
            : base()
        {
        }

        public override void AddVariableField(IVariableField field)
        {
            if (field is IControlField)
            {
                var controlField = (IControlField)field;
                var tag = controlField.Tag;
                if (Verifier.IsControlNumberField(tag))
                {
                    if (Verifier.HasControlNumberField(GetControlFields()))
                        ControlFields[0] = controlField;
                    else
                        ControlFields.Insert(0, controlField);

                    ControlFields.Sort();
                }
                else if (Verifier.IsControlField(tag))
                {
                    ControlFields.Add(controlField);
                    ControlFields.Sort();
                }
            }
            else
            {
                DataFields.Add((IDataField)field);
                DataFields.Sort();
            }
        }
    }
}