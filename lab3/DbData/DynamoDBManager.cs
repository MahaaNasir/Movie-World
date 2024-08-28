using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lab3.DbData
{
    public class DynamoDBManager
    {
        private readonly IAmazonDynamoDB _dynamoDBClient;

        public DynamoDBManager(IAmazonDynamoDB dynamoDBClient)
        {
            _dynamoDBClient = dynamoDBClient;
        }

        public async Task CreateTableIfNotExistsAsync()
        {
            var tableName = "Users";

            try
            {
                await DescribeTableAsync(tableName);
                Console.WriteLine($"Table '{tableName}' already exists.");
            }
            catch (ResourceNotFoundException)
            {
                await CreateTableAsync(tableName);
                Console.WriteLine($"Table '{tableName}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task DescribeTableAsync(string tableName)
        {
            var request = new DescribeTableRequest
            {
                TableName = tableName
            };

            await _dynamoDBClient.DescribeTableAsync(request);
        }

        private async Task CreateTableAsync(string tableName)
        {
            var keySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "UserId",
                    KeyType = KeyType.HASH // Partition key
                }
            };

            var attributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "UserId",
                    AttributeType = ScalarAttributeType.N // Assuming UserId is a number
                }
                // Add additional attribute definitions as needed
            };

            var provisionedThroughput = new ProvisionedThroughput
            {
                ReadCapacityUnits = 5,
                WriteCapacityUnits = 5
            };

            var request = new CreateTableRequest
            {
                TableName = tableName,
                KeySchema = keySchema,
                AttributeDefinitions = attributeDefinitions,
                ProvisionedThroughput = provisionedThroughput
            };

            try
            {
                var response = await _dynamoDBClient.CreateTableAsync(request);
                Console.WriteLine("Table creation response: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating table: " + ex.Message);
                throw; // Rethrow the exception to handle it at a higher level if needed
            }
        }
    }
}
