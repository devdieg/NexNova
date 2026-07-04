using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaSecurity
    {
        #region Campos
        private HashSet<string> blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> allowedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<string> blockedKeywords = new List<string>();
        private bool safeSearchEnabled = true;
        private bool blockAds = true;
        private bool blockTrackers = true;
        private bool blockPopups = true;
        private bool blockMalware = true;
        #endregion

        #region Constructor
        public NovaSecurity()
        {
            InitializeDefaultRules();
        }
        #endregion

        #region Inicialización
        private void InitializeDefaultRules()
        {
            // Dominios maliciosos conocidos (lista básica)
            blockedDomains.UnionWith(new[]
            {
                "malware.com",
                "phishing-site.com",
                "fake-bank.com",
                "dangerous-download.com"
            });

            // Palabras clave para SafeSearch
            blockedKeywords.AddRange(new[]
            {
                "porn",
                "xxx",
                "adult",
                "explicit",
                "nude",
                "sex",
                "violence",
                "gore",
                "hate",
                "racism"
            });

            // Dominios de anuncios y trackers
            blockedDomains.UnionWith(new[]
            {
                "doubleclick.net",
                "google-analytics.com",
                "facebook.net",
                "googlesyndication.com",
                "adservice.google.com",
                "ads.youtube.com"
            });
        }
        #endregion

        #region Métodos Públicos
        public bool IsUrlAllowed(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                // Parsear URL
                var uri = new Uri(url);

                // Verificar protocolo
                if (!IsProtocolAllowed(uri.Scheme))
                    return false;

                // Verificar dominio bloqueado
                if (IsDomainBlocked(uri.Host))
                    return false;

                // Verificar contenido en la URL
                if (ContainsBlockedContent(url))
                    return false;

                // Verificar puerto
                if (!IsPortAllowed(uri.Port))
                    return false;

                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Security check error: {ex.Message}");
                return false;
            }
        }

        public bool IsContentSafe(string content)
        {
            if (string.IsNullOrEmpty(content))
                return true;

            if (!safeSearchEnabled)
                return true;

            // Verificar contenido explícito
            foreach (var keyword in blockedKeywords)
            {
                if (content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }

            return true;
        }

        public string FilterContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Filtrar scripts maliciosos
            content = FilterScripts(content);

            // Filtrar iframes
            content = FilterIframes(content);

            // Filtrar popups
            if (blockPopups)
                content = FilterPopups(content);

            // Filtrar trackers
            if (blockTrackers)
                content = FilterTrackers(content);

            return content;
        }

        public void BlockDomain(string domain)
        {
            if (!string.IsNullOrEmpty(domain))
            {
                blockedDomains.Add(domain.Trim().ToLower());
            }
        }

        public void AllowDomain(string domain)
        {
            if (!string.IsNullOrEmpty(domain))
            {
                allowedDomains.Add(domain.Trim().ToLower());
                blockedDomains.Remove(domain.Trim().ToLower());
            }
        }

        public void AddBlockedKeyword(string keyword)
        {
            if (!string.IsNullOrEmpty(keyword) && !blockedKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            {
                blockedKeywords.Add(keyword);
            }
        }

        public void RemoveBlockedKeyword(string keyword)
        {
            blockedKeywords.RemoveAll(k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> GetBlockedDomains()
        {
            return blockedDomains.ToList();
        }

        public List<string> GetAllowedDomains()
        {
            return allowedDomains.ToList();
        }

        public List<string> GetBlockedKeywords()
        {
            return new List<string>(blockedKeywords);
        }
        #endregion

        #region Métodos de Filtrado
        private string FilterScripts(string content)
        {
            // Remover scripts maliciosos conocidos
            var patterns = new[]
            {
                @"<script[^>]*src\s*=\s*['""]https?://[^'""]*malware[^'""]*['""][^>]*>.*?</script>",
                @"<script[^>]*>.*?eval\s*\(.*?</script>",
                @"<script[^>]*>.*?document\.write\s*\(.*?</script>",
                @"<script[^>]*>.*?window\.location\s*=.*?</script>"
            };

            foreach (var pattern in patterns)
            {
                content = Regex.Replace(content, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return content;
        }

        private string FilterIframes(string content)
        {
            // Remover iframes de dominios no confiables
            string pattern = @"<iframe[^>]*src\s*=\s*['""]https?://[^'""]*['""][^>]*>.*?</iframe>";

            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                // Extraer src
                var srcMatch = Regex.Match(match.Value, @"src\s*=\s*['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
                if (srcMatch.Success)
                {
                    string src = srcMatch.Groups[1].Value;
                    if (!IsDomainAllowed(src))
                    {
                        content = content.Replace(match.Value, "");
                    }
                }
            }

            return content;
        }

        private string FilterPopups(string content)
        {
            // Remover JavaScript de popups
            var popupPatterns = new[]
            {
                @"window\.open\s*\(",
                @"alert\s*\(",
                @"confirm\s*\(",
                @"prompt\s*\(",
                @"onload\s*=\s*['""].*?window\.open.*?['""]"
            };

            foreach (var pattern in popupPatterns)
            {
                content = Regex.Replace(content, pattern, "// Blocked by NovaSecurity: $&",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return content;
        }

        private string FilterTrackers(string content)
        {
            // Remover scripts de trackers conocidos
            var trackerPatterns = new[]
            {
                @"<script[^>]*src\s*=\s*['""].*?google-analytics\.com.*?['""][^>]*>",
                @"<script[^>]*src\s*=\s*['""].*?facebook\.net.*?['""][^>]*>",
                @"<img[^>]*src\s*=\s*['""].*?pixel\.*?['""][^>]*>",
                @"<iframe[^>]*src\s*=\s*['""].*?tracking\.*?['""][^>]*>"
            };

            foreach (var pattern in trackerPatterns)
            {
                content = Regex.Replace(content, pattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return content;
        }
        #endregion

        #region Métodos de Verificación
        private bool IsProtocolAllowed(string protocol)
        {
            // Permitir solo HTTP, HTTPS y archivos locales
            return protocol.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                   protocol.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                   protocol.Equals("file", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDomainBlocked(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;

            host = host.ToLower();

            // Verificar si está en la lista blanca
            if (allowedDomains.Contains(host))
                return false;

            // Verificar si está en la lista negra
            foreach (var blockedDomain in blockedDomains)
            {
                if (host.Contains(blockedDomain) || host.EndsWith("." + blockedDomain))
                    return true;
            }

            // Verificar si es una IP local (permitir)
            if (host == "localhost" || host == "127.0.0.1" || host == "::1")
                return false;

            return false;
        }

        private bool IsDomainAllowed(string url)
        {
            try
            {
                var uri = new Uri(url);
                return !IsDomainBlocked(uri.Host);
            }
            catch
            {
                return false;
            }
        }

        private bool ContainsBlockedContent(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            url = url.ToLower();

            // Verificar palabras clave bloqueadas
            foreach (var keyword in blockedKeywords)
            {
                if (url.Contains(keyword.ToLower()))
                    return true;
            }

            // Verificar patrones maliciosos
            var maliciousPatterns = new[]
            {
                @"\.exe$",
                @"\.dll$",
                @"\.bat$",
                @"\.cmd$",
                @"\.vbs$",
                @"\.js$",
                @"data:text/html",
                @"javascript:"
            };

            foreach (var pattern in maliciousPatterns)
            {
                if (Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsPortAllowed(int port)
        {
            // Puertos comunes permitidos
            int[] allowedPorts = { 80, 443, 8080, 8443 };

            // Permitir cualquier puerto para localhost
            if (port == -1) // Puerto por defecto
                return true;

            return allowedPorts.Contains(port);
        }
        #endregion

        #region Configuración
        public void EnableSafeSearch(bool enabled)
        {
            safeSearchEnabled = enabled;
        }

        public void EnableAdBlock(bool enabled)
        {
            blockAds = enabled;
        }

        public void EnableTrackerBlock(bool enabled)
        {
            blockTrackers = enabled;
        }

        public void EnablePopupBlock(bool enabled)
        {
            blockPopups = enabled;
        }

        public void EnableMalwareBlock(bool enabled)
        {
            blockMalware = enabled;
        }

        public SecuritySettings GetSettings()
        {
            return new SecuritySettings
            {
                SafeSearchEnabled = safeSearchEnabled,
                BlockAds = blockAds,
                BlockTrackers = blockTrackers,
                BlockPopups = blockPopups,
                BlockMalware = blockMalware,
                BlockedDomainsCount = blockedDomains.Count,
                AllowedDomainsCount = allowedDomains.Count,
                BlockedKeywordsCount = blockedKeywords.Count
            };
        }
        #endregion
    }

    public class SecuritySettings
    {
        public bool SafeSearchEnabled { get; set; }
        public bool BlockAds { get; set; }
        public bool BlockTrackers { get; set; }
        public bool BlockPopups { get; set; }
        public bool BlockMalware { get; set; }
        public int BlockedDomainsCount { get; set; }
        public int AllowedDomainsCount { get; set; }
        public int BlockedKeywordsCount { get; set; }
    }
}