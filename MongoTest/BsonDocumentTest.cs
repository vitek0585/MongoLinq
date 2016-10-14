using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;
using Xunit.Abstractions;

namespace MongoTest
{
    public class BsonDocumentTest
    {
        private readonly ITestOutputHelper _helper;

        private IMongoCollection<User> _users;
        public BsonDocumentTest(ITestOutputHelper helper)
        {
            JsonWriterSettings.Defaults.Indent = true;
            _helper = helper;
           
            var settings = new MongoClientSettings()
            {
                Server = new MongoServerAddress("localhost")
            };
            var mongoClient = new MongoClient(settings);
            _users = mongoClient.GetDatabase("Test").GetCollection<User>("users");
        }

        [Fact]
        public User CreateUser(int id = 1, int registartionId = 20, string name = "Ivan")
        {
            var currentTime = DateTime.Now;
            var currentTimeUtc = DateTime.UtcNow;
            var userBson = new BsonDocument()
            {
                {
                    "_id",id
                },
                {
                    "RegistartionId", registartionId //BsonInt32.Create(registartionId)
                },
                {
                    "Name",BsonString.Create(name)
                },
                {
                    "Birthday",new DateTime(1990,10,22)
                },
                //Driver will automatically convert your datetime to mongodb 
                //format and store in as UTC date, and will convert back to your 
                //local timezone back when you will read it 
                //(actually you can change this behavior via driver settings). 
                //So, take it in the mind that dates in mongodb always in UTC format.
                {
                    "RegistrationLocal", currentTime
                },
                {
                    "RegistrationUtc", currentTimeUtc
                }
            };

            _helper.WriteLine("Current time");
            _helper.WriteLine(currentTime.ToString("O"));
            _helper.WriteLine("Current time ToJson");
            _helper.WriteLine(currentTime.ToJson());
            _helper.WriteLine("Current UtcTime");
            _helper.WriteLine(currentTimeUtc.ToString("O"));
            _helper.WriteLine("Current UtcTime ToJson");
            _helper.WriteLine(currentTimeUtc.ToJson());
            _helper.WriteLine("User in bson");
            _helper.WriteLine(userBson.ToJson());
            _helper.WriteLine("User to poco");
            var userPoco = BsonSerializer.Deserialize<User>(userBson);
            _helper.WriteLine(userPoco.ToJson());
            userPoco.RegistrationLocal = currentTime;

            return userPoco;
        }

        [Fact]
        public void PopulateDb()
        {
            for (int i = 0; i < 10; i++)
            {
                var user = CreateUser(i, i + 10, $"{Convert.ToChar((i + 65) % 90).ToString().ToUpper()}van");
                _users.InsertOne(user);
            }
        }

        #region Find
        [Fact]
        public void FindSimple()
        {
            IFindFluent<User, User> foundUsers = _users.Find(FilterDefinition<User>.Empty);
            List<User> users = foundUsers.ToList();
            Print(users);
        }

        [Fact]
        public void FindWithFilter()
        {
            IFindFluent<User, User> foundUsers = _users.Find(new BsonDocument() { { "Name", "van" } });
            List<User> users = foundUsers.ToList();
            Print(users);
        }

        [Fact]
        public void FindFilter()
        {
            var filterDefinition = Builders<User>.Filter.Ne(user => user.Name, "Ivan");

            PrintQueryFilter(filterDefinition);

            var foundUsers = _users.Find(filterDefinition).ToList();
            Print(foundUsers);
        }

        [Fact]
        public void FindFilterSort()
        {
            var filterDefinition = Builders<User>.Filter.Where(user => user.Name.Contains("Avan")) |
            Builders<User>.Filter.Gte(user => user.RegistartionId, 15);

            //if you need to add conditional in later
            //filterDefinition &= Builders<User>.Filter.Ne(u => u.Id, 1);

            var sortDefinition = Builders<User>.Sort.Combine(
            Builders<User>.Sort.Ascending(u => u.RegistartionId),
            Builders<User>.Sort.Ascending(u => u.RegistrationLocal));

            PrintQueryFilter(filterDefinition);
            PrintQuerySort(sortDefinition);

            var foundUsers = _users.Find(filterDefinition).Sort(sortDefinition).ToList();

            //var foundUsers = _users.Find(u => u.Name.Contains("Avan") || u.RegistartionId >= 15)
            //.SortBy(u => u.RegistartionId)
            //.ThenByDescending(u => u.RegistrationLocal).ToList();

            Print(foundUsers);
        }

