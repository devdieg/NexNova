using NexNova.Models;
using System;
using NovaCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NexNova.NexNova;
using NHtml = NexNova.Models;

namespace NexNova
{
    public partial class NexNova : Form
    {
        #region Constants and Icons
        private const string ICON_NEW_TAB = "＋";
        private const string ICON_PRIVATE = "🔒";
        private const string ICON_CLOSE = "✕";
        private const string ICON_SEARCH = "🔍";
        private const string ICON_FAVORITE = "★";
        private const string ICON_MENU = "☰";
        private const string ICON_SECURE = "🔒";
        private const string ICON_INSECURE = "⚠";
        private const string ICON_PDF = "📄";
        #endregion

        #region Fields
        private int tabCount = 0;
        private Panel currentTabContent = null;
        private Panel currentTabButtonPanel = null;
        private Panel currentBackdrop = null;
        private bool isFullscreen = false;
        private FormWindowState previousWindowState;
        private Rectangle previousBounds;
        private Stack<TabState> closedTabsHistory = new Stack<TabState>();
        private IBrowserEngine browserEngine;

        // Settings
        private string defaultSearchEngine = "Google";
        private bool enableDevTools = false;
        private bool openLastSessionOnStartup = false;
        private bool darkTheme = true;
        private bool isDefaultBrowser = false;
        private bool blockPopups = true;
        private bool enablePrecaching = true;
        private double zoomLevel = 1.0;
        private Color accentColor = Color.FromArgb(86, 93, 128);
        private string language = "en";
        private Form customMenu = null;
        private bool isMenuOpen = false;
        private event EventHandler zoomLevelChanged;

        // File names
        private const string SettingsFileName = "nexnova_settings.json";
        private const string SessionFileName = "nexnova_session.json";
        private const string FavoritesFileName = "favorites.json";
        private const string BookmarksFileName = "bookmarks.json";
        private const string ErrorLogFileName = "errors.log";
        private const string VisitLogFileName = "lastvisitedpages.log";
        private const string CacheFileName = "tabcache.json";

        // Data
        private List<string> history = new List<string>();
        private List<string> favorites = new List<string>();
        private List<Bookmark> bookmarks = new List<Bookmark>();
        private List<string> frequentTabs = new List<string>();

        // NovaCore
        private IBrowserEngine currentBrowserEngine;
        #endregion

        #region Constructor and Initialization
        public NexNova(string initialUrl = null)
        {
            ShowSplashScreen();
            InitializeComponent();

            browserEngine = new NovaCoreEngine();

            if (menuButton == null)
            {
                throw new Exception("menuButton no fue encontrado. Asegúrate de que existe en el diseñador.");
            }

            InitializeCustomMenu();

            if (newTabBtn != null)
                newTabBtn.Click += (s, e) => AddNewTab(GetDefaultHomepageUrl());
            SetupTabContextMenuEvents();
            KeyPreview = true;
            KeyDown += Form_KeyDown;
            Resize += (s, e) => ReflowTabButtons();

            LoadSettings();
            LoadFavorites();
            LoadBookmarks();
            LoadFrequentTabs();

            ApplyThemeToChrome();
            SetupTabs();

            Load += NexNova_Load;
            FormClosing += NexNova_FormClosing;

            if (!string.IsNullOrEmpty(initialUrl))
            {
                Load += (s, e) => AddNewTab(initialUrl);
            }
        }
        #endregion

        #region Initialization Methods
        private void ShowSplashScreen()
        {
            var splash = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(400, 300),
                BackColor = Color.FromArgb(86, 93, 128),
                TopMost = true
            };

            var logoLabel = new Label
            {
                Text = "NexNova",
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(120, 100)
            };

            var versionLabel = new Label
            {
                Text = "Version 60",
                Font = new Font("Ubuntu Mono", 12, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(150, 160)
            };

            var loadingLabel = new Label
            {
                Text = "Loading...",
                Font = new Font("Ubuntu Mono", 10, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(165, 200)
            };

            splash.Controls.Add(logoLabel);
            splash.Controls.Add(versionLabel);
            splash.Controls.Add(loadingLabel);

            splash.Show();
            Application.DoEvents();

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 500)
            {
                Application.DoEvents();
            }

            splash.Close();
        }

        private void SetupButtonPositions()
        {
            if (menuButton != null)
            {
                menuButton.Location = new Point(this.ClientSize.Width - menuButton.Width - 10, 8);
                menuButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                menuButton.BringToFront();
            }

            if (newTabBtn != null)
            {
                newTabBtn.Location = new Point(menuButton.Left - newTabBtn.Width - 5, 8);
                newTabBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                newTabBtn.BringToFront();
            }

            // Asegurar que el tabBar empiece desde la izquierda
            if (tabBar != null)
            {
                tabBar.Padding = new Padding(8, 8, 0, 0);
            }
        }

        private void InitializeMenuStyling()
        {
            if (contextMenuStrip == null) return;

            contextMenuStrip.Renderer = new ProfessionalMenuRenderer(darkTheme);
            contextMenuStrip.BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.WhiteSmoke;
            contextMenuStrip.ForeColor = darkTheme ? Color.White : Color.Black;

            foreach (ToolStripItem item in contextMenuStrip.Items)
            {
                item.BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.WhiteSmoke;
                item.ForeColor = darkTheme ? Color.White : Color.Black;

                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.ShowShortcutKeys = true;
                    menuItem.ShortcutKeyDisplayString = menuItem.ShortcutKeys.ToString();
                }
            }

            if (tabContextMenuStrip != null)
            {
                tabContextMenuStrip.Renderer = new ProfessionalMenuRenderer(darkTheme);
                tabContextMenuStrip.BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.WhiteSmoke;

                foreach (ToolStripItem item in tabContextMenuStrip.Items)
                {
                    item.BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.WhiteSmoke;
                    item.ForeColor = darkTheme ? Color.White : Color.Black;
                }
            }
        }

        private class ProfessionalMenuRenderer : ToolStripProfessionalRenderer
        {
            private bool darkTheme;

            public ProfessionalMenuRenderer(bool darkTheme) : base(new ProfessionalColorTable())
            {
                this.darkTheme = darkTheme;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected)
                {
                    base.OnRenderMenuItemBackground(e);
                }
                else
                {
                    var rect = new Rectangle(Point.Empty, e.Item.Size);
                    using (var brush = new SolidBrush(darkTheme ? Color.FromArgb(50, 50, 70) : Color.FromArgb(240, 240, 255)))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (darkTheme)
                {
                    e.TextColor = e.Item.Selected ? Color.White : Color.FromArgb(220, 220, 220);
                }
                else
                {
                    e.TextColor = e.Item.Selected ? Color.Black : Color.FromArgb(50, 50, 50);
                }

                base.OnRenderItemText(e);
            }
        }

        private void SetupMenuEvents()
        {
            if (newTabBtn != null)
                newTabBtn.Click += (s, e) => AddNewTab(GetDefaultHomepageUrl());

            if (menuButton != null)
                menuButton.Click += (s, e) => ToggleCustomMenu();

            if (saveToFavoritesToolStripMenuItem != null)
                saveToFavoritesToolStripMenuItem.Click += (s, e) => { /* ... */ };

            if (saveToBookmarksToolStripMenuItem != null)
                saveToBookmarksToolStripMenuItem.Click += (s, e) => { /* ... */ };
        }

        private void SetupTabContextMenuEvents()
        {
            saveToFavoritesToolStripMenuItem.Click += (s, e) =>
            {
                if (currentTabContent?.Tag is TabMeta meta)
                {
                    string url = meta.CurrentUrl ?? meta.InitialUrl;
                    if (!string.IsNullOrEmpty(url) && !favorites.Contains(url))
                    {
                        favorites.Add(url);
                        SaveFavorites();
                        UpdateStatus("Page added to Favorites");
                    }
                }
            };

            saveToBookmarksToolStripMenuItem.Click += (s, e) =>
            {
                if (currentTabContent?.Tag is TabMeta meta)
                {
                    string url = meta.CurrentUrl ?? meta.InitialUrl;
                    string title = meta.Title ?? "New Bookmark";

                    if (!string.IsNullOrEmpty(url))
                    {
                        var bookmark = new Bookmark
                        {
                            Url = url,
                            Title = title,
                            Created = DateTime.Now
                        };

                        bookmarks.Add(bookmark);
                        SaveBookmarks();

                        if (meta.BookmarksBar != null)
                        {
                            UpdateBookmarksBar(meta.BookmarksBar);
                        }

                        UpdateStatus("Page added to Bookmarks");
                    }
                }
            };
        }

        private void SetupTabs()
        {
            if (menuButton != null)
            {
                menuButton.Location = new Point(this.Width - menuButton.Width - 10, 8);
                menuButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                menuButton.BringToFront();
            }

            if (newTabBtn != null)
            {
                newTabBtn.Location = new Point(menuButton.Left - newTabBtn.Width - 5, 8);
                newTabBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                newTabBtn.BringToFront();
            }

            this.Resize += (s, e) =>
            {
                if (menuButton != null)
                {
                    menuButton.Location = new Point(this.Width - menuButton.Width - 10, 8);
                }
                if (newTabBtn != null)
                {
                    newTabBtn.Location = new Point(menuButton.Left - newTabBtn.Width - 5, 8);
                }
            };
        }
        #endregion

        #region Custom Menu System
        private void InitializeCustomMenu()
        {
            if (menuButton != null)
            {
                menuButton.Click += (s, e) => ToggleCustomMenu();
            }
        }

        private void ToggleCustomMenu()
        {
            if (isMenuOpen)
            {
                CloseCustomMenu();
            }
            else
            {
                ShowCustomMenu();
            }
        }

        private void ShowCustomMenu()
        {
            try
            {
                if (customMenu != null && !customMenu.IsDisposed)
                {
                    CloseCustomMenu();
                }

                isMenuOpen = true;

                customMenu = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    TopMost = true,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.FromArgb(245, 247, 250),
                    Padding = new Padding(1),
                    Size = new Size(350, 500),
                    MinimumSize = new Size(300, 400),
                    MaximumSize = new Size(400, 600),
                    ShowIcon = false,
                    ControlBox = false
                };

                var menuPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = darkTheme ? Color.FromArgb(32, 36, 48) : Color.FromArgb(245, 247, 250),
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };

