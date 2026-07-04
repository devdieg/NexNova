using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using NexNova.Models;

namespace NexNova.NovaCore
{
    public class NovaCoreEngine : Control, IBrowserEngine
    {
        private NovaDom dom = new NovaDom();
        private string currentUrl = "";
        private string documentTitle = "NexNova";
        private NovaHtmlDocument currentDocument;
        private bool isLoading = false;

        public event EventHandler<string> PageLoaded;
        public event EventHandler<string> TitleChanged;

        public NovaCoreEngine()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.White;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
        }

        public async Task Navigate(string url)
        {
            try
            {
                isLoading = true;
                currentUrl = url;

                // Simulación básica
                currentDocument = new NovaHtmlDocument
                {
                    Title = "Test Page",
                    BaseUrl = url,
                    Body = new NovaHtmlElement
                    {
                        TagName = "body",
                        Children = new List<NovaHtmlElement>
                        {
                            new NovaHtmlElement { TagName = "h1", InnerText = "NexNova Browser" },
                            new NovaHtmlElement { TagName = "p", InnerText = $"Loaded: {url}" },
                            new NovaHtmlElement
                            {
                                TagName = "a",
                                InnerText = "Click me",
                                Attributes = new Dictionary<string, string> { { "href", "https://example.com" } }
                            }
                        }
                    }
                };

                dom.Build(currentDocument);
                documentTitle = currentDocument.Title;

                TitleChanged?.Invoke(this, documentTitle);
                PageLoaded?.Invoke(this, url);

                Invalidate(); // Redibujar
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        public void GoBack() { }
        public void GoForward() { }
        public void Refresh() => Navigate(currentUrl);

        public async Task<string> ExecuteScript(string script)
        {
            await Task.Delay(1);
            return $"Executed: {script}";
        }

        public string GetTitle() => documentTitle;
        public bool IsLoading => isLoading;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentDocument == null)
            {
                // Dibujar mensaje de bienvenida
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.DarkGray))
                {
                    string message = "NexNova Browser\nPowered by NovaCore";
                    var size = e.Graphics.MeasureString(message, font);
                    var x = (this.Width - size.Width) / 2;
                    var y = (this.Height - size.Height) / 2;
                    e.Graphics.DrawString(message, font, brush, x, y);
                }
            }
            else
            {
                // Dibujar contenido básico
                using (var font = new Font("Arial", 12))
                using (var brush = new SolidBrush(Color.Black))
                {
                    e.Graphics.DrawString(currentDocument.Title, font, brush, 10, 10);
                    e.Graphics.DrawString($"URL: {currentUrl}", font, brush, 10, 40);

                    // Dibujar enlace
                    using (var linkFont = new Font("Arial", 12, FontStyle.Underline))
                    using (var linkBrush = new SolidBrush(Color.Blue))
                    {
                        e.Graphics.DrawString("Click me (Example link)", linkFont, linkBrush, 10, 70);
                    }
                }
            }
        }

        private void ShowError(string message)
        {
            currentDocument = new NovaHtmlDocument
            {
                Title = "Error",
                Body = new NovaHtmlElement
                {
                    TagName = "body",
                    Children = new List<NovaHtmlElement>
                    {
                        new NovaHtmlElement { TagName = "h1", InnerText = "Error" },
                        new NovaHtmlElement { TagName = "p", InnerText = message }
                    }
                }
            };
            dom.Build(currentDocument);
            Invalidate();
        }
    }
}