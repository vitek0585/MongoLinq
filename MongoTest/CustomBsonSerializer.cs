using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoTest
{
    public class SampleSerialize : SerializerBase<Sample>, IBsonDocumentSerializer//,IBsonArraySerializer
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Sample value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_id");
            writer.WriteString(value.Id);
            writer.WriteName("a");
            writer.WriteString(value.A);
            writer.WriteName("b");
            writer.WriteStartArray();
            
            for (int i = 0; i < value.B.Count; i++)
            {
                writer.WriteStartDocument();
                writer.WriteName("_id");
                writer.WriteString(value.B[i].Id);
                writer.WriteName("a");
                writer.WriteString(value.B[i].A);
                writer.WriteEndDocument();
            }
           
            writer.WriteEndArray();
            writer.WriteEndDocument();
        }

        public override Sample Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var sample = new Sample();
            context.Reader.ReadStartDocument();
            sample.Id = context.Reader.ReadString();
            sample.A = context.Reader.ReadString();
            context.Reader.ReadStartArray();
            context.Reader.ReadStartDocument();
            var sample1 = new Sample();
            sample1.Id = context.Reader.ReadString();
            sample1.A = context.Reader.ReadString();
            context.Reader.ReadEndDocument();
            context.Reader.ReadEndArray();
            context.Reader.ReadEndDocument();
            sample.B = new List<Sample>();
            sample.B.Add(sample1);
            return sample;
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = new BsonSerializationInfo(String.Empty, this,typeof(IEnumerable<Sample>));
            return true;
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var propertyType = typeof(Sample).GetProperty(memberName).PropertyType;
            serializationInfo = new BsonSerializationInfo(memberName.ToLower(),new ArraySerializer<Sample>(), propertyType);
            return true;
        }
    }


    public class Sample
    {
        public string Id { get; set; }

        public string A { get; set; }

        public List<Sample> B { get; set; }
    }

    public class CustomBsonSerializer : SerializerBase<MobileData>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MobileData value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("_id");
            writer.WriteString(value.Id);
            writer.WriteName(nameof(value.Data));
            writer.WriteStartArray();
            
            writer.WriteEndArray();
            writer.WriteEndDocument();
        }

        public override MobileData Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var md = new MobileData();
            context.Reader.ReadStartDocument();
            md.Id = context.Reader.ReadString();
            context.Reader.ReadStartArray();
            md.Data = GetElements(context);
            context.Reader.ReadEndArray();
            context.Reader.ReadEndDocument();

            return md;
        }

        private IEnumerable<Element> GetElements(BsonDeserializationContext context)
        {
            var elements = new List<Element>();

            while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                context.Reader.ReadStartDocument();
                var element = new Element();
                element.Name = context.Reader.ReadName(new Utf8NameDecoder());
                switch (context.Reader.GetCurrentBsonType())
                {
                    case BsonType.Null:
                        element.Value = null;
                        context.Reader.ReadNull();
                        break;
                    case BsonType.String:
                        element.Value = context.Reader.ReadString();
                        break;
                    case BsonType.Int32:
                        element.Value = context.Reader.ReadInt32().ToString();
                        break;
                }

                if (context.Reader.ReadBsonType() == BsonType.Array)
                {
                    context.Reader.ReadStartArray();
                    element.ChildElements = new List<Element>();
                    element.ChildElements.AddRange(GetElements(context));
                    context.Reader.ReadEndArray();
                }
                context.Reader.ReadEndDocument();
                elements.Add(element);
            }


            return elements;
        }
    }
}

