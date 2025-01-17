﻿using FileDBSerializer.ObjectSerializer.DeserializationHandlers;
using FileDBSerializing.EncodingAwareStrings;
using FileDBSerializing.LookUps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FileDBSerializing.ObjectSerializer
{
    public class FileDBDocumentDeserializer<T> where T : class, new()
    {
        private FileDBSerializerOptions Options;
        private T TargetObject;
        private Type TargetType;

        public FileDBDocumentDeserializer(FileDBSerializerOptions options)
        {
            Options = options;
            TargetObject = new T();
            TargetType = TargetObject.GetType();
        }

        public T GetObjectStructureFromFileDBDocument(IFileDBDocument doc)
        {
            var targetType = typeof(T);

            var target = Activator.CreateInstance(targetType) as T;
            if (target is null)
                throw new InvalidOperationException($"Could not create an instance of {targetType}. A parameterless, public constructor is needed!");

            var nodeNames = doc.Roots.Select(x => x.Name).Distinct();

            foreach (var nodeName in nodeNames)
            {
                var nodes = doc.Roots.SelectNodes(nodeName);

                var property = targetType.GetPropertyWithRenaming(nodeName);

                if (property is null && !Options.IgnoreMissingProperties)
                    throw new InvalidProgramException($"{nodeName} could not be resolved to a property of {targetType.Name}");
                else if (property is null)
                    continue;

                var handler = HandlerProvider.GetHandlerFor(property);

                var obj = handler.Handle(nodes, property.PropertyType, Options);
                property.SetValue(target, obj);
            }

            return target;
        }
    }
}
