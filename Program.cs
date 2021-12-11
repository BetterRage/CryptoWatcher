using System;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.Toolkit.Uwp.Notifications;

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
            (string s, float p)[] cryptoData = GetCryptosFromAPIResult(result, cryptos);
            foreach (var crypto in cryptoData)
            {
                Console.WriteLine(string.Format(crypto.s.PadRight(5, ' '), crypto.p));
            }
            Console.ReadKey();

        }

        private static string[] ReadCryptosFromFile()
        {
            using FileStream configFile = File.OpenRead("config.txt");
            using StreamReader cryptoReader = new(configFile);
            string[] cryptos = cryptoReader.ReadToEnd().Split("\n")[1].Split(" ");
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
            return keysIn.Split(Environment.NewLine);
        }

        static int ReadCurrentKeyIndexFromFile()
        {
            FileStream configFile = File.OpenRead("config.txt");
            using StreamReader keyIndexReader = new(configFile);
            int keyIndex = Convert.ToInt32(keyIndexReader.ReadLine());
            keyIndexReader.Close();
            configFile.Close();
            configFile = File.OpenWrite("config.txt");
            using StreamWriter keyIndexWriter = new(configFile);
            keyIndexWriter.Write(++keyIndex);
            keyIndexWriter.Close();
            configFile.Close();
            configFile.Dispose();
            return keyIndex;
        }

        static (string, float)[] GetCryptosFromAPIResult(string apiResult, string[] cryptos)
        {
            JObject data = JObject.Parse(apiResult);
            JArray cryptosIn = (JArray)data["data"];
            List<JToken> filteredCryptos = new();
            foreach (JToken jToken in cryptosIn)
            {
                foreach (string crypto in cryptos)
                {
                    if (crypto == (string)jToken["symbol"])
                        filteredCryptos.Add(jToken);
                }
            }
            (string s, float p)[] formatedCryptos = new (string s, float p)[filteredCryptos.Count];
            for (int i = 0; i < filteredCryptos.Count; i++)
            {
                formatedCryptos[i].s = (string)filteredCryptos[i]["symbol"];
                formatedCryptos[i].p = (float)filteredCryptos[i]["quote"]["EUR"]["price"];
            }
            return formatedCryptos;
        }
    }
}
