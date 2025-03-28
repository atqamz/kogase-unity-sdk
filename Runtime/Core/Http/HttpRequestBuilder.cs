using System;
using System.Collections.Generic;
using System.Text;
using Kogase.Utils;

namespace Kogase.Core
{
    public class HttpRequestBuilder
    {
        internal const string GetMethod = "GET";
        internal const string PostMethod = "POST";
        internal const string PutMethod = "PUT";
        internal const string PatchMethod = "PATCH";
        internal const string DeleteMethod = "DELETE";

        private readonly StringBuilder formBuilder = new StringBuilder(1024);
        private readonly StringBuilder queryBuilder = new StringBuilder(256);
        private readonly StringBuilder urlBuilder = new StringBuilder(256);
        private HttpRequestPrototype result;

        private static HttpRequestBuilder CreatePrototype(string method, string url)
        {
            var builder = new HttpRequestBuilder
            {
                result = new HttpRequestPrototype
                {
                    Method = method
                }
            };

            builder.urlBuilder.Append(url);

            return builder;
        }

        public static HttpRequestBuilder CreateGet(string url)
        {
            return HttpRequestBuilder.CreatePrototype(GetMethod, url);
        }

        public static HttpRequestBuilder CreatePost(string url)
        {
            return HttpRequestBuilder.CreatePrototype(PostMethod, url);
        }

        public static HttpRequestBuilder CreatePut(string url)
        {
            return HttpRequestBuilder.CreatePrototype(PutMethod, url);
        }

        public static HttpRequestBuilder CreatePatch(string url)
        {
            return HttpRequestBuilder.CreatePrototype(PatchMethod, url);
        }

        public static HttpRequestBuilder CreateDelete(string url)
        {
            return HttpRequestBuilder.CreatePrototype(DeleteMethod, url);
        }

        public HttpRequestBuilder WithPathParam(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception($"Path parameter key is null or empty.");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"The path value of key={key} is null or empty.");
            }

            this.urlBuilder.Replace("{" + key + "}", Uri.EscapeDataString(value));

            return this;
        }

        public HttpRequestBuilder WithPathParams(IDictionary<string, string> pathParams)
        {
            foreach (var param in pathParams)
            {
                WithPathParam(param.Key, param.Value);
            }

            return this;
        }

        public HttpRequestBuilder WithQueryParam(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Query parameter key is null or empty.");
            }

            if (string.IsNullOrEmpty(value))
            {
                return this;
            }

            if (this.queryBuilder.Length > 0)
            {
                this.queryBuilder.Append("&");
            }

            this.queryBuilder.Append($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");

            return this;
        }

        public HttpRequestBuilder WithQueryParams(IDictionary<string, string> queryParams)
        {
            foreach (var query in queryParams)
            {
                WithQueryParam(query.Key, query.Value);
            }

            return this;
        }

        public HttpRequestBuilder WithQueryParam(string key, ICollection<string> values)
        {
            foreach (string value in values)
            {
                WithQueryParam(key, value);
            }

            return this;
        }

        public HttpRequestBuilder WithBasicAuth()
        {
            this.result.AuthType = HttpAuth.BASIC;
            return this;
        }

        public HttpRequestBuilder WithBasicAuth(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Username and password for Basic Authorization shouldn't be empty or null");
            }

            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            this.result.Headers["Authorization"] = "Basic " + credentials;
            this.result.AuthType = HttpAuth.BASIC;

            return this;
        }

        public HttpRequestBuilder WithBearerAuth()
        {
            this.result.AuthType = HttpAuth.BEARER;
            return this;
        }

        public HttpRequestBuilder WithBearerAuth(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token for Bearer Authorization shouldn't be empty or null");
            }

            this.result.Headers["Authorization"] = "Bearer " + token;
            this.result.AuthType = HttpAuth.BEARER;

            return this;
        }

        public HttpRequestBuilder WithHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Header key shouldn't be empty or null");
            }

            this.result.Headers[key] = value;
            return this;
        }

        public HttpRequestBuilder WithContentType(HttpMediaType mediaType)
        {
            this.result.Headers["Content-Type"] = mediaType.ToString();
            return this;
        }

        public HttpRequestBuilder Accepts(HttpMediaType mediaType)
        {
            this.result.Headers["Accept"] = mediaType.ToString();
            return this;
        }

        public HttpRequestBuilder WithFormParam(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Form parameter key is null or empty.");
            }

            if (string.IsNullOrEmpty(value))
            {
                return this;
            }

            if (this.formBuilder.Length > 0)
            {
                this.formBuilder.Append("&");
            }

            this.formBuilder.Append($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");

            return this;
        }

        public HttpRequestBuilder WithFormParams(IDictionary<string, string> formParams)
        {
            foreach (var param in formParams)
            {
                WithFormParam(param.Key, param.Value);
            }

            return this;
        }

        public HttpRequestBuilder WithBody(string body)
        {
            if (!this.result.Headers.ContainsKey("Content-Type"))
            {
                this.result.Headers.Add("Content-Type", HttpMediaType.TextPlain.ToString());
            }

            this.result.BodyBytes = Encoding.UTF8.GetBytes(body);
            return this;
        }

        public HttpRequestBuilder WithBody(byte[] body)
        {
            if (!this.result.Headers.ContainsKey("Content-Type"))
            {
                this.result.Headers.Add("Content-Type", HttpMediaType.ApplicationOctetStream.ToString());
            }

            this.result.BodyBytes = body;
            return this;
        }

        public HttpRequestBuilder WithJsonBody<T>(T body)
        {
            if (!this.result.Headers.ContainsKey("Content-Type"))
            {
                this.result.Headers.Add("Content-Type", HttpMediaType.ApplicationJson.ToString());
            }

            this.result.BodyBytes = body.ToUtf8Json();
            return this;
        }

        public IHttpRequest GetResult()
        {
            if (this.queryBuilder.Length > 0)
            {
                this.urlBuilder.Append("?");
                this.urlBuilder.Append(this.queryBuilder);
            }

            if (this.formBuilder.Length > 0)
            {
                this.result.Headers["Content-Type"] = HttpMediaType.ApplicationForm.ToString();
                this.result.BodyBytes = Encoding.UTF8.GetBytes(this.formBuilder.ToString());
            }

            this.result.Url = this.urlBuilder.ToString();
            return this.result;
        }

        private class HttpRequestPrototype : IHttpRequest
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Method { get; set; }
            public string Url { get; set; }
            public HttpAuth AuthType { get; set; } = HttpAuth.NONE;
            public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
            public byte[] BodyBytes { get; set; }
            public int Priority { get; set; } = 0;
        }
    }
} 