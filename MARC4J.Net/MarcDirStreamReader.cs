using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MARC4J.Net.MARC;

namespace MARC4J.Net
{
    /// <summary>
    /// A Marc reader which instead of handling a single file of MARC records
    /// it handles a directory, which it will scan for all .mrc files, and 
    /// iterate through all of them in turn.
    /// </summary>
    public class MarcDirStreamReader : IMarcReader
    {
        #region Fields

        FileInfo[] list;
        IMarcReader curFileReader;
        int curFileNum;
        bool permissive;
        bool convertToUTF8;
        string defaultEncoding;

        #endregion

        #region Ctors
        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// </summary>
        /// <param name="dirName">The path of the directory from which to read all of the .mrc files</param>
        public MarcDirStreamReader(String dirName) : this(new DirectoryInfo(dirName)) { }

        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// </summary>
        /// <param name="dir">The path of the directory from which to read all of the .mrc files</param>
        public MarcDirStreamReader(DirectoryInfo dir) : this(dir, false, false, null) { }

        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// Takes the values passed in for permissive and convertToUTF8 and passes them on 
        /// to each of the MarcPermissiveStreamReader that it creates.
        /// </summary>
        /// <param name="dirName">The path of the directory from which to read all of the .mrc files</param>
        /// <param name="permissive">Set to true to specify that reader should try to handle and recover from errors in the input.</param>
        /// <param name="convertToUTF8">Set to true to specify that reader should convert the records being read to UTF-8 encoding as they are being read.</param>
        public MarcDirStreamReader(string dirName, bool permissive, bool convertToUTF8) : this(new DirectoryInfo(dirName), permissive, convertToUTF8) { }

        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// Takes the values passed in for permissive and convertToUTF8 and passes them on 
        /// to each of the MarcPermissiveStreamReader that it creates.
        /// </summary>
        /// <param name="dir">The path of the directory from which to read all of the .mrc files</param>
        /// <param name="permissive">Set to true to specify that reader should try to handle and recover from errors in the input.</param>
        /// <param name="convertToUTF8">Set to true to specify that reader should convert the records being read to UTF-8 encoding as they are being read.</param>

        public MarcDirStreamReader(DirectoryInfo dir, bool permissive, bool convertToUTF8) : this(dir, permissive, convertToUTF8, null) { }

        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// Takes the values passed in for permissive and convertToUTF8 and passes them on 
        /// to each of the MarcPermissiveStreamReader that it creates.
        /// </summary>
        /// <param name="dirName">The path of the directory from which to read all of the .mrc files</param>
        /// <param name="permissive">Set to true to specify that reader should try to handle and recover from errors in the input.</param>
        /// <param name="convertToUTF8">Set to true to specify that reader should convert the records being read to UTF-8 encoding as they are being read.</param>
        /// <param name="defaultEncoding">Specifies the character encoding that the records being read are presumed to be in..</param>
        public MarcDirStreamReader(String dirName, bool permissive, bool convertToUTF8, string defaultEncoding) : this(new DirectoryInfo(dirName), permissive, convertToUTF8, defaultEncoding) { }

        /// <summary>
        /// Constructs an instance that traverses the directory specified in the parameter.
        /// Takes the values passed in for permissive and convertToUTF8 and passes them on 
        /// to each of the MarcPermissiveStreamReader that it creates.
        /// </summary>
        /// <param name="dir">The path of the directory from which to read all of the .mrc files</param>
        /// <param name="permissive">Set to true to specify that reader should try to handle and recover from errors in the input.</param>
        /// <param name="convertToUTF8">Set to true to specify that reader should convert the records being read to UTF-8 encoding as they are being read.</param>
        /// <param name="defaultEncoding">Specifies the character encoding that the records being read are presumed to be in..</param>

        public MarcDirStreamReader(DirectoryInfo dir, bool permissive, bool convertToUTF8, string defaultEncoding)
        {
            this.permissive = permissive;
            this.convertToUTF8 = convertToUTF8;
            this.defaultEncoding = defaultEncoding;
            list = dir.GetFiles("*.mrc");
            Array.Sort<FileInfo>(list);
            curFileNum = 0;
            curFileReader = null;
        }

        #endregion

        private void NextFile()
        {
            if (curFileNum != list.Length)
            {
                try
                {
                    Console.WriteLine("Switching to input file: " + list[curFileNum].Name);
                    if (defaultEncoding != null)
                    {
                        using (var fs = list[curFileNum++].OpenRead())
                        {
                            curFileReader = new MarcPermissiveStreamReader(fs, permissive, convertToUTF8, defaultEncoding);
                        }
                    }
                    else
                    {
                        using (var fs = list[curFileNum++].OpenRead())
                        {
                            curFileReader = new MarcPermissiveStreamReader(fs, permissive, convertToUTF8);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    NextFile();
                }
            }
            else
            {
                curFileReader = null;
            }
        }

        /// <summary>
        /// Returns the next record in the iteration.
        /// </summary>
        /// <returns></returns>
        private IRecord Next()
        {
            if (curFileReader == null || !curFileReader.MoveNext())
            {
                NextFile();
            }
            return (curFileReader == null ? null : curFileReader.Current);
        }

        #region IMarcReader implementation
        public IRecord Current
        {
            get { return curFileReader.Current; }
        }

        public void Dispose()
        {
            if (list != null)
            {
                Array.Clear(list, 0, list.Length);
                list = null;
            }
            if (curFileReader != null)
            {
                curFileReader.Dispose();
                curFileReader = null;
            }
            defaultEncoding = null;
            GC.SuppressFinalize(this);
        }

        object IEnumerator.Current
        {
            get { return curFileReader.Current; }
        }

        /// <summary>
        /// Returns true if the iteration has more records, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (curFileReader == null || !curFileReader.MoveNext())
            {
                NextFile();
            }
            return (curFileReader == null ? false : curFileReader.MoveNext());
        }

        public void Reset()
        {
            curFileNum = 0;
        }

        public IEnumerator<IRecord> GetEnumerator()
        {
            while (MoveNext())
                yield return curFileReader.Current;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}