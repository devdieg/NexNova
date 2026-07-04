using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaDom
    {
        private NovaHtmlElement root;
        private NovaHtmlElement focusedElement;
        private Dictionary<string, NovaHtmlElement> idCache = new Dictionary<string, NovaHtmlElement>();
        private Dictionary<string, List<NovaHtmlElement>> classCache = new Dictionary<string, List<NovaHtmlElement>>();
        private Dictionary<string, List<NovaHtmlElement>> tagCache = new Dictionary<string, List<NovaHtmlElement>>();
        private int nextId = 1;

        public NovaHtmlElement Root => root;
        public NovaHtmlElement FocusedElement => focusedElement;

        public void Build(NovaHtmlDocument document)
        {
            if (document == null) return;

            // Crear elemento raíz
            root = new NovaHtmlElement
            {
                TagName = "html",
                Id = GenerateId()
            };

            // Agregar head y body
            if (document.Head != null)
            {
                document.Head.Id = GenerateId();
                root.Children.Add(document.Head);
            }

            if (document.Body != null)
            {
                document.Body.Id = GenerateId();
                root.Children.Add(document.Body);
            }

            RebuildCaches();
        }

        private string GenerateId() => $"element_{nextId++}";

        private void RebuildCaches()
        {
            idCache.Clear();
            classCache.Clear();
            tagCache.Clear();
            BuildCaches(root);
        }

        private void BuildCaches(NovaHtmlElement element)
        {
            if (element == null) return;

            // ID
            if (!string.IsNullOrEmpty(element.Id))
                idCache[element.Id] = element;

            // Clases
            if (element.HasAttribute("class"))
            {
                string[] classes = element.GetAttribute("class").Split(' ');
                foreach (var className in classes)
                {
                    if (!string.IsNullOrEmpty(className))
                    {
                        if (!classCache.ContainsKey(className))
                            classCache[className] = new List<NovaHtmlElement>();
                        if (!classCache[className].Contains(element))
                            classCache[className].Add(element);
                    }
                }
            }

            // Tag
            string tagName = element.TagName.ToLower();
            if (!tagCache.ContainsKey(tagName))
                tagCache[tagName] = new List<NovaHtmlElement>();
            if (!tagCache[tagName].Contains(element))
                tagCache[tagName].Add(element);

            // Hijos
            if (element.Children != null)
            {
                foreach (var child in element.Children)
                    BuildCaches(child);
            }
        }

        public NovaHtmlElement GetElementById(string id) =>
            idCache.ContainsKey(id) ? idCache[id] : null;

        public List<NovaHtmlElement> GetElementsByClassName(string className) =>
            classCache.ContainsKey(className) ? new List<NovaHtmlElement>(classCache[className]) : new List<NovaHtmlElement>();

        public List<NovaHtmlElement> GetElementsByTagName(string tagName)
        {
            tagName = tagName.ToLower();
            return tagCache.ContainsKey(tagName) ? new List<NovaHtmlElement>(tagCache[tagName]) : new List<NovaHtmlElement>();
        }

        public NovaHtmlElement GetElementAtPoint(Point point) => FindElementAtPoint(root, point);

        private NovaHtmlElement FindElementAtPoint(NovaHtmlElement element, Point point)
        {
            if (element == null || element.RenderBounds == Rectangle.Empty)
                return null;

            if (element.RenderBounds.Contains(point))
            {
                // Verificar hijos primero (de último a primero)
                if (element.Children != null)
                {
                    for (int i = element.Children.Count - 1; i >= 0; i--)
                    {
                        var found = FindElementAtPoint(element.Children[i], point);
                        if (found != null) return found;
                    }
                }

                // Si ningún hijo contiene el punto, este es el elemento
                if (IsInteractiveElement(element) || !string.IsNullOrEmpty(element.InnerText))
                    return element;
            }

            return null;
        }

        private bool IsInteractiveElement(NovaHtmlElement element)
        {
            if (element == null) return false;
            string tagName = element.TagName.ToLower();
            return tagName == "a" || tagName == "button" || tagName == "input" ||
                   tagName == "textarea" || tagName == "select";
        }

        public void SetFocus(NovaHtmlElement element)
        {
            focusedElement = element;
        }

        public void RemoveFocus()
        {
            focusedElement = null;
        }

        public List<NovaHtmlElement> GetAllElements()
        {
            var elements = new List<NovaHtmlElement>();
            CollectElements(root, elements);
            return elements;
        }

        private void CollectElements(NovaHtmlElement element, List<NovaHtmlElement> collection)
        {
            if (element == null) return;
            collection.Add(element);
            if (element.Children != null)
            {
                foreach (var child in element.Children)
                    CollectElements(child, collection);
            }
        }
    }
}