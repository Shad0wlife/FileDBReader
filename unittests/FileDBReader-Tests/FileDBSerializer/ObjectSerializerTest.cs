﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileDBReader_Tests;
using FileDBSerializing.ObjectSerializer;
using FileDBSerializing.Tests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace FileDBSerializing.Tests
{
    [TestClass]
    public class ObjectSerializerTest
    {
        [TestMethod]
        public void SerializeTest_V1() 
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version1.filedb");

            var obj = TestDataSources.GetTestAsset();
            FileDBSerializer<RootObject> objectserializer = new FileDBSerializer<RootObject>(FileDBDocumentVersion.Version1);
            
            Stream result = new MemoryStream();
            objectserializer.Serialize(result, obj);

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, result));
        }

        [TestMethod]
        public void SerializeTest_V2()
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            var obj = TestDataSources.GetTestAsset();
            FileDBSerializer<RootObject> objectserializer = new FileDBSerializer<RootObject>(FileDBDocumentVersion.Version2);
            MemoryStream result = new MemoryStream();
            objectserializer.Serialize(result, obj);

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, result));
        }

        [TestMethod]
        public void DeserializeTest_V1()
        {
            var x = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version1.filedb");

            DocumentParser<FileDBDocument_V1> parser = new();
            IFileDBDocument doc = parser.LoadFileDBDocument(x);

            FileDBDocumentDeserializer<RootObject> objectdeserializer = new FileDBDocumentDeserializer<RootObject>(new() { Version = FileDBDocumentVersion.Version1 });

            var DeserializedDocument = objectdeserializer.GetObjectStructureFromFileDBDocument(doc);

            DeserializedDocument.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }

        [TestMethod]
        public void DeserializeTest_V2()
        {
            var x = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            DocumentParser<FileDBDocument_V2> parser = new();
            IFileDBDocument doc = parser.LoadFileDBDocument(x);

            FileDBDocumentDeserializer<RootObject> objectdeserializer = new FileDBDocumentDeserializer<RootObject>(new() { Version = FileDBDocumentVersion.Version1 });

            var DeserializedDocument = objectdeserializer.GetObjectStructureFromFileDBDocument(doc);

            DeserializedDocument.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }

        [TestMethod]
        public void StaticConvertTest_Serialize()
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            var obj = TestDataSources.GetTestAsset();

            Stream Result = FileDBConvert.SerializeObject(obj, new() { Version = FileDBDocumentVersion.Version2 });

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, Result));
        }

        [TestMethod]
        public void StaticConvertTest_Deserialize()
        {
            var source = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");
            RootObject? result = FileDBConvert.DeserializeObject<RootObject>(source, new() { Version = FileDBDocumentVersion.Version2 });

            result.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }
    }
}
