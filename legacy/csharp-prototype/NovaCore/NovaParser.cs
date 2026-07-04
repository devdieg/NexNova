using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaParser
    {
        #region Eventos
        public event EventHandler<DocumentParsedEventArgs> DocumentParsed;
        #endregion

        #region Métodos Públicos
        public HtmlDocument Parse(string html, string baseUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(html))
                    return null;

                var document = new HtmlDocument
                {
                    BaseUrl = baseUrl,
                    OriginalHtml = html
                };

                // Limpiar HTML
                html = CleanHtml(html);

                // Extraer doctype
                document.Doctype = ExtractDoctype(html);

                // Extraer título
                document.Title = ExtractTitle(html);

                // Parsear head
                document.Head = ParseHead(html);

                // Parsear body
                document.Body = ParseBody(html);

                // Extraer metadatos
                document.Metadata = ExtractMetadata(html);

                // Extraer scripts y estilos
                ExtractScriptsAndStyles(html, document);

                // Disparar evento
                DocumentParsed?.Invoke(this, new DocumentParsedEventArgs(document));

                return document;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Métodos de Parseo
        private string CleanHtml(string html)
        {
            // Remover comentarios
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

            // Normalizar espacios
            html = Regex.Replace(html, @"\s+", " ");

            // Remover scripts y estilos temporalmente para parseo más simple
            // (se procesan por separado)

            return html;
        }

        private string ExtractDoctype(string html)
        {
            var match = Regex.Match(html, @"<!DOCTYPE\s+(.*?)\s*>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "html";
        }

        private string ExtractTitle(string html)
        {
            var match = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : "Untitled";
        }

        private HtmlElement ParseHead(string html)
        {
            var headMatch = Regex.Match(html, @"<head>(.*?)</head>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!headMatch.Success)
                return new HtmlElement { TagName = "head" };

            return ParseElement(headMatch.Groups[1].Value, "head");
        }

        private HtmlElement ParseBody(string html)
        {
            var bodyMatch = Regex.Match(html, @"<body>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!bodyMatch.Success)
            {
                // Si no hay body, usar el contenido completo
                bodyMatch = Regex.Match(html, @"</head>(.*?)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!bodyMatch.Success)
                    return new HtmlElement { TagName = "body" };
            }

            return ParseElement(bodyMatch.Groups[1].Value, "body");
        }

        private HtmlElement ParseElement(string content, string defaultTag = "div")
        {
            var element = new HtmlElement { TagName = defaultTag };
            var children = new List<HtmlElement>();

            int pos = 0;
            while (pos < content.Length)
            {
                // Saltar espacios
                while (pos < content.Length && char.IsWhiteSpace(content[pos]))
                    pos++;

                if (pos >= content.Length)
                    break;

                // Buscar tag de apertura
                if (content[pos] == '<')
                {
                    int tagStart = pos;
                    int tagEnd = content.IndexOf('>', tagStart);
                    if (tagEnd == -1)
                        break;

                    string tagContent = content.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();

                    // Verificar si es tag de cierre
                    if (tagContent.StartsWith("/"))
                    {
                        pos = tagEnd + 1;
                        continue;
                    }

                    // Parsear tag
                    var childElement = ParseTag(tagContent, content, tagEnd + 1);
                    if (childElement != null)
                    {
                        children.Add(childElement);
                        pos = GetTagEndPosition(content, tagStart, childElement.TagName);
                    }
                    else
                    {
                        pos = tagEnd + 1;
                    }
                }
                else
                {
                    // Texto
                    int textEnd = content.IndexOf('<', pos);
                    if (textEnd == -1)
                        textEnd = content.Length;

                    string text = content.Substring(pos, textEnd - pos).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        children.Add(new HtmlElement
                        {
                            TagName = "#text",
                            InnerText = text
                        });
                    }

                    pos = textEnd;
                }
            }

            element.Children = children;
            return element;
        }

        private HtmlElement ParseTag(string tagContent, string fullContent, int startPos)
        {
            // Separar nombre del tag de los atributos
            var parts = tagContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            string tagName = parts[0].ToLower();

            // Tags auto-cerrados
            bool isSelfClosing = tagContent.EndsWith("/") ||
                                tagName == "img" || tagName == "br" || tagName == "hr" ||
                                tagName == "meta" || tagName == "link" || tagName == "input";

            var element = new HtmlElement
            {
                TagName = tagName,
                Attributes = ParseAttributes(tagContent)
            };

            if (isSelfClosing)
            {
                return element;
            }

            // Encontrar contenido hasta el tag de cierre
            string endTag = $"</{tagName}>";
            int endPos = fullContent.IndexOf(endTag, startPos, StringComparison.OrdinalIgnoreCase);

            if (endPos == -1)
            {
                // Tag no cerrado, tratar como auto-cerrado
                return element;
            }

            // Extraer contenido interno
            string innerContent = fullContent.Substring(startPos, endPos - startPos);

            // Parsear hijos recursivamente
            element.Children = ParseChildren(innerContent);

            // Extraer texto si no hay hijos
            if ((element.Children == null || element.Children.Count == 0) && !string.IsNullOrWhiteSpace(innerContent))
            {
                element.InnerText = innerContent.Trim();
            }

            return element;
        }

        private List<HtmlElement> ParseChildren(string content)
        {
            var children = new List<HtmlElement>();
            int pos = 0;

            while (pos < content.Length)
            {
                // Saltar espacios
                while (pos < content.Length && char.IsWhiteSpace(content[pos]))
                    pos++;

                if (pos >= content.Length)
                    break;

                // Texto
                if (content[pos] != '<')
                {
                    int textEnd = content.IndexOf('<', pos);
                    if (textEnd == -1)
                        textEnd = content.Length;

                    string text = content.Substring(pos, textEnd - pos).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        children.Add(new HtmlElement
                        {
                            TagName = "#text",
                            InnerText = text
                        });
                    }

                    pos = textEnd;
                    continue;
                }

                // Elemento
                int tagStart = pos;
                int tagEnd = content.IndexOf('>', tagStart);
                if (tagEnd == -1)
                    break;

                string tagContent = content.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();

                // Tag de cierre
                if (tagContent.StartsWith("/"))
                {
                    pos = tagEnd + 1;
                    continue;
                }

                // Parsear elemento
                var element = ParseTag(tagContent, content, tagEnd + 1);
                if (element != null)
                {
                    children.Add(element);
                    pos = GetTagEndPosition(content, tagStart, element.TagName);
                }
                else
                {
                    pos = tagEnd + 1;
                }
            }

            return children;
        }

        private Dictionary<string, string> ParseAttributes(string tagContent)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Usar regex para encontrar atributos
            var matches = Regex.Matches(tagContent, @"(\w+)\s*=\s*""([^""]*)""|(\w+)\s*=\s*'([^']*)'|(\w+)\s*=\s*([^\s>]+)");

            foreach (Match match in matches)
            {
                string key = null;
                string value = null;

                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    key = match.Groups[1].Value.ToLower();
                    value = match.Groups[2].Value;
                }
                else if (!string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    key = match.Groups[3].Value.ToLower();
                    value = match.Groups[4].Value;
                }
                else if (!string.IsNullOrEmpty(match.Groups[5].Value))
                {
                    key = match.Groups[5].Value.ToLower();
                    value = match.Groups[6].Value;
                }

                if (!string.IsNullOrEmpty(key))
                {
                    attributes[key] = value;
                }
            }

            return attributes;
        }

        private int GetTagEndPosition(string content, int tagStart, string tagName)
        {
            string endTag = $"</{tagName}>";
            int endPos = content.IndexOf(endTag, tagStart, StringComparison.OrdinalIgnoreCase);

            if (endPos == -1)
            {
                // Buscar tag de cierre incorrecto
                endPos = content.IndexOf(">", tagStart);
                return endPos + 1;
            }

            return endPos + endTag.Length;
        }

        private Dictionary<string, string> ExtractMetadata(string html)
        {
            var metadata = new Dictionary<string, string>();

            // Extraer meta tags
            var metaMatches = Regex.Matches(html, @"<meta\s+(.*?)\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in metaMatches)
            {
                var attrs = ParseAttributes(match.Groups[1].Value);

                string name = attrs.ContainsKey("name") ? attrs["name"] :
                             attrs.ContainsKey("property") ? attrs["property"] : null;
                string content = attrs.ContainsKey("content") ? attrs["content"] : null;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                {
                    metadata[name] = content;
                }
            }

            return metadata;
        }

        private void ExtractScriptsAndStyles(string html, HtmlDocument document)
        {
            // Extraer scripts
            var scriptMatches = Regex.Matches(html, @"<script(?:\s+[^>]*)?>(.*?)</script>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in scriptMatches)
            {
                string scriptContent = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(scriptContent))
                {
                    document.Scripts.Add(scriptContent);
                }

                // Extraer src
                var attrs = ParseAttributes(match.Value);
                if (attrs.ContainsKey("src"))
                {
                    document.ExternalScripts[attrs["src"]] = null; // Se cargará después
                }
            }

            // Extraer estilos
            var styleMatches = Regex.Matches(html, @"<style(?:\s+[^>]*)?>(.*?)</style>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in styleMatches)
            {
                string styleContent = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(styleContent))
                {
                    document.Styles.Add(styleContent);
                }
            }

            // Extraer links a CSS
            var linkMatches = Regex.Matches(html, @"<link\s+(.*?)\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in linkMatches)
            {
                var attrs = ParseAttributes(match.Groups[1].Value);

                if (attrs.ContainsKey("rel") && attrs["rel"].ToLower() == "stylesheet" && attrs.ContainsKey("href"))
                {
                    document.ExternalStyles[attrs["href"]] = null; // Se cargará después
                }
            }
        }
        #endregion
    }

    public class DocumentParsedEventArgs : EventArgs
    {
        public HtmlDocument Document { get; }

        public DocumentParsedEventArgs(HtmlDocument document)
        {
            Document = document;
        }
    }
}