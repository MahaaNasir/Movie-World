using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace lab3
{
    class Connection
    {
        public AmazonDynamoDBClient Connect()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json", optional: true, reloadOnChange: true);

            string accessKeyID = builder.Build().GetSection("AWSCredentials").GetSection("AccesskeyID").Value;
            string secretKey = builder.Build().GetSection("AWSCredentials").GetSection("Secretaccesskey").Value;
            var credentials = new BasicAWSCredentials(accessKeyID, secretKey);

            var region = RegionEndpoint.GetBySystemName(builder.Build().GetSection("AWSCredentials").GetSection("Region").Value);

            var client = new AmazonDynamoDBClient(credentials, region);
            return client;
        }

        public AmazonS3Client ConnectS3()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppSettings.json", optional: true, reloadOnChange: true);

            string accessKeyID = builder.Build().GetSection("AWSCredentials").GetSection("AccesskeyID").Value;
            string secretKey = builder.Build().GetSection("AWSCredentials").GetSection("Secretaccesskey").Value;

            var region = RegionEndpoint.GetBySystemName(builder.Build().GetSection("AWSCredentials").GetSection("Region").Value);

            var client = new AmazonS3Client(accessKeyID, secretKey, region);
            return client;
        }
    }
}
