using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace cjlogisticsChatBot
{
    public class AzureAuthToken
    {
        /// URL of the token service
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        /// Name of header used to pass the subscription key to the token service
        private const string OcpApimSubscriptionKeyHeader = "353c57887e05492485845aa47759c25b";
        
        /// After obtaining a valid token, this class will cache it for this duration.
        /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

        /// Cache the value of the last valid token obtained from the token service.
        private string _storedTokenValue = string.Empty;

        /// When the last valid token was obtained.
        private DateTime _storedTokenTime = DateTime.MinValue;

        /// Gets the subscription key.
        public string SubscriptionKey { get; }

        /// Gets the HTTP status code for the most recent request to the token service.
        public HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// Creates a client to obtain an access token.
        /// </summary>
        /// <param name="key">Subscription key to use to get an authentication token.</param>
        public AzureAuthToken(string key)
        {
            Debug.WriteLine("***** AzureAuthToken | key : " + key);
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "A subscription key is required");
            }

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public async Task<string> GetAccessTokenAsync()
        {
            Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | SubscriptionKey : " + this.SubscriptionKey + " | OcpApimSubscriptionKeyHeader : " + OcpApimSubscriptionKeyHeader + " | _storedTokenValue : " + _storedTokenValue);
            if (string.IsNullOrWhiteSpace(this.SubscriptionKey))
            {
                return string.Empty;
            }

            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | (1) _storedTokenValue : " + _storedTokenValue);
                return _storedTokenValue;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = ServiceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | StringContent2");
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.SendAsync(request);
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | response.StatusCode : "+ response.StatusCode);
                this.RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | StringContent3");
                var token = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | StringContent4");
                _storedTokenTime = DateTime.Now;
                _storedTokenValue = "Bearer " + token;
                Debug.WriteLine("***** AzureAuthToken | GetAccessTokenAsync | (2) _storedTokenValue : " + _storedTokenValue);
                return _storedTokenValue;
            }
        }

        /// <summary>
        /// Gets a token for the specified subscription. Synchronous version.
        /// Use of async version preferred
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public string GetAccessToken()
        {
            Debug.WriteLine("***** GetAccessToken");
            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
            {
                return _storedTokenValue;
            }

            string accessToken = null;
            var task = Task.Run(async () =>
            {
                accessToken = await this.GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                System.Threading.Thread.Yield();
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            if (task.IsCanceled)
            {
                throw new Exception("Timeout obtaining access token.");
            }
            Debug.WriteLine("***** GetAccessToken | accessToken : "+ accessToken);
            return accessToken;
        }

    }
}