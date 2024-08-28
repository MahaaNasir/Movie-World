using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lab3
{
    public class DynamoDBInitializer
    {
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private const string TableName = "Movies";

        public DynamoDBInitializer()
        {
            // Initialize the DynamoDB client
            _dynamoDBClient = new AmazonDynamoDBClient();
        }

        public async Task CreateTableAsync()
        {
            try
            {
                // Define the table schema
                var request = new CreateTableRequest
                {
                    TableName = TableName,
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition
                        {
                            AttributeName = "MovieId",
                            AttributeType = ScalarAttributeType.N // N for Number (int)
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "Genre",
                            AttributeType = ScalarAttributeType.S // S for String
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "MovieName",
                            AttributeType = ScalarAttributeType.S // S for String
                        },
                        new AttributeDefinition
                        {
                            AttributeName = "UserId",
                            AttributeType = ScalarAttributeType.N // N for Number (int)
                        }
                    },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = "MovieId",
                            KeyType = KeyType.HASH // HASH for hash key (primary key)
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 5
                    }
                };

                // Create the table
                var response = await _dynamoDBClient.CreateTableAsync(request);

                // Wait for the table to be created
                await WaitForTableToBeActive(TableName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating table: " + ex.Message);
            }
        }

        private async Task WaitForTableToBeActive(string tableName)
        {
            string status = null;
            do
            {
                await Task.Delay(5000); // Wait for 5 seconds before checking again

                var response = await _dynamoDBClient.DescribeTableAsync(tableName);
                status = response.Table.TableStatus;

            } while (status != "ACTIVE");
        }
    }
}
