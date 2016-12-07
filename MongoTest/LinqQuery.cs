using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
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

        private MongoClientSettings _mongoClientSettings;

        public LinqQuery(ITestOutputHelper helper)
        {
            JsonWriterSettings.Defaults.Indent = true;
            _helper = helper;
            _mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost"));

            //_mongoClientSettings.ClusterConfigurator =
            //    builder => builder.Subscribe<CommandStartedEvent>(c => _helper.WriteLine(c.Command.ToString()));

            var mongoClient = new MongoClient(_mongoClientSettings);
            _users = mongoClient.GetDatabase("Test").GetCollection<User>("users");
            _countries = mongoClient.GetDatabase("Test").GetCollection<Country>("countries");
            _phones = mongoClient.GetDatabase("Test").GetCollection<Phones>("phones");
        }

        [Fact]
        public void CreateUsers()
        {
            for (int i = 0; i < 20; i++)
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                var reg = new DateTime(random.Next(1990, 2001), random.Next(1, 13), random.Next(1, 25));
                var user = new User()
                {
                    Name = $"{Convert.ToChar((i + 65) % 90).ToString().ToUpper()}van",
                    Birthday = new DateTime(random.Next(1990, 2001), random.Next(1, 13), random.Next(1, 25)),
                    RegistrationLocal = reg,
                    RegistrationUtc = reg,
                    RegistartionId = random.Next(1, 5),
                };

                _users.InsertOne(user);
            }
        }
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
        #region Linq

        [Fact]
        public void WhereSelect()
        {
            var users = _users.AsQueryable().Where(u => u.Name.EndsWith("an") && u.RegistartionId % 2 == 0)
                .Select(u => new { u.Name, u.RegistartionId }).ToList();
            users.ForEach(u => _helper.WriteLine($"{u.Name} - {u.RegistartionId}"));
        }

        [Fact]
        public void Aggregate()
        {
            var users = _users.Aggregate().Project(u => new { u.RegistartionId })
                .Group(u => u.RegistartionId, u => new { u.Key, Count = u.Count() }).Limit(4).ToList();

            var users1 = _users.Aggregate().Match(u => u.Phones != null).Project(u => new { u.Id, c = u.Phones.Count() }).ToList();
            users.ForEach(u => _helper.WriteLine($"RegistartionId {u.Key} Count {u.Count}"));
            users1.ForEach(u => _helper.WriteLine($"{u.c}"));
        }

        [Fact]
        public void AggregateLinq()
        {
            var users = _users.AsQueryable().Select(u => new { u.RegistartionId })
                .GroupBy(u => u.RegistartionId).Select(u => new { u.Key, Count = u.Count() }).ToList();
            users.ForEach(u => _helper.WriteLine($"RegistartionId {u.Key} Count {u.Count}"));
        }

        #endregion
        #region Join

        [Fact]
        public void JoinOneToManyPart1()
        {
            var users = _users.Aggregate()
            .Lookup<User, Country, User>(_countries, u => u.CountryId, c => c.Id, u => u.Country).Unwind<User, User>(u => u.Country).ToList();

            _helper.WriteLine($"Found users - {users.Count}");
            foreach (var user in users)
            {
                _helper.WriteLine($"User name - {user.Name} Country name - {user.Country.CountryName}");
            }

            var aggrUsers = _users.Aggregate()
                .Lookup<User, Phones, User>(_phones, u => u.Id, p => p.UserId, u => u.Phones)
                .Project(Builders<User>.Projection.Expression(u => new
                {
                    name = u.Name,
                    count = u.Phones.Count()
                })).ToList();

            _helper.WriteLine($"Found users - {aggrUsers.Count}");
            foreach (var user in aggrUsers)
            {
                _helper.WriteLine($"User name - {user.name} Phone count - {user.count}");
            }
        }

        [Fact]
        public void JoinOneToManyPart2()
        {
            var users = _users.Aggregate()
                .Lookup<User, Phones, User>(_phones, u => u.Id, p => p.UserId, u => u.Phones).ToList();
            _helper.WriteLine($"Found users - {users.Count}");
            foreach (var user in users)
            {
                _helper.WriteLine($"User name - {user.Name}");
                foreach (var userPhone in user.Phones)
                {
                    _helper.WriteLine($"Phone number - {userPhone.Number}");
                }
            }
        }

        #endregion
    }
}
