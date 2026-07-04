using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using NHtml = NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaRenderer
    {
        #region Constantes
        private const int DEFAULT_FONT_SIZE = 12;
        private const string DEFAULT_FONT_FAMILY = "Arial";
        private readonly Color DEFAULT_TEXT_COLOR = Color.Black;
        private readonly Color DEFAULT_BACKGROUND = Color.White;
        private readonly Color LINK_COLOR = Color.Blue;
        private readonly Color LINK_VISITED_COLOR = Color.Purple;
        private readonly Color BORDER_COLOR = Color.LightGray;
        #endregion

        #region Campos
        private Dictionary<string, Font> fontCache = new Dictionary<string, Font>();
        private Dictionary<string, Brush> brushCache = new Dictionary<string, Brush>();
        private Dictionary<string, Pen> penCache = new Dictionary<string, Pen>();
        private HashSet<string> visitedLinks = new HashSet<string>();
        #endregion

        #region Métodos Públicos
        public void Render(Graphics g, NovaDom dom, Size viewportSize)
        {
            try
            {
                // Configurar calidad de renderizado
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Limpiar fondo
                g.Clear(DEFAULT_BACKGROUND);

                if (dom?.Root == null)
                    return;

                // Calcular layout
                CalculateLayout(dom.Root, viewportSize);

                // Renderizar desde la raíz
                RenderElement(g, dom.Root, 0, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Render error: {ex.Message}");
            }
        }
        #endregion

        #region Métodos de Layout
        private void CalculateLayout(HtmlElement element, Size viewportSize, int parentX = 0, int parentY = 0)
        {
            if (element == null)
                return;

            // Aplicar estilos de caja
            ApplyBoxModel(element);

            // Posicionar elemento
            element.RenderBounds = CalculateElementBounds(element, parentX, parentY, viewportSize);

            // Calcular layout para hijos
            int childX = element.RenderBounds.X;
            int childY = element.RenderBounds.Y;

            foreach (var child in element.Children ?? new List<HtmlElement>())
            {
                CalculateLayout(child, viewportSize, childX, childY);

                // Actualizar posición para siguiente hijo (layout vertical simple)
                if (child.RenderBounds != Rectangle.Empty)
                {
                    childY = child.RenderBounds.Bottom;
                }
            }

            // Ajustar altura si es necesario
            if (element.Children != null && element.Children.Count > 0)
            {
                var lastChild = element.Children.Last();
                if (lastChild.RenderBounds != Rectangle.Empty)
                {
                    int contentBottom = lastChild.RenderBounds.Bottom;
                    if (contentBottom > element.RenderBounds.Bottom)
                    {
                        element.RenderBounds = new Rectangle(
                            element.RenderBounds.X,
                            element.RenderBounds.Y,
                            element.RenderBounds.Width,
                            contentBottom - element.RenderBounds.Y
                        );
                    }
                }
            }
        }

        private void ApplyBoxModel(HtmlElement element)
        {
            // Valores por defecto
            int marginTop = 0, marginRight = 0, marginBottom = 0, marginLeft = 0;
            int paddingTop = 0, paddingRight = 0, paddingBottom = 0, paddingLeft = 0;
            int borderWidth = 0;

            // Aplicar estilos
            if (element.Styles != null)
            {
                // Margen
                if (element.Styles.ContainsKey("margin"))
                    ParseBoxValue(element.Styles["margin"], ref marginTop, ref marginRight, ref marginBottom, ref marginLeft);
                else
                {
                    if (element.Styles.ContainsKey("margin-top")) int.TryParse(element.Styles["margin-top"], out marginTop);
                    if (element.Styles.ContainsKey("margin-right")) int.TryParse(element.Styles["margin-right"], out marginRight);
                    if (element.Styles.ContainsKey("margin-bottom")) int.TryParse(element.Styles["margin-bottom"], out marginBottom);
                    if (element.Styles.ContainsKey("margin-left")) int.TryParse(element.Styles["margin-left"], out marginLeft);
                }

                // Padding
                if (element.Styles.ContainsKey("padding"))
                    ParseBoxValue(element.Styles["padding"], ref paddingTop, ref paddingRight, ref paddingBottom, ref paddingLeft);
                else
                {
                    if (element.Styles.ContainsKey("padding-top")) int.TryParse(element.Styles["padding-top"], out paddingTop);
                    if (element.Styles.ContainsKey("padding-right")) int.TryParse(element.Styles["padding-right"], out paddingRight);
                    if (element.Styles.ContainsKey("padding-bottom")) int.TryParse(element.Styles["padding-bottom"], out paddingBottom);
                    if (element.Styles.ContainsKey("padding-left")) int.TryParse(element.Styles["padding-left"], out paddingLeft);
                }

                // Borde
                if (element.Styles.ContainsKey("border-width")) int.TryParse(element.Styles["border-width"], out borderWidth);
            }

            // Almacenar en elemento
            element.Margin = new BoxModel(marginTop, marginRight, marginBottom, marginLeft);
            element.Padding = new BoxModel(paddingTop, paddingRight, paddingBottom, paddingLeft);
            element.BorderWidth = borderWidth;
        }

        private Rectangle CalculateElementBounds(HtmlElement element, int parentX, int parentY, Size viewportSize)
        {
            // Tamaño basado en tipo de elemento
            Size elementSize = GetElementSize(element);

            // Aplicar ancho/alto de estilos
            if (element.Styles != null)
            {
                if (element.Styles.ContainsKey("width") && element.Styles["width"].EndsWith("px"))
                {
                    if (int.TryParse(element.Styles["width"].Replace("px", ""), out int width))
                        elementSize.Width = width;
                }

                if (element.Styles.ContainsKey("height") && element.Styles["height"].EndsWith("px"))
                {
                    if (int.TryParse(element.Styles["height"].Replace("px", ""), out int height))
                        elementSize.Height = height;
                }
            }

            // Aplicar márgenes
            int totalWidth = elementSize.Width + element.Margin.Left + element.Margin.Right;
            int totalHeight = elementSize.Height + element.Margin.Top + element.Margin.Bottom;

            // Posicionamiento (simple flow)
            int x = parentX + element.Margin.Left;
            int y = parentY + element.Margin.Top;

            return new Rectangle(x, y, totalWidth, totalHeight);
        }

        private Size GetElementSize(HtmlElement element)
        {
            switch (element.TagName.ToLower())
            {
                case "h1":
                    return new Size(800, 40);
                case "h2":
                    return new Size(800, 32);
                case "h3":
                    return new Size(800, 28);
                case "h4":
                case "h5":
                case "h6":
                    return new Size(800, 24);
                case "p":
                    // Calcular tamaño basado en texto
                    using (var font = GetFont(element))
                    using (var tempGraphics = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        var size = tempGraphics.MeasureString(element.InnerText ?? "", font);
                        return new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
                    }
                case "img":
                    // Tamaño de imagen (se cargará luego)
                    return new Size(100, 100);
                case "div":
                case "section":
                case "article":
                    return new Size(800, 100);
                case "a":
                    // Tamaño basado en texto del enlace
                    using (var font = GetFont(element))
                    using (var tempGraphics = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        var size = tempGraphics.MeasureString(element.InnerText ?? element.Attributes?.GetValueOrDefault("href", ""), font);
                        return new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
                    }
                case "input":
                    if (element.Attributes?.GetValueOrDefault("type", "text") == "text")
                        return new Size(200, 30);
                    else if (element.Attributes?.GetValueOrDefault("type") == "button")
                        return new Size(100, 30);
                    else
                        return new Size(200, 30);
                case "button":
                    return new Size(100, 30);
                case "textarea":
                    return new Size(300, 100);
                default:
                    return new Size(800, 20);
            }
        }
        #endregion

        #region Métodos de Renderizado
        private void RenderElement(Graphics g, HtmlElement element, int offsetX, int offsetY)
        {
            if (element == null || element.RenderBounds == Rectangle.Empty)
                return;

            // Calcular posición real
            Rectangle bounds = new Rectangle(
                element.RenderBounds.X + offsetX,
                element.RenderBounds.Y + offsetY,
                element.RenderBounds.Width,
                element.RenderBounds.Height
            );

            // Dibujar fondo
            DrawBackground(g, element, bounds);

            // Dibujar borde
            DrawBorder(g, element, bounds);

            // Dibujar contenido
            DrawContent(g, element, bounds);

            // Renderizar hijos
            foreach (var child in element.Children ?? new List<HtmlElement>())
            {
                RenderElement(g, child, bounds.X, bounds.Y);
            }
        }

        private void DrawBackground(Graphics g, HtmlElement element, Rectangle bounds)
        {
            Color bgColor = DEFAULT_BACKGROUND;

            if (element.Styles != null && element.Styles.ContainsKey("background-color"))
            {
                bgColor = ParseColor(element.Styles["background-color"], DEFAULT_BACKGROUND);
            }
            else if (element.Attributes?.ContainsKey("bgcolor") == true)
            {
                bgColor = ParseColor(element.Attributes["bgcolor"], DEFAULT_BACKGROUND);
            }

            if (bgColor != DEFAULT_BACKGROUND && bgColor.A > 0)
            {
                using (var brush = GetCachedBrush(bgColor))
                {
                    // Ajustar por padding
                    Rectangle fillRect = new Rectangle(
                        bounds.X + element.Padding.Left,
                        bounds.Y + element.Padding.Top,
                        bounds.Width - element.Padding.Left - element.Padding.Right,
                        bounds.Height - element.Padding.Top - element.Padding.Bottom
                    );

                    g.FillRectangle(brush, fillRect);
                }
            }
        }

        private void DrawBorder(Graphics g, HtmlElement element, Rectangle bounds)
        {
            if (element.BorderWidth > 0)
            {
                using (var pen = GetCachedPen(BORDER_COLOR, element.BorderWidth))
                {
                    // Ajustar por mitad del ancho del borde para dibujar dentro
                    Rectangle borderRect = new Rectangle(
                        bounds.X + element.BorderWidth / 2,
                        bounds.Y + element.BorderWidth / 2,
                        bounds.Width - element.BorderWidth,
                        bounds.Height - element.BorderWidth
                    );

                    g.DrawRectangle(pen, borderRect);
                }
            }
        }

        private void DrawContent(Graphics g, HtmlElement element, Rectangle bounds)
        {
            // Área de contenido (dentro del padding)
            Rectangle contentRect = new Rectangle(
                bounds.X + element.Padding.Left,
                bounds.Y + element.Padding.Top,
                bounds.Width - element.Padding.Left - element.Padding.Right,
                bounds.Height - element.Padding.Top - element.Padding.Bottom
            );

            switch (element.TagName.ToLower())
            {
                case "#text":
                    DrawText(g, element, contentRect);
                    break;
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                case "p":
                case "span":
                case "div":
                case "section":
                case "article":
                case "header":
                case "footer":
                case "nav":
                case "main":
                    DrawText(g, element, contentRect);
                    break;
                case "a":
                    DrawLink(g, element, contentRect);
                    break;
                case "img":
                    DrawImage(g, element, contentRect);
                    break;
                case "input":
                    DrawInput(g, element, contentRect);
                    break;
                case "button":
                    DrawButton(g, element, contentRect);
                    break;
                case "textarea":
                    DrawTextarea(g, element, contentRect);
                    break;
                case "ul":
                case "ol":
                    DrawList(g, element, contentRect);
                    break;
                case "li":
                    DrawListItem(g, element, contentRect);
                    break;
                case "br":
                    // Salto de línea - manejado en el layout
                    break;
                case "hr":
                    DrawHorizontalRule(g, element, contentRect);
                    break;
                default:
                    // Elementos no soportados se tratan como texto
                    if (!string.IsNullOrEmpty(element.InnerText))
                    {
                        DrawText(g, element, contentRect);
                    }
                    break;
            }
        }

        private void DrawText(Graphics g, HtmlElement element, Rectangle bounds)
        {
            if (string.IsNullOrEmpty(element.InnerText))
                return;

            using (var font = GetFont(element))
            using (var brush = GetTextBrush(element))
            {
                var format = GetTextFormat(element);
                g.DrawString(element.InnerText, font, brush, bounds, format);
            }
        }

        private void DrawLink(Graphics g, HtmlElement element, Rectangle bounds)
        {
            string linkText = element.InnerText;
            if (string.IsNullOrEmpty(linkText) && element.Attributes?.ContainsKey("href") == true)
            {
                linkText = element.Attributes["href"];
            }

            if (string.IsNullOrEmpty(linkText))
                return;

            // Determinar color del enlace
            Color linkColor = LINK_COLOR;
            if (element.Attributes?.ContainsKey("href") == true)
            {
                string href = element.Attributes["href"];
                if (visitedLinks.Contains(href))
                {
                    linkColor = LINK_VISITED_COLOR;
                }
            }

            // Aplicar estilo subrayado
            FontStyle style = FontStyle.Underline;
            if (element.Styles?.ContainsKey("font-style") == true)
            {
                if (element.Styles["font-style"] == "italic")
                    style |= FontStyle.Italic;
            }

            if (element.Styles?.ContainsKey("font-weight") == true)
            {
                if (element.Styles["font-weight"] == "bold")
                    style |= FontStyle.Bold;
            }

            using (var font = new Font(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, style))
            using (var brush = new SolidBrush(linkColor))
            {
                var format = GetTextFormat(element);
                g.DrawString(linkText, font, brush, bounds, format);
            }
        }

        private void DrawImage(Graphics g, HtmlElement element, Rectangle bounds)
        {
            // Por ahora, dibujar un placeholder
            using (var pen = new Pen(Color.LightGray, 1))
            using (var brush = new SolidBrush(Color.WhiteSmoke))
            {
                g.FillRectangle(brush, bounds);
                g.DrawRectangle(pen, bounds);

                // Texto alternativo
                string altText = element.Attributes?.GetValueOrDefault("alt", "Image");
                using (var font = new Font(DEFAULT_FONT_FAMILY, 10))
                using (var textBrush = new SolidBrush(Color.DarkGray))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    g.DrawString(altText, font, textBrush, bounds, format);
                }
            }
        }

        private void DrawInput(Graphics g, HtmlElement element, Rectangle bounds)
        {
            string inputType = element.Attributes?.GetValueOrDefault("type", "text");
            string value = element.Attributes?.GetValueOrDefault("value", "");

            // Dibujar borde
            using (var pen = new Pen(Color.DarkGray, 1))
            {
                g.DrawRectangle(pen, bounds);
            }

            // Dibujar texto
            if (!string.IsNullOrEmpty(value))
            {
                using (var font = new Font(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE))
                using (var brush = new SolidBrush(DEFAULT_TEXT_COLOR))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };

                    // Ajustar para padding interno
                    Rectangle textBounds = new Rectangle(
                        bounds.X + 5,
                        bounds.Y,
                        bounds.Width - 10,
                        bounds.Height
                    );

                    g.DrawString(value, font, brush, textBounds, format);
                }
            }

            // Placeholder
            else if (element.Attributes?.ContainsKey("placeholder") == true)
            {
                string placeholder = element.Attributes["placeholder"];
                using (var font = new Font(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, FontStyle.Italic))
                using (var brush = new SolidBrush(Color.Gray))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };

                    Rectangle textBounds = new Rectangle(
                        bounds.X + 5,
                        bounds.Y,
                        bounds.Width - 10,
                        bounds.Height
                    );

                    g.DrawString(placeholder, font, brush, textBounds, format);
                }
            }
        }

        private void DrawButton(Graphics g, HtmlElement element, Rectangle bounds)
        {
            string buttonText = element.InnerText ?? "Button";

            // Dibujar fondo del botón
            Color bgColor = Color.SteelBlue;
            if (element.Styles?.ContainsKey("background-color") == true)
            {
                bgColor = ParseColor(element.Styles["background-color"], bgColor);
            }

            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, bounds);
            }

            // Dibujar borde
            using (var pen = new Pen(Color.DarkSlateBlue, 2))
            {
                g.DrawRectangle(pen, bounds);
            }

            // Dibujar texto
            using (var font = new Font(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(buttonText, font, brush, bounds, format);
            }
        }

        private void DrawTextarea(Graphics g, HtmlElement element, Rectangle bounds)
        {
            string value = element.Attributes?.GetValueOrDefault("value", "");

            // Dibujar borde
            using (var pen = new Pen(Color.DarkGray, 1))
            {
                g.DrawRectangle(pen, bounds);
            }

            // Dibujar fondo
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, bounds);
            }

            // Dibujar texto
            if (!string.IsNullOrEmpty(value))
            {
                using (var font = new Font("Consolas", 10))
                using (var brush = new SolidBrush(DEFAULT_TEXT_COLOR))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        FormatFlags = StringFormatFlags.NoWrap
                    };

                    // Ajustar para padding interno
                    Rectangle textBounds = new Rectangle(
                        bounds.X + 5,
                        bounds.Y + 5,
                        bounds.Width - 10,
                        bounds.Height - 10
                    );

                    g.DrawString(value, font, brush, textBounds, format);
                }
            }
        }

        private void DrawList(Graphics g, HtmlElement element, Rectangle bounds)
        {
            // Lista - el contenido se dibuja a través de los items
        }

        private void DrawListItem(Graphics g, HtmlElement element, Rectangle bounds)
        {
            // Bullet point
            int bulletSize = 6;
            Rectangle bulletRect = new Rectangle(
                bounds.X,
                bounds.Y + (bounds.Height - bulletSize) / 2,
                bulletSize,
                bulletSize
            );

            using (var brush = new SolidBrush(DEFAULT_TEXT_COLOR))
            {
                g.FillEllipse(brush, bulletRect);
            }

            // Texto
            Rectangle textRect = new Rectangle(
                bounds.X + bulletSize + 5,
                bounds.Y,
                bounds.Width - bulletSize - 5,
                bounds.Height
            );

            DrawText(g, element, textRect);
        }

        private void DrawHorizontalRule(Graphics g, HtmlElement element, Rectangle bounds)
        {
            int y = bounds.Y + bounds.Height / 2;

            using (var pen = new Pen(Color.Gray, 2))
            {
                g.DrawLine(pen, bounds.X, y, bounds.X + bounds.Width, y);
            }
        }
        #endregion

        #region Métodos de Ayuda
        private Font GetFont(HtmlElement element)
        {
            string fontFamily = DEFAULT_FONT_FAMILY;
            float fontSize = DEFAULT_FONT_SIZE;
            FontStyle style = FontStyle.Regular;

            if (element.Styles != null)
            {
                if (element.Styles.ContainsKey("font-family"))
                    fontFamily = element.Styles["font-family"];

                if (element.Styles.ContainsKey("font-size"))
                {
                    string sizeStr = element.Styles["font-size"];
                    if (sizeStr.EndsWith("px"))
                    {
                        if (float.TryParse(sizeStr.Replace("px", ""), out float size))
                            fontSize = size;
                    }
                }

                if (element.Styles.ContainsKey("font-weight"))
                {
                    if (element.Styles["font-weight"] == "bold")
                        style |= FontStyle.Bold;
                }

                if (element.Styles.ContainsKey("font-style"))
                {
                    if (element.Styles["font-style"] == "italic")
                        style |= FontStyle.Italic;
                }
            }

            // Tamaños para headers
            switch (element.TagName.ToLower())
            {
                case "h1":
                    fontSize = 32;
                    style |= FontStyle.Bold;
                    break;
                case "h2":
                    fontSize = 24;
                    style |= FontStyle.Bold;
                    break;
                case "h3":
                    fontSize = 20;
                    style |= FontStyle.Bold;
                    break;
                case "h4":
                    fontSize = 16;
                    style |= FontStyle.Bold;
                    break;
                case "h5":
                    fontSize = 14;
                    style |= FontStyle.Bold;
                    break;
                case "h6":
                    fontSize = 12;
                    style |= FontStyle.Bold;
                    break;
            }

            string cacheKey = $"{fontFamily}_{fontSize}_{style}";
            if (!fontCache.ContainsKey(cacheKey))
            {
                try
                {
                    fontCache[cacheKey] = new Font(fontFamily, fontSize, style);
                }
                catch
                {
                    fontCache[cacheKey] = new Font(DEFAULT_FONT_FAMILY, fontSize, style);
                }
            }

            return fontCache[cacheKey];
        }

        private Brush GetTextBrush(HtmlElement element)
        {
            Color color = DEFAULT_TEXT_COLOR;

            if (element.Styles != null && element.Styles.ContainsKey("color"))
            {
                color = ParseColor(element.Styles["color"], DEFAULT_TEXT_COLOR);
            }
            else if (element.Attributes?.ContainsKey("color") == true)
            {
                color = ParseColor(element.Attributes["color"], DEFAULT_TEXT_COLOR);
            }

            string cacheKey = color.ToArgb().ToString();
            if (!brushCache.ContainsKey(cacheKey))
            {
                brushCache[cacheKey] = new SolidBrush(color);
            }

            return brushCache[cacheKey];
        }

        private Brush GetCachedBrush(Color color)
        {
            string cacheKey = color.ToArgb().ToString();
            if (!brushCache.ContainsKey(cacheKey))
            {
                brushCache[cacheKey] = new SolidBrush(color);
            }

            return brushCache[cacheKey];
        }

        private Pen GetCachedPen(Color color, float width)
        {
            string cacheKey = $"{color.ToArgb()}_{width}";
            if (!penCache.ContainsKey(cacheKey))
            {
                penCache[cacheKey] = new Pen(color, width);
            }

            return penCache[cacheKey];
        }

        private StringFormat GetTextFormat(HtmlElement element)
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisWord
            };

            if (element.Styles != null)
            {
                if (element.Styles.ContainsKey("text-align"))
                {
                    switch (element.Styles["text-align"].ToLower())
                    {
                        case "center":
                            format.Alignment = StringAlignment.Center;
                            break;
                        case "right":
                            format.Alignment = StringAlignment.Far;
                            break;
                        case "justify":
                            format.Alignment = StringAlignment.Near;
                            format.FormatFlags &= ~StringFormatFlags.NoWrap;
                            break;
                    }
                }
            }

            return format;
        }

        private Color ParseColor(string colorStr, Color defaultColor)
        {
            if (string.IsNullOrEmpty(colorStr))
                return defaultColor;

            colorStr = colorStr.Trim().ToLower();

            // Nombres de colores básicos
            switch (colorStr)
            {
                case "black": return Color.Black;
                case "white": return Color.White;
                case "red": return Color.Red;
                case "green": return Color.Green;
                case "blue": return Color.Blue;
                case "yellow": return Color.Yellow;
                case "cyan": return Color.Cyan;
                case "magenta": return Color.Magenta;
                case "gray": return Color.Gray;
                case "grey": return Color.Gray;
                case "lightgray": return Color.LightGray;
                case "darkgray": return Color.DarkGray;
                case "orange": return Color.Orange;
                case "purple": return Color.Purple;
                case "brown": return Color.Brown;
                case "pink": return Color.Pink;
                case "transparent": return Color.Transparent;
            }

            // Hex (#RRGGBB o #RGB)
            if (colorStr.StartsWith("#"))
            {
                try
                {
                    if (colorStr.Length == 7) // #RRGGBB
                    {
                        int r = Convert.ToInt32(colorStr.Substring(1, 2), 16);
                        int g = Convert.ToInt32(colorStr.Substring(3, 2), 16);
                        int b = Convert.ToInt32(colorStr.Substring(5, 2), 16);
                        return Color.FromArgb(r, g, b);
                    }
                    else if (colorStr.Length == 4) // #RGB
                    {
                        int r = Convert.ToInt32(colorStr.Substring(1, 1) + colorStr.Substring(1, 1), 16);
                        int g = Convert.ToInt32(colorStr.Substring(2, 1) + colorStr.Substring(2, 1), 16);
                        int b = Convert.ToInt32(colorStr.Substring(3, 1) + colorStr.Substring(3, 1), 16);
                        return Color.FromArgb(r, g, b);
                    }
                }
                catch { }
            }

            // rgb(r, g, b)
            if (colorStr.StartsWith("rgb("))
            {
                try
                {
                    string values = colorStr.Substring(4, colorStr.Length - 5);
                    var parts = values.Split(',');
                    if (parts.Length == 3)
                    {
                        int r = int.Parse(parts[0].Trim());
                        int g = int.Parse(parts[1].Trim());
                        int b = int.Parse(parts[2].Trim());
                        return Color.FromArgb(r, g, b);
                    }
                }
                catch { }
            }

            return defaultColor;
        }

        private void ParseBoxValue(string value, ref int top, ref int right, ref int bottom, ref int left)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            switch (parts.Length)
            {
                case 1: // todos los lados iguales
                    if (int.TryParse(parts[0].Replace("px", ""), out int all))
                    {
                        top = right = bottom = left = all;
                    }
                    break;
                case 2: // vertical | horizontal
                    if (int.TryParse(parts[0].Replace("px", ""), out int vertical))
                    {
                        top = bottom = vertical;
                    }
                    if (int.TryParse(parts[1].Replace("px", ""), out int horizontal))
                    {
                        right = left = horizontal;
                    }
                    break;
                case 3: // top | horizontal | bottom
                    if (int.TryParse(parts[0].Replace("px", ""), out int t))
                        top = t;
                    if (int.TryParse(parts[1].Replace("px", ""), out int h))
                        right = left = h;
                    if (int.TryParse(parts[2].Replace("px", ""), out int b))
                        bottom = b;
                    break;
                case 4: // top | right | bottom | left
                    if (int.TryParse(parts[0].Replace("px", ""), out int t4))
                        top = t4;
                    if (int.TryParse(parts[1].Replace("px", ""), out int r))
                        right = r;
                    if (int.TryParse(parts[2].Replace("px", ""), out int b4))
                        bottom = b4;
                    if (int.TryParse(parts[3].Replace("px", ""), out int l))
                        left = l;
                    break;
            }
        }
        #endregion
    }
}