using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWSServerless1
{
    public static class LoadSampleData
    {
        public static void Main(string[] args)
        {
            Task.Run(async () => await LoadingData_async(args[0]));
        }

        public static async Task<bool> LoadingData_async(string filePath, Table table = null)
        {
            if (table == null)
            {
                // default table value handling
            }

            var movieArray = await ReadJsonContactFile_async(filePath);

            if (movieArray != null)
                await LoadJsonContactData_async(table, movieArray);

            return true;
        }

        public static async Task<JArray> ReadJsonContactFile_async(string jsonContactFilePath)
        {
            StreamReader sr = null;
            JsonTextReader jtr = null;
            JArray contactArray = null;

            Console.WriteLine("  -- Reading the contacts data from a JSON file...");

            try
            {
                sr = new StreamReader(jsonContactFilePath);
                jtr = new JsonTextReader(sr);
                contactArray = (JArray)await JToken.ReadFromAsync(jtr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("     ERROR: could not read the file!\n          Reason: {0}.", ex.Message);
            }
            finally
            {
                jtr?.Close();
                sr?.Close();
            }

            return contactArray;
        }

        public static async Task<bool> LoadJsonContactData_async(Table contactsTable, JArray contactsArray)
        {
            int n = contactsArray.Count;
            Console.Write("     -- Starting to load {0:#,##0} contact records into the ContactTable asynchronously...\n" + "" +
              "        Wrote: ", n);
            for (int i = 0, j = 99; i < n; i++)
            {
                try
                {
                    string itemJson = contactsArray[i].ToString();
                    Document doc = Document.FromJson(itemJson);
                    Task putItem = contactsTable.PutItemAsync(doc);
                    if (i >= j)
                    {
                        j++;
                        Console.Write("{0,5:#,##0}, ", j);
                        if (j % 1000 == 0)
                            Console.Write("\n               ");
                        j += 99;
                    }
                    await putItem;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
    }
}