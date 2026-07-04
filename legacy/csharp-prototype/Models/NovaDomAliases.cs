using System.Collections.Generic;

namespace NexNova.Models
{
    public class HtmlDocument
    {
        public NovaHtmlDocument Inner { get; } = new NovaHtmlDocument();

        public string Doctype
        {
            get => Inner.Doctype;
            set => Inner.Doctype = value;
        }

        public string Title
        {
            get => Inner.Title;
            set => Inner.Title = value;
        }

        public string BaseUrl
        {
            get => Inner.BaseUrl;
            set => Inner.BaseUrl = value;
        }

        public string OriginalHtml
        {
            get => Inner.OriginalHtml;
            set => Inner.OriginalHtml = value;
        }

        public HtmlElement Head
        {
            get => Inner.Head == null ? null : new HtmlElement(Inner.Head);
            set => Inner.Head = value?.Inner;
        }

        public HtmlElement Body
        {
            get => Inner.Body == null ? null : new HtmlElement(Inner.Body);
            set => Inner.Body = value?.Inner;
        }

        public Dictionary<string, string> Metadata => Inner.Metadata;
        public List<string> Scripts => Inner.Scripts;
        public Dictionary<string, string> ExternalScripts => Inner.ExternalScripts;
        public List<string> Styles => Inner.Styles;
        public Dictionary<string, string> ExternalStyles => Inner.ExternalStyles;
    }

    public class HtmlElement
    {
        public NovaHtmlElement Inner { get; }

        public HtmlElement() { }

        public HtmlElement(NovaHtmlElement element)
        {
            Inner = element;
        }

        public string TagName
        {
            get => Inner.TagName;
            set => Inner.TagName = value;
        }

        public string InnerText
        {
            get => Inner.InnerText;
            set => Inner.InnerText = value;
        }

        public Dictionary<string, string> Attributes => Inner.Attributes;

        public List<HtmlElement> Children
        {
            get
            {
                var list = new List<HtmlElement>();
                foreach (var c in Inner.Children)
                    list.Add(new HtmlElement(c));
                return list;
            }
            set
            {
                Inner.Children.Clear();
                if (value == null) return;
                foreach (var c in value)
                    Inner.AddChild(c.Inner);
            }
        }
    }
}