using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Xunit;
using Xunit.Abstractions;

namespace MongoTest
{
    public class LinqQuery
    {
        private readonly ITestOutputHelper _helper;

        private IMongoCollection<User> _users;
        private IMongoCollection<Country> _countries;
        private IMongoCollection<Phones> _phones;
        public LinqQuery(ITestOutputHelper helper)
        {
            JsonWriterSettings.Defaults.Indent = true;
            _helper = helper;
            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost"));

            mongoClientSettings.ClusterConfigurator =
                builder => builder.Subscribe<CommandStartedEvent>(c => _helper.WriteLine(c.Command.ToString()));

            var mongoClient = new MongoClient(mongoClientSettings);
            _users = mongoClient.GetDatabase("Test").GetCollection<User>("users");
            _countries = mongoClient.GetDatabase("Test").GetCollection<Country>("countries");
            _phones = mongoClient.GetDatabase("Test").GetCollection<Phones>("phones");
            ObjectId.GenerateNewId();
        }

        [Fact]
        public void CreateUsers()
        {
            for (int i = 0; i < 20; i++)
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                var reg = new DateTime(random.Next(1990, 2001), random.Next(1, 13), random.Next(1, 25));
                _users.InsertOne(new User()
                {
                    Name = $"{Convert.ToChar((i + 65) % 90).ToString().ToUpper()}van",
                    Birthday = new DateTime(random.Next(1990, 2001), random.Next(1, 13), random.Next(1, 25)),
                    RegistrationLocal = reg,
                    RegistrationUtc = reg,
                    RegistartionId = random.Next(1, 5)
                });
            }
        }

        #region Linq

        [Fact]
        public void WhereSelect()
        {
            var users = _users.AsQueryable().Where(u => u.Name.EndsWith("an") && u.RegistartionId > 20)
                .Select(u => u.Name).ToList();
            users.ForEach(_helper.WriteLine);
        }

        [Fact]
        public void Aggregate()
        {
            var users = _users.Aggregate().Project(u => new { u.Birthday })
                .Group(u => u.Birthday, u => new { u.Key, Count = u.Count() }).Limit(1).ToList();

            var users1 = _users.Aggregate().Project(u => new { u.Id, c = u.Phones.Count() }).ToList();
            users.ForEach(u => _helper.WriteLine($"{u.Key} {u.Count}"));
            users1.ForEach(u => _helper.WriteLine($"{u.c}"));
        }

        [Fact]
        public void AggregateLinq()
        {
            var users = _users.AsQueryable().Select(u => new { u.Birthday })
                .GroupBy(u => u.Birthday).Select(u => new { u.Key, Count = u.Count() }).ToList();
            users.ForEach(u => _helper.WriteLine($"{u.Key} {u.Count}"));
        }

        #endregion
        #region Join

        [Fact]
        public void CreateCountries()
        {
            var countries = new[] { "Ukraine", "Germany", "France", "Usa" };
            for (int i = 0; i < countries.Length; i++)
            {
                _countries.InsertOne(new Country() { Id = i + 1, CountryName = countries[i] });
            }
        }

        [Fact]
        public void AddCountryForUser()
        {
            var users = _users.Find(FilterDefinition<User>.Empty).ToList();
            for (int i = 0; i < users.Count; i++)
            {
                var countryId = (int)((i + 1) % (_countries.Count(FilterDefinition<Country>.Empty) + 1));
                _users.UpdateOne(u => u.Id == users[i].Id,
                    Builders<User>.Update.Set(u => u.CountryId, countryId));
            }
        }

        [Fact]
        public void JoinManyToOne()
        {
            var bsonDocument = new { name = "asdasd", count = 100 }.ToBsonDocument();
            var users = _users.Aggregate()
                .Lookup<User, Country, User>(_countries, u => u.CountryId, c => c.Id, u => u.Country).Unwind<User, User>(u => u.Country).ToList();

            //Print(users);
            var projectionDefinition = Builders<User>.Projection.Expression(u => new { u.Name, count = u.Phones.Count() });
            var aggrUsers = _users.Aggregate()
                .Lookup<User, Phones, User>(_phones, u => u.Id, p => p.UserId, u => u.Phones)
                .Project(Builders<User>.Projection
                .Expression(u => new
                {
                    name = u.Name,
                    count = u.Phones.Count()
                })).ToList();

            Print(aggrUsers);
        }

        [Fact]
        public void AddPhones()
        {
            var users = _users.AsQueryable().ToList();
            for (int i = 0; i < 30; i++)
            {
                var currentUserPosition = new Random(Guid.NewGuid().GetHashCode()).Next(0, users.Count);
                var userId = users.ElementAt(currentUserPosition).Id;
                _phones.InsertOne(new Phones()
                {
                    Number = new Random(Guid.NewGuid().GetHashCode()).Next(100000, 1000000).ToString(),
                    UserId = userId
                });
            }
        }

        [Fact]
        public void JoinOneToMany()
        {
            var users = _users.Aggregate()
                .Lookup<User, Phones, User>(_phones, u => u.Id, p => p.UserId, u => u.Phones).ToList();
            Print(users);
        }

        #endregion

        private void Print(IEnumerable<User> foundUsers)
        {
            _helper.WriteLine($"Found users - {foundUsers.Count()}");
            foreach (var user in foundUsers) { _helper.WriteLine(user.ToJson()); }
        }

        private void Print(IEnumerable<dynamic> foundUsers)
        {
            _helper.WriteLine($"Found users - {foundUsers.Count()}");
            foreach (var user in foundUsers) { _helper.WriteLine(user.name); }
        }
        private void Print(IEnumerable<BsonDocument> foundUsers)
        {
            _helper.WriteLine($"Found users - {foundUsers.Count()}");
            foreach (var user in foundUsers) { _helper.WriteLine(user.ToJson()); }
        }
    }

    public class Country
    {
        //[BsonId]
        public int Id { get; set; }

        [BsonElement("country")]
        public string CountryName { get; set; }
    }
}
