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

        private IMongoCollection<MobileData> _items;
        private IMongoCollection<Sample> _samples;

        public CredoLabTest(ITestOutputHelper helper)
        {
            JsonWriterSettings.Defaults.Indent = true;
            BsonSerializer.RegisterSerializer(typeof(MobileData), new CustomBsonSerializer());
            BsonSerializer.RegisterSerializer(typeof(Sample), new SampleSerialize());

            _helper = helper;
            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost"));
            var mongoClient = new MongoClient(mongoClientSettings);

            _items = mongoClient.GetDatabase("Test").GetCollection<MobileData>("credolab");
            _samples = mongoClient.GetDatabase("Test").GetCollection<Sample>("serialize");
            //BsonClassMap.RegisterClassMap<MobileData>(map =>
            //{
            //    map.AutoMap();
            //    map.SetIgnoreExtraElements(true);
            //    map.GetMemberMap(x => x.Id).SetElementName("_id");
            //    map.GetMemberMap(x => x.Data);
            //});

            //BsonClassMap.RegisterClassMap<Element>(map =>
            //{
            //    map.AutoMap();
            //    map.SetIgnoreExtraElements(true);
            //    map.GetMemberMap(x => x.Name);
            //    map.GetMemberMap(x => x.Value).Getter()SetElementName();
            //});
        }

        [Fact]
        public void SelectSample()
        {
            //var itemsJson = _samples.Aggregate().Match(s => s.A.StartsWith("M")).ToList();
            //var itemsJson = _samples.AsQueryable()..Where(r=>r.B.Count>0).ToList();
        }


        [Fact]
        public void Select()
        {
            var countCantacts = _items.AsQueryable().ToList().SelectMany(item => item.Data)
            .First(d=>d.Value==nameof(CredoAppConstants.Contact)).ChildElements.Count;
            //.First(element => element.Name == nameof(CredoAppConstants.Contact)).ChildElements.Count;

            Assert.Equal(59,countCantacts);
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
            var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.Save);
            var moduleBuilder = assBuilder.DefineDynamicModule("CredoLabModule", "credolab.mobile.dll");

            var typeBuilder = moduleBuilder.DefineType("CredoLab.Mobile.CredoAppConstants", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new Type[] { });

            foreach (var keyValuePair in names)
            {
                var nestedType = typeBuilder.DefineNestedType($"{keyValuePair.Key}", TypeAttributes.NestedPublic | TypeAttributes.Class, typeof(object), new Type[] { });
                //var defineTypeInitializer = nestedType.DefineConstructor(MethodAttributes.SpecialName|MethodAttributes.Static,
                //CallingConventions.Any, null);
                //var ilGenerator = defineTypeInitializer.GetILGenerator();

                foreach (var constantName in keyValuePair.Value)
                {
                    var fieldBuilder = nestedType.DefineField(constantName.ToUpper(), typeof(string), FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                    fieldBuilder.SetConstant(constantName);
                    //ilGenerator.Emit(OpCodes.Ldarg_0);
                    //ilGenerator.Emit(OpCodes.Ldstr, constantName);
                    //ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);

                }
                //ilGenerator.Emit(OpCodes.Call);
                //ilGenerator.Emit(OpCodes.Nop);
                //ilGenerator.Emit(OpCodes.Ret);
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