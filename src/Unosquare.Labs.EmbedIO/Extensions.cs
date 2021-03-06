﻿namespace Unosquare.Labs.EmbedIO
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
#if NET46
    using System.Net;
    using System.Net.WebSockets;
#else
    using Net;
#endif

    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    public static partial class Extensions
    {
        #region Constants

        private const string UrlEncodedContentType = "application/x-www-form-urlencoded";

        #endregion

        #region Session Management Methods

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static void DeleteSession(this HttpListenerContext context, WebServer server)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static void DeleteSession(this WebServer server, HttpListenerContext context)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the given session object.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="session">The session info.</param>
        /// <returns></returns>
        public static void DeleteSession(this WebServer server, SessionInfo session)
        {
            server.SessionModule?.DeleteSession(session);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, HttpListenerContext context)
        {
            return server.SessionModule?.GetSession(context);
        }
        
        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebSocketContext context, WebServer server)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, WebSocketContext context)
        {
            return server.SessionModule?.GetSession(context);
        }

        #endregion

        #region HTTP Request Helpers
        
        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestPath(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the request path for the specified context case sensitive.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestPathCaseSensitive(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath;
        }

        /// <summary>
        /// Retrieves the Request HTTP Verb (also called Method) of this context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            HttpVerbs verb;
            Enum.TryParse(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out verb);
            return verb;
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// If the value does not exist it returns null.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string QueryString(this HttpListenerContext context, string key)
        {
            return context.InQueryString(key) ? context.Request.QueryString[key] : null;
        }

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static bool InQueryString(this HttpListenerContext context, string key)
        {
            return context.Request.QueryString.AllKeys.Contains(key);
        }

        /// <summary>
        /// Retrieves the specified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static string RequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.HasRequestHeader(headerName) == false ? string.Empty : context.Request.Headers[headerName];
        }

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static bool HasRequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.Request.Headers[headerName] != null;
        }

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestBody(this HttpListenerContext context)
        {
            if (context.Request.HasEntityBody == false)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion

        #region HTTP Response Manipulation Methods

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(Constants.HeaderExpires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(Constants.HeaderLastModified,
                DateTime.UtcNow.ToString(Constants.BrowserTimeFormat, Constants.StandardCultureInfo));
            context.Response.AddHeader(Constants.HeaderCacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(Constants.HeaderPragma, "no-cache");
        }

        /// <summary>
        /// Sets a response statis code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        public static void Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetLeftPart(UriPartial.Authority);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);
        }

        #endregion

        #region JSON and Exception Extensions

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex)
        {
            return ex.ExceptionMessage(string.Empty);
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="priorMessage">The prior message.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex, string priorMessage)
        {
            var fullMessage = string.IsNullOrWhiteSpace(priorMessage) ? ex.Message : priorMessage + "\r\n" + ex.Message;
            if (ex.InnerException != null && string.IsNullOrWhiteSpace(ex.InnerException.Message) == false)
                return ExceptionMessage(ex.InnerException, fullMessage);

            return fullMessage;
        }


        /// <summary>
        /// Outputs a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, object data)
        {
            var jsonFormatting = Formatting.None;
#if DEBUG
            jsonFormatting = Formatting.Indented;
#endif
            var json = JsonConvert.SerializeObject(data, jsonFormatting);
            return context.JsonResponse(json);
        }

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, string json)
        {
            var buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            return true;
        }

        /// <summary>
        /// Parses the json as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            var requestBody = context.RequestBody();
            return requestBody == null ? null : JsonConvert.DeserializeObject<T>(requestBody);
        }

        /// <summary>
        /// Parses the json as a given type from the request body string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this string requestBody)
            where T : class
        {
            return requestBody == null ? null : JsonConvert.DeserializeObject<T>(requestBody);
        }


        /// <summary>
        /// Prettifies the given JSON string by adding indenting.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static string PrettifyJson(this string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        #endregion

        #region Data Parsing Methods

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [Obsolete("Use RequestFormDataDictionary methods instead")]
        public static Dictionary<string, string> RequestFormData(this HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HasEntityBody == false) return null;

            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    var stringData = reader.ReadToEnd();
                    return RequestFormData(stringData);
                }
            }
        }

        /// <summary>
        /// Returns a dictionary of KVPs from Request data
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        [Obsolete("Use RequestFormDataDictionary methods instead")]
        public static Dictionary<string, string> RequestFormData(this string requestBody)
        {
            var dictionary = ParseFormDataAsDictionary(requestBody);
            var result = new Dictionary<string, string>();
            foreach (var kvp in dictionary)
            {
                var listValue = kvp.Value as List<string>;
                if (listValue == null)
                {
                    result[kvp.Key] = kvp.Value as string;
                }
                else
                {
                    result[kvp.Key] = string.Join("\r\n", listValue.ToArray());
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a dictionary of KVPs from Request data
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
        {
            return ParseFormDataAsDictionary(requestBody);
        }

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this HttpListenerContext context)
        {
            var request = context.Request;
            if (request.HasEntityBody == false) return null;

            using (var body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                {
                    var stringData = reader.ReadToEnd();
                    return RequestFormDataDictionary(stringData);
                }
            }
        }

        /// <summary>
        /// Parses the form data given the request body string.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <param name="contentTypeHeader">The content type header.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">multipart/form-data Content Type parsing is not yet implemented</exception>
        private static Dictionary<string, object> ParseFormDataAsDictionary(string requestBody, string contentTypeHeader = UrlEncodedContentType)
        {
            // TODO: implement multipart/form-data parsing
            // example available here: http://stackoverflow.com/questions/5483851/manually-parse-raw-http-data-with-php

            if (contentTypeHeader.ToLowerInvariant().StartsWith("multipart/form-data"))
                throw new NotImplementedException("multipart/form-data Content Type parsing is not yet implemented");

            // verify there is data to parse
            if (string.IsNullOrWhiteSpace(requestBody)) return null;

            // define a character for KV pairs
            var kvpSeparator = new [] { '=' };

            // Create the result object
            var resultDictionary = new Dictionary<string, object>();

            // Split the request body into key-value pair strings
            var keyValuePairStrings = requestBody.Split('&');

            foreach (var kvps in keyValuePairStrings)
            {
                // Skip KVP strings if they are empty
                if (string.IsNullOrWhiteSpace(kvps))
                    continue;

                // Split by the equals char into key values.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                var kvpsParts = kvps.Split(kvpSeparator, 2);

                // We don't want empty KVPs
                if (kvpsParts.Length == 0)
                    continue;

                // Decode the key and the value. Discard Special Characters
                var key = System.Net.WebUtility.UrlDecode(kvpsParts[0]);
                if (key.IndexOf("[", StringComparison.OrdinalIgnoreCase) > 0) key = key.Substring(0, key.IndexOf("[", StringComparison.OrdinalIgnoreCase));

                var value = kvpsParts.Length >= 2 ? System.Net.WebUtility.UrlDecode(kvpsParts[1]) : null;

                // If the result already contains the key, then turn the value of that key into a List of strings
                if (resultDictionary.ContainsKey(key))
                {
                    // Check if this key has a List value already
                    var listValue = resultDictionary[key] as List<string>;
                    if (listValue == null)
                    {
                        // if we don't have a list value for this key, then create one and add the existing item
                        var existingValue = resultDictionary[key] as string;
                        resultDictionary[key] = new List<string>();
                        listValue = (List<string>) resultDictionary[key];
                        listValue.Add(existingValue);
                    }

                    // By this time, we are sure listValue exists. Simply add the item
                    listValue.Add(value);
                }
                else
                {
                    // Simply set the key to the parsed value
                    resultDictionary[key] = value;
                }

            }

            return resultDictionary;
        }

        #endregion

        #region Hashing and Compression Methods

        /// <summary>
        /// Compresses the specified buffer stream using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static MemoryStream Compress(this Stream buffer)
        {
            buffer.Position = 0;
            var targetStream = new MemoryStream();

            using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
            {
                buffer.CopyTo(compressor);
            }

            return targetStream;
        }

        /// <summary>
        /// Computes the MD5 hash of the given stream.
        /// Do not use for large streams as this reads ALL bytes at once
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static string ComputeMd5Hash(Stream stream)
        {
            var md5 = MD5.Create();
#if !NETCOREAPP1_1 && !NETSTANDARD1_6
            const int bufferSize = 4096;

            var readAheadBuffer = new byte[bufferSize];
            var readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

            do
            {
                var bytesRead = readAheadBytesRead;
                var buffer = readAheadBuffer;

                readAheadBuffer = new byte[bufferSize];
                readAheadBytesRead = stream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                if (readAheadBytesRead == 0)
                    md5.TransformFinalBlock(buffer, 0, bytesRead);
                else
                    md5.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            } while (readAheadBytesRead != 0);

            return GetHashString(md5.Hash);
#else
            using (var ms = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);

                return GetHashString(md5.ComputeHash(ms.ToArray()));
            }
#endif
        }

        /// <summary>
        /// Gets a hexadecimal representation of the hash bytes
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        private static string GetHashString(byte[] hash)
        {
            var sb = new StringBuilder();

            foreach (var t in hash)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Computes the MD5 hash of the given byte array
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <returns></returns>
        public static string ComputeMd5Hash(byte[] inputBytes)
        {
            var hash = MD5.Create().ComputeHash(inputBytes);
            return GetHashString(hash);
        }

        /// <summary>
        /// Computes the MD5 hash of the given input string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ComputeMd5Hash(string input)
        {
            return ComputeMd5Hash(Constants.DefaultEncoding.GetBytes(input));
        }

        #endregion
    }
}
