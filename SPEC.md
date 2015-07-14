Details about the OAF format
-

 - Each file added to the archive is stored with an accompanying header
 - The header does not have to be next to the file, it has a relative offset indicating where the file is located
 - The headers do not have to be together at the beginning of the archive
 - Storing an OAF inside an OAF has undefined read behaviour

Headers have the form
-
                 _Name_  _Type_   _Bytes_  _Description_
           Marker.Open    byte[]      12    Marker to help searching for the file header
            headerSize    int          4    Number of bytes this header takes up in the archive file, includes Marker.Open and Marker.Close
          hCompression    byte         1    The type of compression used between (but not including) hCompression and Marker.Close, decompress this header before attempting to read more
           path.Length    int          4    Number of bytes path takes up
                  path    string      ??    UTF-8 string of the content's path. Path seperater char is /
    contentRelativePos    long         8    The relative position the content is located in the archive file from the end of the header (i.e. abs position = header start position + headerSize + contentRelativePos)
           contentSize    long         8    Number of bytes the content takes up in the archive file
       contentFullSize    long         8    Number of bytes the content takes up when extracted
          cCompression    byte         1    The type of compression used on the content
          timeModified    long         8    Time the content was last modified (excluding archiving) in number of nanoseconds since 1970-01-01T00:00:00.000Z (Window's time minus 116444736000000000)
           timeCreated    long         8    Time the content was created in number of nanoseconds since 1970-01-01T00:00:00.000Z (Window's time minus 116444736000000000)
           header.mode    int          4    Placeholder; Describes file attributes
         header.userId    int          4    Placeholder; Describes file attributes
        header.groupId    int          4    Placeholder; Describes file attributes
             entryType    byte         1    The type of entry this header is for, e.g. file, directory, etc.
       contentHashType    byte         1    The type of hash done on the content, e.g. None, CRC32
           contentHash    long         8    The hash described by contentHashType, truncated to the first 8 bytes
        headerHashType    byte         1    The type of hash done on this header between (but not including) hCompression and headerHashType, e.g. None, CRC32
            headerHash    long         8    The hash described by headerHashType, truncated to the first 8 bytes
          Marker.Close    byte[]      12    Marker to help searching for the file header

---

Header markers
-

    Marker.Open     00 07 FF 3C 49 54 45 4D 3E FF 7F 08
    Marker.Close    07 FF 3C 2F 49 54 45 4D 3E FF 7F 08

---

Accepted Entry Types
-

    0x00    File
    0x35    Directory (not yet implemented)
    0xFF    ArchiveComment (not yet implemented)

 - `Directory`s have content size `0`
 - The text of an `ArchiveComment` goes in the content section
 - If multiple `ArchiveComment`s are within a file, the correct output is to show them in the same order as their headers with at least one `\n` between them. An single empty line between them is recommended but not required.
 - It is okay to merge multiple `ArchiveComment`s together when creating the archive.

---

Accepted Compression Types
-

    0x00    None
    0x01    GZIP (not yet implemented)

---

Accepted Hashing Types
-

    0x00    None
    0x01    CRC32 (not yet implemented)
