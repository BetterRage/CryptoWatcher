using System;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace CryptoService
{
    static class Program
    {
        static void Main()
        {
            string[] keys = ReadAPIKeysFromFile();
            int keyIndex = ReadCurrentKeyIndexFromFile();
            string[] cryptos = ReadCryptosFromFile();
            keyIndex %= keys.Length;
            string result = MakeAPICall(keys[keyIndex]).Result;
            IEnumerable<(string s, float p)> cryptoData = GetCryptosFromAPIResult(result, cryptos);
            int length = cryptoData.Max(x => x.s.Length);
            foreach (var (s, p) in cryptoData)
            {
                Console.Write(s.PadRight(length + 3, ' '));
                Console.Write(p);
                Console.WriteLine("€");
            }
            Console.ReadKey();

        }

        private static string[] ReadCryptosFromFile()
        {
            using FileStream configFile = File.OpenRead("config.txt");
            using StreamReader cryptoReader = new(configFile);
            string[] cryptos = cryptoReader.ReadLine().Split(" ");
            cryptoReader.Close();
            configFile.Close();
            return cryptos;
        }

        public static async Task<string> MakeAPICall(string key)
        {
            var URL = new UriBuilder("https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest");
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["start"] = "1";
            queryString["limit"] = "100";
            queryString["convert"] = "EUR";
            URL.Query = queryString.ToString();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", key);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(URL.ToString());
            string message = await response.Content.ReadAsStringAsync();
            return message;
        }

        static string[] ReadAPIKeysFromFile()
        {
            using FileStream keysFile = File.OpenRead("keys.txt");
            using StreamReader keysReader = new(keysFile);
            string keysIn = keysReader.ReadToEnd();
            keysFile.Close();
            keysReader.Close();
            return keysIn.Split(Environment.NewLine).Skip(1).ToArray();
        }

        static int ReadCurrentKeyIndexFromFile()
        {
            FileStream configFile = File.OpenRead("keys.txt");
            using StreamReader keyIndexReader = new(configFile);
            int keyIndex = Convert.ToInt32(keyIndexReader.ReadLine());
            keyIndexReader.Close();
            configFile.Close();
            configFile = File.OpenWrite("keys.txt");
            using StreamWriter keyIndexWriter = new(configFile);
            keyIndexWriter.Write(++keyIndex);
            keyIndexWriter.Close();
            configFile.Close();
            configFile.Dispose();
            return keyIndex;
        }

        static IEnumerable<(string, float)> GetCryptosFromAPIResult(string apiResult, string[] cryptos)
        {
            JObject data = JObject.Parse(apiResult);
            JArray cryptosIn = (JArray)data["data"];
            foreach (JToken jToken in cryptosIn)
                foreach (string crypto in cryptos)
                    if (crypto == (string)jToken["symbol"])
                        yield return
                            ((string)jToken["name"],
                            (float)jToken["quote"]["EUR"]["price"]);
        }
    }
}