                var container = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Padding = new Padding(0),
                    Margin = new Padding(0),
                    BackColor = Color.Transparent
                };

                // Elementos del menú
                AddMenuItem(container, "➕", "New Tab", "Ctrl+T", () =>
                {
                    AddNewTab(GetDefaultHomepageUrl());
                    CloseCustomMenu();
                });

                AddMenuItem(container, "🪟", "New Window", "Ctrl+N", () =>
                {
                    var newWindow = new NexNova();
                    newWindow.Show();
                    CloseCustomMenu();
                });

                AddMenuItem(container, "🔒", "New Private Window", "Ctrl+Shift+P", () =>
                {
                    AddNewTab(GetDefaultHomepageUrl(), true);
                    CloseCustomMenu();
                });

                AddSeparator(container);
                AddZoomSubMenu(container);
                AddSeparator(container);

                AddMenuItem(container, "⭐", "Favorites", "Ctrl+Shift+O", () =>
                {
                    ShowOverlay("Favorites");
                    CloseCustomMenu();
                });

                AddMenuItem(container, "📜", "History", "Ctrl+H", () =>
                {
                    ShowOverlay("History");
                    CloseCustomMenu();
                });

                AddMenuItem(container, "⚙", "Settings", "Ctrl+,", () =>
                {
                    ShowOverlay("Settings");
                    CloseCustomMenu();
                });

                AddMenuItem(container, "❓", "Help & Feedback", "", () =>
                {
                    ShowOverlay("Help");
                    CloseCustomMenu();
                });

                AddSeparator(container);

                AddMenuItem(container, "❌", "Close NexNova", "Alt+F4", () =>
                {
                    Application.Exit();
                    CloseCustomMenu();
                });

                menuPanel.Controls.Add(container);
                customMenu.Controls.Add(menuPanel);
                PositionCustomMenu();

                customMenu.Paint += (s, e) =>
                {
                    using (var pen = new Pen(darkTheme ? Color.FromArgb(60, 64, 76) : Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, customMenu.Width - 1, customMenu.Height - 1);
                    }
                };

                if (customMenu != null)
                {
                    customMenu.Deactivate += (s, e) => CloseCustomMenu();
                    customMenu.LostFocus += (s, e) => CloseCustomMenu();
                    customMenu.KeyPreview = true;
                    customMenu.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Escape)
                            CloseCustomMenu();
                    };

                    customMenu.Show();
                    customMenu.Focus();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error al mostrar menú: {ex.Message}", "ShowCustomMenu");
                isMenuOpen = false;
                customMenu = null;
            }
        }

        private void AddMenuItem(FlowLayoutPanel container, string icon, string text, string shortcut, Action action)
        {
            var itemPanel = new Panel
            {
                Height = 40,
                Width = container.Width - 24,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Padding(12, 4, 12, 4),
                Padding = new Padding(12, 0, 12, 0)
            };

            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 12),
                ForeColor = darkTheme ? Color.FromArgb(200, 200, 200) : Color.FromArgb(80, 80, 80),
                Location = new Point(0, 10),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var textLabel = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                ForeColor = darkTheme ? Color.White : Color.Black,
                Location = new Point(35, 11),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            if (!string.IsNullOrEmpty(shortcut))
            {
                var shortcutLabel = new Label
                {
                    Text = shortcut,
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    ForeColor = darkTheme ? Color.FromArgb(150, 150, 150) : Color.FromArgb(120, 120, 120),
                    Location = new Point(itemPanel.Width - 120, 11),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleRight
                };

                itemPanel.Controls.Add(shortcutLabel);
            }

            itemPanel.Controls.Add(iconLabel);
            itemPanel.Controls.Add(textLabel);

            itemPanel.Resize += (s, e) =>
            {
                if (!string.IsNullOrEmpty(shortcut))
                {
                    var shortcutCtrl = itemPanel.Controls.OfType<Label>().FirstOrDefault(c => c.Text == shortcut);
                    if (shortcutCtrl != null)
                    {
                        shortcutCtrl.Location = new Point(itemPanel.Width - shortcutCtrl.Width - 12, 11);
                    }
                }
            };

            itemPanel.MouseEnter += (s, e) =>
            {
                itemPanel.BackColor = darkTheme ? Color.FromArgb(50, 54, 66) : Color.FromArgb(230, 232, 235);
                itemPanel.Invalidate();
            };

            itemPanel.MouseLeave += (s, e) =>
            {
                itemPanel.BackColor = Color.Transparent;
                itemPanel.Invalidate();
            };

            itemPanel.Click += (s, e) =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    LogError($"Error en acción del menú: {ex.Message}", "MenuItemClick");
                }
            };

            container.Controls.Add(itemPanel);
        }

        private void AddZoomSubMenu(FlowLayoutPanel container)
        {
            var zoomPanel = new Panel
            {
                Height = 50,
                Width = container.Width - 24,
                BackColor = Color.Transparent,
                Margin = new Padding(12, 8, 12, 8),
                Padding = new Padding(12, 0, 12, 0)
            };

            var iconLabel = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 12),
                ForeColor = darkTheme ? Color.FromArgb(200, 200, 200) : Color.FromArgb(80, 80, 80),
                Location = new Point(0, 17),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var textLabel = new Label
            {
                Text = "Zoom",
                Font = new Font("Segoe UI", 10),
                ForeColor = darkTheme ? Color.White : Color.Black,
                Location = new Point(35, 18),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var zoomControlsPanel = new Panel
            {
                Size = new Size(140, 30),
                Location = new Point(zoomPanel.Width - 160, 10),
                BackColor = Color.Transparent
            };

            var zoomLabel = new Label
            {
                Text = $"{zoomLevel * 100:0}%",
                Font = new Font("Segoe UI", 10),
                ForeColor = darkTheme ? Color.White : Color.Black,
                Size = new Size(50, 32),
                Location = new Point(40, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var zoomOutBtn = new Button
            {
                Text = "−",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(32, 32),
                Location = new Point(0, 0),
                BackColor = darkTheme ? Color.FromArgb(60, 64, 76) : Color.FromArgb(220, 222, 225),
                ForeColor = darkTheme ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            zoomOutBtn.FlatAppearance.BorderSize = 0;
            zoomOutBtn.Click += (s, e) =>
            {
                try
                {
                    ZoomOut();
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        try
                        {
                            if (zoomControlsPanel.InvokeRequired)
                            {
                                zoomControlsPanel.Invoke(new Action(() =>
                                {
                                    UpdateZoomLabel(zoomLabel);
                                }));
                            }
                            else
                            {
                                UpdateZoomLabel(zoomLabel);
                            }
                        }
                        catch { }
                    });
                }
                catch (Exception ex)
                {
                    LogError($"Error en ZoomOut: {ex.Message}", "ZoomOut");
                }
            };

            var zoomInBtn = new Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(32, 32),
                Location = new Point(98, 0),
                BackColor = darkTheme ? Color.FromArgb(60, 64, 76) : Color.FromArgb(220, 222, 225),
                ForeColor = darkTheme ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            zoomInBtn.FlatAppearance.BorderSize = 0;
            zoomInBtn.Click += (s, e) =>
            {
                try
                {
                    ZoomIn();
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        try
                        {
                            if (zoomControlsPanel.InvokeRequired)
                            {
                                zoomControlsPanel.Invoke(new Action(() =>
                                {
                                    UpdateZoomLabel(zoomLabel);
                                }));
                            }
                            else
                            {
                                UpdateZoomLabel(zoomLabel);
                            }
                        }
                        catch { }
                    });
                }
                catch (Exception ex)
                {
                    LogError($"Error en ZoomIn: {ex.Message}", "ZoomIn");
                }
            };

            void UpdateZoomLabel(Label label)
            {
                try
                {
                    if (label != null && !label.IsDisposed)
                    {
                        label.Text = $"{zoomLevel * 100:0}%";
                    }
                }
                catch { }
            }

            zoomLevelChanged += (s, e) =>
            {
                try
                {
                    if (zoomControlsPanel.InvokeRequired)
                    {
                        zoomControlsPanel.Invoke(new Action(() => UpdateZoomLabel(zoomLabel)));
                    }
                    else
                    {
                        UpdateZoomLabel(zoomLabel);
                    }
                }
                catch { }
            };

            zoomControlsPanel.Controls.Add(zoomOutBtn);
            zoomControlsPanel.Controls.Add(zoomLabel);
            zoomControlsPanel.Controls.Add(zoomInBtn);

            zoomPanel.Resize += (s, e) =>
            {
                try
                {
                    zoomControlsPanel.Location = new Point(zoomPanel.Width - zoomControlsPanel.Width - 12, 10);
                }
                catch { }
            };

            zoomPanel.Controls.Add(iconLabel);
            zoomPanel.Controls.Add(textLabel);
            zoomPanel.Controls.Add(zoomControlsPanel);
            container.Controls.Add(zoomPanel);
        }

        private void AddSeparator(FlowLayoutPanel container)
        {
            var separator = new Panel
            {
                Height = 1,
                Width = container.Width - 24,
                BackColor = darkTheme ? Color.FromArgb(60, 64, 76) : Color.FromArgb(210, 212, 215),
                Margin = new Padding(12, 6, 12, 6)
            };

            container.Controls.Add(separator);
        }

        private void PositionCustomMenu()
        {
            try
            {
                if (customMenu == null || menuButton == null || customMenu.IsDisposed) return;

                Point menuButtonLocation = menuButton.PointToScreen(new Point(0, 0));
                int x = menuButtonLocation.X + menuButton.Width - customMenu.Width;
                int y = menuButtonLocation.Y + menuButton.Height;

                Screen screen = Screen.FromControl(this);
                Rectangle workingArea = screen.WorkingArea;

                if (x + customMenu.Width > workingArea.Right)
                    x = workingArea.Right - customMenu.Width;

                if (x < workingArea.Left)
                    x = workingArea.Left;

                if (y + customMenu.Height > workingArea.Bottom)
                    y = menuButtonLocation.Y - customMenu.Height;

                if (y < workingArea.Top)
                    y = workingArea.Top;

                customMenu.Location = new Point(x, y);
                customMenu.BringToFront();
            }
            catch (Exception ex)
            {
                LogError($"Error posicionando menú: {ex.Message}", "PositionCustomMenu");
            }
        }

        private void CloseCustomMenu()
        {
            try
            {
                if (customMenu != null && !customMenu.IsDisposed)
                {
                    customMenu.Hide();
                    customMenu.Close();
                    customMenu.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error cerrando menú: {ex.Message}", "CloseCustomMenu");
            }
            finally
            {
                customMenu = null;
                isMenuOpen = false;
            }
        }
        #endregion

        #region Tab Management
        public void AddNewTab(string initialUrl = null, bool isPrivate = false)
        {
            if (string.IsNullOrEmpty(initialUrl))
                initialUrl = GetDefaultHomepageUrl();

            tabCount++;

            // ==================== TAB HEADER ====================
            var header = new Panel
            {
                Width = 180,
                Height = 36,
                BackColor = darkTheme ? Color.FromArgb(46, 51, 73) : Color.LightSteelBlue,
                Padding = new Padding(6),
                Margin = new Padding(5),
                ContextMenuStrip = tabContextMenuStrip
            };

            var titleBtn = new Button
            {
                Text = (isPrivate ? ICON_PRIVATE + " " : "") + "New Tab",
                Dock = DockStyle.Fill,
                BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue,
                ForeColor = darkTheme ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleBtn.FlatAppearance.BorderSize = 0;

            var closeBtn = new Button
            {
                Text = ICON_CLOSE,
                Dock = DockStyle.Right,
                Width = 30,
                BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;

            // ==================== TAB CONTENT ====================
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.FromArgb(240, 245, 255),
                Visible = false
            };

            // ==================== BAR 1 (Top - Small) ====================
            var searchBox1 = new TextBox
            {
                Height = 28,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                PlaceholderText = "Search or enter address...",
                ForeColor = darkTheme ? Color.White : Color.Black,
                BackColor = darkTheme ? Color.FromArgb(60, 66, 94) : Color.White,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            var searchBtn1 = new Button
            {
                Text = ICON_SEARCH,
                Width = 20,
                Height = 5,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.LightSkyBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8),
                Dock = DockStyle.Right
            };
            searchBtn1.FlatAppearance.BorderSize = 0;

            var searchRow1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.Gainsboro,
                Padding = new Padding(10, 6, 10, 6)
            };
            searchRow1.Controls.Add(searchBox1);
            searchRow1.Controls.Add(searchBtn1);

            // ==================== BAR 2 (Bottom - Large, Search Only) ====================
            var searchBox2 = new TextBox
            {
                Height = 50,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                PlaceholderText = $"Search with {defaultSearchEngine}...",
                ForeColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.Black,
                BackColor = darkTheme ? Color.FromArgb(240, 240, 240) : Color.White,
                Dock = DockStyle.Fill
            };

            var searchBtn2 = new Button
            {
                Text = ICON_SEARCH,
                Width = 35,
                Height = 1,
                BackColor = Color.SteelBlue,
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right
            };
            searchBtn2.FlatAppearance.BorderSize = 0;

            var searchRow2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.FromArgb(86, 93, 128),
                Padding = new Padding(100, 40, 100, 0),
                Visible = true
            };
            searchRow2.Controls.Add(searchBox2);
            searchRow2.Controls.Add(searchBtn2);

            // ==================== BOOKMARKS BAR ====================
            var bookmarksBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.Transparent,
                Padding = new Padding(50, 20, 50, 0),
                Visible = true,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            // ==================== BROWSER CONTAINER ====================
            var browserContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = darkTheme ? Color.FromArgb(8, 8, 8) : Color.White
            };

            // ==================== ADD CONTROLS TO CONTENT ====================
            content.Controls.Add(browserContainer);
            content.Controls.Add(bookmarksBar);
            content.Controls.Add(searchRow2);
            content.Controls.Add(searchRow1);

            // ==================== TAB METADATA ====================
            var meta = new TabMeta
            {
                BrowserEngine = null,
                BrowserContainer = browserContainer,
                IsPrivate = isPrivate,
                SearchBox1 = searchBox1,
                SearchBox2 = searchBox2,
                TitleButton = titleBtn,
                InitialUrl = initialUrl,
                CurrentUrl = initialUrl,
                ZoomLevel = zoomLevel,
                BookmarksBar = bookmarksBar
            };
            content.Tag = meta;

            // ==================== EVENT HANDLERS ====================
            searchBtn1.Click += (s, e) => DoSearch1(searchBox1.Text, meta);
            searchBox1.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    DoSearch1(searchBox1.Text, meta);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            searchBtn2.Click += (s, e) => DoSearch2(searchBox2.Text, meta);
            searchBox2.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    DoSearch2(searchBox2.Text, meta);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            titleBtn.Click += (s, e) =>
            {
                CloseOverlay();
                foreach (Control c in contentArea.Controls) c.Visible = false;
                content.Visible = true;
                currentTabContent = content;
                currentTabButtonPanel = header;
                HighlightActiveTab(header);

                if ((meta.InitialUrl == GetDefaultHomepageUrl() || string.IsNullOrEmpty(meta.InitialUrl)) &&
                    meta.BrowserEngine == null)
                {
                    RestoreNewTabUI(meta);
                }
            };

            closeBtn.Click += (s, e) => CloseTabImmediate(header);

            // ==================== ADD CONTROLS ====================
            header.Controls.Add(titleBtn);
            header.Controls.Add(closeBtn);
            tabBar.Controls.Add(header);
            contentArea.Controls.Add(content);

            // ==================== INITIAL STATE ====================
            if (initialUrl == GetDefaultHomepageUrl() || string.IsNullOrEmpty(initialUrl))
            {
                searchRow2.Visible = true;
                bookmarksBar.Visible = true;
                browserContainer.Padding = new Padding(0, 290, 0, 0);
                UpdateBookmarksBar(bookmarksBar);
                searchBox2.Focus();
            }
            else
            {
                searchRow2.Visible = false;
                bookmarksBar.Visible = false;
                browserContainer.Padding = new Padding(0, 0, 0, 0);
                searchBox1.Text = initialUrl;
                _ = Task.Run(async () => await LazyLoadBrowser(initialUrl, meta));
            }

            // Show this tab
            foreach (Control c in contentArea.Controls) c.Visible = false;
            content.Visible = true;
            currentTabContent = content;
            currentTabButtonPanel = header;
            HighlightActiveTab(header);

            ReflowTabButtons();
        }

        private void DoSearch1(string input, TabMeta meta)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            string url = FormatUrl(input);
            bool isFromNewTab = (meta.InitialUrl == GetDefaultHomepageUrl() ||
                                string.IsNullOrEmpty(meta.InitialUrl)) &&
                               meta.BrowserEngine == null;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    if (isFromNewTab)
                    {
                        NuclearHideUIAndFixLayout(meta);
                    }
                    NavigateToUrl(url, meta, isFromNewTab);
                }));
            }
            else
            {
                if (isFromNewTab)
                {
                    NuclearHideUIAndFixLayout(meta);
                }
                NavigateToUrl(url, meta, isFromNewTab);
            }
        }

        private void DoSearch2(string input, TabMeta meta)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            string url = BuildSearchUrl(input);

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    NuclearHideUIAndFixLayout(meta);
                    System.Threading.Thread.Sleep(100);
                    NavigateFromNewTab(url, meta);
                }));
            }
            else
            {
                NuclearHideUIAndFixLayout(meta);
                System.Threading.Thread.Sleep(100);
                NavigateFromNewTab(url, meta);
            }
        }

        private void NavigateFromNewTab(string url, TabMeta meta)
        {
            meta.InitialUrl = url;
            meta.CurrentUrl = url;

            if (meta.SearchBox1 != null)
            {
                meta.SearchBox1.Text = url;
            }

            if (meta.SearchBox1 != null && meta.SearchBox1.CanFocus)
            {
                meta.SearchBox1.Focus();
                meta.SearchBox1.SelectAll();
            }

            if (meta.BrowserEngine != null)
            {
                meta.BrowserEngine.Navigate(url);
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(150);

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(async () =>
                        {
                            await CreateAndInitializeBrowser(url, meta);
                        }));
                    }
                    else
                    {
                        await CreateAndInitializeBrowser(url, meta);
                    }
                });
            }
        }

        private void NavigateToUrl(string url, TabMeta meta, bool isFromNewTab = false)
        {
            if (meta.BrowserEngine != null)
            {
                meta.BrowserEngine.Navigate(url);
            }
            else
            {
                meta.InitialUrl = url;
                meta.CurrentUrl = url;
                meta.SearchBox1.Text = url;
                _ = Task.Run(async () => await LazyLoadBrowser(url, meta, isFromNewTab));
            }
        }

        private async Task LazyLoadBrowser(string url, TabMeta meta, bool isFromNewTabSearch = false)
        {
            if (meta.BrowserEngine != null) return;

            if (isFromNewTabSearch)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => NuclearHideUIAndFixLayout(meta)));
                }
                else
                {
                    NuclearHideUIAndFixLayout(meta);
                }
            }

            if (IsPdfFile(url))
            {
                string pdfFilePath = GetPdfFilePathFromUrl(url);
                if (File.Exists(pdfFilePath))
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => ShowPdfViewer(pdfFilePath, meta)));
                    }
                    else
                    {
                        ShowPdfViewer(pdfFilePath, meta);
                    }
                    return;
                }
            }

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(async () =>
                {
                    await CreateAndInitializeBrowser(url, meta);
                }));
            }
            else
            {
                await CreateAndInitializeBrowser(url, meta);
            }
        }

        private async Task CreateAndInitializeBrowser(string url, TabMeta meta)
        {
            if (meta.BrowserContainer != null)
            {
                meta.BrowserContainer.Padding = new Padding(0, 40, 0, 0);
                meta.BrowserContainer.PerformLayout();
            }

            var novaCore = new NovaCoreEngine();
            meta.BrowserEngine = novaCore;

            if (meta.BrowserContainer != null)
            {
                meta.BrowserContainer.Controls.Clear();
                meta.BrowserContainer.Controls.Add(novaCore);
                novaCore.Dock = DockStyle.Fill;
                meta.BrowserContainer.SuspendLayout();
                meta.BrowserContainer.ResumeLayout(true);
                meta.BrowserContainer.PerformLayout();
            }

            // Configurar eventos
            novaCore.PageLoaded += (s, loadedUrl) =>
            {
                meta.CurrentUrl = loadedUrl;
                if (meta.SearchBox1 != null)
                {
                    meta.SearchBox1.Text = loadedUrl;
                }

                // Actualizar título
                string title = novaCore.GetTitle() ?? "NexNova";
                UpdateTabTitle(meta, title);

                // Agregar a historial
                if (!meta.IsPrivate && !history.Contains(loadedUrl))
                {
                    history.Add(loadedUrl);
                    AddToFrequentTabs(loadedUrl);
                }

                UpdateStatus($"Navigated to {new Uri(loadedUrl).Host}");
            };

            novaCore.TitleChanged += (s, title) =>
            {
                UpdateTabTitle(meta, title);
            };

            // Navegar a la URL
            await novaCore.Navigate(url);

            if (meta.BrowserContainer?.Parent?.Parent is Panel tabContent)
            {
                tabContent.Invalidate(true);
                tabContent.Update();
                Application.DoEvents();
            }
        }

        private void UpdateTabTitle(TabMeta meta, string title)
        {
            if (meta.TitleButton != null)
            {
                string privateIcon = meta.IsPrivate ? ICON_PRIVATE + " " : "";
                string securityIcon = meta.CurrentUrl?.StartsWith("https") == true ? ICON_SECURE : ICON_INSECURE;
                string finalTitle = privateIcon + securityIcon + " " +
                                  (title.Length > 20 ? title.Substring(0, 20) + "…" : title);

                if (meta.TitleButton.InvokeRequired)
                {
                    meta.TitleButton.Invoke(new Action(() =>
                    {
                        meta.TitleButton.Text = finalTitle;
                    }));
                }
                else
                {
                    meta.TitleButton.Text = finalTitle;
                }
            }
        }

        private void NuclearHideUIAndFixLayout(TabMeta meta)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => NuclearHideUIAndFixLayout(meta)));
                return;
            }

            try
            {
                Panel contentPanel = null;
                foreach (Control c in contentArea.Controls)
                {
                    if (c.Tag == meta)
                    {
                        contentPanel = c as Panel;
                        break;
                    }
                }

                if (contentPanel == null) return;

                Panel barra1 = null;
                Panel browserContainer = meta.BrowserContainer;
                List<Control> controlsToRemove = new List<Control>();

                foreach (Control c in contentPanel.Controls)
                {
                    if (c is Panel panel1 && panel1.Controls.Contains(meta.SearchBox1))
                    {
                        barra1 = panel1;
                    }
                    else if (c == browserContainer)
                    {
                        // Keep it
                    }
                    else if (c is Panel panel2 &&
                            (panel2.Controls.Contains(meta.SearchBox2) || panel2 == meta.BookmarksBar))
                    {
                        controlsToRemove.Add(panel2);
                    }
                    else if (c is Panel)
                    {
                        controlsToRemove.Add(c);
                    }
                }

                foreach (var c in controlsToRemove)
                {
                    contentPanel.Controls.Remove(c);
                    c.Dispose();
                }

                if (barra1 == null && meta.SearchBox1 != null)
                {
                    barra1 = CreateBarra1(meta);
                    if (barra1 != null)
                    {
                        contentPanel.Controls.Add(barra1);
                        barra1.BringToFront();
                    }
                }

                contentPanel.BackColor = darkTheme ? Color.FromArgb(8, 8, 8) : Color.White;

                if (barra1 != null)
                {
                    barra1.BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.Gainsboro;
                }

                if (browserContainer == null)
                {
                    browserContainer = new Panel
                    {
                        Name = "BrowserContainer_" + meta.GetHashCode(),
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0, 40, 0, 0),
                        BackColor = darkTheme ? Color.FromArgb(8, 8, 8) : Color.White,
                        Size = new Size(contentPanel.Width, contentPanel.Height),
                        Location = new Point(0, 0),
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                AnchorStyles.Left | AnchorStyles.Right,
                        MinimumSize = new Size(100, 100)
                    };
                    meta.BrowserContainer = browserContainer;
                    contentPanel.Controls.Add(browserContainer);
                }
                else
                {
                    browserContainer.Padding = new Padding(0, 40, 0, 0);
                    browserContainer.Dock = DockStyle.Fill;
                    browserContainer.Size = new Size(contentPanel.Width, contentPanel.Height);
                    browserContainer.Location = new Point(0, 0);
                    browserContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                            AnchorStyles.Left | AnchorStyles.Right;
                    browserContainer.BringToFront();
                }

                if (browserContainer != null)
                {
                    Application.DoEvents();
                    browserContainer.Width = contentPanel.ClientSize.Width;
                    browserContainer.Height = contentPanel.ClientSize.Height;

                    if (meta.BrowserEngine is Control browserControl && browserContainer.Controls.Contains(browserControl))
                    {
                        browserControl.Width = browserContainer.ClientSize.Width;
                        browserControl.Height = browserContainer.ClientSize.Height - 40;
                        browserControl.Location = new Point(0, 40);
                        browserControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                             AnchorStyles.Left | AnchorStyles.Right;
                    }
                }

                if (barra1 != null)
                {
                    barra1.Dock = DockStyle.Top;
                    barra1.Height = 40;
                    barra1.BringToFront();
                }

                if (barra1 != null) barra1.BringToFront();
                if (browserContainer != null) browserContainer.SendToBack();

                contentPanel.SuspendLayout();
                contentPanel.ResumeLayout(true);
                contentPanel.PerformLayout();

                contentPanel.Refresh();
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);

                if (meta.SearchBox1 != null && !string.IsNullOrEmpty(meta.InitialUrl))
                {
                    meta.SearchBox1.Text = meta.InitialUrl;
                    meta.SearchBox1.Focus();
                    meta.SearchBox1.SelectAll();
                }

                meta.BookmarksBar = null;
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "NuclearHideUIAndFixLayout");
            }
        }

        private Panel CreateBarra1(TabMeta meta)
        {
            try
            {
                var searchBox1 = new TextBox
                {
                    Height = 28,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    PlaceholderText = "Search or enter address...",
                    ForeColor = darkTheme ? Color.White : Color.Black,
                    BackColor = darkTheme ? Color.FromArgb(60, 66, 94) : Color.White,
                    BorderStyle = BorderStyle.None,
                    Dock = DockStyle.Fill,
                    Text = meta.InitialUrl ?? ""
                };

                var searchBtn1 = new Button
                {
                    Text = ICON_SEARCH,
                    Width = 40,
                    Height = 28,
                    BackColor = accentColor,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10),
                    Dock = DockStyle.Right
                };
                searchBtn1.FlatAppearance.BorderSize = 0;

                var searchRow1 = new Panel
                {
                    Height = 40,
                    BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.Gainsboro,
                    Padding = new Padding(10, 6, 10, 6),
                    Dock = DockStyle.Top
                };

                searchRow1.Controls.Add(searchBox1);
                searchRow1.Controls.Add(searchBtn1);

                meta.SearchBox1 = searchBox1;

                searchBtn1.Click += (s, e) => DoSearch1(searchBox1.Text, meta);
                searchBox1.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        DoSearch1(searchBox1.Text, meta);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                };

                return searchRow1;
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "CreateBarra1");
                return null;
            }
        }

        private void RestoreNewTabUI(TabMeta meta)
        {
            try
            {
                if (meta.BrowserContainer?.Parent is Panel contentPanel)
                {
                    contentPanel.BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.FromArgb(240, 245, 255);
                }

                var searchRow2 = meta.SearchBox2?.Parent?.Parent as Panel;
                if (searchRow2 != null)
                {
                    searchRow2.Visible = true;
                    searchRow2.Height = 150;
                    searchRow2.Padding = new Padding(100, 40, 100, 0);
                    searchRow2.BackColor = Color.Transparent;
                }

                if (meta.BookmarksBar != null)
                {
                    meta.BookmarksBar.Visible = true;
                    meta.BookmarksBar.Height = 100;
                    meta.BookmarksBar.Padding = new Padding(50, 20, 50, 0);
                    meta.BookmarksBar.BackColor = Color.Transparent;
                    UpdateBookmarksBar(meta.BookmarksBar);
                }

                var searchRow1 = meta.SearchBox1?.Parent as Panel;
                if (searchRow1 != null)
                {
                    searchRow1.BackColor = Color.Transparent;
                }

                if (meta.BrowserContainer != null)
                {
                    meta.BrowserContainer.Padding = new Padding(0, 290, 0, 0);

                    if (meta.BrowserEngine is Control browserControl)
                    {
                        browserControl.Location = new Point(0, 290);
                        browserControl.Size = new Size(meta.BrowserContainer.Width,
                                                   meta.BrowserContainer.Height - 290);
                    }
                }

                if (meta.SearchBox2 != null && meta.SearchBox2.CanFocus)
                {
                    meta.SearchBox2.Focus();
                    meta.SearchBox2.SelectAll();
                }

                if (meta.SearchBox1 != null)
                {
                    meta.SearchBox1.Text = "";
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "RestoreNewTabUI");
            }
        }

        private void UpdateBookmarksBar(Panel bookmarksBarContainer)
        {
            bookmarksBarContainer.Controls.Clear();

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            int buttonWidth = 120;
            int buttonHeight = 40;
            int spacing = 10;

            foreach (var bookmark in bookmarks.Take(10))
            {
                var bookmarkBtn = new Button
                {
                    Text = bookmark.Title.Length > 15 ? bookmark.Title.Substring(0, 15) + "…" : bookmark.Title,
                    Size = new Size(buttonWidth, buttonHeight),
                    BackColor = darkTheme ? Color.FromArgb(60, 66, 94) : accentColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9),
                    Tag = bookmark.Url,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, spacing, 0)
                };
                bookmarkBtn.FlatAppearance.BorderSize = 0;

                var toolTip = new ToolTip();
                toolTip.SetToolTip(bookmarkBtn, $"{bookmark.Title}\n{bookmark.Url}");

                bookmarkBtn.Click += (s, e) =>
                {
                    if (currentTabContent?.Tag is TabMeta meta)
                    {
                        string url = (s as Button)?.Tag?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(url))
                        {
                            var searchRow2 = meta.SearchBox2?.Parent?.Parent as Panel;
                            if (searchRow2 != null) searchRow2.Visible = false;
                            if (meta.BookmarksBar != null) meta.BookmarksBar.Visible = false;
                            if (meta.BrowserContainer != null)
                                meta.BrowserContainer.Padding = new Padding(0, 40, 0, 0);

                            if (meta.BrowserEngine != null)
                            {
                                meta.BrowserEngine.Navigate(url);
                            }
                            else
                            {
                                meta.InitialUrl = url;
                                meta.CurrentUrl = url;
                                meta.SearchBox1.Text = url;
                                _ = Task.Run(async () => await LazyLoadBrowser(url, meta));
                            }
                        }
                    }
                };

                flowPanel.Controls.Add(bookmarkBtn);
            }

            if (bookmarks.Count > 10)
            {
                var showAllBtn = new Button
                {
                    Text = "Show All...",
                    Size = new Size(buttonWidth, buttonHeight),
                    BackColor = Color.FromArgb(70, 130, 180),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, spacing, 0)
                };
                showAllBtn.FlatAppearance.BorderSize = 0;
                showAllBtn.Click += (s, e) => ShowOverlay("Bookmarks");

                flowPanel.Controls.Add(showAllBtn);
            }

            bookmarksBarContainer.Controls.Add(flowPanel);
        }
        #endregion

        #region Tab Operations
        private void ReflowTabButtons()
        {
            if (tabBar == null) return;

            int availableWidth = tabBar.Width - 60;
            if (menuButton != null)
                availableWidth -= (menuButton.Width + 16);

            int count = tabBar.Controls.Count - 1;
            if (count <= 0) return;

            int maxW = 180;
            int minW = 48;
            int w = Math.Max(minW, Math.Min(maxW, availableWidth / count));

            int x = 8;
            foreach (Control c in tabBar.Controls)
            {
                if (c == newTabBtn)
                {
                    c.Location = new Point(tabBar.Width - c.Width - 8, 8);
                    continue;
                }

                c.Width = w;
                c.Location = new Point(x, 8);
                x += w + 5;
            }
        }

        private void HighlightActiveTab(Panel active)
        {
            if (tabBar == null) return;

            foreach (Control c in tabBar.Controls)
            {
                if (c is Panel p)
                {
                    p.BackColor = darkTheme ? Color.FromArgb(46, 51, 73) : Color.LightSteelBlue;
                    foreach (Control ch in p.Controls)
                    {
                        if (ch is Button b && b.Text != ICON_CLOSE)
                            b.BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue;
                    }
                }
            }

            if (active != null)
            {
                active.BackColor = darkTheme ? Color.FromArgb(60, 66, 94) : Color.SteelBlue;
                foreach (Control ch in active.Controls)
                {
                    if (ch is Button b && b.Text != ICON_CLOSE)
                        b.BackColor = accentColor;
                }
            }
        }

        private void CloseTabImmediate(Panel header)
        {
            if (header == null) return;

            Panel matchedContent = null;
            TabMeta matchedMeta = null;

            foreach (Control c in contentArea.Controls)
            {
                if (c is Panel p && p.Tag is TabMeta tm && tm.TitleButton != null)
                {
                    if (tm.TitleButton.Parent == header)
                    {
                        matchedContent = p;
                        matchedMeta = tm;
                        break;
                    }
                }
            }

            if (matchedContent == null)
            {
                int idx = -1;
                for (int i = 0; i < tabBar.Controls.Count; i++)
                    if (tabBar.Controls[i] == header)
                    {
                        idx = i;
                        break;
                    }

                if (idx >= 0 && idx < contentArea.Controls.Count)
                {
                    matchedContent = contentArea.Controls[idx] as Panel;
                    matchedMeta = matchedContent?.Tag as TabMeta;
                }
            }

            if (matchedMeta != null)
            {
                var tabState = new TabState
                {
                    Url = matchedMeta.CurrentUrl ?? matchedMeta.InitialUrl ?? GetDefaultHomepageUrl(),
                    IsPrivate = matchedMeta.IsPrivate
                };
                closedTabsHistory.Push(tabState);
            }

            if (tabBar.Controls.Contains(header))
                tabBar.Controls.Remove(header);

            try { header.Dispose(); } catch { }

            if (matchedContent != null && contentArea.Controls.Contains(matchedContent))
            {
                if (matchedMeta != null && !matchedMeta.IsPdfViewer)
                {
                    try
                    {
                        (matchedMeta.BrowserEngine as IDisposable)?.Dispose();
                    }
                    catch { }
                }
                contentArea.Controls.Remove(matchedContent);
                try { matchedContent.Dispose(); } catch { }
            }

            if (tabBar.Controls.Count == 1)
            {
                Application.Exit();
                return;
            }

            if (matchedContent == currentTabContent || currentTabContent == null)
            {
                var lastContent = contentArea.Controls.Cast<Control>().LastOrDefault();
                if (lastContent != null)
                    lastContent.Visible = true;

                currentTabContent = lastContent as Panel;

                var lastHeader = tabBar.Controls.Cast<Control>().Where(c => c != newTabBtn).LastOrDefault() as Panel;
                currentTabButtonPanel = lastHeader;
                HighlightActiveTab(lastHeader);
            }

            ReflowTabButtons();
        }

        private void RestoreLastClosedTab()
        {
            if (closedTabsHistory.Count > 0)
            {
                var lastTab = closedTabsHistory.Pop();
                AddNewTab(lastTab.Url, lastTab.IsPrivate);
                UpdateStatus($"Restored tab: {lastTab.Url}");
            }
            else
            {
                UpdateStatus("No tabs to restore");
            }
        }

        private void SwitchToTab(int tabIndex)
        {
            if (tabBar == null || tabBar.Controls.Count <= 1) return;

            int totalTabs = tabBar.Controls.Count - 1;
            if (tabIndex < 0 || tabIndex >= totalTabs) return;

            int currentIndex = 0;
            foreach (Control c in tabBar.Controls)
            {
                if (c == newTabBtn) continue;

                if (currentIndex == tabIndex)
                {
                    if (c is Panel tabPanel)
                    {
                        foreach (Control child in tabPanel.Controls)
                        {
                            if (child is Button btn && btn.Text != ICON_CLOSE)
                            {
                                btn.PerformClick();
                                return;
                            }
                        }
                    }
                }
                currentIndex++;
            }
        }

        private void SwitchToLastTab()
        {
            if (tabBar == null || tabBar.Controls.Count <= 1) return;

            Panel lastTabPanel = null;
            foreach (Control c in tabBar.Controls)
            {
                if (c != newTabBtn && c is Panel panel)
                {
                    lastTabPanel = panel;
                }
            }

            if (lastTabPanel != null)
            {
                foreach (Control child in lastTabPanel.Controls)
                {
                    if (child is Button btn && btn.Text != ICON_CLOSE)
                    {
                        btn.PerformClick();
                        return;
                    }
                }
            }
        }
        #endregion

        #region URL Processing
        private string FormatUrl(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            input = input.Trim();

            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
            {
                return input;
            }

            if (input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            if (File.Exists(input))
            {
                return "file:///" + Path.GetFullPath(input).Replace("\\", "/");
            }

            if (input.StartsWith("nex://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            if (input.Contains(".") && !input.Contains(" "))
            {
                if (!input.StartsWith("http://") && !input.StartsWith("https://"))
                {
                    if (!input.StartsWith("www."))
                    {
                        return "https://www." + input;
                    }
                    else
                    {
                        return "https://" + input;
                    }
                }
                return input;
            }

            return BuildSearchUrl(input);
        }

        private string BuildSearchUrl(string query)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            return defaultSearchEngine switch
            {
                "DuckDuckGo" => $"https://duckduckgo.com/?q={encodedQuery}",
                "Blyx" => $"https://localhost:3000/search?q={encodedQuery}",
                _ => $"https://www.google.com/search?q={encodedQuery}",
            };
        }

        private string GetDefaultHomepageUrl()
        {
            return defaultSearchEngine switch
            {
                "DuckDuckGo" => "https://duckduckgo.com",
                "Blyx" => "https://localhost:3000",
                _ => "https://www.google.com"
            };
        }
        #endregion

        #region SafeSearch
        private void InitializeHierarchicalContextAwareMultiLayeredContentSafetyEnforcementProtocol(IBrowserEngine browser)
        {
            // Esta funcionalidad se implementará en NovaSecurity.cs
        }
        #endregion

        #region PDF Viewer
        private void ShowPdfViewer(string pdfFilePath, TabMeta currentMeta = null)
        {
            if (!File.Exists(pdfFilePath))
            {
                MessageBox.Show("PDF file not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (currentMeta != null && currentMeta.BrowserEngine == null && string.IsNullOrEmpty(currentMeta.InitialUrl))
            {
                ShowPdfInTab(currentMeta, pdfFilePath);
                return;
            }

            tabCount++;

            var header = new Panel
            {
                Width = 180,
                Height = 36,
                BackColor = darkTheme ? Color.FromArgb(46, 51, 73) : Color.LightSteelBlue,
                Padding = new Padding(6),
                Margin = new Padding(5),
                ContextMenuStrip = tabContextMenuStrip
            };

            var titleBtn = new Button
            {
                Text = ICON_PDF + " " + Path.GetFileName(pdfFilePath),
                Dock = DockStyle.Fill,
                BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue,
                ForeColor = darkTheme ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleBtn.FlatAppearance.BorderSize = 0;

            var closeBtn = new Button
            {
                Text = ICON_CLOSE,
                Dock = DockStyle.Right,
                Width = 30,
                BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            closeBtn.FlatAppearance.BorderSize = 0;

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.FromArgb(240, 245, 255),
                Visible = false
            };

            var pdfPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var pdfTitle = new Label
            {
                Text = Path.GetFileName(pdfFilePath),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = darkTheme ? Color.White : Color.Black,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pdfInfo = new Label
            {
                Text = $"Size: {new FileInfo(pdfFilePath).Length / 1024} KB | Created: {File.GetCreationTime(pdfFilePath):yyyy-MM-dd}",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = darkTheme ? Color.LightGray : Color.DarkGray,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var openWithDefaultBtn = new Button
            {
                Text = "Open with System PDF Viewer",
                Height = 35,
                Dock = DockStyle.Bottom,
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openWithDefaultBtn.FlatAppearance.BorderSize = 0;

            var pdfContent = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                ForeColor = darkTheme ? Color.White : Color.Black,
                ReadOnly = true,
                Text = GetPdfPreviewText(pdfFilePath)
            };

            openWithDefaultBtn.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex.Message, "OpenPDFWithDefault");
                    MessageBox.Show($"Failed to open PDF: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pdfPanel.Controls.Add(pdfContent);
            pdfPanel.Controls.Add(openWithDefaultBtn);
            pdfPanel.Controls.Add(pdfInfo);
            pdfPanel.Controls.Add(pdfTitle);

            content.Controls.Add(pdfPanel);

            var meta = new TabMeta
            {
                BrowserEngine = null,
                IsPrivate = false,
                SearchBox1 = null,
                SearchBox2 = null,
                TitleButton = titleBtn,
                InitialUrl = $"file:///{pdfFilePath}",
                CurrentUrl = $"file:///{pdfFilePath}",
                IsPdfViewer = true,
                PdfFilePath = pdfFilePath,
                BookmarksBar = null
            };
            content.Tag = meta;

            titleBtn.Click += (s, e) =>
            {
                CloseOverlay();
                foreach (Control c in contentArea.Controls) c.Visible = false;
                content.Visible = true;
                currentTabContent = content;
                currentTabButtonPanel = header;
                HighlightActiveTab(header);

                if (meta.InitialUrl == GetDefaultHomepageUrl() || string.IsNullOrEmpty(meta.InitialUrl))
                {
                    RestoreNewTabBackground(meta, content);
                }
            };

            closeBtn.Click += (s, e) => CloseTabImmediate(header);

            header.Controls.Add(titleBtn);
            header.Controls.Add(closeBtn);
            tabBar.Controls.Add(header);
            contentArea.Controls.Add(content);

            foreach (Control c in contentArea.Controls) c.Visible = false;
            content.Visible = true;
            currentTabContent = content;
            currentTabButtonPanel = header;
            HighlightActiveTab(header);

            ReflowTabButtons();
        }

        private void ShowPdfInTab(TabMeta meta, string pdfFilePath)
        {
            meta.IsPdfViewer = true;
            meta.PdfFilePath = pdfFilePath;
            meta.InitialUrl = $"file:///{pdfFilePath}";
            meta.CurrentUrl = $"file:///{pdfFilePath}";

            meta.BrowserContainer.Controls.Clear();

            var pdfPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var pdfTitle = new Label
            {
                Text = Path.GetFileName(pdfFilePath),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = darkTheme ? Color.White : Color.Black,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var pdfInfo = new Label
            {
                Text = $"Size: {new FileInfo(pdfFilePath).Length / 1024} KB | Created: {File.GetCreationTime(pdfFilePath):yyyy-MM-dd}",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = darkTheme ? Color.LightGray : Color.DarkGray,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var openWithDefaultBtn = new Button
            {
                Text = "Open with System PDF Viewer",
                Height = 35,
                Dock = DockStyle.Bottom,
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openWithDefaultBtn.FlatAppearance.BorderSize = 0;

            var pdfContent = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                ForeColor = darkTheme ? Color.White : Color.Black,
                ReadOnly = true,
                Text = GetPdfPreviewText(pdfFilePath)
            };

            openWithDefaultBtn.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex.Message, "OpenPDFWithDefault");
                    MessageBox.Show($"Failed to open PDF: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pdfPanel.Controls.Add(pdfContent);
            pdfPanel.Controls.Add(openWithDefaultBtn);
            pdfPanel.Controls.Add(pdfInfo);
            pdfPanel.Controls.Add(pdfTitle);

            meta.BrowserContainer.Controls.Add(pdfPanel);

            if (meta.TitleButton != null)
            {
                meta.TitleButton.Text = ICON_PDF + " " + Path.GetFileName(pdfFilePath);
            }

            if (meta.SearchBox1 != null && meta.SearchBox1.Parent != null)
                meta.SearchBox1.Parent.Visible = false;
            if (meta.SearchBox2 != null && meta.SearchBox2.Parent != null)
                meta.SearchBox2.Parent.Parent.Visible = false;
            if (meta.BookmarksBar != null)
                meta.BookmarksBar.Visible = false;

            meta.BrowserContainer.Padding = new Padding(0);
        }

        private string GetPdfPreviewText(string pdfFilePath)
        {
            try
            {
                byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);
                string pdfText = System.Text.Encoding.UTF8.GetString(pdfBytes);

                if (pdfText.Contains("%PDF-"))
                {
                    return $"PDF File: {Path.GetFileName(pdfFilePath)}\n\n" +
                           $"This is a PDF document. For full viewing capabilities:\n" +
                           $"1. Use the 'Open with System PDF Viewer' button below\n" +
                           $"2. Or install a proper PDF library in NexNova\n\n" +
                           $"File path: {pdfFilePath}\n" +
                           $"File size: {new FileInfo(pdfFilePath).Length} bytes\n" +
                           $"PDF Version: {GetPdfVersion(pdfText)}";
                }
                else
                {
                    return "This doesn't appear to be a valid PDF file.";
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "GetPdfPreviewText");
                return $"Error reading PDF file: {ex.Message}";
            }
        }

        private string GetPdfVersion(string pdfText)
        {
            try
            {
                int pdfIndex = pdfText.IndexOf("%PDF-");
                if (pdfIndex >= 0)
                {
                    int endIndex = pdfText.IndexOf("\n", pdfIndex);
                    if (endIndex > pdfIndex)
                    {
                        return pdfText.Substring(pdfIndex, endIndex - pdfIndex).Trim();
                    }
                }
            }
            catch (Exception ex) { LogError(ex.Message, "GetPdfVersion"); }
            return "Unknown";
        }

        private bool IsPdfFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            string lowerUrl = url.ToLower();
            if (lowerUrl.EndsWith(".pdf")) return true;
            if (lowerUrl.Contains(".pdf?")) return true;
            if (lowerUrl.StartsWith("file://") && lowerUrl.Contains(".pdf")) return true;

            return false;
        }

        private string GetPdfFilePathFromUrl(string url)
        {
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = url.Substring(7).Replace("/", "\\");
                return Uri.UnescapeDataString(filePath);
            }
            return url;
        }

        private void RestoreNewTabBackground(TabMeta meta, Panel content)
        {
            content.BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.FromArgb(240, 245, 255);

            var searchRow2 = meta.SearchBox2?.Parent?.Parent as Panel;
            if (searchRow2 != null)
            {
                searchRow2.Visible = true;
                searchRow2.BackColor = Color.Transparent;
            }

            if (meta.BookmarksBar != null)
            {
                meta.BookmarksBar.Visible = true;
                meta.BookmarksBar.BackColor = Color.Transparent;
            }

            if (meta.BrowserContainer != null)
                meta.BrowserContainer.Padding = new Padding(0, 290, 0, 0);

            var searchRow1 = meta.SearchBox1?.Parent as Panel;
            if (searchRow1 != null)
            {
                searchRow1.BackColor = Color.Transparent;
            }

            if (meta.SearchBox2 != null)
                meta.SearchBox2.Focus();
        }
        #endregion

        #region Theme and UI
        private void ApplyThemeToChrome()
        {
            BackColor = darkTheme ? Color.FromArgb(12, 12, 12) : Color.WhiteSmoke;

            if (tabBar != null)
                tabBar.BackColor = darkTheme ? Color.FromArgb(28, 31, 44) : Color.Gainsboro;

            if (contentArea != null)
                contentArea.BackColor = darkTheme ? Color.FromArgb(8, 8, 8) : Color.White;

            if (newTabBtn != null)
                newTabBtn.BackColor = darkTheme ? Color.FromArgb(60, 66, 94) : Color.SteelBlue;

            if (menuButton != null)
                menuButton.BackColor = accentColor;

            if (tabBar != null)
            {
                foreach (Control c in tabBar.Controls)
                {
                    if (c is Panel p)
                    {
                        p.BackColor = darkTheme ? Color.FromArgb(46, 51, 73) : Color.LightSteelBlue;
                        foreach (Control ch in p.Controls)
                        {
                            if (ch is Button b)
                            {
                                b.BackColor = darkTheme ? Color.FromArgb(66, 72, 102) : Color.LightSteelBlue;
                                b.ForeColor = darkTheme ? Color.White : Color.Black;
                            }
                        }
                    }
                }
            }
        }

        private void MenuButton_Click(object sender, EventArgs e)
        {
            if (menuButton?.ContextMenuStrip != null)
            {
                var cms = menuButton.ContextMenuStrip;
                cms.Show(menuButton, new Point(menuButton.Width - cms.Width, menuButton.Height));
            }
        }
        #endregion

        #region Overlays
        private void ShowOverlay(string title)
        {
            CloseOverlay();

            var backdrop = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(160, 0, 0, 0)
            };
            Controls.Add(backdrop);
            backdrop.BringToFront();
            currentBackdrop = backdrop;

            var overlay = new Panel
            {
                Width = (int)(ClientSize.Width * 0.9),
                Height = (int)(ClientSize.Height * 0.9),
                BackColor = darkTheme ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 247, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            overlay.Left = (ClientSize.Width - overlay.Width) / 2;
            overlay.Top = (ClientSize.Height - overlay.Height) / 2;
            backdrop.Controls.Add(overlay);

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = darkTheme ? Color.White : Color.FromArgb(40, 44, 60),
                AutoSize = true,
                Location = new Point(24, 24)
            };
            overlay.Controls.Add(titleLabel);

            int contentX = 24;
            int contentY = titleLabel.Bottom + 16;
            int contentW = overlay.Width - 48;
            int contentH = overlay.Height - contentY - 24;

            if (title == "History")
            {
                var list = new ListBox
                {
                    Location = new Point(contentX, contentY),
                    Size = new Size(contentW, contentH - 40),
                    Font = new Font("Segoe UI", 10),
                    BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                for (int i = history.Count - 1; i >= 0; i--)
                    list.Items.Add(history[i]);

                overlay.Controls.Add(list);

                list.DoubleClick += (s, e) =>
                {
                    if (list.SelectedItem != null)
                        AddNewTab(list.SelectedItem.ToString());
                    CloseOverlay();
                };

                var clearBtn = new Button
                {
                    Text = "Clear History" + " " + ICON_CLOSE,
                    Width = 140,
                    Height = 36,
                    BackColor = Color.FromArgb(230, 65, 65),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(overlay.Width - 164, 24)
                };
                clearBtn.FlatAppearance.BorderSize = 0;
                overlay.Controls.Add(clearBtn);

                clearBtn.Click += (s, e) =>
                {
                    history.Clear();
                    list.Items.Clear();
                    SaveSettings();
                };
            }
            else if (title == "Favorites")
            {
                var list = new ListBox
                {
                    Location = new Point(contentX, contentY),
                    Size = new Size(contentW, contentH),
                    Font = new Font("Segoe UI", 10),
                    BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                foreach (var f in favorites)
                    list.Items.Add(f);

                overlay.Controls.Add(list);

                list.DoubleClick += (s, e) =>
                {
                    if (list.SelectedItem != null)
                        AddNewTab(list.SelectedItem.ToString());
                    CloseOverlay();
                };

                var removeBtn = new Button
                {
                    Text = "Remove Selected",
                    Width = 140,
                    Height = 36,
                    BackColor = Color.FromArgb(230, 65, 65),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(overlay.Width - 164, 24)
                };
                removeBtn.FlatAppearance.BorderSize = 0;
                overlay.Controls.Add(removeBtn);

                removeBtn.Click += (s, e) =>
                {
                    if (list.SelectedItem != null)
                    {
                        favorites.Remove(list.SelectedItem.ToString());
                        list.Items.Remove(list.SelectedItem);
                        SaveFavorites();
                    }
                };
            }
            else if (title == "Bookmarks")
            {
                var list = new ListBox
                {
                    Location = new Point(contentX, contentY),
                    Size = new Size(contentW, contentH - 40),
                    Font = new Font("Segoe UI", 10),
                    BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                foreach (var b in bookmarks)
                    list.Items.Add($"{b.Title} - {b.Url}");

                overlay.Controls.Add(list);

                list.DoubleClick += (s, e) =>
                {
                    if (list.SelectedIndex >= 0)
                        AddNewTab(bookmarks[list.SelectedIndex].Url);
                    CloseOverlay();
                };

                var removeBtn = new Button
                {
                    Text = "Remove Selected",
                    Width = 140,
                    Height = 36,
                    BackColor = Color.FromArgb(230, 65, 65),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(overlay.Width - 164, 24)
                };
                removeBtn.FlatAppearance.BorderSize = 0;
                overlay.Controls.Add(removeBtn);

                removeBtn.Click += (s, e) =>
                {
                    if (list.SelectedIndex >= 0)
                    {
                        bookmarks.RemoveAt(list.SelectedIndex);
                        list.Items.RemoveAt(list.SelectedIndex);
                        SaveBookmarks();

                        if (currentTabContent?.Tag is TabMeta meta && meta.BookmarksBar != null)
                        {
                            UpdateBookmarksBar(meta.BookmarksBar);
                        }
                    }
                };
            }
            else if (title == "Settings")
            {
                int currentY = contentY;

                // Language Group
                var langGroup = new GroupBox
                {
                    Text = "Language",
                    Location = new Point(contentX, currentY),
                    Size = new Size(contentW, 80),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = darkTheme ? Color.White : Color.Black,
                    BackColor = darkTheme ? Color.FromArgb(40, 44, 60) : Color.WhiteSmoke
                };

                var rbEnglish = new RadioButton
                {
                    Text = "English",
                    Location = new Point(16, 25),
                    AutoSize = true,
                    Checked = language == "en",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbSpanish = new RadioButton
                {
                    Text = "Spanish",
                    Location = new Point(120, 25),
                    AutoSize = true,
                    Checked = language == "es",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbFrench = new RadioButton
                {
                    Text = "French",
                    Location = new Point(240, 25),
                    AutoSize = true,
                    Checked = language == "fr",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbGerman = new RadioButton
                {
                    Text = "German",
                    Location = new Point(360, 25),
                    AutoSize = true,
                    Checked = language == "de",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                langGroup.Controls.Add(rbEnglish);
                langGroup.Controls.Add(rbSpanish);
                langGroup.Controls.Add(rbFrench);
                langGroup.Controls.Add(rbGerman);
                overlay.Controls.Add(langGroup);

                currentY += langGroup.Height + 15;

                // Search Engine Group
                var engineGroup = new GroupBox
                {
                    Text = "Default Search Engine",
                    Location = new Point(contentX, currentY),
                    Size = new Size(contentW, 90),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = darkTheme ? Color.White : Color.Black,
                    BackColor = darkTheme ? Color.FromArgb(40, 44, 60) : Color.WhiteSmoke
                };

                var rbGoogle = new RadioButton
                {
                    Text = "Google",
                    Location = new Point(16, 22),
                    AutoSize = true,
                    Checked = defaultSearchEngine == "Google",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbDuck = new RadioButton
                {
                    Text = "DuckDuckGo",
                    Location = new Point(120, 22),
                    AutoSize = true,
                    Checked = defaultSearchEngine == "DuckDuckGo",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbBlyx = new RadioButton
                {
                    Text = "Blyx",
                    Location = new Point(260, 22),
                    AutoSize = true,
                    Checked = defaultSearchEngine == "Blyx",
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                engineGroup.Controls.Add(rbGoogle);
                engineGroup.Controls.Add(rbDuck);
                engineGroup.Controls.Add(rbBlyx);
                overlay.Controls.Add(engineGroup);

                currentY += engineGroup.Height + 15;

                // Other Settings
                var cbDev = new CheckBox
                {
                    Text = "Enable Developer Tools",
                    Location = new Point(contentX, currentY),
                    AutoSize = true,
                    Checked = enableDevTools,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };
                overlay.Controls.Add(cbDev);
                currentY += cbDev.Height + 8;

                var cbSession = new CheckBox
                {
                    Text = "Open Last Session on Startup",
                    Location = new Point(contentX, currentY),
                    AutoSize = true,
                    Checked = openLastSessionOnStartup,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };
                overlay.Controls.Add(cbSession);
                currentY += cbSession.Height + 8;

                var cbPopups = new CheckBox
                {
                    Text = "Block Popups",
                    Location = new Point(contentX, currentY),
                    AutoSize = true,
                    Checked = blockPopups,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };
                overlay.Controls.Add(cbPopups);
                currentY += cbPopups.Height + 8;

                var cbPrecache = new CheckBox
                {
                    Text = "Enable Precaching",
                    Location = new Point(contentX, currentY),
                    AutoSize = true,
                    Checked = enablePrecaching,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };
                overlay.Controls.Add(cbPrecache);
                currentY += cbPrecache.Height + 15;

                // Theme Group
                var themeGroup = new GroupBox
                {
                    Text = "Theme",
                    Location = new Point(contentX, currentY),
                    Size = new Size(contentW, 90),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = darkTheme ? Color.White : Color.Black,
                    BackColor = darkTheme ? Color.FromArgb(40, 44, 60) : Color.WhiteSmoke
                };

                var rbDark = new RadioButton
                {
                    Text = "Dark",
                    Location = new Point(16, 22),
                    AutoSize = true,
                    Checked = darkTheme,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                var rbLight = new RadioButton
                {
                    Text = "Light",
                    Location = new Point(90, 22),
                    AutoSize = true,
                    Checked = !darkTheme,
                    ForeColor = darkTheme ? Color.White : Color.Black
                };

                themeGroup.Controls.Add(rbDark);
                themeGroup.Controls.Add(rbLight);
                overlay.Controls.Add(themeGroup);

                currentY += themeGroup.Height + 20;

                // Buttons
                var saveBtn = new Button
                {
                    Text = "Save",
                    Width = 120,
                    Height = 36,
                    BackColor = Color.FromArgb(40, 170, 100),
                    ForeColor = Color.White,
                    Location = new Point(contentX, currentY),
                    FlatStyle = FlatStyle.Flat
                };
                saveBtn.FlatAppearance.BorderSize = 0;
                overlay.Controls.Add(saveBtn);

                var cancelBtn = new Button
                {
                    Text = "Cancel",
                    Width = 120,
                    Height = 36,
                    BackColor = Color.FromArgb(200, 200, 200),
                    ForeColor = Color.Black,
                    Location = new Point(saveBtn.Right + 12, currentY),
                    FlatStyle = FlatStyle.Flat
                };
                cancelBtn.FlatAppearance.BorderSize = 0;
                overlay.Controls.Add(cancelBtn);

                saveBtn.Click += (s, e) =>
                {
                    if (rbEnglish.Checked) language = "en";
                    else if (rbSpanish.Checked) language = "es";
                    else if (rbFrench.Checked) language = "fr";
                    else if (rbGerman.Checked) language = "de";

                    defaultSearchEngine =
                        rbGoogle.Checked ? "Google" :
                        rbDuck.Checked ? "DuckDuckGo" : "Blyx";

                    enableDevTools = cbDev.Checked;
                    openLastSessionOnStartup = cbSession.Checked;
                    blockPopups = cbPopups.Checked;
                    enablePrecaching = cbPrecache.Checked;
                    darkTheme = rbDark.Checked;

                    SaveSettings();
                    ApplyThemeToChrome();

                    MessageBox.Show(
                        "Some settings require restart to take effect.",
                        "Settings Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    CloseOverlay();
                };

                cancelBtn.Click += (s, e) => CloseOverlay();
            }
            else if (title == "Help")
            {
                var helpText = new TextBox
                {
                    Location = new Point(contentX, contentY),
                    Size = new Size(contentW, contentH),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Segoe UI", 10),
                    BackColor = darkTheme ? Color.FromArgb(20, 20, 20) : Color.White,
                    ForeColor = darkTheme ? Color.White : Color.Black,
                    BorderStyle = BorderStyle.None,
                    Text = @"NexNova Browser v60 - Help

Shorcuts:
• Ctrl+T: New Tab
• Ctrl+W: Close Tab
• Ctrl+Shift+P: New Private Window
• Ctrl+H: History
• Ctrl+,: Settings
• F11: Fullscreen mode
• F12: Developer Tools (if enabled)
• Ctrl+Plus: Zoom in
• Ctrl+Minus: Zoom out
• Ctrl+0: Reset zoom

Features (v60):
• NovaCore Engine (100% custom)
• URLs, searches, file://, and commands
• Bookmarks bar below search bars
• PDF viewer for local files
• Private browsing mode
• Session persistence
• Multiple search engines
• Dark/Light themes

Right-click on any tab to:
• Save to Favorites
• Save to Bookmarks

To change language, go to Settings (upper-right corner, then click on the menu button, or use Ctrl+,), available languages are English and Spanish.
You can also change these things in the Settings menu:
• Default search engine
• Theme and accent color
• Privacy options
• Developer tools"
                };

                overlay.Controls.Add(helpText);
            }

            backdrop.Click += (s, e) => CloseOverlay();
            overlay.Click += (s, e) => { /* swallow */ };
        }

        private void CloseOverlay()
        {
            if (currentBackdrop != null)
            {
                Controls.Remove(currentBackdrop);

                try { currentBackdrop.Dispose(); } catch { }
                currentBackdrop = null;
            }
        }
        #endregion

        #region Persistence
        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (!File.Exists(path)) return;

                string txt = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<PersistedSettings>(txt);

                if (cfg != null)
                {
                    defaultSearchEngine = cfg.DefaultSearchEngine ?? "Google";
                    enableDevTools = cfg.EnableDevTools;
                    openLastSessionOnStartup = cfg.OpenLastSessionOnStartup;
                    darkTheme = cfg.DarkTheme;
                    isDefaultBrowser = cfg.IsDefaultBrowser;
                    blockPopups = cfg.BlockPopups;
                    enablePrecaching = cfg.EnablePrecaching;
                    zoomLevel = cfg.ZoomLevel;
                    accentColor = Color.FromArgb(cfg.AccentColorArgb);
                    language = cfg.Language ?? "en";
                }
            }
            catch (Exception ex) { LogError(ex.Message, "LoadSettings"); }
        }

        private void SaveSettings()
        {
            try
            {
                var cfg = new PersistedSettings
                {
                    DefaultSearchEngine = defaultSearchEngine,
                    EnableDevTools = enableDevTools,
                    OpenLastSessionOnStartup = openLastSessionOnStartup,
                    DarkTheme = darkTheme,
                    IsDefaultBrowser = isDefaultBrowser,
                    BlockPopups = blockPopups,
                    EnablePrecaching = enablePrecaching,
                    ZoomLevel = zoomLevel,
                    AccentColorArgb = accentColor.ToArgb(),
                    Language = language
                };

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { LogError(ex.Message, "SaveSettings"); }
        }

        private void LoadFavorites()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FavoritesFileName);
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path);
                    favorites = JsonSerializer.Deserialize<List<string>>(txt) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "LoadFavorites");
                favorites = new List<string>();
            }
        }

        private void SaveFavorites()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FavoritesFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { LogError(ex.Message, "SaveFavorites"); }
        }

        private void LoadBookmarks()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BookmarksFileName);
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path);
                    bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(txt) ?? new List<Bookmark>();
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "LoadBookmarks");
                bookmarks = new List<Bookmark>();
            }
        }

        private void SaveBookmarks()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BookmarksFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { LogError(ex.Message, "SaveBookmarks"); }
        }

        private void LoadFrequentTabs()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CacheFileName);
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path);
                    frequentTabs = JsonSerializer.Deserialize<List<string>>(txt) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message, "LoadFrequentTabs");
                frequentTabs = new List<string>();
            }
        }

        private void SaveFrequentTabs()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CacheFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(frequentTabs, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { LogError(ex.Message, "SaveFrequentTabs"); }
        }

        private void AddToFrequentTabs(string url)
        {
            try
            {
                if (frequentTabs.Contains(url))
                {
                    frequentTabs.Remove(url);
                }
                frequentTabs.Insert(0, url);

                if (frequentTabs.Count > 20)
                {
                    frequentTabs = frequentTabs.Take(20).ToList();
                }
            }
            catch (Exception ex) { LogError(ex.Message, "AddToFrequentTabs"); }
        }

        private List<TabState> LoadSession()
        {
            var list = new List<TabState>();
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SessionFileName);
                if (!File.Exists(path)) return list;

                string txt = File.ReadAllText(path);
                list = JsonSerializer.Deserialize<List<TabState>>(txt) ?? new List<TabState>();
            }
            catch (Exception ex) { LogError(ex.Message, "LoadSession"); }
            return list;
        }

        private void SaveSession()
        {
            try
            {
                var list = new List<TabState>();
                foreach (Control c in contentArea.Controls)
                {
                    if (c is Panel tabContent && tabContent.Tag is TabMeta meta)
                    {
                        string url = meta.CurrentUrl ?? meta.InitialUrl ?? GetDefaultHomepageUrl();
                        list.Add(new TabState { Url = url, IsPrivate = meta.IsPrivate });
                    }
                }

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SessionFileName);
                File.WriteAllText(path, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { LogError(ex.Message, "SaveSession"); }
        }

        private void LogError(string error, string context = "")
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, ErrorLogFileName);
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR - {context}: {error}\n";
                File.AppendAllText(logPath, logEntry);
            }
            catch { }
        }

        private void UpdateStatus(string message)
        {
            Console.WriteLine($"Status: {message}");
        }
        #endregion

        #region Event Handlers
        private void NexNova_Load(object sender, EventArgs e)
        {
            ApplyThemeToChrome();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string potentialUrl = args[1];
                if (Uri.TryCreate(potentialUrl, UriKind.Absolute, out Uri uriResult))
                {
                    AddNewTab(potentialUrl);
                    return;
                }
                else if (File.Exists(potentialUrl))
                {
                    AddNewTab("file:///" + Path.GetFullPath(potentialUrl).Replace("\\", "/"));
                    return;
                }
            }

            if (openLastSessionOnStartup)
            {
                var tabs = LoadSession();
                if (tabs.Count > 0)
                {
                    foreach (var t in tabs) AddNewTab(t.Url, t.IsPrivate);
                    return;
                }
            }

            if (tabCount == 0)
                AddNewTab(GetDefaultHomepageUrl());
        }

        private void NexNova_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSession();
            SaveSettings();
            SaveFavorites();
            SaveBookmarks();
            SaveFrequentTabs();
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (isFullscreen)
                {
                    ToggleFullscreen();
                }
                else
                {
                    CloseOverlay();
                }
                e.Handled = true;
                return;
            }

            bool ctrl = e.Control;
            bool shift = e.Shift;

            if (ctrl && e.KeyCode == Keys.T)
            {
                AddNewTab(GetDefaultHomepageUrl());
                e.Handled = true;
                return;
            }

            if (ctrl && shift && e.KeyCode == Keys.P)
            {
                AddNewTab(GetDefaultHomepageUrl(), true);
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.W)
            {
                if (currentTabButtonPanel != null && tabBar.Controls.Contains(currentTabButtonPanel))
                    CloseTabImmediate(currentTabButtonPanel);
                else if (tabBar.Controls.Count > 1)
                    CloseTabImmediate(tabBar.Controls[tabBar.Controls.Count - 2] as Panel);

                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.H)
            {
                ShowOverlay("History");
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.Oemcomma)
            {
                ShowOverlay("Settings");
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.Shift && e.KeyCode == Keys.O)
            {
                ShowOverlay("Favorites");
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.Add)
            {
                ZoomIn();
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.Subtract)
            {
                ZoomOut();
                e.Handled = true;
                return;
            }

            if (ctrl && e.KeyCode == Keys.D0)
            {
                ResetZoom();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
                return;
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                RestoreLastClosedTab();
                e.Handled = true;
                return;
            }

            if (e.Control && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D8)
            {
                int tabIndex = (int)e.KeyCode - (int)Keys.D1;
                SwitchToTab(tabIndex);
                e.Handled = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.D9)
            {
                SwitchToLastTab();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.F12 && enableDevTools)
            {
                // Implementar DevTools para NovaCore
                e.Handled = true;
            }
        }
        #endregion

        #region Zoom and Fullscreen
        private async void ZoomIn()
        {
            try
            {
                double newZoom = Math.Min(3.0, zoomLevel + 0.1);
                zoomLevel = newZoom;

                if (currentTabContent?.Tag is TabMeta meta)
                {
                    meta.ZoomLevel = newZoom;
                    if (meta.BrowserEngine != null)
                    {
                        await meta.BrowserEngine.ExecuteScript($"document.body.style.zoom = '{newZoom * 100}%'");
                    }
                }

                zoomLevelChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Error en ZoomIn: {ex.Message}", "ZoomIn");
            }
        }

        private async void ZoomOut()
        {
            try
            {
                double newZoom = Math.Max(0.25, zoomLevel - 0.1);
                zoomLevel = newZoom;

                if (currentTabContent?.Tag is TabMeta meta)
                {
                    meta.ZoomLevel = newZoom;
                    if (meta.BrowserEngine != null)
                    {
                        await meta.BrowserEngine.ExecuteScript($"document.body.style.zoom = '{newZoom * 100}%'");
                    }
                }

                zoomLevelChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Error en ZoomOut: {ex.Message}", "ZoomOut");
            }
        }

        private async void ResetZoom()
        {
            try
            {
                zoomLevel = 1.0;

                if (currentTabContent?.Tag is TabMeta meta)
                {
                    meta.ZoomLevel = 1.0;
                    if (meta.BrowserEngine != null)
                    {
                        await meta.BrowserEngine.ExecuteScript($"document.body.style.zoom = '100%'");
                    }
                }

                zoomLevelChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogError($"Error en ResetZoom: {ex.Message}", "ResetZoom");
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                if (isFullscreen)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = previousWindowState;
                    Bounds = previousBounds;
                    isFullscreen = false;
                }
                else
                {
                    previousWindowState = WindowState;
                    previousBounds = Bounds;
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    isFullscreen = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error en ToggleFullscreen: {ex.Message}", "ToggleFullscreen");
            }
        }
        #endregion

        #region Internal Classes and Interfaces
        public interface IBrowserEngine
        {
            Task Navigate(string url);
            void GoBack();
            void GoForward();
            void Refresh();
            Task<string> ExecuteScript(string script);
            string GetTitle();

            event EventHandler<string> PageLoaded;
            event EventHandler<string> TitleChanged;
        }

        private class TabState
        {
            public string Url { get; set; } = "https://www.google.com";
            public bool IsPrivate { get; set; } = false;
        }

        private class PersistedSettings
        {
            public string DefaultSearchEngine { get; set; } = "Google";
            public bool EnableDevTools { get; set; } = false;
            public bool OpenLastSessionOnStartup { get; set; } = false;
            public bool DarkTheme { get; set; } = true;
            public bool IsDefaultBrowser { get; set; } = false;
            public bool BlockPopups { get; set; } = true;
            public bool EnablePrecaching { get; set; } = true;
            public double ZoomLevel { get; set; } = 1.0;
            public int AccentColorArgb { get; set; } = Color.FromArgb(86, 93, 128).ToArgb();
            public string Language { get; set; } = "en";
        }

        private class Bookmark
        {
            public string Url { get; set; } = "";
            public string Title { get; set; } = "";
            public DateTime Created { get; set; } = DateTime.Now;
        }

        internal class TabMeta
        {
            public IBrowserEngine BrowserEngine { get; set; }
            public Panel BrowserContainer { get; set; }
            public bool IsPrivate { get; set; }
            public TextBox SearchBox1 { get; set; }
            public TextBox SearchBox2 { get; set; }
            public Button TitleButton { get; set; }
            public string InitialUrl { get; set; }
            public string CurrentUrl { get; set; }
            public string Title { get; set; }
            public bool IsPdfViewer { get; set; } = false;
            public string PdfFilePath { get; set; } = "";
            public string SecurityStatus { get; set; } = "unknown";
            public double ZoomLevel { get; set; } = 1.0;
            public Panel BookmarksBar { get; set; }
        }
    }
    #endregion
}
