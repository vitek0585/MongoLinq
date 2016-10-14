using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;

namespace MongoTest
{
    public class MobileData
    {
        //[BsonId(IdGenerator = typeof(BsonObjectIdGenerator))]
        public string Id { get; set; }

        public IEnumerable<Element> Data { get; set; }
    }

    [JsonConverter(typeof(MobileDataJsonConverter))]
    public class Element
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public List<Element> ChildElements { get; set; }

        public Element()
        {
        }

        public Element(string name)
        {
            Name = name;
        }

        public Element(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}