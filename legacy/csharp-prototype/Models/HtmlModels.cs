using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NexNova.Models
{
    public class NovaHtmlDocument
    {
        public string Doctype { get; set; } = "html";
        public string Title { get; set; } = "Untitled";
        public NovaHtmlElement Head { get; set; }
        public NovaHtmlElement Body { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public List<string> Scripts { get; set; } = new List<string>();
        public List<string> Styles { get; set; } = new List<string>();
        public Dictionary<string, string> ExternalScripts { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ExternalStyles { get; set; } = new Dictionary<string, string>();
        public string BaseUrl { get; set; }
        public string OriginalHtml { get; set; }

        public List<string> GetCssUrls() => ExternalStyles.Keys.ToList();
        public List<string> GetScriptUrls() => ExternalScripts.Keys.ToList();

        public List<string> GetImageUrls()
        {
            var urls = new List<string>();
            SearchImages(Body, urls);
            return urls;
        }

        private void SearchImages(NovaHtmlElement element, List<string> urls)
        {
            if (element == null) return;

            if (element.TagName == "img" && element.HasAttribute("src"))
            {
                urls.Add(element.GetAttribute("src"));
            }

            if (element.Children != null)
            {
                foreach (var child in element.Children)
                {
                    SearchImages(child, urls);
                }
            }
        }

        public List<string> GetInlineScripts() => Scripts;

        public Dictionary<string, string> GetExternalScriptsContent()
        {
            return ExternalScripts;
        }

        public Dictionary<string, string> GetStylesForElement(NovaHtmlElement element)
        {
            var styles = new Dictionary<string, string>();

            if (element == null) return styles;

            // Estilos inline
            if (element.HasAttribute("style"))
            {
                var inlineStyles = ParseInlineStyles(element.GetAttribute("style"));
                foreach (var style in inlineStyles)
                {
                    styles[style.Key] = style.Value;
                }
            }

            return styles;
        }

        private Dictionary<string, string> ParseInlineStyles(string styleText)
        {
            var styles = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(styleText))
                return styles;

            var declarations = styleText.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var declaration in declarations)
            {
                var parts = declaration.Split(':', 2);
                if (parts.Length == 2)
                {
                    var property = parts[0].Trim().ToLower();
                    var value = parts[1].Trim();
                    styles[property] = value;
                }
            }

            return styles;
        }

        public void AddCss(string url, string content) => ExternalStyles[url] = content;
        public void AddScript(string url, string content) => ExternalScripts[url] = content;
    }

    // Renombrar elemento
    public class NovaHtmlElement
    {
        public string Id { get; set; }
        public string TagName { get; set; } = "div";
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public string InnerText { get; set; }
        public List<NovaHtmlElement> Children { get; set; } = new List<NovaHtmlElement>();
        public NovaHtmlElement Parent { get; set; }
        public Dictionary<string, string> Styles { get; set; } = new Dictionary<string, string>();

        // Propiedades de renderizado
        public Rectangle RenderBounds { get; set; }
        public NovaBoxModel Margin { get; set; } = new NovaBoxModel();
        public NovaBoxModel Padding { get; set; } = new NovaBoxModel();
        public int BorderWidth { get; set; }

        // Métodos de conveniencia
        public bool HasAttribute(string name) => Attributes != null && Attributes.ContainsKey(name);
        public string GetAttribute(string name) => HasAttribute(name) ? Attributes[name] : null;
        public void SetAttribute(string name, string value)
        {
            Attributes ??= new Dictionary<string, string>();
            Attributes[name] = value;
        }

        public void AddChild(NovaHtmlElement child)
        {
            if (child == null) return;
            Children ??= new List<NovaHtmlElement>();
            Children.Add(child);
            child.Parent = this;
        }
    }

    public class NovaBoxModel
    {
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Left { get; set; }

        public NovaBoxModel() { }
        public NovaBoxModel(int top, int right, int bottom, int left)
        {
            Top = top; Right = right; Bottom = bottom; Left = left;
        }
        public NovaBoxModel(int all) : this(all, all, all, all) { }
    }
}