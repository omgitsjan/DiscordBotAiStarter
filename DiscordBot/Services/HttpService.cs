using System.Net;
using DiscordBot.Interfaces;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Provides HTTP utilities for making REST API requests and returning simplified results.
    /// </summary>
    public class HttpService(IRestClient httpClient) : IHttpService
    {
        /// <summary>
        /// Sends an HTTP request to the specified resource and returns the response.
        /// Supports custom headers, HTTP methods, and optional JSON bodies.
        /// </summary>
        /// <param name="resource">The target URL or endpoint.</param>
        /// <param name="method">HTTP method (default: GET).</param>
        /// <param name="errorMessage">Optional custom error message if the request fails.</param>
        /// <param name="headers">Optional list of HTTP headers.</param>
        /// <param name="jsonBody">Optional JSON body for POST/PUT requests.</param>
        /// <returns>HttpResponse containing status and content or error.</returns>
        public async Task<HttpResponse> GetResponseFromUrl(
            string resource,
            Method method = Method.Get,
            string? errorMessage = null,
            List<KeyValuePair<string, string>>? headers = null,
            object? jsonBody = null)
        {
            var request = new RestRequest(resource, method);

            // Add headers if provided
            if (headers != null && headers.Count > 0)
            {
                foreach (var header in headers)
                    request.AddHeader(header.Key, header.Value);
            }

            // Attach JSON body if provided
            if (jsonBody != null)
            {
                request.AddJsonBody(jsonBody);
            }

            RestResponse response;
            try
            {
                // Execute the request and await the response
                response = await httpClient.ExecuteAsync(request);
            }
            catch (Exception e)
            {
                // Catch all exceptions and build a failed response
                string failMessage = $"({nameof(GetResponseFromUrl)}): Unknown error: {e.Message}";
                Program.Log(failMessage, LogLevel.Error);
                return new HttpResponse(false, failMessage);
            }

            // If the HTTP response was successful, return its content
            if (response.IsSuccessStatusCode)
            {
                return new HttpResponse(true, response.Content);
            }

            // If failed, build a detailed error message for logging and the caller
            string content = $"StatusCode: {response.StatusCode} | {errorMessage ?? response.ErrorMessage}";
            Program.Log(content, LogLevel.Error);
            return new HttpResponse(false, content);
        }
    }

    /// <summary>
    /// A simple wrapper for HTTP response results (success + content).
    /// </summary>
    public class HttpResponse(bool isSuccessStatusCode, string? content)
    {
        /// <summary>
        /// Indicates whether the HTTP request was successful (2xx).
        /// </summary>
        public bool IsSuccessStatusCode { get; set; } = isSuccessStatusCode;

        /// <summary>
        /// The response content or error message.
        /// </summary>
        public string? Content { get; set; } = content;
    }
}
