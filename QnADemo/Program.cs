using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;

/*

    You can use the authoring key instead of the endpoint key. 
    The authoring key allows 1000 endpoint queries a month.

*/

namespace QnADemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("QnA Intent Demo using LUIS");
            Console.WriteLine("Demo Source: FBI FAQ");

            string userQuery = "";
            while (userQuery != "ExitDemo")
            {
                Console.Write("Ask a question <\"ExitDemo\" to exit>: ");
                userQuery = Console.ReadLine();

                if (userQuery == "ExitDemo")
                {
                    continue;
                }
                else
                {
                    string intent = Request(userQuery).Result;
                    string answer = GetAnswer(intent);
                    Console.WriteLine("Intent: " + intent);
                    Console.WriteLine("Answer: " + answer);
                    Console.WriteLine();
                }
            }

        }

        static string GetAnswer(string intent)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            string db = "LUISDemo";
            string col = "FBIFAQ";
            var collection = client.GetDatabase(db).GetCollection<BsonDocument>(col);
            var filter = Builders<BsonDocument>.Filter.Eq("intent", intent);

            var result = collection.Find(filter).FirstOrDefault();
            QnADoc qna = BsonSerializer.Deserialize<QnADoc>(result);

            return qna.Answer;
        }

        static async Task<string> Request(string query)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            string luisAppId = "13b26986-bb82-4f52-a305-3665755d70ba";

            // The "q" parameter contains the utterance to send to LUIS
            queryString["q"] = query;
            var endpointUri = "http://localhost:5000/luis/v2.0/apps/" + luisAppId + "?" + queryString;
            var response = await client.GetAsync(endpointUri);

            var strResponseContent = await response.Content.ReadAsStringAsync();

            // Display the JSON result from LUIS
            Intent intent = Intent.FromJson(strResponseContent.ToString());

            return intent.TopScoringIntent.Intent;
        }


    }
    public partial class Intent
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("topScoringIntent")]
        public TopScoringIntent TopScoringIntent { get; set; }

        [JsonProperty("entities")]
        public object[] Entities { get; set; }
    }

    public partial class TopScoringIntent
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }
    }

    public partial class Intent
    {
        public static Intent FromJson(string json) => JsonConvert.DeserializeObject<Intent>(json, QnADemo.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    public class QnADoc
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("intent")]
        public string Intent { get; set; }

        [BsonElement("answer")]
        public string Answer { get; set; }
    }
}