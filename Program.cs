using System.Xml.Serialization;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace GameTimers;

public enum TimerStatus { Afgelopen=1, Loopt=2, Gestopt=3, Afgehandeld=4 }

public sealed class TimerItem
{
    public string Name { get; set; } = "";
    public string Remark { get; set; } = "";
    public int DurationSeconds { get; set; }
    public TimerStatus Status { get; set; } = TimerStatus.Loopt;
    public DateTime? StartTime { get; set; }
    public int StoppedRemainingSeconds { get; set; }
    public bool IsPriority { get; set; } = false;
}

public sealed class AppData
{
    public string ListName { get; set; } = "";
    public int OriginX { get; set; } = -1;
    public int OriginY { get; set; } = -1;
    public int SizeWidth { get; set; } = 720;
    public int SizeHeight { get; set; } = 480;
    public int ColWidthName { get; set; } = 260;
    public int ColWidthInterval { get; set; } = 100;
    public int ColWidthRemaining { get; set; } = 120;
    public int ColWidthStatus { get; set; } = 60;
    public bool ShowLargePopup { get; set; } = true;
    public string Language { get; set; } = "system"; // "system", "nl", "en"
    public List<TimerItem> Timers { get; set; } = new();
}

public static class Localization
{
    public static string CurrentLanguageSetting { get; set; } = "system";

