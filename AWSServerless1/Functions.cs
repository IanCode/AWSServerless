using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSServerless1
{
    public class Functions
    {
        // This const is the name of the environment variable that the serverless.template will use to set
        // the name of the DynamoDB table used to store contacts.
        const string TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP = "ContactTable";

        public const string ID_QUERY_STRING_NAME = "Id";
        IDynamoDBContext DDBContext { get; set; }

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            // Check to see if a table name was passed in through environment variables and if so 
            // add the table mapping.
            var tableName = System.Environment.GetEnvironmentVariable(TABLENAME_ENVIRONMENT_VARIABLE_LOOKUP);
            if(!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Contact)] = new Amazon.Util.TypeMapping(typeof(Contact), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Address)] = new Amazon.Util.TypeMapping(typeof(Address), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(PhoneType)] = new Amazon.Util.TypeMapping(typeof(PhoneType), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Phone)] = new Amazon.Util.TypeMapping(typeof(Phone), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(new AmazonDynamoDBClient(), config);
        }

        /// <summary>
        /// Constructor used for testing passing in a preconfigured DynamoDB client.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        public Functions(IAmazonDynamoDB ddbClient, string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Contact)] = new Amazon.Util.TypeMapping(typeof(Contact), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Address)] = new Amazon.Util.TypeMapping(typeof(Address), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(PhoneType)] = new Amazon.Util.TypeMapping(typeof(PhoneType), tableName);
                AWSConfigsDynamoDB.Context.TypeMappings[typeof(Phone)] = new Amazon.Util.TypeMapping(typeof(Phone), tableName);
            }

            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            this.DDBContext = new DynamoDBContext(ddbClient, config);
        }

        /// <summary>
        /// A Lambda function that returns back a page worth of contacts.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of contacts</returns>
        public async Task<APIGatewayProxyResponse> GetContactsAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Getting contacts");
            var search = this.DDBContext.ScanAsync<Contact>(null);
            var page = await search.GetNextSetAsync();
            context.Logger.LogLine($"Found {page.Count} contacts");

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(page),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            return response;
        }

        /// <summary>
        /// A Lambda function that returns the contact identified by contactId
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> GetContactAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string contactId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                contactId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                contactId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(contactId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Getting contact {contactId}");
            var contact = await DDBContext.LoadAsync<Contact>(contactId);
            context.Logger.LogLine($"Found contact: {contact != null}");

            if (contact == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(contact),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that adds a contact.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> AddContactAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var contact = JsonConvert.DeserializeObject<Contact>(request?.Body);
            contact.Id = Guid.NewGuid().ToString();
            contact.CreatedTimestamp = DateTime.Now;

            context.Logger.LogLine($"Saving contact with id {contact.Id}");
            await DDBContext.SaveAsync<Contact>(contact);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = contact.Id.ToString(),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that removes a contact from the DynamoDB table.
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> RemoveContactAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string contactId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                contactId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                contactId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(contactId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Deleting contact with id {contactId}");
            await this.DDBContext.DeleteAsync<Contact>(contactId);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
    }
}
