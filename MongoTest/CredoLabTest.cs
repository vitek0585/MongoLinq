using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CredoLab.Mobile;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;
using Xunit.Abstractions;

namespace MongoTest
{
    public class CredoLabTest
    {
        private ITestOutputHelper _helper;

        private IMongoCollection<BsonDocument> _items;
        private IMongoCollection<Sample> _samples;

        private IMongoCollection<MobileData> _mobileDatas;

        public CredoLabTest(ITestOutputHelper helper)
        {

            JsonWriterSettings.Defaults.Indent = true;
            BsonSerializer.RegisterSerializer(typeof(MobileData), new CustomBsonSerializer());
            BsonSerializer.RegisterSerializer(typeof(Sample), new SampleSerialize());

            _helper = helper;
            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost"));
            mongoClientSettings.ClusterConfigurator =
                builder => builder.Subscribe<CommandStartedEvent>(c => _helper.WriteLine(c.Command.ToString()));
            var mongoClient = new MongoClient(mongoClientSettings);

            _items = mongoClient.GetDatabase("Test").GetCollection<BsonDocument>("credolab");
            _mobileDatas = mongoClient.GetDatabase("Test").GetCollection<MobileData>("credolab");
            _samples = mongoClient.GetDatabase("Test").GetCollection<Sample>("serialize");

        }

        [Fact]
        public void SelectSample()
        {
            //var itemsJson = _samples.Aggregate().Match(s => s.A.StartsWith("M")).ToList();
            var itemsJson = _samples.AsQueryable().Where(r => r.B.Count > 0).ToList();
        }


        [Fact]
        public void Select()
        {
            var projectionDefinition = Builders<BsonDocument>.Projection.Combine(
                    new BsonDocument() { { "_id", "$Data.DataSourceType" } });// count=b["C"].AsBsonArray.Count});
            var countCantacts =
                _items.Aggregate()
                    .Unwind(f => f["Data"])
                    .Match(d => d["Data.DataSourceType"] == "Contact").ToList();

            var enumerable = _mobileDatas.AsQueryable().ToList().First().Data
                .First(e => e.Value == CredoAppConstants.Contact.DATA_SOURCE_TYPE_NAME)
                .ChildElements.SelectMany(e => e.ChildElements).Where(e1 => e1.Name == CredoAppConstants.Contact.PHOTO_ID 
                && e1.Value == "True");


        }

        [Fact]
        public void ParseDataMobileItem()
        {
            var itemsJson = _items.AsQueryable().First().ToJson();
            var deserializeObject = Newtonsoft.Json.JsonConvert.DeserializeObject<MobileData>(itemsJson);
            var names = GenerateAssembly(deserializeObject.Data);
            CreateAssembly(names);
            //SaveToFile(names);
        }

        private void CreateAssembly(Dictionary<string, IEnumerable<string>> names)
        {
            AssemblyName assName = new AssemblyName("CredoLab.Mobile");
            assName.Version = new Version(0, 0, 1);
            var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.Save);
            var moduleBuilder = assBuilder.DefineDynamicModule("CredoLabModule", "credolab.mobile.dll");

            var typeBuilder = moduleBuilder.DefineType("CredoLab.Mobile.CredoAppConstants", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new Type[] { });
            var dataSourceTypeField = typeBuilder.DefineField("Data_Source_Type".ToUpper(), typeof(string), FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
            dataSourceTypeField.SetConstant("DataSourceType");
            var dataField = typeBuilder.DefineField("Data".ToUpper(), typeof(string), FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
            dataField.SetConstant("Data");
            foreach (var keyValuePair in names)
            {
                var nestedType = typeBuilder.DefineNestedType($"{keyValuePair.Key}", TypeAttributes.NestedPublic | TypeAttributes.Class, typeof(object), new Type[] { });
                var dataSourceName = nestedType.DefineField("Data_Source_Type_Name".ToUpper(), typeof(string), FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                dataSourceName.SetConstant(keyValuePair.Key);
                foreach (var constantName in keyValuePair.Value)
                {
                    var fieldBuilder = nestedType.DefineField(constantName.ToUpper(), typeof(string), FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                    fieldBuilder.SetConstant(constantName);
                }
                nestedType.CreateType();
            }
            typeBuilder.CreateType();
            assBuilder.Save("credolab.mobile.dll");
        }

        private Dictionary<string, IEnumerable<string>> GenerateAssembly(IEnumerable<Element> deserializeObject)
        {
            var types = new Dictionary<string, IEnumerable<string>>();

            foreach (var element in deserializeObject)
            {
                var names = new List<string>();

                if (element.ChildElements != null)
                {
                    names.AddRange(GetNestedElementNames(element.ChildElements));
                }

                types.Add(element.Value, names.Distinct());
            }

            return types;
        }

        private IEnumerable<string> GetNestedElementNames(IEnumerable<Element> deserializeObject)
        {
            var names = new List<string>();
            foreach (var element in deserializeObject)
            {
                names.Add(element.Name);

                if (element.ChildElements != null)
                {
                    names.AddRange(GetNestedElementNames(element.ChildElements));
                }
            }

            return names;
        }

        private void SaveToFile(Dictionary<string, IEnumerable<string>> names)
        {
            var fileStream = File.OpenWrite(Path.Combine(Environment.CurrentDirectory, "Names.txt"));
            using (var streamWriter = new StreamWriter(fileStream))
            {
                foreach (var keyValuePair in names)
                {
                    streamWriter.WriteLine(keyValuePair.Key);
                    foreach (var s in keyValuePair.Value) { streamWriter.WriteLine("\t" + s); }
                }
                streamWriter.Flush();
            }
        }
    }
}