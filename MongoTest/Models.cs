using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoTest
{
    //[BsonIgnoreExtraElements]
    //[BsonNoId]
    public class User
    {
        [BsonId(IdGenerator = typeof(BsonObjectIdGenerator))]
        public BsonValue Id { get; set; }//Represent how _id

        //[BsonElement("RegId")]
        //[BsonIgnoreIfDefault]
        public int RegistartionId { get; set; }

        public string Name { get; set; }

        // [BsonDateTimeOptions(DateOnly = true)]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Birthday { get; set; }

        // [BsonDateTimeOptions(Kind = DateTimeKind.Local, Representation = BsonType.DateTime)]
        public DateTime RegistrationLocal { get; set; }

        public DateTime RegistrationUtc { get; set; }

        [BsonIgnoreIfNull]
        public IEnumerable<Phones> Phones { get; set; }

        [BsonIgnoreIfDefault]
        public int CountryId { get; set; }

        //[BsonIgnore]
        public Country Country { get; set; }
    }

    public class Phones
    {
        [BsonId(IdGenerator = typeof(BsonObjectIdGenerator))]
        public BsonValue Id { get; set; }
        public BsonValue UserId { get; set; }
        public string Number { get; set; }
    }

    public class Country
    {
        //[BsonId]
        public int Id { get; set; }

        [BsonElement("country")]
        public string CountryName { get; set; }
    }
}