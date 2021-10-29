﻿using FileDBReader;
using FileDBReader.src;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FileDBReader
{
    /// <summary>
    /// converts all text in an xml file into hex strings using conversion rules set up in an external xml file
    /// </summary>
    public class XmlExporter
    {
        public XmlExporter() {
            
        }

        /// <summary>
        /// Exports an xmldocument and returns the resulting xmldocument
        /// </summary>
        /// <param name="docpath">path of the input document</param>
        /// <param name="interpreterPath">path of the interpreterfile</param>
        /// <returns>the resulting document</returns>
        public XmlDocument Export(String docpath, String interpreterPath) {
            XmlDocument doc = new XmlDocument();
            doc.Load(docpath);
            return Export(doc, new Interpreter(Interpreter.ToInterpreterDoc(interpreterPath)));
        }

        public XmlDocument Export(XmlDocument doc, Interpreter Interpreter)
        {
            foreach (KeyValuePair<String, Conversion> k in Interpreter.Conversions)
            {
                try {
                    var Nodes = doc.SelectNodes(k.Key);
                    ConvertNodeSet(Nodes.Cast<XmlNode>(), k.Value);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("I don't even know what this is for. This is an error message even dumber than the old one. Modders gonna take over the world!!");
                }
            }

            if (Interpreter.HasDefaultType())
            {
                String Inverse = Interpreter.GetInverseXPath();
                var Base = doc.SelectNodes("//*[text()]");
                var toFilter = doc.SelectNodes(Inverse);
                var defaults = HexHelper.ExceptNodelists(Base, toFilter);
                ConvertNodeSet(defaults, Interpreter.DefaultType);
            }

            foreach (InternalCompression comp in Interpreter.InternalCompressions)
            {
                var Nodes = doc.SelectNodes(comp.Path);
                foreach (XmlNode node in Nodes)
                {
                    FileWriter fileWriter = new FileWriter();

                    var contentNode = node.SelectSingleNode("./Content");
                    XmlDocument xmldoc = new XmlDocument();
                    XmlNode f = xmldoc.ImportNode(contentNode, true);
                    xmldoc.AppendChild(xmldoc.ImportNode(f, true));

                    var stream = fileWriter.Export(xmldoc, new MemoryStream(), comp.CompressionVersion);

                    //Convert This String To Hex Data
                    node.InnerText = HexHelper.StreamToHexString(stream);

                    //try to overwrite the bytesize since it's always exported the same way
                    var ByteSize = node.SelectSingleNode("./preceding-sibling::ByteCount");
                    if (ByteSize != null)
                    {
                        long BufferSize = stream.Length;
                        Type type = typeof(int);
                        ByteSize.InnerText = HexHelper.ByteArrayToString(ConverterFunctions.ConversionRulesExport[type](BufferSize.ToString(), new UnicodeEncoding()));
                    }
                }
            }

            return doc; 
        }



        private void ConvertNodeSet(IEnumerable<XmlNode> matches, Conversion Conversion)
        {
            foreach (XmlNode match in matches)
            {
                switch (Conversion.Structure)
                {
                    case ContentStructure.List:
                        try
                        {
                            exportAsList(match, Conversion.Type, Conversion.Encoding, false);
                        }
                        catch (InvalidConversionException e)
                        {
                            Console.WriteLine("Invalid Conversion at: {1}, Data: {0}, Target Type: {2}", e.ContentToConvert, e.NodeName, e.TargetType);
                        }
                        break;
                    case ContentStructure.Default:
                        try
                        {
                            ExportSingleNode(match, Conversion.Type, Conversion.Encoding, Conversion.Enum, false);
                        }
                        catch (InvalidConversionException e)
                        {
                            Console.WriteLine("Invalid Conversion at: {1}, Data: {0}, Target Type: {2}", e.ContentToConvert, e.NodeName, e.TargetType);
                        }
                        break;
                    case ContentStructure.Cdata:
                        try 
                        {
                            exportAsList(match, Conversion.Type, Conversion.Encoding, true);
                        }
                        catch (InvalidConversionException e)
                        {
                            Console.WriteLine("Invalid Conversion at: {1}, Data: {0}, Target Type: {2}", e.ContentToConvert, e.NodeName, e.TargetType);
                        }
                        break; 
                }
            }
        }


        private void exportAsList(XmlNode n, Type type, Encoding e, bool RespectCdata) {
            //don't do anything with empty nodes
            if (!n.InnerText.Equals("")) 
            {
                String text = n.InnerText;
                if (RespectCdata)
                    text = text.Substring(6, text.Length - 7);
                String[] arr = text.Split(" ");
                if (!arr[0].Equals(""))
                {
                    //use stringbuilder and for loop for performance reasons
                    StringBuilder sb = new StringBuilder("");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        String s = arr[i];
                        try
                        {
                            sb.Append(HexHelper.ByteArrayToString(ConverterFunctions.ConversionRulesExport[type](s, e)));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidConversionException(type, n.Name, "List Value");
                        }
                    }
                    String result = sb.ToString();
                    if (RespectCdata)
                        result = "CDATA[" + result + "]";
                    n.InnerText = result;
                }
            }

            
            
        }

        private void ExportSingleNode(XmlNode n, Type type, Encoding e, RuntimeEnum Enum, bool RespectCdata) {
            String Text;

            if (!Enum.IsEmpty())
            {
                Text = Enum.GetKey(n.InnerText);
            }
            else 
            {
                Text = n.InnerText;
            }

            if (RespectCdata)
                Text = Text.Substring(6, Text.Length - 7);

            byte[] converted;
            try
            {
                converted = ConverterFunctions.ConversionRulesExport[type](Text, e);
            }
            catch (Exception ex)
            {
                throw new InvalidConversionException(type, n.Name, n.InnerText);
            }
            String hex = HexHelper.ByteArrayToString(converted);

            if (RespectCdata) 
                hex = "CDATA[" + HexHelper.ByteArrayToString(BitConverter.GetBytes(hex.Length/2))+ hex + "]";

            n.InnerText = hex;
        }

        #region DEPRACATED 

        [Obsolete("Export(XmlDocument doc, XmlDocument doc) is deprecated, please use Export(XmlDocument doc, Interpreter i) instead.")]
        public XmlDocument Export(XmlDocument doc, XmlDocument interpreter)
        {
            //default type
            XmlNode defaultAttrib = null;
            defaultAttrib = interpreter.SelectSingleNode("/Converts/Default");
            var converts = interpreter.SelectNodes("/Converts/Converts/Convert");
            var internalFileDBs = interpreter.SelectNodes("/Converts/InternalCompression/Element");

            //converts
            foreach (XmlNode x in converts)
            {
                try
                {
                    String Path = x.Attributes["Path"].Value;
                    var Nodes = doc.SelectNodes(Path);
                    ConvertNodeSet(Nodes, x);
                }
                catch (InvalidConversionException e)
                {
                    Console.WriteLine("Path causes conversion errors: {0} \n NodeName: {1} \n Text to Convert: {2}, Target Type: {3}", x.Attributes["Path"].Value, e.NodeName, e.ContentToConvert, e.TargetType);
                }
            }

            //defaultType
            if (defaultAttrib != null)
            {
                //get a combined xpath of all
                List<String> StringList = new List<string>();
                foreach (XmlNode convert in converts)
                    StringList.Add(convert.Attributes["Path"].Value);
                foreach (XmlNode internalFileDB in internalFileDBs)
                    StringList.Add(internalFileDB.Attributes["Path"].Value);
                String xPath = String.Join(" | ", StringList);

                //select all text not in combined path#
                var Base = doc.SelectNodes("//*[text()]");
                var toFilter = doc.SelectNodes(xPath);
                var defaults = HexHelper.ExceptNodelists(Base, toFilter);

                //convert that to default type
                ConvertNodeSet(defaults, defaultAttrib);
            }
            //internal filedbs
            foreach (XmlNode n in internalFileDBs)
            {
                var nodes = doc.SelectNodes(n.Attributes["Path"].Value);
                foreach (XmlNode node in nodes)
                {
                    //get internal document
                    var contentNode = node.SelectSingleNode("./Content");
                    XmlDocument xmldoc = new XmlDocument();
                    XmlNode f = xmldoc.ImportNode(contentNode, true);
                    xmldoc.AppendChild(xmldoc.ImportNode(f, true));

                    //compress the document
                    FileWriter fileWriter = new FileWriter();

                    int FileVersion = 1;
                    //get File Version of internal compression
                    var VersionNode = n.Attributes["CompressionVersion"];
                    if (!(VersionNode == null))
                    {
                        FileVersion = Int32.Parse(VersionNode.Value);
                    }
                    else
                    {
                        //todo remove this
                        Console.WriteLine("Your interpreter should specify a version for internal FileDBs. For this conversion, 1 was auto-chosen. Make sure your versions match up!");
                    }

                    var stream = fileWriter.Export(xmldoc, new MemoryStream(), FileVersion);

                    //get this stream to hex 
                    node.InnerText = HexHelper.ByteArrayToString(HexHelper.StreamToByteArray(stream));

                    //try to overwrite the bytesize since it's always exported the same way
                    var ByteSize = node.SelectSingleNode("./preceding-sibling::ByteCount");
                    if (ByteSize != null)
                    {
                        long BufferSize = stream.Length;
                        Type type = typeof(int);
                        ByteSize.InnerText = HexHelper.ByteArrayToString(ConverterFunctions.ConversionRulesExport[type](BufferSize.ToString(), new UnicodeEncoding()));
                    }
                }
            }

            return doc;
        }

        [Obsolete]
        private void ConvertNodeSet(XmlNodeList matches, XmlNode ConverterInfo)
        {
            IEnumerable<XmlNode> cast = matches.Cast<XmlNode>();
            ConvertNodeSet(cast, ConverterInfo);
        }

        [Obsolete("ConvertNodeSet(IEnumerable<XmlNode> matches, XmlNode ConverterInfo) is deprecated, please use Export(IEnumerable<XmlNode> matches, Conversion c) instead.")]
        private void ConvertNodeSet(IEnumerable<XmlNode> matches, XmlNode ConverterInfo)
        {
            //get type the nodeset should be converted to
            var type = Type.GetType("System." + ConverterInfo.Attributes["Type"].Value);
            //get encoding
            Encoding encoding = new UnicodeEncoding();
            if (ConverterInfo.Attributes["Encoding"] != null)
                encoding = Encoding.GetEncoding(ConverterInfo.Attributes["Encoding"].Value);
            //get structure
            String Structure = "Default";
            if (ConverterInfo.Attributes["Structure"] != null)
                Structure = ConverterInfo.Attributes["Structure"].Value;
            //get Cdata
            bool IsCdataNode = false;
            if (ConverterInfo.Attributes["IsCdataNode"] != null)
                IsCdataNode = true;

            //getEnum
            var EnumEntries = ConverterInfo.SelectNodes("./Enum/Entry");
            RuntimeEnum Enum = new RuntimeEnum();
            if (EnumEntries != null)
            {
                foreach (XmlNode EnumEntry in EnumEntries)
                {
                    try
                    {
                        var Value = EnumEntry.Attributes["Value"];
                        var Name = EnumEntry.Attributes["Name"];
                        if (Value != null && Name != null)
                        {
                            Enum.AddValue(Name.Value, Value.Value);
                        }
                        else
                        {
                            Console.WriteLine("An XML Node Enum Entry was not defined correctly. Please check your interpreter file if every EnumEntry has an ID and a Name");
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                    }
                }
            }

            foreach (XmlNode node in matches)
            {
                switch (Structure)
                {
                    case "List":
                        try
                        {
                            exportAsList(node, type, encoding, IsCdataNode);
                        }
                        catch (InvalidConversionException e)
                        {
                            Console.WriteLine("Invalid Conversion at: {1}, Data: {0}, Target Type: {2}", e.ContentToConvert, e.NodeName, e.TargetType);
                        }
                        break;
                    case "Default":
                        try
                        {
                            ExportSingleNode(node, type, encoding, Enum, IsCdataNode);
                        }
                        catch (InvalidConversionException e)
                        {
                            Console.WriteLine("Invalid Conversion at: {1}, Data: {0}, Target Type: {2}", e.ContentToConvert, e.NodeName, e.TargetType);
                        }
                        break;
                }
            }
        }

        #endregion
    }
}