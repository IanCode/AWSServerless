using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Newtonsoft.Json;

using Xunit;

namespace AWSServerless1.Tests
{
    public class FunctionTest : IDisposable
    { 
        string TableName { get; }
        IAmazonDynamoDB DDBClient { get; }
        
        public FunctionTest()
        {
            this.TableName = "AWSServerless1-Contacts-" + DateTime.Now.Ticks;
            this.DDBClient = new AmazonDynamoDBClient(RegionEndpoint.USWest2);

            SetupTableAsync().Wait();
        }

        [Fact]
        public async Task ContactTestAsync()
        {
            TestLambdaContext context;
            APIGatewayProxyRequest request;
            APIGatewayProxyResponse response;

            Functions functions = new Functions(this.DDBClient, this.TableName);

            // Add a new contact
            var myContact = new Contact()
            {
                Id = new Guid().ToString(),
                CreatedTimestamp = DateTime.Now,
                Name = "Lebron James",
                PrimaryEmail = "lbj@gmail.com",
                SecondaryEmails = new List<string>()
                {
                    "greatestever@yahoo.com",
                    "23man@yandex.mail"
                },
                Phones = new List<Phone>()
                {
                    new Phone()
                    {
                        Number = "1234444456",
                        PhoneNumberType = PhoneType.Mobile,
                        CallingCode = "+1"
                    }
                },
                StreetAddress = new Address()
                {
                    City = "Los Angeles",
                    State = "California",
                    Street = "34 Malibu Heights",
                    ZipCode = "90210"
                }
            };

            var myContact2 = new Contact()
            {
                Id = new Guid().ToString(),
                CreatedTimestamp = DateTime.Now,
                Name = "Steph Curry",
                PrimaryEmail = "scbay@gmail.com",
                SecondaryEmails = new List<string>()
                {
                    "greatestshooterever@yahoo.com",
                    "goldenstate@yandex.mail"
                },
                Phones = new List<Phone>()
                {
                    new Phone()
                    {
                        Number = "1234412456",
                        PhoneNumberType = PhoneType.Mobile,
                        CallingCode = "+1"
                    }
                },
                StreetAddress = new Address()
                {
                    City = "San Francisco",
                    State = "California",
                    Street = "34 Embarcadero Way",
                    ZipCode = "90211"
                }
            };

            var body1 = JsonConvert.SerializeObject(myContact);
            var body2 = JsonConvert.SerializeObject(myContact2);
            //request = new APIGatewayProxyRequest
            //{
            //    Body = JsonConvert.SerializeObject(myContact2)
            //};
            //context = new TestLambdaContext();
            //response = await functions.AddContactAsync(request, context);
            //Assert.Equal(200, response.StatusCode);

            request = new APIGatewayProxyRequest
            {
                Body = JsonConvert.SerializeObject(myContact)
            };
            context = new TestLambdaContext();
            response = await functions.AddContactAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            var contactId = response.Body;

            // Confirm we can get the contact back out
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, contactId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetContactAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            Contact readContact = JsonConvert.DeserializeObject<Contact>(response.Body);
            Assert.True(myContact.Equals(readContact));

            // List the contacts
            request = new APIGatewayProxyRequest
            {
            };
            context = new TestLambdaContext();
            response = await functions.GetContactsAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            Contact[] contacts = JsonConvert.DeserializeObject<Contact[]>(response.Body);
			Assert.Single(contacts);
            Assert.True(myContact.Equals(contacts[0]));


            // Delete the contact
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, contactId } }
            };
            context = new TestLambdaContext();
            response = await functions.RemoveContactAsync(request, context);
            Assert.Equal(200, response.StatusCode);

            // Make sure the contact was deleted.
            request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string> { { Functions.ID_QUERY_STRING_NAME, contactId } }
            };
            context = new TestLambdaContext();
            response = await functions.GetContactAsync(request, context);
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }



        /// <summary>
        /// Create the DynamoDB table for testing. This table is deleted as part of the object dispose method.
        /// </summary>
        /// <returns></returns>
        private async Task SetupTableAsync()
        {
            
            CreateTableRequest request = new CreateTableRequest
            {
                TableName = this.TableName,
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 2,
                    WriteCapacityUnits = 2
                },
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        KeyType = KeyType.HASH,
                        AttributeName = Functions.ID_QUERY_STRING_NAME
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = Functions.ID_QUERY_STRING_NAME,
                        AttributeType = ScalarAttributeType.S
                    }
                }
            };

            await this.DDBClient.CreateTableAsync(request);

            var describeRequest = new DescribeTableRequest { TableName = this.TableName };
            DescribeTableResponse response = null;
            do
            {
                Thread.Sleep(1000);
                response = await this.DDBClient.DescribeTableAsync(describeRequest);
            } while (response.Table.TableStatus != TableStatus.ACTIVE);
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.DDBClient.DeleteTableAsync(this.TableName).Wait();
                    this.DDBClient.Dispose();
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }
}
