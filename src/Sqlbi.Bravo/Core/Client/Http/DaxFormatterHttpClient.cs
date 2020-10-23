using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Core.Client.Http.Interfaces;
using Sqlbi.Bravo.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Client.Http
{
    internal class DaxFormatterHttpClient : IDaxFormatterHttpClient, IDisposable
    {
        private readonly HashSet<HttpStatusCode> _locationChangedHttpStatusCodes = new HashSet<HttpStatusCode>
        {
            HttpStatusCode.Moved,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Found,
            HttpStatusCode.Redirect,
            HttpStatusCode.RedirectMethod,
            HttpStatusCode.SeeOther,
            HttpStatusCode.RedirectKeepVerb,
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.PermanentRedirect
        };

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) 
        {
            IgnoreNullValues = true 
        };

        private readonly SemaphoreSlim _locationChangedSemaphore = new SemaphoreSlim(1);
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private Uri _uri;
        private bool _disposed;

        public DaxFormatterHttpClient(HttpClient client, ILogger<DaxFormatterHttpClient> logger, IHostApplicationLifetime lifetime)
        {
            _client = client;
            _logger = logger;
            _lifetime = lifetime;

            _logger.Trace();
            _client.Timeout = AppConstants.DaxFormatterTextFormatTimeout;
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            _client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue(nameof(DecompressionMethods.GZip)));
        }

        public async Task<DaxFormatterResponse> FormatAsync(DaxFormatterRequest request)
        {
            _logger.Trace();

            if (_lifetime.ApplicationStopping.IsCancellationRequested)
                return default;

            try
            {
                if (_uri == default)
                    await InitializeUriAsync().ConfigureAwait(false);

                var json = JsonSerializer.Serialize(request, _serializerOptions);

                using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
                using var response = await _client.PostAsync(_uri, content).ConfigureAwait(false);
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(stream);

                var message = reader.ReadToEnd();
                _logger.Debug(LogEvents.DaxFormatterHttpClientFormatResponse, message);

                var result = JsonSerializer.Deserialize<DaxFormatterResponse>(message, _serializerOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(LogEvents.DaxFormatterHttpClientFormatError, ex);
                throw;
            }

            async Task InitializeUriAsync()
            {
                if (_uri == default)
                {
                    await _locationChangedSemaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (_uri == default)
                        {
                            using var response = await _client.GetAsync(AppConstants.DaxFormatterTextFormatUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                            var uri = _locationChangedHttpStatusCodes.Contains(response.StatusCode) ? response.Headers.Location : AppConstants.DaxFormatterTextFormatUri;
                            Interlocked.CompareExchange(ref _uri, uri, default);
                            _logger.Debug(LogEvents.DaxFormatterHttpClientUriChanged, $"{ _uri }");
                        }
                    }
                    finally
                    {
                        _locationChangedSemaphore.Release();
                    }
                }
            }
        }

        public async IAsyncEnumerable<DaxFormatterResponse> FormatAsync(IEnumerable<DaxFormatterRequest> requests)
        {
            _logger.Trace();

            foreach (var request in requests)
                yield return await FormatAsync(request);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _locationChangedSemaphore.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
