using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MongoTest
{
    public class MobileDataJsonConverter : JsonConverter
    {
        private readonly string _childsElementsName = nameof(Element.ChildElements)[0].ToString();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var element = value as Element;
            writer.WriteStartObject();
            if (element != null)
            {
                writer.WritePropertyName(element.Name);
                serializer.Serialize(writer, element.Value);
                if (element.ChildElements != null)
                {
                    writer.WritePropertyName(_childsElementsName);
                    serializer.Serialize(writer, element.ChildElements);
                }
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var jsonObject = JObject.Load(reader);
                var element = new Element();
                var properties = jsonObject.Properties().ToList();
                element.Name = properties[0].Name;
                element.Value = (string)properties[0].Value;
                if (jsonObject[_childsElementsName] != null)
                {
                    element.ChildElements = jsonObject[_childsElementsName].ToObject<List<Element>>();
                }

                return element;
            }

            return JArray.Load(reader).ToObject<List<Element>>();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Element).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }
    }

}