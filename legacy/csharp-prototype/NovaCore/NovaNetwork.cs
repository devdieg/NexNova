using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaNetwork
    {
        #region Campos
        private HttpClient httpClient;
        private Dictionary<string, HttpClient> privateClients = new Dictionary<string, HttpClient>();
        private CookieContainer cookieContainer = new CookieContainer();
        #endregion

        #region Eventos
        public event EventHandler<ResourceLoadedEventArgs> ResourceLoaded;
        #endregion

        #region Constructor
        public NovaNetwork()
        {
            InitializeHttpClient();
        }
        #endregion

        #region Inicialización
        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseCookies = true,
                UseProxy = false // Por ahora, sin proxy
            };

            // Ignorar errores SSL para desarrollo (¡CUIDADO EN PRODUCCIÓN!)
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // En producción, esto debería validar certificados
                return true; // Aceptar todos los certificados
            };

            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NexNova/60 NovaCore/1.0");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        #endregion

        #region Métodos Públicos
        public async Task<NetworkResponse> DownloadAsync(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return NetworkResponse.CreateError("URL is empty");

                // Validar URL
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                    return NetworkResponse.CreateError($"Invalid URL: {url}");

                // Determinar cliente (privado o público)
                HttpClient client = httpClient;
                if (IsPrivateUrl(url))
                {
                    client = GetPrivateClient(url);
                }

                // Realizar solicitud
                var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return NetworkResponse.CreateError($"HTTP {response.StatusCode}: {response.ReasonPhrase}");

                // Leer contenido
                byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();

                // Determinar encoding
                Encoding encoding = GetEncoding(response.Content.Headers.ContentType?.CharSet);
                string content = encoding.GetString(contentBytes);

                // Obtener tipo MIME
                string contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";

                // Obtener URL final (después de redirecciones)
                string finalUrl = response.RequestMessage.RequestUri.ToString();

                // Disparar evento
                ResourceLoaded?.Invoke(this, new ResourceLoadedEventArgs(url, contentType, contentBytes.Length));

                return NetworkResponse.SuccessResponse(
    content,
    contentType,
    finalUrl,
    response.Headers
);


            }
            catch (HttpRequestException ex)
            {
                return NetworkResponse.CreateError($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return NetworkResponse.CreateError("Request timeout");
            }
            catch (Exception ex)
            {
                return NetworkResponse.CreateError($"Download error: {ex.Message}");
            }
        }

        public async Task<byte[]> DownloadBytesAsync(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return null;

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                    return null;

                HttpClient client = httpClient;
                if (IsPrivateUrl(url))
                {
                    client = GetPrivateClient(url);
                }

                return await client.GetByteArrayAsync(uri);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> DownloadStringAsync(string url)
        {
            var response = await DownloadAsync(url);
            return response.Success ? response.Content : null;
        }

        public void ClearCookies()
        {
            cookieContainer = new CookieContainer();
            InitializeHttpClient();
        }

        public void SetCookie(string url, string name, string value)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    cookieContainer.Add(uri, new Cookie(name, value));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetCookie error: {ex.Message}");
            }
        }

        public List<Cookie> GetCookies(string url)
        {
            var cookies = new List<Cookie>();

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    var cookieCollection = cookieContainer.GetCookies(uri);
                    foreach (Cookie cookie in cookieCollection)
                    {
                        cookies.Add(cookie);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCookies error: {ex.Message}");
            }

            return cookies;
        }
        #endregion

        #region Métodos Privados
        private bool IsPrivateUrl(string url)
        {
            // URLs que requieren sesiones privadas
            return url.Contains("localhost") ||
                   url.Contains("127.0.0.1") ||
                   url.Contains("private") ||
                   url.Contains("inprivate");
        }

        private HttpClient GetPrivateClient(string url)
        {
            string domain = GetDomain(url);

            if (!privateClients.ContainsKey(domain))
            {
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5,
                    UseCookies = true,
                    UseProxy = false
                };

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NexNova/60 NovaCore/1.0 (Private)");
                client.Timeout = TimeSpan.FromSeconds(15);

                privateClients[domain] = client;
            }

            return privateClients[domain];
        }

        private string GetDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }

        private Encoding GetEncoding(string charset)
        {
            if (string.IsNullOrEmpty(charset))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
        #endregion

        #region Métodos de Utilidad
        public static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static string CombineUrls(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return baseUrl;

            if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out Uri absoluteUri))
                return absoluteUri.ToString();

            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri) &&
                Uri.TryCreate(baseUri, relativeUrl, out Uri combinedUri))
                return combinedUri.ToString();

            return relativeUrl;
        }
        #endregion
    }

    public class NetworkResponse
    {
        public bool Success { get; }
        public string Content { get; }
        public string ContentType { get; }
        public string Url { get; }
        public long Size { get; }
        public HttpResponseHeaders Headers { get; }
        public string Error { get; }

        private NetworkResponse(bool success, string content, string contentType, string url,
                              long size, HttpResponseHeaders headers, string error)
        {
            Success = success;
            Content = content;
            ContentType = contentType;
            Url = url;
            Size = size;
            Headers = headers;
            Error = error;
        }

        public static NetworkResponse SuccessResponse(string content, string contentType,
                                                    string url, HttpResponseHeaders headers)
        {
            return new NetworkResponse(true, content, contentType, url,
                                     content?.Length ?? 0, headers, null);
        }

        public static NetworkResponse CreateError(string error)
        {
            return new NetworkResponse(false, null, null, null, 0, null, error);
        }
    }

    public class ResourceLoadedEventArgs : EventArgs
    {
        public string Url { get; }
        public string ContentType { get; }
        public long Size { get; }

        public ResourceLoadedEventArgs(string url, string contentType, long size)
        {
            Url = url;
            ContentType = contentType;
            Size = size;
        }
    }

    // Clase simple para encabezados de respuesta
    public class HttpResponseHeaders
    {
        private Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();

        public void Add(string name, string value)
        {
            if (!headers.ContainsKey(name))
                headers[name] = new List<string>();

            headers[name].Add(value);
        }

        public IEnumerable<string> GetValues(string name)
        {
            return headers.ContainsKey(name) ? headers[name] : new List<string>();
        }

        public bool Contains(string name)
        {
            return headers.ContainsKey(name);
        }
    }
}