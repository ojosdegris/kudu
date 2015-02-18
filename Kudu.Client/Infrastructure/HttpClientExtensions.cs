using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kudu.Client.Infrastructure
{
    public static class HttpClientExtensions
    {
        public static T GetJson<T>(this HttpClient client, string url)
        {
            var response = client.GetAsync(url);
            var content = response.Result.EnsureSuccessful().Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<T>(content);
        }

        public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return await CreateOutputFromResponse<T>(response);
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient client)
        {
            using (var stringContent = new StringContent(String.Empty))
            {
                HttpResponseMessage result = await client.PostAsync(String.Empty, stringContent);
                return result.EnsureSuccessful();
            }
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, string requestUri)
        {
            using (var stringContent = new StringContent(String.Empty))
            {
                HttpResponseMessage result = await client.PostAsync(requestUri, stringContent);
                return result.EnsureSuccessful();
            }
        }

        public static async Task<HttpResponseMessage> PutAsync(this HttpClient client, string requestUri, bool ensureSuccessful = true)
        {
            using (var stringContent = new StringContent(String.Empty))
            {
                HttpResponseMessage result = await client.PutAsync(requestUri, stringContent);
                return ensureSuccessful ? result.EnsureSuccessful() : result;
            }
        }

        public static async Task<HttpResponseMessage> PutAsync(this HttpClient client, string requestUri, params KeyValuePair<string, string>[] items)
        {
            using (var jsonContent = HttpClientHelper.CreateJsonContent(items))
            {
                HttpResponseMessage result = await client.PutAsync(requestUri, jsonContent);
                return result.EnsureSuccessful();
            }
        }

        public static async Task<HttpResponseMessage> DeleteSafeAsync(this HttpClient client, string requestUri)
        {
            HttpResponseMessage result = await client.DeleteAsync(requestUri);
            return result.EnsureSuccessful();
        }

        public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, string url, params KeyValuePair<string, string>[] items)
        {
            using (var jsonContent = HttpClientHelper.CreateJsonContent(items))
            {
                HttpResponseMessage result = await client.PostAsync(url, jsonContent);
                return result.EnsureSuccessful();
            }
        }

        public static async Task<TOutput> PostJsonAsync<TInput, TOutput>(this HttpClient client, string url, TInput param)
        {
            HttpResponseMessage result = await client.PostAsJsonAsync(url, param);

            string content = await result.EnsureSuccessful().Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TOutput>(content);
        }

        public static async Task<TOutput> PutJsonAsync<TInput, TOutput>(this HttpClient client, string url, TInput param)
        {
            HttpResponseMessage response = await client.PutAsJsonAsync(url, param);
            return await CreateOutputFromResponse<TOutput>(response);
        }

        public static async Task<TOutput> DeleteJsonAsync<TOutput>(this HttpClient client, string url)
        {
            HttpResponseMessage response = await client.DeleteAsync(url);
            return await CreateOutputFromResponse<TOutput>(response);
        }

        private static async Task<TOutput> CreateOutputFromResponse<TOutput>(HttpResponseMessage response)
        {
            string content = await response.EnsureSuccessful().Content.ReadAsStringAsync();
            var outputType = typeof(TOutput);
            if (HttpResponseResultUtils.IsTypeOfHttpResponseRresult(outputType))
            {
                Type bodyType = outputType.GenericTypeArguments[0]; // HttpResponseResult<T> takes one generic type
                var bodyObject = JsonConvert.DeserializeObject(value: content, type: bodyType);
                var headerDict = new Dictionary<string, IEnumerable<string>>();

                foreach (var item in response.Headers)
                {
                    headerDict.Add(item.Key, item.Value);
                }

                return (TOutput)HttpResponseResultUtils.CreateHttpResponseResultInstance(outputType, headerDict, bodyObject);
            }

            return JsonConvert.DeserializeObject<TOutput>(content);
        }
    }
}
