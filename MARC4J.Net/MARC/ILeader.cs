using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MARC4J.Net.MARC
{
    public interface ILeader : ISerializable
    {
        long Id { get; set; }
        /// <summary>
        /// Logical record length (positions 00-04).
        /// </summary>
        int RecordLength { get; set; }

        /// <summary>
        /// Record status (position 05).
        /// </summary>
        char RecordStatus { get; set; }

        /// <summary>
        /// Type of record (position 06).
        /// </summary>
        char TypeOfRecord { get; set; }

        /// <summary>
        /// Implementation defined values (position 07-08).
        /// </summary>
        char[] ImplDefined1 { get; set; }

        /// <summary>
        /// Character encoding scheme (position 09).
        /// </summary>
        char CharCodingScheme { get; set; }

        /// <summary>
        /// Indicator count (position 10).
        /// </summary>
        int IndicatorCount { get; set; }

        /// <summary>
        /// Subfield code length (position 11).
        /// </summary>
        int SubfieldCodeLength { get; set; }

        /// <summary>
        /// Base address of data (positions 12-16).
        /// </summary>
        int BaseAddressOfData { get; set; }

        /// <summary>
        /// Implementation defined values (positions 17-19).
        /// </summary>
        char[] ImplDefined2 { get; set; }

        /// <summary>
        /// Entry map (positions 20-23).
        /// </summary>
        char[] EntryMap { get; set; }
        
        
        /// <summary>
        /// Creates a leader object from a string object.
        /// Indicator count and subfield code length are defaulted to 2 if they are not integer values.
        /// </summary>
        /// <param name="ldr">Leader</param>
        void UnMarshal(String ldr);

        /// <summary>
        /// Creates a string object from this leader object.
        /// </summary>
        /// <returns></returns>
        String Marshal();

    }

}