/*
 * Created by SharpDevelop.
 * User: Paul
 * Date: 13/07/2015
 * Time: 18:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;

namespace OAFArchive
{
    public class OAFArchiveReader : IDisposable
    {
        public Stream ArchiveStream;
        private bool closeStreamOnExit = false;
        private long startPosition;
        
        public List<OAFFileHeader> headers = new List<OAFFileHeader>();
        
        public OAFArchiveReader(Stream stream, bool findAllHeaders = true)
        {
            ArchiveStream = stream;
            startPosition = ArchiveStream.Position;
            if (findAllHeaders) FindAllHeaders();
        }
        public OAFArchiveReader(string archive_file_path, bool findAllHeaders = true)
        {
            ArchiveStream = File.OpenRead(archive_file_path);
            closeStreamOnExit = true;
            startPosition = 0;
            if (findAllHeaders) FindAllHeaders();
        }
        
        #region IDisposable Members
        
        public void Dispose()
        {
            if (closeStreamOnExit)
                ArchiveStream.Close();
        }
        
        #endregion
        
        #region Array methods
        
        #region Boyer-Moore
        
        private static int[] BMGenBadChars(byte[] needle, int len)
        {
            const int A = 256; // alphabet length
            int i;
            int[] badChars = new int[A];
            for (i = 0; i < A; ++i)
                badChars[i] = len;
            for (i = 0; i < len - 1; ++i)
                badChars[needle[i]] = len - i - 1;
            return badChars;
        }
        
        private static int[] BMGenAllSuffixes(byte[] needle, int len)
        {
            int[] suffixes = new int[len];
            int i;
            int f = 0;
            int g = len - 1;
            for (i = len - 2; i >= 0; --i)
            {
                if ( i > g && suffixes[i + len - 1 - f] < i - g)
                    suffixes[i] = suffixes[i + len - 1 - f];
                else
                {
                    if (i < g)
                        g = i;
                    f = i;
                    while (g >= 0 && needle[g] == needle[g + len - 1 - f])
                        --g;
                    suffixes[i] = f - g;
                }
            }
            return suffixes;
        }
        
        private static int[] BMGenGoodSuffixes(byte[] needle, int len)
        {
            int[] goodSuffixes = new int[len];
            int i;
            int j = 0;
            int[] suffixes = BMGenAllSuffixes(needle, len);
            for (i = 0; i < len; ++i)
                goodSuffixes[i] = len;
            for (i = len - 1; i >= 0; --i)
                if (suffixes[i] == i + 1)
                    for (; j < len - 1 - i; ++j)
                        if (goodSuffixes[j] == len)
                            goodSuffixes[j] = len - 1 - i;
            for (i = 0; i < len - 2; ++i)
                goodSuffixes[len - 1 - suffixes[i]] = len = 1 - i;
            return goodSuffixes;
        }
        
        private static int BMMax(int a, int b)
        {
            if (a > b)
                return a;
            return b;
        }
        
        private static int BMIndexOf(byte[] haystack, byte[] needle, int offset = 0)
        {
            int len = needle.Length;
            int max = haystack.Length;
            int[] goodSuffixes = BMGenGoodSuffixes(needle, len);
            int[] badChars = BMGenBadChars(needle, len);
            
            int i;
            int j = offset;
            while (j <= max - len) {
                for (i = len - 1; i >= 0; --i)
                    if (needle[i] != haystack[i])
                        break;
                if (i < 0)
                    return j;
                else
                    j += BMMax(goodSuffixes[i], badChars[haystack[i + j]] - len + 1 + i);
            }
            return -1;
        }
        
        #endregion
        
        private static int IndexOf(byte[] haystack, byte[] needle, int offset = 0)
        {
            int max_j = needle.Length;
            int max_i = haystack.Length - max_j + 1;
            bool found = false;
            int i;
            int j;
            
            for (i = offset; i < max_i; ++i)
            {
                for (j = 0; j < max_j; ++j)
                {
                    if (haystack[i + j] != needle[j])
                        break;
                }
                if (j == max_j)
                {
                    found = true;
                    break;
                }
            }
            if (found) return i;
            return -1;
        }
        
        private static byte[] Slice(byte[] bytes, int start = 0, int? stop = null)
        {
            if (stop == null) stop = bytes.Length;
            int len = (int)stop - start;
            byte[] buffer = new byte[len];
            Array.Copy(bytes, start, buffer, 0, len);
            return buffer;
        }
        
        #endregion
        
        #region decode byte[] methods
        
        private static int ToInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
        
        private static long ToLong(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
        
        private static byte ToByte(byte[] bytes)
        {
            return bytes[0];
        }
        
        public static string ToString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        
        #endregion
        
        private static byte[] GetNextDetail(byte[] buffer, int len, ref int pos)
        {
            int start = pos;
            pos += len;
            return Slice(buffer, start, pos);
        }
        
        private OAFFileHeader ImpossibleHeader()
        {
            OAFFileHeader header = new OAFFileHeader();
            
            // set some impossible values
            header.headerPosition = -1;
            header.headerSize = -1;
            header.contentSize = -1;
            
            return header;
        }
        
        private OAFFileHeader FindNextHeader(long lookFrom = -1, int max_buffer = 4096)
        {
            OAFFileHeader header = new OAFFileHeader();
            long position = ArchiveStream.Position;
            if (lookFrom == -1)
                lookFrom = position;
            
            // System.Diagnostics.Debug.Write("Searching from: " + lookFrom.ToString() + " ---- " + (100 * lookFrom / ArchiveStream.Length).ToString()  + " ---- " + headers.Count + "\r\n");
            
            while (lookFrom < ArchiveStream.Length)
            {
                if (lookFrom > position)
                {
                    ArchiveStream.Position = lookFrom;
                }
                else
                {
                    lookFrom = position;
                }
                int sizeRead = 0;
                int lastRead;
                byte[] buffer = new byte[max_buffer];
                
                while (0 < (lastRead = ArchiveStream.Read(buffer, sizeRead, max_buffer - sizeRead)))
                {
                    sizeRead += lastRead;
                    if ((max_buffer - sizeRead) <= 0)
                        break;
                }
                
                ArchiveStream.Position = position;
                
                buffer = Slice(buffer, 0, sizeRead);
                
                int offset = 0;
                while (offset < (sizeRead - Marker.Open.Length + Marker.Close.Length))
                {
                    int index = BMIndexOf(buffer, Marker.Open, offset);
                    
                    if (index < 0 || sizeRead < (index + Marker.Open.Length + 4) ) {
                        // no more chance of finding a header in this buffer :(
                        break;
                    }
                    header.headerSize = ToInt(Slice(buffer, index + Marker.Open.Length, index + Marker.Open.Length + 4));
                    
                    if (sizeRead < (index + header.headerSize))
                    {
                        if (ArchiveStream.Length < (lookFrom + index + header.headerSize))
                            return ImpossibleHeader();
                        return FindNextHeader(lookFrom + index, header.headerSize);
                    }
    
                    if (-1 == BMIndexOf(Slice(buffer, index + header.headerSize - Marker.Close.Length, index + header.headerSize), Marker.Close))
                    {
                        // not a header :( try again
                        offset += index + 1;
                        continue;
                    }
                    
                    // great, we're pretty sure it's a header!
                    header.headerPosition = position + index;
                    buffer = Slice(buffer, index, index + header.headerSize);
                    
                    // build the struct
                    int detailOffset = Marker.Open.Length + 4;
                    header.hCompression = (CompressionType)ToByte(GetNextDetail(buffer, 1, ref detailOffset));
                    // if compressed then decompress buffer..
                    // .. now get other details
                    byte headerHashType       = ToByte(GetNextDetail(buffer, 1, ref detailOffset));
                    header.headerHashType     = (HashType)headerHashType;
                    header.headerHash         = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    int pathLength            = ToInt(GetNextDetail(buffer, 4, ref detailOffset));
                    header.path               = ToString(GetNextDetail(buffer, pathLength, ref detailOffset));
                    header.contentRelativePos = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    header.contentSize        = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    header.contentFullSize    = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    header.cCompression       = (CompressionType)ToByte(GetNextDetail(buffer, 1, ref detailOffset));
                    long time                 = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    header.lastModified       = DateTime.FromFileTimeUtc(time + 116444736000000000);
                    time                      = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    header.created            = DateTime.FromFileTimeUtc(time + 116444736000000000);
                    header.mode               = ToInt(GetNextDetail(buffer, 4, ref detailOffset));
                    header.userId             = ToInt(GetNextDetail(buffer, 4, ref detailOffset));
                    header.groupId            = ToInt(GetNextDetail(buffer, 4, ref detailOffset));
                    byte entryType            = ToByte(GetNextDetail(buffer, 1, ref detailOffset));
                    header.entryType          = (EntryType)entryType;
                    byte contentHashType      = ToByte(GetNextDetail(buffer, 1, ref detailOffset));
                    header.contentHashType    = (HashType)contentHashType;
                    header.contentHash        = ToLong(GetNextDetail(buffer, 8, ref detailOffset));
                    
                    return header;
                }
                if (ArchiveStream.Length <= (lookFrom + sizeRead - (Marker.Open.Length - 1) + 4 + Marker.Close.Length))
                {
                    // EOF
                    break;
                }
                lookFrom += sizeRead - (Marker.Open.Length - 1) - 4;
            }
            return ImpossibleHeader();
        }
        
        public List<OAFFileHeader> FindAllHeaders()
        {
            headers.Clear();
            ArchiveStream.Position = startPosition;
            
            OAFFileHeader header;
            while (true)
            {
                header = FindNextHeader();
                if (header.headerPosition < 0) break;
                headers.Add(header);
                ArchiveStream.Position = header.headerPosition + header.headerSize + (header.contentRelativePos == 0 ? header.contentSize : 0);
            }
            
            ArchiveStream.Position = startPosition;
            return headers;
        }
        
        public string[] ListItems(int index_start = 0, int index_end = -1)
        {
            if (index_end == -1) index_end = headers.Count;
            int i;
            int len = index_end - index_start;
            string[] Items = new string[len];
            for (i = 0; i < len; ++i)
            {
                Items[i] = headers[index_start + i].path;
            }
            return Items;
        }
        
        public void ExtractToPath(int itemIndex, string path, int bufferSize = 4096)
        {
            OAFFileHeader header = headers[itemIndex];
            long contentLocation = header.headerPosition + header.headerSize + header.contentRelativePos;
            long contentSize = header.contentSize;
            ArchiveStream.Position = contentLocation;
            
            (new FileInfo(path)).Directory.Create(); // Create path if it doesn't exist
            FileStream file = File.OpenWrite(path);
            
            byte[] buffer = new byte[bufferSize];
            int sizeRead;
            while (0 < (sizeRead = ArchiveStream.Read(buffer, 0, bufferSize)))
            {
                if (sizeRead >= contentSize)
                {
                    file.Write(buffer, 0, (int)contentSize);
                    file.Flush();
                    break;
                }
                file.Write(buffer, 0, sizeRead);
                file.Flush();
                contentSize -= sizeRead;
            }
            file.Close();
            File.SetCreationTimeUtc(path, header.created.Value);
            File.SetLastWriteTimeUtc(path, header.lastModified.Value);
            // mode, userId, groupId etc not implemented
        }
        
        public string Extract(int itemIndex, string rootPath = ".", int bufferSize = 4096)
        {
            OAFFileHeader header = headers[itemIndex];
            string destination = header.path.Replace('/', Path.DirectorySeparatorChar);
            ExtractToPath(
                itemIndex,
                rootPath.TrimEnd(new char[1] {Path.DirectorySeparatorChar}) + Path.DirectorySeparatorChar + destination,
                bufferSize
               );
            return destination;
        }
        
        public void ExtractAll(string rootPath = ".", int bufferSize = 4096)
        {
            int i;
            int len = headers.Count;
            for (i = 0; i < len; ++i)
            {
                Extract(i, rootPath, bufferSize);
            }
        }
    }
}
