﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSLib.LS
{
    public class LSBReader : IDisposable
    {
        private Stream stream;
        private BinaryReader reader;
        private Dictionary<UInt32, string> staticStrings = new Dictionary<UInt32, string>();

        public LSBReader(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public Resource Read()
        {
            using (this.reader = new BinaryReader(stream))
            {
                LSBHeader header;
                header.signature = reader.ReadUInt32();
                if (header.signature != LSBHeader.Signature)
                    throw new InvalidFormatException(String.Format("Illegal signature in header; expected {0}, got {1}", LSBHeader.Signature, header.signature));

                header.totalSize = reader.ReadUInt32();
                if (stream.Length != header.totalSize)
                    throw new InvalidFormatException(String.Format("Invalid LSB file size; expected {0}, got {1}", header.totalSize, stream.Length));

                header.bigEndian = reader.ReadUInt32();
                // The game only uses little-endian files on all platforms currently and big-endian support isn't worth the hassle
                if (header.bigEndian != 0)
                    throw new InvalidFormatException("Big-endian LSB files are not supported");

                header.unknown = reader.ReadUInt32();
                header.metadata.timestamp = reader.ReadUInt64();
                header.metadata.majorVersion = reader.ReadUInt32();
                header.metadata.minorVersion = reader.ReadUInt32();
                header.metadata.revision = reader.ReadUInt32();
                header.metadata.buildNumber = reader.ReadUInt32();

                ReadStaticStrings();

                Resource rsrc = new Resource();
                rsrc.Metadata = header.metadata;
                ReadRegions(rsrc);
                return rsrc;
            }
        }

        private void ReadRegions(Resource rsrc)
        {
            UInt32 regions = reader.ReadUInt32();
            for (UInt32 i = 0; i < regions; i++)
            {
                UInt32 regionNameId = reader.ReadUInt32();
                UInt32 regionOffset = reader.ReadUInt32();

                Region rgn = new Region();
                rgn.RegionName = staticStrings[regionNameId];
                var lastRegionPos = stream.Position;

                stream.Seek(regionOffset, SeekOrigin.Begin);
                ReadNode(rgn);
                rsrc.Regions[rgn.RegionName] = rgn;
                stream.Seek(lastRegionPos, SeekOrigin.Begin);
            }
        }

        private void ReadNode(Node node)
        {
            UInt32 nodeNameId = reader.ReadUInt32();
            UInt32 attributeCount = reader.ReadUInt32();
            UInt32 childCount = reader.ReadUInt32();
            node.Name = staticStrings[nodeNameId];

            for (UInt32 i = 0; i < attributeCount; i++)
            {
                UInt32 attrNameId = reader.ReadUInt32();
                UInt32 attrTypeId = reader.ReadUInt32();
                if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
                    throw new InvalidFormatException(String.Format("Unsupported attribute data type: {0}", attrTypeId));

                node.Attributes[staticStrings[attrNameId]] = ReadAttribute((NodeAttribute.DataType)attrTypeId);
            }

            for (UInt32 i = 0; i < childCount; i++)
            {
                Node child = new Node();
                child.Parent = node;
                ReadNode(child);
                node.AppendChild(child);
            }
        }

        private NodeAttribute ReadAttribute(NodeAttribute.DataType type)
        {
            switch (type)
            {
                case NodeAttribute.DataType.DT_String:
                case NodeAttribute.DataType.DT_Path:
                case NodeAttribute.DataType.DT_FixedString:
                case NodeAttribute.DataType.DT_LSString:
                    {
                        var attr = new NodeAttribute(type);
                        attr.Value = ReadString(true);
                        return attr;
                    }

                case NodeAttribute.DataType.DT_WString:
                case NodeAttribute.DataType.DT_LSWString:
                    {
                        var attr = new NodeAttribute(type);
                        attr.Value = ReadWideString(true);
                        return attr;
                    }

                case NodeAttribute.DataType.DT_TranslatedString:
                    {
                        var attr = new NodeAttribute(type);
                        var str = new TranslatedString();
                        str.Value = ReadString(true);
                        str.Handle = ReadString(true);
                        attr.Value = str;
                        return attr;
                    }

                case NodeAttribute.DataType.DT_ScratchBuffer:
                    {
                        var attr = new NodeAttribute(type);
                        var bufferLength = reader.ReadInt32();
                        attr.Value = reader.ReadBytes(bufferLength);
                        return attr;
                    }

                default:
                    return BinUtils.ReadAttribute(type, reader);
            }
        }

        private void ReadStaticStrings()
        {
            UInt32 strings = reader.ReadUInt32();
            for (UInt32 i = 0; i < strings; i++)
            {
                string s = ReadString(false);
                UInt32 index = reader.ReadUInt32();
                if (staticStrings.ContainsKey(index))
                    throw new InvalidFormatException(String.Format("String ID {0} duplicated in static string map", index));
                staticStrings.Add(index, s);
            }
        }

        private string ReadString(bool nullTerminated)
        {
            int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
            byte[] bytes = reader.ReadBytes(length);

            // Remove stray null bytes at the end of the string
            // Some LSB files seem to save translated string keys incurrectly, and append two NULL bytes
            // (or one null byte and another stray byte) to the end of the value.
            bool hasBogusNullBytes = false;
            while (length > 0 && bytes[length - 1] == 0)
            {
                length--;
                hasBogusNullBytes = true;
            }

            string str = System.Text.Encoding.UTF8.GetString(bytes, 0, length);

            if (nullTerminated)
            {
                if (reader.ReadByte() != 0 && !hasBogusNullBytes)
                    throw new InvalidFormatException("Illegal null terminated string");
            }

            return str;
        }

        private string ReadWideString(bool nullTerminated)
        {
            int length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
            byte[] bytes = reader.ReadBytes(length * 2);
            string str = System.Text.Encoding.Unicode.GetString(bytes);
            if (nullTerminated)
            {
                if (reader.ReadUInt16() != 0)
                    throw new InvalidFormatException("Illegal null terminated widestring");
            }

            return str;
        }
    }
}
