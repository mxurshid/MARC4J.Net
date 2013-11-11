using System;
using System.Collections;
using System.Collections.Generic;

namespace MARC4J.Net.MARC
{
    public class DefaultMarcFactory : MarcFactory
    {
        public DefaultMarcFactory()
        {
        }

        public override IControlField NewControlField()
        {
            return new ControlField();
        }

        public override IControlField NewControlField(String tag)
        {
            return new ControlField(tag);
        }

        public override IControlField NewControlField(String tag, String data)
        {
            return new ControlField(tag, data);
        }

        public override IDataField NewDataField()
        {
            return new DataField();
        }

        public override IDataField NewDataField(String tag, char ind1, char ind2)
        {
            return new DataField(tag, ind1, ind2);
        }

        public override IDataField NewDataField(String tag, char ind1, char ind2, params String[] subfieldCodesAndData)
        {
            var df = new DataField(tag, ind1, ind2);
            if (subfieldCodesAndData.Length % 2 == 1)
            {
                throw new MarcException("Error: must provide even number of parameters for subfields: code, data, code, data, ...");
            }
            for (int i = 0; i < subfieldCodesAndData.Length; i += 2)
            {
                if (subfieldCodesAndData[i].Length != 1)
                {
                    throw new MarcException("Error: subfieldCode must be a single character");
                }
                var sf = NewSubfield(subfieldCodesAndData[i][0], subfieldCodesAndData[i + 1]);
                df.AddSubfield(sf);
            }
            return (df);
        }

        public override ILeader NewLeader()
        {
            return new Leader();
        }

        public override ILeader NewLeader(String ldr)
        {
            return new Leader(ldr);
        }

        public override IRecord NewRecord()
        {
            return NewRecord(new Leader("00000nam a2200000 a 4500"));
        }

        public override ISubfield NewSubfield()
        {
            return new Subfield();
        }

        public override ISubfield NewSubfield(char code)
        {
            return new Subfield(code);
        }

        public override ISubfield NewSubfield(char code, String data)
        {
            return new Subfield(code, data);
        }

        public override IRecord NewRecord(ILeader leader)
        {
            IRecord record = new Record();
            record.Leader = leader;
            return record;
        }

        public override IRecord NewRecord(String leader)
        {
            return NewRecord(new Leader(leader));
        }
    }
}