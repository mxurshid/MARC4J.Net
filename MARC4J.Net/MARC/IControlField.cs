using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public interface IControlField : IVariableField
    {
        String Data { get; set; }
    }
}