        [Fact]
        public void FindFilterSortProjection()
        {
            var filterDefinition = Builders<User>.Filter.Where(user => user.Name.Contains("van"));
            var sortDefinition = Builders<User>.Sort.Combine(
            Builders<User>.Sort.Descending(u => u.RegistartionId),
            Builders<User>.Sort.Ascending(u => u.RegistrationLocal));

            var projectionDefinition = Builders<User>.Projection.Expression(
                u => new UserInfo() { FullUserInfo = $"{u.Name} {u.RegistartionId.ToString()}" });

            PrintQueryFilter(filterDefinition);
            PrintQuerySort(sortDefinition);

            var foundUsers = _users.Find(filterDefinition)
            .Sort(sortDefinition)
            .Project(projectionDefinition)
            .ToList();

            foundUsers.ForEach(u => _helper.WriteLine(u.ToString()));
        }

        #endregion
        #region Update

        [Fact]
        public void Update()
        {
            var builder = Builders<User>.Filter;
            //registartionId - not found
            var filter = builder.Eq(u => u.Name, "Ivan");
            //var update = Builders<User>.Update.Set(u => u.Name, "Alexander")
            //.PushEach(u => u.Phones.Numbers, new string[] { "0923", "234234" });
            //var result = _users.UpdateOne(filter, update);
            //_helper.WriteLine("Modified Count " + result.ModifiedCount);
        }

        [Fact]
        public void UpdateMany()
        {
            var builder = Builders<User>.Filter;
            //registartionId - not found
            var filter = builder.Gt(u => u.RegistartionId, 16);
            var update = Builders<User>.Update.Inc(u => u.RegistartionId, 10);
            var result = _users.UpdateMany(filter, update);
            _helper.WriteLine("Modified Count " + result.ModifiedCount);
        }
        #endregion
        #region Insert
        [Fact]
        public void Insert()
        {
            var user = new User()
            {
                Name = "Vitek",
                RegistartionId = 1000,
                //Phones = new Phones() { Numbers = new List<string>() { "0950567", "0620567" } }
            };
            _users.InsertOne(user);
            _helper.WriteLine("User Id " + user.Id);
        }
        #endregion
        #region Replace
        [Fact]
        public void Replace()
        {
            var user = new User()
            {
                Id = 1,
                Name = "Alena",
                //Phones = new Phones() { Numbers = new List<string>() { "102", "103" } }
            };
            var replaceOneResult = _users.ReplaceOne(u => u.Id == 1, user);//,new UpdateOptions() {IsUpsert = true});
            _helper.WriteLine("Modified Count " + replaceOneResult.ModifiedCount);
        }
        #endregion
        #region Delete
        [Fact]
        public void Delete()
        {
            var filterDefinition = Builders<User>.Filter.Eq(user => user.Id, 2);
            var result = _users.DeleteOne(filterDefinition);
            _helper.WriteLine(result.DeletedCount.ToString());
        }
        #endregion

        private void Print(IEnumerable<User> foundUsers)
        {
            _helper.WriteLine($"Found users - {foundUsers.Count()}");
            foreach (var user in foundUsers) { _helper.WriteLine(user.ToJson()); }
        }

        private void PrintQueryFilter(FilterDefinition<User> filterDefinition)
        {
            //https://docs.mongodb.com/manual/reference/method/js-collection/
            _helper.WriteLine("Filter query");
            _helper.WriteLine(filterDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<User>(),
                BsonSerializer.SerializerRegistry).ToString());
        }

        private void PrintQuerySort(SortDefinition<User> sortDefinition)
        {
            _helper.WriteLine("Sort query");
            _helper.WriteLine(sortDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<User>(),
                BsonSerializer.SerializerRegistry).ToString());
        }
    }

    public class UserInfo
    {
        public string FullUserInfo { get; set; }

        public override string ToString()
        {
            return FullUserInfo;
        }
    }
}
