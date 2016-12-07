using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.GridFS;
using Xunit;
using Xunit.Abstractions;

namespace MongoTest
{
    public class BlobTest
    {
        private MongoGridFS _mongoGridFs;

        private IMongoDatabase _blobDataBase;

        public BlobTest()
        {
            JsonWriterSettings.Defaults.Indent = true;
            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost"));
            var mongoServer = new MongoServer(MongoServerSettings.FromClientSettings(mongoClientSettings));
            
            _mongoGridFs = new MongoGridFS(mongoServer, "Test",new MongoGridFSSettings());


            var mongoClient = new MongoClient(mongoClientSettings);
            _blobDataBase = mongoClient.GetDatabase("Test");
        }

        [Fact]
        public void Upload()
        {
            var pathToFile = "credolab.jpg";
            var fileStream = File.Open(pathToFile, FileMode.Open);

            var option = new MongoGridFSCreateOptions()
            {
                Id = ObjectId.GenerateNewId(),
                ContentType = Path.GetExtension(pathToFile),
                Metadata = new BsonDocument() { { "creator","Vitek"}}
            };

            _mongoGridFs.Upload(fileStream, Path.GetFileName(pathToFile), option);
        }

        [Fact]
        public void UploadBucket()
        {
            var pathToFile = "credolab.jpg";
            var fileStream = File.Open(pathToFile, FileMode.Open);
            var bucket = new GridFSBucket(_blobDataBase, new GridFSBucketOptions()
            {
                BucketName = "Simple"
            });
            bucket.UploadFromStream(Path.GetFileNameWithoutExtension(pathToFile), fileStream,new GridFSUploadOptions()
            {
                Metadata = new BsonDocument() { { "creator", "Vitek" } }
            });
        }

        [Fact]
        public void DownloadBucket()
        {
            var bucket = new GridFSBucket(_blobDataBase, new GridFSBucketOptions()
            {
                BucketName = "Simple"
            }); 
            var stream = bucket.OpenDownloadStream(ObjectId.Parse("58486e92c6a8bd38a41549e9"),new GridFSDownloadByNameOptions()
            {
                
            });
            using (var newFs = new FileStream("new-image.jpg", FileMode.Create))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                newFs.Write(bytes, 0, bytes.Length);
            }
        }
    }
}