    public static bool IsEnglish
    {
        get
        {
            if (CurrentLanguageSetting.Equals("en", StringComparison.OrdinalIgnoreCase))
                return true;
            if (CurrentLanguageSetting.Equals("nl", StringComparison.OrdinalIgnoreCase))
                return false;

            // Als "system": check of systeemtaal Nederlands is. Zo niet -> Engels
            return !CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string Get(string nlText, string enText)
    {
        return IsEnglish ? enText : nlText;
    }
}

public sealed class MainForm : Form
{
    readonly AppData data = new();
    readonly string dataFile;
    readonly string dataDir;
    readonly ListView list = new();
    readonly Button btnAdd = new();
    readonly Button btnSettings = new();
    readonly Button btnHelp = new();
    readonly TextBox searchBox = new();
    readonly CheckBox chkFilterPrio = new();
    readonly Button btnClearSearch = new();
    readonly ToolTip tips = new();
    readonly System.Windows.Forms.Timer clock = new() { Interval = 1000 };
    readonly ImageList statusImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
    bool internalRefresh;
    bool isFullyLoaded = false;
    ListViewItem? lastHoveredItem = null;

    // Beheer van de actieve pop-up
    Form? activePopupForm = null;
    TableLayoutPanel? activePopupLayout = null;
    readonly List<TimerItem> currentPopupTimers = new();

    public MainForm(string? inputDataDir, string? listName)
    {
        var exeDir = AppContext.BaseDirectory;
        dataDir = string.IsNullOrWhiteSpace(inputDataDir) ? exeDir : Path.GetFullPath(inputDataDir);
        Directory.CreateDirectory(dataDir);
        dataFile = Path.Combine(dataDir, "GameTimers.xml");

        InitAppIcon();
        InitIcons();

        if (File.Exists(dataFile))
        {
            try
            {
                var xs = new XmlSerializer(typeof(AppData));
                using var fs = File.OpenRead(dataFile);
                var loaded = (AppData?)xs.Deserialize(fs);
                if (loaded != null) { 
                    data.ListName = loaded.ListName; 
                    data.OriginX = loaded.OriginX; 
                    data.OriginY = loaded.OriginY; 
                    data.SizeWidth = loaded.SizeWidth; 
                    data.SizeHeight = loaded.SizeHeight; 
                    data.ColWidthName = loaded.ColWidthName;
                    data.ColWidthInterval = loaded.ColWidthInterval;
                    data.ColWidthRemaining = loaded.ColWidthRemaining;
                    data.ColWidthStatus = loaded.ColWidthStatus;
                    data.ShowLargePopup = loaded.ShowLargePopup;
                    data.Language = string.IsNullOrWhiteSpace(loaded.Language) ? "system" : loaded.Language;
                    data.Timers = loaded.Timers ?? new(); 
                }
            } catch {}
        }
        
        Localization.CurrentLanguageSetting = data.Language;

        if (!string.IsNullOrWhiteSpace(listName)) data.ListName = listName;
        Text = string.IsNullOrWhiteSpace(data.ListName) ? "GameTimers" : $"GameTimers - {data.ListName}";
        
        Font = new Font("Segoe UI", 10);

        Load += MainForm_Load;
        Shown += (_, _) => { isFullyLoaded = true; };
        FormClosing += MainForm_FormClosing;

        ResizeEnd += (_, _) => UpdateBoundsAndSave();
        LocationChanged += (_, _) => { if (WindowState == FormWindowState.Normal) UpdateBoundsAndSave(); };

        // Bovenste balk
        var topPanel = new Panel { 
            Dock = DockStyle.Top, 
            Height = 38, 
            Padding = new Padding(6, 4, 6, 4)
        };

        searchBox.PlaceholderText = Localization.Get("Filter", "Filter");
        searchBox.Width = 150;
        searchBox.Location = new Point(6, 7);
        searchBox.TextChanged += (_, _) => {
            UpdateClearButtonState();
            UpdateDisplay(true);
        };
        
        chkFilterPrio.Text = "";
        chkFilterPrio.AutoSize = false;
        chkFilterPrio.Size = new Size(18, 18);
        chkFilterPrio.Location = new Point(searchBox.Right + 8, 11);
        chkFilterPrio.CheckedChanged += (_, _) => {
            UpdateClearButtonState();
            UpdateDisplay(true);
        };
        
        SetupControlToolTip(chkFilterPrio, Localization.Get("Toon alleen timers met prioriteit", "Show priority timers only"));

        SetupButton(btnClearSearch, statusImages.Images["Clear"], new Point(chkFilterPrio.Right + 6, 7));
        btnClearSearch.Click += (_, _) => {
            if (!string.IsNullOrEmpty(searchBox.Text) || chkFilterPrio.Checked)
            {
                searchBox.Clear();
                chkFilterPrio.Checked = false;
            }
        };
        SetupControlToolTip(btnClearSearch, Localization.Get("Filter en prioriteit wissen", "Clear filter and priority"));

        SetupButton(btnAdd, statusImages.Images["Plus"], new Point(topPanel.Width - 90, 7));
        btnAdd.Click += (_, _) => AddNewTimer();
        SetupControlToolTip(btnAdd, Localization.Get("Nieuwe timer", "New timer"));

        SetupButton(btnSettings, statusImages.Images["Settings"], new Point(topPanel.Width - 60, 7));
        btnSettings.Click += (_, _) => OpenSettingsDialog();
        SetupControlToolTip(btnSettings, Localization.Get("Instellingen", "Settings"));

        SetupButton(btnHelp, statusImages.Images["Help"], new Point(topPanel.Width - 30, 7));
        btnHelp.Click += (_, _) => OpenHelpFile();
        SetupControlToolTip(btnHelp, Localization.Get("Help / Handleiding", "Help / Manual"));

        topPanel.Resize += (_, _) => {
            btnAdd.Location = new Point(topPanel.Width - 90, 7);
            btnSettings.Location = new Point(topPanel.Width - 60, 7);
            btnHelp.Location = new Point(topPanel.Width - 30, 7);
        };

        topPanel.Controls.Add(searchBox);
        topPanel.Controls.Add(chkFilterPrio);
        topPanel.Controls.Add(btnClearSearch);
        topPanel.Controls.Add(btnAdd);
        topPanel.Controls.Add(btnSettings);
        topPanel.Controls.Add(btnHelp);

        list.Dock = DockStyle.Fill; list.View = View.Details; list.FullRowSelect = true; list.GridLines = true;
        list.HideSelection = false; list.MultiSelect = false;
        
        list.OwnerDraw = true;
        list.DrawColumnHeader += (s, e) => e.DrawDefault = true;
        list.DrawSubItem += ListDrawSubItem;

        list.MouseMove += List_MouseMove;
        list.MouseLeave += (_, _) => { tips.Hide(list); lastHoveredItem = null; };

        list.Columns.Add(Localization.Get("Naam", "Name"), data.ColWidthName); 
        list.Columns.Add(Localization.Get("Interval", "Interval"), data.ColWidthInterval); 
        list.Columns.Add(Localization.Get("Resterend", "Remaining"), data.ColWidthRemaining); 
        list.Columns.Add("", data.ColWidthStatus);

        list.ColumnWidthChanged += (_, _) => UpdateBoundsAndSave();
        
        list.MouseClick += ListMouseClick;
        list.DoubleClick += (_, _) => EditSelected();
        var menu = new ContextMenuStrip();
        menu.Opening += (_, e) => {
            menu.Items.Clear();
            if (list.SelectedItems.Count == 0 || list.SelectedItems[0].Tag == null) { e.Cancel = true; return; }
            var t = (TimerItem)list.SelectedItems[0].Tag!;
            if (t.Status == TimerStatus.Loopt || t.Status == TimerStatus.Afgelopen) {
                menu.Items.Add(Localization.Get("Stop", "Stop"), null, (_, _) => Stop(t));
                menu.Items.Add(Localization.Get("Opnieuw", "Restart"), null, (_, _) => Restart(t));
                menu.Items.Add(Localization.Get("Afgehandeld", "Handled"), null, (_, _) => HandleTimer(t));
            } else if (t.Status == TimerStatus.Gestopt || t.Status == TimerStatus.Afgehandeld) {
                menu.Items.Add(Localization.Get("Opnieuw", "Restart"), null, (_, _) => Restart(t));
            }
            menu.Items.Add(Localization.Get("Aanpassen", "Edit"), null, (_, _) => EditSelected());
            menu.Items.Add(Localization.Get("Verwijder", "Delete"), null, (_, _) => Delete(t));
        };
        list.ContextMenuStrip = menu;

        Controls.Add(list); 
        Controls.Add(topPanel);
        clock.Tick += (_, _) => UpdateDisplay(false);
        clock.Start();
        
        KeyPreview = true;
        UpdateClearButtonState();
        UpdateDisplay(true);
    }

    private void InitAppIcon()
    {
        try
        {
            // Zoek naar eventuele .ico bestanden in de applicatiemap
            var exeDir = AppContext.BaseDirectory;
            var icoFiles = Directory.GetFiles(exeDir, "*.ico");
            
            if (icoFiles.Length > 0)
            {
                Icon = new Icon(icoFiles[0]);
            }
            else
            {
                // Gebruik de ingebouwde icoon uit het executable bestand zelf
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
        }
        catch
        {
            // Val terug op standaard form-icoon bij fouten
        }
    }

    private void UpdateControlTexts()
    {
        searchBox.PlaceholderText = Localization.Get("Filter", "Filter");
        tips.SetToolTip(searchBox, Localization.Get("Type hier om de lijst te filteren op naam", "Type here to filter list by name"));
        SetupControlToolTip(chkFilterPrio, Localization.Get("Toon alleen timers met prioriteit", "Show priority timers only"));
        SetupControlToolTip(btnClearSearch, Localization.Get("Filter en prioriteit wissen", "Clear filter and priority"));
        SetupControlToolTip(btnAdd, Localization.Get("Nieuwe timer", "New timer"));
        SetupControlToolTip(btnSettings, Localization.Get("Instellingen", "Settings"));
        SetupControlToolTip(btnHelp, Localization.Get("Help / Handleiding", "Help / Manual"));

        if (list.Columns.Count >= 3)
        {
            list.Columns[0].Text = Localization.Get("Naam", "Name");
            list.Columns[1].Text = Localization.Get("Interval", "Interval");
            list.Columns[2].Text = Localization.Get("Resterend", "Remaining");
        }

        UpdateDisplay(true);
    }

    private void SetupButton(Button btn, Image? img, Point location)
    {
        btn.Size = new Size(24, 24);
        btn.Location = location;
        btn.Image = img;
        btn.ImageAlign = ContentAlignment.MiddleCenter;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.UseVisualStyleBackColor = true;
        btn.Cursor = Cursors.Hand;
    }

    private void SetupControlToolTip(Control ctrl, string text)
    {
        tips.SetToolTip(ctrl, text);
        ctrl.MouseEnter += (s, e) => tips.Show(text, ctrl, 0, ctrl.Height + 2, 2500);
    }

    void UpdateClearButtonState()
    {
        bool hasFilter = !string.IsNullOrEmpty(searchBox.Text) || chkFilterPrio.Checked;
        btnClearSearch.Image = hasFilter ? statusImages.Images["Clear"] : null;
        btnClearSearch.Cursor = hasFilter ? Cursors.Hand : Cursors.Default;
    }

    private void OpenHelpFile()
    {
        try
        {
            string bodyContent = Localization.IsEnglish ? Program_HLP.GetContent_ENG() : Program_HLP.GetContent();
            string docTitle = Localization.Get("GameTimers - Handleiding", "GameTimers - Manual");
            string docLang = Localization.IsEnglish ? "en" : "nl";

            string htmlHeader = $@"<!DOCTYPE html>
<html lang=""{docLang}"">
<head>
    <meta charset=""utf-8"">
    <title>{docTitle}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 30px;
            background-color: #f9f9f9;
            color: #333;
            line-height: 1.6;
        }}
        .container {{
            max-width: 800px;
            margin: 0 auto;
            background: #fff;
            padding: 25px 35px;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        h1 {{ color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 8px; }}
        h2 {{ color: #2980b9; margin-top: 25px; }}
        ul {{ padding-left: 20px; }}
        li {{ margin-bottom: 8px; }}
        code {{ font-family: monospace; background: #eee; padding: 2px 5px; border-radius: 3px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>{docTitle}</h1>";

            string htmlFooter = @"
    </div>
</body>
</html>";

            string fullHtml = htmlHeader + "\n" + bodyContent + "\n" + htmlFooter;

            string exeDir = AppContext.BaseDirectory;
            string fileName = Localization.IsEnglish ? "GameTimers_HLP_ENG.HTM" : "GameTimers_HLP.HTM";
            string helpFilePath = Path.Combine(exeDir, fileName);

            File.WriteAllText(helpFilePath, fullHtml, System.Text.Encoding.UTF8);

            Process.Start(new ProcessStartInfo
            {
                FileName = helpFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Localization.Get($"Kan het helpbestand niet openen: {ex.Message}", $"Cannot open help file: {ex.Message}"),
                Localization.Get("Fout", "Error"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void List_MouseMove(object? sender, MouseEventArgs e)
    {
        var item = list.GetItemAt(e.X, e.Y);
        if (item != lastHoveredItem)
        {
            lastHoveredItem = item;
            if (item != null)
            {
                if (item.Tag is TimerItem t && !string.IsNullOrWhiteSpace(t.Remark))
                {
                    tips.SetToolTip(list, t.Remark);
                }
                else if (item.Tag is string emptyMsg)
                {
                    tips.SetToolTip(list, emptyMsg);
                }
                else
                {
                    tips.SetToolTip(list, null);
                }
            }
            else
            {
                tips.SetToolTip(list, null);
            }
        }
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        int w = data.SizeWidth > 0 ? data.SizeWidth : 300;
        int h = data.SizeHeight > 0 ? data.SizeHeight : 200;

        Rectangle targetBounds = new Rectangle(data.OriginX, data.OriginY, w, h);
        bool isVisibleOnAnyScreen = Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(targetBounds));

        if (isVisibleOnAnyScreen)
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = targetBounds;
        }
        else
        {
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(w, h);
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        UpdateBoundsAndSave();
    }

    void InitIcons()
    {
        Bitmap bmpClock = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpClock)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Black, 1.5f);
            g.DrawEllipse(p, 1, 1, 13, 13);
            g.DrawLine(p, 7.5f, 7.5f, 7.5f, 3.5f);
            g.DrawLine(p, 7.5f, 7.5f, 10.5f, 7.5f);
        }
        statusImages.Images.Add("Loopt", bmpClock);

        Bitmap bmpBell = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpBell)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Brush b = new SolidBrush(Color.Red);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(4, 3, 8, 8, 180, 180);
            path.AddLine(12, 7, 13, 11);
            path.AddLine(13, 11, 3, 11);
            path.AddLine(3, 11, 4, 7);
            g.FillPath(b, path);
            g.FillEllipse(b, 6.5f, 12, 3, 2.5f);
        }
        statusImages.Images.Add("Afgelopen", bmpBell);

        Bitmap bmpStop = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpStop)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Black, 1.5f);
            PointF[] pts = new PointF[] {
                new PointF(5, 1), new PointF(10, 1),
                new PointF(14, 5), new PointF(14, 10),
                new PointF(10, 14), new PointF(5, 14),
                new PointF(1, 10), new PointF(1, 5)
            };
            g.DrawPolygon(p, pts);
            g.DrawLine(p, 4, 7.5f, 11, 7.5f);
        }
        statusImages.Images.Add("Gestopt", bmpStop);

        Bitmap bmpCheck = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpCheck)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Black, 2.0f);
            g.DrawLines(p, new Point[] { new Point(2, 8), new Point(6, 12), new Point(13, 3) });
        }
        statusImages.Images.Add("Afgehandeld", bmpCheck);

        Bitmap bmpPlus = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpPlus)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Black, 2.0f);
            g.DrawLine(p, 8, 2, 8, 14);
            g.DrawLine(p, 2, 8, 14, 8);
        }
        statusImages.Images.Add("Plus", bmpPlus);

        Bitmap bmpSettings = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpSettings)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Black, 1.8f);
            g.DrawEllipse(p, 5, 5, 6, 6);
            for (int i = 0; i < 8; i++) {
                double angle = i * Math.PI / 4;
                float x1 = 8 + (float)(5.5 * Math.Cos(angle));
                float y1 = 8 + (float)(5.5 * Math.Sin(angle));
                float x2 = 8 + (float)(7.5 * Math.Cos(angle));
                float y2 = 8 + (float)(7.5 * Math.Sin(angle));
                g.DrawLine(p, x1, y1, x2, y2);
            }
        }
        statusImages.Images.Add("Settings", bmpSettings);

        Bitmap bmpClear = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpClear)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Gray, 2.0f);
            g.DrawLine(p, 4, 4, 12, 12);
            g.DrawLine(p, 12, 4, 4, 12);
        }
        statusImages.Images.Add("Clear", bmpClear);

        Bitmap bmpExclamation = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpExclamation)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Pen p = new Pen(Color.Red, 2.5f);
            g.DrawLine(p, 8, 2, 8, 10);
            using Brush b = new SolidBrush(Color.Red);
            g.FillEllipse(b, 6.5f, 12.5f, 3, 3);
        }
        statusImages.Images.Add("Exclamation", bmpExclamation);

        Bitmap bmpHelp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmpHelp)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using Font f = new Font("Segoe UI", 10, FontStyle.Bold);
            TextRenderer.DrawText(g, "?", f, new Rectangle(0, -1, 16, 16), Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        statusImages.Images.Add("Help", bmpHelp);
    }

    void ListDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item == null) return;

        if (e.Item.Tag is string)
        {
            if (e.ColumnIndex == 0)
            {
                using var b = new SolidBrush(list.BackColor);
                e.Graphics.FillRectangle(b, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, new Font(list.Font, FontStyle.Italic), e.Bounds, Color.Gray, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            return;
        }

        if (e.Item.Tag is not TimerItem t) return;

        Color backColor;
        if (e.ColumnIndex == 3 && t.IsPriority)
        {
            backColor = Color.Yellow;
        }
        else
        {
            backColor = e.Item.Selected ? SystemColors.Highlight : e.Item.BackColor;
        }

        Color textColor = e.Item.Selected && !(e.ColumnIndex == 3 && t.IsPriority) 
            ? SystemColors.HighlightText 
            : list.ForeColor;

        using (Brush b = new SolidBrush(backColor)) {
            e.Graphics.FillRectangle(b, e.Bounds);
        }

        bool isBold = (t.Status != TimerStatus.Afgehandeld) && (e.ColumnIndex == 0 || e.ColumnIndex == 2);

        if (e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 2) {
            using var fontToUse = isBold ? new Font(list.Font, FontStyle.Bold) : new Font(list.Font, FontStyle.Regular);
            TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, fontToUse, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
        else if (e.ColumnIndex == 3) {
            string key = StatusIconKey(t.Status);
            bool hasRemark = !string.IsNullOrWhiteSpace(t.Remark);
            
            int totalWidth = 16 + (hasRemark ? 18 : 0);
            int startX = e.Bounds.Left + (e.Bounds.Width - totalWidth) / 2;
            int y = e.Bounds.Top + (e.Bounds.Height - 16) / 2;

            if (statusImages.Images.ContainsKey(key)) {
                Image img = statusImages.Images[key]!;
                e.Graphics.DrawImage(img, startX, y);
            }

            if (hasRemark && statusImages.Images.ContainsKey("Exclamation")) {
                Image imgExcl = statusImages.Images["Exclamation"]!;
                e.Graphics.DrawImage(imgExcl, startX + 18, y);
            }
        }
    }

    void OpenSettingsDialog()
    {
        using var dlgTips = new ToolTip();
        using var f = new Form {
            Text = Localization.Get("Instellingen", "Settings"),
            Width = 440,
            Height = 230,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Icon = Icon
        };

        int controlX = 180; // Vaste horizontale positie voor alle besturingselementen

        // 1. Taalkeuze
        var lblLang = new Label {
            Left = 20,
            Top = 18,
            Text = Localization.Get("Taal:", "Language:"),
            AutoSize = true
        };

        var cmbLang = new ComboBox {
            Left = controlX,
            Top = 15,
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        cmbLang.Items.Add(Localization.Get("Automatisch (Systeemtaal)", "Automatic (System Default)"));
        cmbLang.Items.Add("Nederlands");
        cmbLang.Items.Add("English");

        cmbLang.SelectedIndex = data.Language.ToLowerInvariant() switch {
            "nl" => 1,
            "en" => 2,
            _ => 0
        };

        // 2. Pop-up Checkbox met label vooraf
        var lblPopup = new Label {
            Left = 20,
            Top = 53,
            Text = Localization.Get("Pop-up bij afgeronde timers:", "Show pop-up for finished timers:"),
            AutoSize = true
        };

        var chkPopup = new CheckBox {
            Left = controlX,
            Top = 52,
            AutoSize = false,
            Size = new Size(18, 18),
            Checked = data.ShowLargePopup
        };

        // 3. Knoppen op dezelfde horizontale lijn
        var btnOpenXml = new Button {
            Text = Localization.Get("XML openen", "Open XML"),
            Left = controlX,
            Top = 88,
            Width = 105
        };
        dlgTips.SetToolTip(btnOpenXml, Localization.Get($"Open XML-bestand:\n{dataFile}", $"Open XML file:\n{dataFile}"));

        btnOpenXml.Click += (_, _) => {
            try
            {
                Save();
                if (File.Exists(dataFile))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dataFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        Localization.Get("Het XML-bestand bestaat nog niet.", "The XML file does not exist yet."),
                        "GameTimers",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Localization.Get($"Kan het bestand niet openen: {ex.Message}", $"Cannot open file: {ex.Message}"),
                    Localization.Get("Fout", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        };

        var btnOpenMap = new Button {
            Text = Localization.Get("Map openen", "Open Folder"),
            Left = controlX + 115,
            Top = 88,
            Width = 105
        };
        dlgTips.SetToolTip(btnOpenMap, Localization.Get($"Open datamap in verkenner:\n{dataDir}", $"Open data folder in explorer:\n{dataDir}"));

        btnOpenMap.Click += (_, _) => {
            try
            {
                if (Directory.Exists(dataDir))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dataDir,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        Localization.Get("De datamap bestaat niet.", "The data folder does not exist."),
                        "GameTimers",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Localization.Get($"Kan de map niet openen: {ex.Message}", $"Cannot open folder: {ex.Message}"),
                    Localization.Get("Fout", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        };

        // 4. Actieknoppen onderaan
        var ok = new Button { Text = "OK", Left = 235, Top = 140, Width = 75, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = Localization.Get("Annuleren", "Cancel"), Left = 320, Top = 140, Width = 80, DialogResult = DialogResult.Cancel };

        f.Controls.AddRange(new Control[] { lblLang, cmbLang, lblPopup, chkPopup, btnOpenXml, btnOpenMap, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;

        if (f.ShowDialog(this) == DialogResult.OK) {
            data.ShowLargePopup = chkPopup.Checked;
            
            string oldLang = data.Language;
            data.Language = cmbLang.SelectedIndex switch {
                1 => "nl",
                2 => "en",
                _ => "system"
            };

            Localization.CurrentLanguageSetting = data.Language;
            Save();

            if (oldLang != data.Language)
            {
                UpdateControlTexts();
            }
        }
    }

    void ShowTimerDialog(TimerItem? existingItem)
    {
        bool isNew = existingItem == null;
        var item = existingItem ?? new TimerItem();

        using var dlgTips = new ToolTip();
        using var f = new Form { 
            Text = isNew ? Localization.Get("Nieuwe timer", "New timer") : Localization.Get("Timer aanpassen", "Edit timer"), 
            Width = 400, 
            Height = 250, 
            StartPosition = FormStartPosition.CenterParent, 
            FormBorderStyle = FormBorderStyle.FixedDialog, 
            MaximizeBox = false, 
            MinimizeBox = false,
            Icon = Icon
        };

        var lblName = new Label { Left = 20, Top = 23, Text = Localization.Get("Naam:", "Name:"), AutoSize = true };
        var txtName = new TextBox { Left = 120, Top = 20, Width = 240, Text = item.Name };
        
        var lblInterval = new Label { Left = 20, Top = 58, Text = Localization.Get("Interval:", "Interval:"), AutoSize = true };
        var txtInterval = new TextBox { Left = 120, Top = 55, Width = 240, Text = isNew ? "" : DurationText(item.DurationSeconds) };
        
        dlgTips.SetToolTip(txtInterval, Localization.Get("Bijvoorbeeld: 45, 45 min, 00:45, 1:30 of 1:30 uur", "E.g.: 45, 45 min, 00:45, 1:30 or 1:30 hours"));

        var lblRemark = new Label { Left = 20, Top = 93, Text = Localization.Get("Opmerking:", "Remark:"), AutoSize = true };
        var txtRemark = new TextBox { Left = 120, Top = 90, Width = 240, Text = item.Remark };

        var lblPrio = new Label { Left = 20, Top = 127, Text = Localization.Get("Prioriteit:", "Priority:"), AutoSize = true };
        var chkPrio = new CheckBox {
            Left = 120,
            Top = 126,
            Text = "",
            AutoSize = false,
            Size = new Size(18, 18),
            Checked = item.IsPriority
        };

        var ok = new Button { Text = "OK", Left = 195, Top = 165, Width = 75 };
        var cancel = new Button { Text = Localization.Get("Annuleren", "Cancel"), Left = 280, Top = 165, Width = 80, DialogResult = DialogResult.Cancel };

        ok.Click += (_, _) => {
            if (string.IsNullOrWhiteSpace(txtName.Text)) {
                MessageBox.Show(
                    Localization.Get("Geef de timer een naam.", "Please give the timer a name."),
                    "GameTimers",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtName.Focus();
                return;
            }

            if (!TryDuration(txtInterval.Text, out var sec) || sec <= 0) {
                MessageBox.Show(
                    Localization.Get("Ongeldige tijdsindeling!\nGebruik bijvoorbeeld: 45, 45 min, 00:45 of 1:30.", "Invalid time format!\nFor example use: 45, 45 min, 00:45 or 1:30."),
                    "GameTimers",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtInterval.Focus();
                txtInterval.SelectAll();
                return;
            }

            item.Name = txtName.Text.Trim();
            item.DurationSeconds = sec;
            item.Remark = txtRemark.Text.Trim();
            item.IsPriority = chkPrio.Checked;

            if (isNew) {
                item.Status = TimerStatus.Loopt;
                item.StartTime = DateTime.Now;
                item.StoppedRemainingSeconds = 0;
                data.Timers.Add(item);
            } else if (item.Status == TimerStatus.Gestopt) {
                item.StoppedRemainingSeconds = sec;
            } else if (item.Status == TimerStatus.Afgelopen) {
                var askRestart = MessageBox.Show(
                    Localization.Get($"Wilt u de timer '{item.Name}' direct opnieuw laten lopen?", $"Do you want to restart the timer '{item.Name}' immediately?"),
                    Localization.Get("Timer opnieuw starten", "Restart timer"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (askRestart == DialogResult.Yes) {
                    item.Status = TimerStatus.Loopt;
                    item.StartTime = DateTime.Now;
                    item.StoppedRemainingSeconds = 0;
                }
            }

            Save();
            UpdateDisplay(true);
            
            f.DialogResult = DialogResult.OK;
            f.Close();
        };

        f.Controls.AddRange(new Control[] { lblName, txtName, lblInterval, txtInterval, lblRemark, txtRemark, lblPrio, chkPrio, ok, cancel });
        f.AcceptButton = ok; 
        f.CancelButton = cancel;

        f.ShowDialog(this);
    }

    void AddNewTimer()
    {
        ShowTimerDialog(null);
    }

    void EditSelected()
    {
        if (list.SelectedItems.Count == 0 || list.SelectedItems[0].Tag is not TimerItem t) return;
        ShowTimerDialog(t);
    }

    void Restart(TimerItem t) { t.Status = TimerStatus.Loopt; t.StartTime = DateTime.Now; t.StoppedRemainingSeconds = 0; Save(); UpdateDisplay(true); }
    void Stop(TimerItem t) { t.StoppedRemainingSeconds = Remaining(t); t.StartTime = null; t.Status = TimerStatus.Gestopt; Save(); UpdateDisplay(true); }

    void HandleTimer(TimerItem t) 
    { 
        if (t.IsPriority)
        {
            var res = MessageBox.Show(
                Localization.Get($"Wilt u de prioriteit van de timer '{t.Name}' verwijderen?", $"Do you want to remove priority for timer '{t.Name}'?"),
                Localization.Get("Prioriteit verwijderen", "Remove priority"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (res == DialogResult.Yes)
            {
                t.IsPriority = false;
            }
        }

        t.Status = TimerStatus.Afgehandeld; 
        t.StartTime = null; 
        t.StoppedRemainingSeconds = 0; 
        Save(); 
        UpdateDisplay(true); 
    }

    void Delete(TimerItem t) { 
        if (MessageBox.Show(
            Localization.Get($"Weet u zeker dat u '{t.Name}' wilt verwijderen?", $"Are you sure you want to delete '{t.Name}'?"), 
            "GameTimers", 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Question) == DialogResult.Yes) 
        { 
            data.Timers.Remove(t); 
            Save(); 
            UpdateDisplay(true); 
        } 
    }

    void ListMouseClick(object? s, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) {
            var hit = list.HitTest(e.Location);
            if (hit.Item != null && hit.Item.Tag is TimerItem) { 
                list.SelectedItems.Clear(); 
                hit.Item.Selected = true; 
            }
        }
    }

    int Remaining(TimerItem t)
    {
        if (t.Status == TimerStatus.Loopt) {
            if (t.StartTime == null) return t.StoppedRemainingSeconds;
            return t.DurationSeconds - (int)Math.Floor((DateTime.Now - t.StartTime.Value).TotalSeconds);
        }
        if (t.Status == TimerStatus.Gestopt) return t.StoppedRemainingSeconds;
        if (t.Status == TimerStatus.Afgehandeld) return 0;
        if (t.StartTime == null) return -1;
        return t.DurationSeconds - (int)Math.Floor((DateTime.Now - t.StartTime.Value).TotalSeconds);
    }

    void UpdateDisplay(bool rebuild)
    {
        if (internalRefresh) return;
        var newlyFinished = new List<TimerItem>();

        foreach (var t in data.Timers) {
            if (t.Status == TimerStatus.Loopt && Remaining(t) <= 0) { 
                t.Status = TimerStatus.Afgelopen; 
                newlyFinished.Add(t);
            }
        }

        if (newlyFinished.Count > 0) { 
            Save(); 
            rebuild = true; 
        }
        
        string filter = searchBox.Text.Trim();
        bool filterPrioOnly = chkFilterPrio.Checked;

        var ordered = data.Timers
            .Where(t => (string.IsNullOrWhiteSpace(filter) || t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) &&
                        (!filterPrioOnly || t.IsPriority))
            .OrderBy(t => (int)t.Status)
            .ThenBy(t => Remaining(t))
            .ThenBy(t => t.DurationSeconds)
            .ToList();

        if (rebuild) {
            internalRefresh = true; list.BeginUpdate(); list.Items.Clear();

            if (ordered.Count == 0 && (!string.IsNullOrWhiteSpace(filter) || filterPrioOnly))
            {
                string msg;
                if (!string.IsNullOrWhiteSpace(filter) && filterPrioOnly)
                {
                    msg = Localization.Get($"Geen timers gevonden met '{filter}' in de naam EN hoge prioriteit", $"No timers found with '{filter}' in name AND high priority");
                }
                else if (!string.IsNullOrWhiteSpace(filter))
                {
                    msg = Localization.Get($"Geen timers gevonden met '{filter}' in de naam", $"No timers found with '{filter}' in name");
                }
                else
                {
                    msg = Localization.Get("Geen timers gevonden met hoge prioriteit", "No high priority timers found");
                }

                var emptyItem = new ListViewItem(msg);
                emptyItem.SubItems.Add("");
                emptyItem.SubItems.Add("");
                emptyItem.SubItems.Add("");
                emptyItem.Tag = msg;
                list.Items.Add(emptyItem);
            }
            else
            {
                foreach (var t in ordered) {
                    var r = Remaining(t);
                    var item = new ListViewItem(t.Name);
                    item.SubItems.Add(DurationText(t.DurationSeconds));
                    item.SubItems.Add(t.Status == TimerStatus.Afgehandeld ? "" : FormatRemaining(r));
                    item.SubItems.Add("");

                    item.Tag = t; list.Items.Add(item);
                    item.BackColor = t.Status switch { TimerStatus.Loopt => Color.Honeydew, TimerStatus.Afgelopen => Color.MistyRose, TimerStatus.Gestopt => Color.Gainsboro, _ => Color.White };
                }
            }
            list.EndUpdate(); internalRefresh = false;
        } else {
            foreach (ListViewItem item in list.Items) {
                if (item.Tag is TimerItem t)
                {
                    var r = Remaining(t);
                    string newText = t.Status == TimerStatus.Afgehandeld ? "" : FormatRemaining(r);
                    
                    if (item.SubItems[2].Text != newText)
                    {
                        item.SubItems[2].Text = newText;
                        list.Invalidate(item.SubItems[2].Bounds);
                    }
                }
            }
        }

        if (data.ShowLargePopup && newlyFinished.Count > 0) {
            foreach (var t in newlyFinished) {
                AddTimerToNotificationPopup(t);
            }
        }
    }

    private void AddTimerToNotificationPopup(TimerItem timer)
    {
        if (!currentPopupTimers.Contains(timer))
        {
            currentPopupTimers.Add(timer);
        }

        if (activePopupForm != null && !activePopupForm.IsDisposed)
        {
            RebuildPopupContent();
            return;
        }

        activePopupForm = new Form
        {
            Text = Localization.Get("Timer Afgelopen", "Timer Finished"),
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
            BackColor = Color.MistyRose,
            Icon = Icon
        };

        activePopupLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        activePopupForm.Controls.Add(activePopupLayout);

        RebuildPopupContent();

        activePopupForm.FormClosed += (_, _) =>
        {
            currentPopupTimers.Clear();
            activePopupForm = null;
            activePopupLayout = null;
        };

        activePopupForm.ShowDialog(this);
    }

    private void RebuildPopupContent()
    {
        if (activePopupLayout == null || activePopupForm == null) return;

        activePopupLayout.SuspendLayout();
        activePopupLayout.Controls.Clear();
        activePopupLayout.RowStyles.Clear();
        activePopupLayout.ColumnStyles.Clear();

        activePopupLayout.ColumnCount = 1;
        activePopupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        activePopupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));

        var contentContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Margin = new Padding(0)
        };
        contentContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int titleFontSize = currentPopupTimers.Count switch
        {
            1 => 30,
            2 => 22,
            _ => 16
        };

        int maxTextWidth = 0;
        using (var g = activePopupForm.CreateGraphics())
        {
            using var fontTitle = new Font("Segoe UI", titleFontSize, FontStyle.Bold);
            using var fontRemark = new Font("Segoe UI", Math.Max(10, titleFontSize - 10), FontStyle.Italic);

            foreach (var t in currentPopupTimers)
            {
                int titleWidth = TextRenderer.MeasureText(g, t.Name, fontTitle).Width;
                if (titleWidth > maxTextWidth) maxTextWidth = titleWidth;

                if (!string.IsNullOrWhiteSpace(t.Remark))
                {
                    int remarkWidth = TextRenderer.MeasureText(g, t.Remark, fontRemark).Width;
                    if (remarkWidth > maxTextWidth) maxTextWidth = remarkWidth;
                }
            }
        }

        Screen currentScreen = Screen.FromControl(this);
        Rectangle screenBounds = currentScreen.WorkingArea;

        int defaultWidth = screenBounds.Width / 3;
        int defaultHeight = screenBounds.Height / 3;

        int requiredWidth = maxTextWidth + 80;
        int targetWidth = Math.Max(defaultWidth, requiredWidth);
        targetWidth = Math.Min(targetWidth, screenBounds.Width - 40);

        int popupX = screenBounds.X + (screenBounds.Width - targetWidth) / 2;
        int popupY = screenBounds.Y + (screenBounds.Height - defaultHeight) / 2;

        activePopupForm.Bounds = new Rectangle(popupX, popupY, targetWidth, defaultHeight);

        foreach (var t in currentPopupTimers)
        {
            var lblTitle = new Label
            {
                Text = t.Name,
                Font = new Font("Segoe UI", titleFontSize, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            contentContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentContainer.Controls.Add(lblTitle, 0, contentContainer.RowCount - 1);

            if (!string.IsNullOrWhiteSpace(t.Remark))
            {
                var lblRemark = new Label
                {
                    Text = t.Remark,
                    Font = new Font("Segoe UI", Math.Max(10, titleFontSize - 10), FontStyle.Italic),
                    ForeColor = Color.DarkSlateGray,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Margin = new Padding(0, 2, 0, 12)
                };
                contentContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                contentContainer.Controls.Add(lblRemark, 0, contentContainer.RowCount - 1);
            }
            else
            {
                lblTitle.Margin = new Padding(0, 4, 0, 12);
            }
        }

        activePopupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
        Label lblSub = new Label
        {
            Text = Localization.Get("Timer is afgerond", "Timer has finished"),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            ForeColor = Color.Black
        };

        activePopupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));
        Button btnClose = new Button
        {
            Text = Localization.Get("Ik heb deze melding gezien", "I have seen this notification"),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Anchor = AnchorStyles.None,
            DialogResult = DialogResult.OK
        };

        activePopupLayout.Controls.Add(contentContainer, 0, 0);
        activePopupLayout.Controls.Add(lblSub, 0, 1);
        activePopupLayout.Controls.Add(btnClose, 0, 2);

        activePopupForm.AcceptButton = btnClose;

        activePopupLayout.ResumeLayout();
    }

    static string StatusIconKey(TimerStatus s) => s switch { TimerStatus.Afgelopen => "Afgelopen", TimerStatus.Loopt => "Loopt", TimerStatus.Gestopt => "Gestopt", _ => "Afgehandeld" };

    static string DurationText(int sec) {
        var ts = TimeSpan.FromSeconds(sec);
        int totalHours = (int)ts.TotalHours;
        return $"{totalHours:D2}:{ts.Minutes:D2}";
    }

    static string FormatRemaining(int sec) {
        var neg = sec < 0;
        sec = Math.Abs(sec);
        var ts = TimeSpan.FromSeconds(sec);
        int totalHours = (int)ts.TotalHours;
        var text = $"{totalHours:D2}:{ts.Minutes:D2}";
        return neg ? "-" + text : text;
    }

    static bool TryDuration(string input, out int seconds) {
        seconds = 0; input = input.Trim().ToLowerInvariant().Replace(" ", "");
        if (input == "") return false;
        
        if (input.Contains(":")) {
            var p = input.Split(':');
            if (p.Length == 2 && int.TryParse(p[0], out var a) && int.TryParse(p[1], out var b)) {
                seconds = a * 3600 + b * 60;
                return seconds > 0;
            }
            return false;
        }

        double mult = 60;
        if (input.EndsWith("hours") || input.EndsWith("hour")) { mult = 3600; input = input.EndsWith("hours") ? input[..^5] : input[..^4]; }
        else if (input.EndsWith("uur")) { mult = 3600; input = input[..^3]; }
        else if (input.EndsWith("min")) { mult = 60; input = input[..^3]; }
        else if (input.EndsWith("sec")) { mult = 1; input = input[..^3]; }
        else if (input.EndsWith("u") || input.EndsWith("h")) { mult = 3600; input = input[..^1]; }
        else if (input.EndsWith("m")) { mult = 60; input = input[..^1]; }
        else if (input.EndsWith("s")) { mult = 1; input = input[..^1]; }

        if (!double.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return false;
        seconds = (int)Math.Round(v * mult);
        return seconds > 0;
    }

    void UpdateBoundsAndSave()
    {
        if (!isFullyLoaded) return;

        if (WindowState == FormWindowState.Normal)
        {
            data.OriginX = Location.X;
            data.OriginY = Location.Y;
            data.SizeWidth = Size.Width;
            data.SizeHeight = Size.Height;
        }

        if (list.Columns.Count >= 4) {
            data.ColWidthName = list.Columns[0].Width;
            data.ColWidthInterval = list.Columns[1].Width;
            data.ColWidthRemaining = list.Columns[2].Width;
            data.ColWidthStatus = list.Columns[3].Width;
        }

        Save();
    }

    void Save() {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);
            var xs = new XmlSerializer(typeof(AppData));
            using var fs = File.Create(dataFile);
            xs.Serialize(fs, data);
        } catch {}
    }
}

static class Program
{
    [STAThread]
    static void Main(string[] args) {
        ApplicationConfiguration.Initialize();
        string? dir = args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : null;
        string? name = args.Length >= 2 ? args[1] : null;
        Application.Run(new MainForm(dir, name));
    }
}