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
    public class OAFArchiveWriter : IDisposable
    {
        public Stream ArchiveStream;
        private bool closeStreamOnExit = false;
        
        public OAFArchiveWriter(Stream stream)
        {
            ArchiveStream = stream;
        }
        public OAFArchiveWriter(string archive_file_path)
        {
            if (File.Exists(archive_file_path))
            {
                ArchiveStream = File.OpenWrite(archive_file_path);
            }
            else
            {
                ArchiveStream = File.Create(archive_file_path);
            }
            closeStreamOnExit = true;
        }
        
        #region IDisposable Members
        
        public void Dispose()
        {
            if (closeStreamOnExit)
                ArchiveStream.Close();
        }
        
        #endregion
        
        #region x to byte[]
        
        private static byte[] ToBytes(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
        private static byte[] ToBytes(long i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
        private static byte[] ToBytes(byte b)
        {
            byte[] bytes = new byte[1];
            bytes[0] = b;
            return bytes;
        }
        private static byte[] ToBytes(string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }
        
        #endregion
        
        private static void SimpleWrite(Stream stream, byte[] buffer, ref int totalWritten)
        {
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
            totalWritten += buffer.Length;
        }
        private void WriteHeader(OAFFileHeader header)
        {
            MemoryStream hStream = new MemoryStream();
            
            int headerSize = 0;
            
            long timeModified;
            if (header.lastModified == null)
                timeModified = DateTime.Now.ToFileTimeUtc() - 116444736000000000;
            else
                timeModified = header.lastModified.Value.ToFileTimeUtc() - 116444736000000000;
            long timeCreated;
            if (header.created == null)
                timeCreated = timeModified;
            else
                timeCreated = header.created.Value.ToFileTimeUtc() - 116444736000000000;
            
            byte[] path = ToBytes(header.path);
            
            SimpleWrite(hStream,                          Marker.Open , ref headerSize); // "<ITEM>"
            SimpleWrite(hStream, ToBytes(                     (int)-1), ref headerSize); // placeholder for header length
            SimpleWrite(hStream, ToBytes(   (byte)header.hCompression), ref headerSize);
            SimpleWrite(hStream, ToBytes( (byte)header.headerHashType), ref headerSize);
            SimpleWrite(hStream, ToBytes(           header.headerHash), ref headerSize);
            SimpleWrite(hStream, ToBytes(                 path.Length), ref headerSize);
            SimpleWrite(hStream,                                 path , ref headerSize);
            SimpleWrite(hStream, ToBytes(   header.contentRelativePos), ref headerSize);
            SimpleWrite(hStream, ToBytes(          header.contentSize), ref headerSize);
            SimpleWrite(hStream, ToBytes(      header.contentFullSize), ref headerSize);
            SimpleWrite(hStream, ToBytes(   (byte)header.cCompression), ref headerSize);
            SimpleWrite(hStream, ToBytes(                timeModified), ref headerSize);
            SimpleWrite(hStream, ToBytes(                 timeCreated), ref headerSize);
            SimpleWrite(hStream, ToBytes(                 header.mode), ref headerSize);
            SimpleWrite(hStream, ToBytes(               header.userId), ref headerSize);
            SimpleWrite(hStream, ToBytes(              header.groupId), ref headerSize);
            SimpleWrite(hStream, ToBytes(      (byte)header.entryType), ref headerSize);
            SimpleWrite(hStream, ToBytes((byte)header.contentHashType), ref headerSize);
            SimpleWrite(hStream, ToBytes(          header.contentHash), ref headerSize);
            SimpleWrite(hStream,                         Marker.Close , ref headerSize); // "</ITEM>"
            hStream.Position = Marker.Open.Length;
            SimpleWrite(hStream, ToBytes(headerSize), ref headerSize);
            
            // write to Archive
            hStream.Position = 0;
            hStream.CopyTo(ArchiveStream);
            ArchiveStream.Flush();
        }
        private void WriteHeader(string path, long contentSize, DateTime? lastModified = null, DateTime? created = null, int mode = 511, EntryType entryType = EntryType.File, int userId = 61, int groupId = 61)
        {
            OAFFileHeader header = new OAFFileHeader();
            
            header.path = path.Replace(Path.DirectorySeparatorChar, '/');
            header.contentSize = contentSize;
            header.contentFullSize = contentSize;
            header.lastModified = lastModified;
            header.created = created;
            header.mode = mode;
            header.userId = userId;
            header.groupId = groupId;
            header.entryType = entryType;
            
            // Just put content in next available space
            header.contentRelativePos = 0;
            
            // Header Compression not implemented
            header.hCompression = CompressionType.None;
            
            // Content Compression not implemented
            header.cCompression = CompressionType.None;
            
            // Hashing not implemented
            header.contentHashType = HashType.None;
            header.contentHash = 0;
            header.headerHashType = HashType.None;
            header.headerHash = 0;
            
            WriteHeader(header);
        }
        private void WriteHeader(string path, FileInfo detail)
        {
            WriteHeader(
                path,
                detail.Length,
                detail.LastWriteTimeUtc,
                detail.CreationTimeUtc
               );
                
        }
        private void WriteContent(Stream data, int bufferSize = 4096)
        {
            int count;
            byte[] buffer = new byte[bufferSize];
            while (0 < (count = data.Read(buffer, 0, bufferSize))) {
                ArchiveStream.Write(buffer, 0, count);
                ArchiveStream.Flush();
            }
        }
        public void Write(string path, FileInfo detail, Stream data)
        {
            WriteHeader(path, detail);
            WriteContent(data);
        }
    }
}
