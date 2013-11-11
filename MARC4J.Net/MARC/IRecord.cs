using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public interface IRecord : ISerializable
    {
        long Id { get; set; }

        /// <summary>
        /// Type of record.
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Adds a <code>VariableField</code>.
        /// <exception cref="MARC4J.Net.MARC.IllegalAddException">when the parameter is not a <code>VariableField</code> instance</exception>
        /// </summary>
        void AddVariableField(IVariableField field);

        void RemoveVariableField(IVariableField field);

        /// <summary>
        /// Returns a list of variable fields
        /// </summary>
        /// <returns></returns>
        IList<IVariableField> GetVariableFields();

        /// <summary>
        /// Returns a list of control fields
        /// </summary>
        /// <returns></returns>
        IList<IControlField> GetControlFields();

        /// <summary>
        /// Returns a list of data fields
        /// </summary>
        /// <returns></returns>
        IList<IDataField> GetDataFields();

        /// <summary>
        /// Returns the control number field or <code>null</code> if no control
        /// number field is available.
        /// </summary>
        /// <returns></returns>
        IControlField GetControlNumberField();

        /// <summary>
        /// Returns the control number or <code>null</code> if no control number is
        /// available.
        /// 
        /// This method returns the data for a <code>IControlField</code> with tag
        /// 001.
        /// </summary>
        /// <returns></returns>
        String GetControlNumber();

        /// <summary>
        /// Returns the first instance of the variable field with the given tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        IVariableField GetVariableField(String tag);

        /// <summary>
        /// Returns a list of variable fields with the given tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        IList<IVariableField> GetVariableFields(String tag);

        /// <summary>
        ///  Returns a list of variable fields for the given tags.
        /// 
        ///  <p>
        ///  For example:
        /// 
        ///  <pre>
        ///  String tags = { &quot;100&quot;, &quot;245&quot;, &quot;260&quot;, &quot;300&quot; };
        /// 
        ///  List fields = record.getVariableFields(tags);
        ///  </pre>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        IList<IVariableField> GetVariableFields(String[] tag);

        /// <summary>
        /// Returns the <code>Leader</code>.
        /// </summary>
        ILeader Leader { get; set; }

        /// <summary>
        /// Returns a List of VariableField objects that have a data element that
        /// matches the given regular expression.
        /// </summary>
        /// <param name="pattern">regex pattern</param>
        /// <returns></returns>
        IList<IVariableField> Find(String pattern);

        /// <summary>
        /// Returns a List of VariableField objects with the given tag that have a
        /// data element that matches the given regular expression.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IList<IVariableField> Find(String tag, String pattern);

        /// <summary>
        /// Returns a List of VariableField objects with the given tags that have a
        /// data element that matches the given regular expression.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        IList<IVariableField> Find(String[] tag, String pattern);

    }
}