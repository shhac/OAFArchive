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
    public enum EntryType : byte
    {
        File = 0,
        Directory = 0x35,
    }
    
    public enum HashType : byte
    {
        None = 0,
        CRC32 = 0x01,
    }
    
    public enum CompressionType : byte
    {
        None = 0,
        GZIP = 0x01,
    }
    
    public static class Marker
    {
        public static byte[] Open  = new byte[12] {0x00, 0x07, 0xFF, 0x3C, 0x49, 0x54, 0x45, 0x4D, 0x3E, 0xFF, 0x7F, 0x08};
        public static byte[] Close = new byte[12] {0x07, 0xFF, 0x3C, 0x2F, 0x49, 0x54, 0x45, 0x4D, 0x3E, 0xFF, 0x7F, 0x08};
    }
    
    public struct OAFFileHeader
    {
        public long headerPosition;
        public int headerSize;
        public CompressionType hCompression;
        public string path;
        public long contentRelativePos;
        public long contentSize;
        public long contentFullSize;
        public CompressionType cCompression;
        public DateTime? lastModified;
        public DateTime? created;
        public int mode;
        public int userId;
        public int groupId;
        public EntryType entryType;
        public HashType contentHashType;
        public long contentHash;
        public HashType headerHashType;
        public long headerHash;
    }
}
