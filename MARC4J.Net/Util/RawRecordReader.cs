using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Util
{
    /// <summary>
    /// Read a binary marc file, treating the records mostly as opaque blocks of data.
    /// Its purpose is to quickly iterate through records looking for one that matches certain
    /// simple criteria, at which point the full marc record can be unpacked for more extensive processing
    /// </summary>
    public class RawRecordReader : IDisposable
    {
        private InputStream input;
        RawRecord nextRec = null;
        RawRecord afterNextRec = null;
        bool mergeRecords = true;

        public RawRecordReader(Stream stream)
            : this(stream, true)
        {
        }

        public RawRecordReader(Stream stream, bool mergeRecords)
        {
            this.mergeRecords = mergeRecords;
            input = new InputStream(stream);
        }

        public bool HasNext()
        {
            if (nextRec == null)
            {
                nextRec = new RawRecord(input);
            }
            if (nextRec != null && nextRec.GetRecordBytes() != null)
            {
                if (afterNextRec == null)
                {
                    afterNextRec = new RawRecord(input);
                    if (mergeRecords)
                    {
                        while (afterNextRec != null && afterNextRec.GetRecordBytes() != null && afterNextRec.GetRecordId().Equals(nextRec.GetRecordId()))
                        {
                            nextRec = new RawRecord(nextRec, afterNextRec);
                            afterNextRec = new RawRecord(input);
                        }
                    }
                }
                return (true);
            }
            return (false);
        }

        public RawRecord Next()
        {
            RawRecord tmpRec = nextRec;
            nextRec = afterNextRec;
            afterNextRec = null;
            return tmpRec;
        }

        //    /**
        //     * 
        //     * @param args
        //     */
        //    public static void main(String[] args)
        //    {
        //        RawRecordReader reader;

        //        if (args.length < 2)
        //        {
        //            System.err.println("Error: No records specified for extraction");
        //        }
        //        try
        //        {
        //            int numToSkip = 0;
        //            int numToOutput = -1;
        //            int offset = 0;
        //            if (args[offset].equals("-"))
        //            {
        //                reader = new RawRecordReader(System.in);
        //            }
        //            else
        //            {    
        //                reader = new RawRecordReader(new FileInputStream(new File(args[offset])));
        //            }
        //            offset++;
        //            while (offset < args.length && ( args[offset].equals("-skip")|| args[offset].equals("-num")))
        //            {
        //                if (args[offset].equals("-skip"))
        //                {
        //                    numToSkip = Integer.parseInt(args[offset+1]);
        //                    offset += 2;
        //                }
        //                else if (args[offset].equals("-num"))
        //                {
        //                    numToOutput = Integer.parseInt(args[offset+1]);
        //                    offset += 2;
        //                }  
        //            }
        //            if (numToSkip != 0 || numToOutput != -1)
        //            {
        //                ProcessInput(reader, numToSkip, numToOutput);
        //            }
        //            else if (args[offset].equals("-id"))
        //            {
        //                PrintIds(reader);
        //            }
        //            else if (args[offset].equals("-h") && args.length >= 3)
        //            {
        //                String idRegex = args[offset+1].trim();
        //                ProcessInput(reader, null, idRegex, null);
        //            }
        //            else if (!args[offset].endsWith(".txt"))
        //            {
        //                String idRegex = args[offset].trim();
        //                ProcessInput(reader, idRegex, null, null);
        //            }
        //            else 
        //            {
        //                File idList = new File(args[offset]);
        //                BufferedReader idStream = new BufferedReader(new InputStreamReader(new BufferedInputStream(new FileInputStream(idList))));
        //                String line;
        //                String findReplace[] = null;
        //                if (args.length > 2) findReplace = args[2].split("->");
        //                LinkedHashSet<String> idsLookedFor = new LinkedHashSet<String>();
        //                while ((line = idStream.readLine()) != null)
        //                {
        //                    if (findReplace != null)
        //                    {
        //                        line = line.replaceFirst(findReplace[0], findReplace[1]);
        //                    }
        //                    idsLookedFor.add(line);
        //                }
        //                idStream.close();
        //                ProcessInput(reader, null, null, idsLookedFor);

        //            }
        //        }
        //        catch (EOFException e)
        //        {
        //            //  Done Reading input,   Be happy
        //        }
        //        catch (IOException e)
        //        {
        //            //  e.printStackTrace();
        ////            logger.error(e.getMessage());
        //        }

        //    }

        private static void ProcessInput(RawRecordReader reader, int numToSkip, int numToOutput)
        {
            int num = 0;
            int numOutput = 0;
            while (reader.HasNext())
            {
                RawRecord rec = reader.Next();
                num++;
                if (num <= numToSkip) continue;
                if (numToOutput == -1 || numOutput < numToOutput)
                {
                    byte[] recordBytes = rec.GetRecordBytes();
                    Console.Write(recordBytes);
                    Console.WriteLine();
                    numOutput++;
                }
            }
        }

        static void PrintIds(RawRecordReader reader)
        {
            while (reader.HasNext())
            {
                RawRecord rec = reader.Next();
                String id = rec.GetRecordId();
                Console.WriteLine(id);
            }
        }

        static void ProcessInput(RawRecordReader reader, String idRegex, String recordHas, HashSet<String> idsLookedFor)
        {
            while (reader.HasNext())
            {
                RawRecord rec = reader.Next();
                String id = rec.GetRecordId();
                if ((idsLookedFor == null && recordHas == null && Regex.IsMatch(id, idRegex)) ||
                     (idsLookedFor != null && idsLookedFor.Contains(id)))
                {
                    byte[] recordBytes = rec.GetRecordBytes();
                    Console.Write(recordBytes);
                    Console.WriteLine();
                }
                else if (idsLookedFor == null && idRegex == null && recordHas != null)
                {
                    String tag = recordHas.Substring(0, 3);
                    String field = rec.GetFieldVal(tag);
                    if (field != null)
                    {
                        byte[] recordBytes = rec.GetRecordBytes();
                        Console.Write(recordBytes);
                        Console.WriteLine();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (input != null)
                input.Dispose();
            nextRec = null;
            afterNextRec = null;
        }
    }
}