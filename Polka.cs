using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

public class Entry {
    public string Title { get; set; }
    public string Format { get; set; }
    public int Priority { get; set; }
}

internal static class Library {
    internal static readonly string[] Formats = {"Фильм", "Сериал", "Аниме", "Книга", "Манга", "Манхва", "Маньхуа", "Комикс", "Ранобэ", "Аудиокнига", "Подкаст", "Мультфильм", "Документальный фильм", "Игра", "Другое"};
    internal static readonly string[] Levels = {"Высокий", "Средний", "Низкий"};
    static readonly XmlSerializer Serializer = new XmlSerializer(typeof(List<Entry>));

    internal static List<Entry> Load(string path) {
        if (!File.Exists(path)) return new List<Entry>();
        using (var file = File.OpenRead(path)) {
            var entries = (List<Entry>)Serializer.Deserialize(file);
            if (entries.Any(e => e == null || String.IsNullOrWhiteSpace(e.Title) || e.Priority < 0 || e.Priority >= Levels.Length))
                throw new InvalidDataException("Недопустимая запись в коллекции.");
            return entries;
        }
    }

    internal static void Save(string path, List<Entry> entries) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + ".tmp";
        try {
            using (var file = File.Create(temp)) Serializer.Serialize(file, entries);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        } finally {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}

public class Preferences {
    public bool Dark { get; set; }
    public string[] Labels { get; set; }
    public Preferences() { Labels = (string[])Library.Levels.Clone(); }

    internal static Preferences Read(string file) {
        if (!File.Exists(file)) return new Preferences();
        using (var stream = File.OpenRead(file)) {
            var p = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(stream);
            if (p.Labels == null || p.Labels.Length != 3 || p.Labels.Any(String.IsNullOrWhiteSpace))
                throw new InvalidDataException("Некорректные названия приоритетов в настройках.");
            return p;
        }
    }
    internal void Save(string file) {
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        string temp = file + ".tmp";
        try {
            using (var stream = File.Create(temp)) new XmlSerializer(typeof(Preferences)).Serialize(stream,this);
            if (File.Exists(file)) File.Replace(temp,file,null); else File.Move(temp,file);
        } finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

internal static class Shape {
    internal static void Fill(Graphics g, Rectangle r, Color color, int radius) {
        int d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        if(d<1) return;
        using(var path=new System.Drawing.Drawing2D.GraphicsPath())
        using(var brush=new SolidBrush(color)) {
            path.AddArc(r.Left,r.Top,d,d,180,90); path.AddArc(r.Right-d,r.Top,d,d,270,90);
            path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); path.AddArc(r.Left,r.Bottom-d,d,d,90,90);
            path.CloseFigure(); g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(brush,path);
        }
    }
}

internal class SoftButton : Button {
    internal Palette Colors;
    bool hover;
    internal SoftButton() { DoubleBuffered=true; }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover=true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover=false; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        if(Colors==null) { base.OnPaint(e); return; }
        bool main=Equals(Tag,"primary");
        var fill=main ? Colors.Accent : Colors.Side;
        if(Enabled && hover) fill=main ? ControlPaint.Light(fill,.12f) : Colors.Tint;
        e.Graphics.Clear(Parent.BackColor);
        Shape.Fill(e.Graphics,ClientRectangle,fill,9);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled ? (main ? Colors.OnAccent : Colors.Ink) : Colors.Muted,
            TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-4,-4));
    }
}

internal class Palette {
    internal Color Canvas, Surface, Ink, Muted, Accent, Tint, Line, Side, OnAccent;
    internal readonly Color[] BadgeColors;
    internal Palette(bool dark) {
        Side = ColorTranslator.FromHtml(dark ? "#302E32" : "#EAF1F7");
        OnAccent = ColorTranslator.FromHtml(dark ? "#27080A" : "#092C4B");
        Canvas = ColorTranslator.FromHtml(dark ? "#202023" : "#FAFCFF");
        Surface = ColorTranslator.FromHtml(dark ? "#2C2D32" : "#FFFFFF");
        Ink = ColorTranslator.FromHtml(dark ? "#FAF5F4" : "#172E47");
        Muted = ColorTranslator.FromHtml(dark ? "#D8BDC0" : "#4D6980");
        Accent = ColorTranslator.FromHtml(dark ? "#FF7579" : "#8CCBFF");
        BadgeColors = new[] {
            Accent,
            ColorTranslator.FromHtml(dark ? "#735121" : "#FFDC99"),
            ColorTranslator.FromHtml(dark ? "#454951" : "#DEE2E8")
        };
        Tint = ColorTranslator.FromHtml(dark ? "#573038" : "#D8EAFB");
        Line = ColorTranslator.FromHtml(dark ? "#444145" : "#DBE5EC");
    }
    internal void Apply(Control root) {
        root.BackColor = Canvas; root.ForeColor = Ink;
        if (root is TextBox || root is ComboBox || root is ListBox) root.BackColor = Surface;
        var soft = root as SoftButton;
        if (soft != null) soft.Colors = this;
        var b = root as Button;
        if (b != null) {
            bool primary = Equals(b.Tag,"primary");
            b.FlatStyle = FlatStyle.Flat; b.UseVisualStyleBackColor = false;
            b.BackColor = primary ? Accent : Surface; b.ForeColor = primary ? Color.White : Ink;
            b.FlatAppearance.BorderColor = primary ? Accent : Line;
            b.Cursor = Cursors.Hand;
        }
        foreach (Control child in root.Controls) Apply(child);
    }
}

internal class SettingsWindow : Form {
    internal Preferences Result;
    internal SettingsWindow(Preferences current) {
        Text = "Настройки"; Font = new Font("Segoe UI",10); ClientSize = new Size(412,360);
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        var theme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Bounds = new Rectangle(22,42,368,30) };
        theme.Items.AddRange(new[]{"Светлая · белый и голубой","Тёмная · графит и красный"});
        theme.SelectedIndex = current.Dark ? 1 : 0;
        Controls.Add(new Label { Text = "Тема оформления", AutoSize = true, Left = 22, Top = 17 });
        Controls.Add(theme);
        Controls.Add(new Label { Text = "Названия приоритетов", AutoSize = true, Left = 22, Top = 97, Font = new Font(Font,FontStyle.Bold) });
        var names = new TextBox[3];
        for (int i = 0; i < 3; i++) {
            int y = 140 + 49*i;
            Controls.Add(new Label { Text = Library.Levels[i], Left = 22, Top = y+3, AutoSize = true });
            names[i] = new TextBox { Text = current.Labels[i], MaxLength = 32, Bounds = new Rectangle(134,y,256,30) };
            Controls.Add(names[i]);
        }
        var ok = new SoftButton { Text="Сохранить", Tag="primary", Bounds=new Rectangle(148,301,122,36) };
        var cancel = new SoftButton { Text="Отмена", DialogResult=DialogResult.Cancel, Bounds=new Rectangle(278,301,112,36) };
        ok.Click += delegate {
            var labels = names.Select(n => n.Text.Trim()).ToArray();
            if (labels.Any(String.IsNullOrWhiteSpace) || labels.Distinct(StringComparer.CurrentCultureIgnoreCase).Count() != 3) {
                MessageBox.Show(this,"Введите три разных непустых названия."); return;
            }
            Result = new Preferences { Dark = theme.SelectedIndex == 1, Labels = labels };
            DialogResult = DialogResult.OK;
        };
        Controls.AddRange(new Control[]{ok,cancel}); AcceptButton=ok; CancelButton=cancel;
        Action preview = delegate { new Palette(theme.SelectedIndex == 1).Apply(this); };
        theme.SelectedIndexChanged += delegate { preview(); }; preview();
    }
}

internal class Editor : Form {
    readonly TextBox title = new TextBox();
    readonly ComboBox format = new ComboBox(), priority = new ComboBox();
    internal Entry Result;

    internal Editor(Entry entry, Preferences prefs, string initialFormat = null) {
        Text = entry == null ? "Новое произведение" : "Редактирование";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(416, 264);
        Font = new Font("Segoe UI", 10);
        title.Text = entry == null ? "" : entry.Title;
        title.MaxLength = 300;
        format.Items.AddRange(Library.Formats);
        priority.Items.AddRange(prefs.Labels);
        format.DropDownStyle = priority.DropDownStyle = ComboBoxStyle.DropDownList;
        format.SelectedIndex = entry == null ? Math.Max(0,Array.IndexOf(Library.Formats, initialFormat)) : Array.IndexOf(Library.Formats, entry.Format);
        priority.SelectedIndex = entry == null ? 1 : entry.Priority;
        Field("Название", title, 12);
        Field("Формат", format, 78);
        Field("Приоритет", priority, 144);
        var ok = new SoftButton { Text = "Сохранить", Tag = "primary", Bounds = new Rectangle(174,218,116,32) };
        var cancel = new SoftButton { Text = "Отмена", Bounds = new Rectangle(298,218,100,32), DialogResult = DialogResult.Cancel };
        ok.Click += delegate {
            if (String.IsNullOrWhiteSpace(title.Text)) {
                MessageBox.Show(this, "Введите название произведения."); title.Focus(); return;
            }
            Result = new Entry { Title = title.Text.Trim(), Format = (string)format.SelectedItem, Priority = priority.SelectedIndex };
            DialogResult = DialogResult.OK;
        };
        Controls.AddRange(new Control[]{ok,cancel});
        AcceptButton = ok; CancelButton = cancel;
        new Palette(prefs.Dark).Apply(this);
    }

    void Field(string caption, Control field, int top) {
        Controls.Add(new Label { Text = caption, Left = 18, Top = top, AutoSize = true });
        field.SetBounds(18, top + 23, 380, 30);
        Controls.Add(field);
    }
}

internal class Shelf : Form {
    readonly string path;
    List<Entry> entries;
    readonly ListBox nav = new ListBox();
    readonly Label heading = new Label(), empty = new Label(), brand = new Label();
    readonly Panel sidebar = new Panel();
    readonly ListBox list = new ListBox();
    readonly Label status = new Label();
    readonly TextBox search = new TextBox();
    readonly Button edit, remove;
    readonly string settingsFile;
    Preferences prefs;
    Palette palette;
    readonly Font titleFont = new Font("Segoe UI",12,FontStyle.Bold);
    readonly Font collectionFont = new Font("Segoe UI",22,FontStyle.Bold);

    internal Shelf(string file) {
        path = file; entries = Library.Load(path);
        settingsFile = Path.Combine(Path.GetDirectoryName(file),"settings.xml");
        prefs = Preferences.Read(settingsFile);
        Text = "Полка — личная коллекция"; Font = new Font("Segoe UI",10);
        ClientSize = new Size(994,640); MinimumSize = new Size(856,450);
        StartPosition = FormStartPosition.CenterScreen; 
        var body = new Panel { Dock=DockStyle.Fill, Padding=new Padding(24,18,24,8) };
        sidebar.Dock=DockStyle.Left; sidebar.Width=202; sidebar.Padding=new Padding(12,20,12,8);
        brand.Text="Полка"; brand.Dock=DockStyle.Top; brand.Height=56; brand.Padding=new Padding(8,0,0,0);
        brand.Font=collectionFont;
        nav.Dock=DockStyle.Fill; nav.BorderStyle=BorderStyle.None; nav.IntegralHeight=false;
        nav.DrawMode=DrawMode.OwnerDrawFixed; nav.ItemHeight=32; nav.AccessibleName="Разделы коллекции";
        nav.Items.Add("Вся коллекция"); nav.Items.AddRange(Library.Formats); nav.SelectedIndex=0;
        nav.DrawItem+=DrawSection; sidebar.Controls.Add(nav); sidebar.Controls.Add(brand);
        var header = new Panel { Dock = DockStyle.Top, Height = 65 };
        heading.Text="Вся коллекция"; heading.Font=collectionFont; heading.AutoSize=true; heading.Top=4; header.Controls.Add(heading);
        var gear = MakeButton("⚙",delegate { Configure(); });
        gear.Font = new Font("Segoe UI Symbol",17); gear.Size = new Size(44,42);
        gear.AccessibleName = "Настройки"; header.Controls.Add(gear);
        header.Resize += delegate { gear.Location = new Point(header.ClientSize.Width - gear.Width,4); };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        var add = MakeButton("+ Добавить",delegate { Change(null); }); add.Tag = "primary"; bar.Controls.Add(add);
        edit = MakeButton("Изменить", delegate { if (Selected != null) Change(Selected); });
        remove = MakeButton("Удалить", delegate {
            var item = Selected;
            if (item != null && MessageBox.Show(this, "Удалить «" + item.Title + "»?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                var next = new List<Entry>(entries); next.Remove(item); Commit(next);
            }
        });
        bar.Controls.Add(edit); bar.Controls.Add(remove);
        bar.Controls.Add(new Label { Text="Поиск", AutoSize=true, Margin=new Padding(12,7,3,0) });
        search.Width = 138; search.AccessibleName = "Поиск по названию"; bar.Controls.Add(search);
        list.Dock = DockStyle.Fill; list.BorderStyle = BorderStyle.None; list.IntegralHeight = false;
        list.DrawMode = DrawMode.OwnerDrawFixed; list.ItemHeight = 66; list.DisplayMember = "Title";
        list.AccessibleName = "Коллекция произведений"; list.DrawItem += DrawEntry;
        list.SelectedIndexChanged += delegate { edit.Enabled = remove.Enabled = Selected != null; };
        list.DoubleClick += delegate { if (Selected != null) Change(Selected); };
        list.Resize += delegate { list.Invalidate(); };
        status.Dock = DockStyle.Bottom; status.Height = 32; status.Padding = new Padding(0,6,0,0);
        var content=new Panel {Dock=DockStyle.Fill};
        empty.Dock=DockStyle.Fill; empty.TextAlign=ContentAlignment.MiddleCenter; empty.Font=titleFont;
        content.Controls.Add(list); content.Controls.Add(empty);
        body.Controls.Add(content); body.Controls.Add(bar); body.Controls.Add(header); body.Controls.Add(status);
        Controls.Add(body); Controls.Add(sidebar);
        nav.SelectedIndexChanged += delegate {
            heading.Text = Convert.ToString(nav.SelectedItem);
            nav.Invalidate();
            RefreshList();
        };
        search.TextChanged += delegate { RefreshList(); };
        ApplyTheme(); RefreshList();
    }
    Entry Selected { get { return list.SelectedItem as Entry; } }
    void ApplyTheme() {
        palette = new Palette(prefs.Dark); palette.Apply(this); list.BackColor = palette.Canvas;
        sidebar.BackColor=brand.BackColor=nav.BackColor=palette.Side;
        nav.Invalidate(); empty.ForeColor=palette.Muted;
        status.ForeColor = palette.Muted; list.Invalidate();
    }
    void Configure() {
        using (var dialog = new SettingsWindow(prefs)) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { dialog.Result.Save(settingsFile); }
            catch (Exception ex) { MessageBox.Show(this,"Не удалось сохранить настройки.\n" + ex.Message); return; }
            prefs = dialog.Result; ApplyTheme();
        }
    }
    void DrawSection(object sender,DrawItemEventArgs e) {
        if(e.Index<0 || palette==null) return;
        bool active = (e.State & DrawItemState.Selected) != 0;
        using(var bg=new SolidBrush(palette.Side)) e.Graphics.FillRectangle(bg,e.Bounds);
        if(active) Shape.Fill(e.Graphics,Rectangle.Inflate(e.Bounds,-1,-2),palette.Tint,8);
        var r=e.Bounds; r.X+=10; r.Width-=15;
        TextRenderer.DrawText(e.Graphics,Convert.ToString(nav.Items[e.Index]),Font,r,palette.Ink,
            TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        if(nav.Focused && active) e.DrawFocusRectangle();
    }
    void DrawEntry(object sender, DrawItemEventArgs e) {
        if (e.Index < 0 || e.Index >= list.Items.Count) return;
        var entry = (Entry)list.Items[e.Index];
        var card = Rectangle.Inflate(e.Bounds,-1,-3);
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (var fill = new SolidBrush(palette.Canvas)) e.Graphics.FillRectangle(fill,e.Bounds);
        if(selected) Shape.Fill(e.Graphics,card,palette.Tint,10);
        else using(var pen=new Pen(palette.Line)) e.Graphics.DrawLine(pen,card.Left+14,card.Bottom,card.Right-14,card.Bottom);
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        var label = prefs.Labels[entry.Priority];
        int width = Math.Min(card.Width / 3,TextRenderer.MeasureText(label,Font).Width+24);
        var badge = new Rectangle(card.Right-width-14,card.Y+15,width,30);
        Shape.Fill(e.Graphics,badge,palette.BadgeColors[entry.Priority],15);
        TextRenderer.DrawText(e.Graphics,label,Font,badge,entry.Priority == 0 ? palette.OnAccent : palette.Ink,flags | TextFormatFlags.HorizontalCenter);
        int textWidth = badge.Left-card.Left-26;
        TextRenderer.DrawText(e.Graphics,entry.Title,titleFont,new Rectangle(card.X+14,card.Y+5,textWidth,27),palette.Ink,flags);
        TextRenderer.DrawText(e.Graphics,entry.Format,Font,new Rectangle(card.X+14,card.Y+31,textWidth,22),palette.Muted,flags);
        if (selected && list.Focused) ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(card,-3,-3),palette.Ink,palette.Tint);
    }
    protected override void Dispose(bool disposing) {
        if (disposing) { titleFont.Dispose(); collectionFont.Dispose(); }
        base.Dispose(disposing);
    }

    static Button MakeButton(string label, EventHandler handler) {
        var button = new SoftButton { Text = label, Size = new Size(96,32) };
        button.Click += handler; return button;
    }
    void Change(Entry old) {
        using (var dialog = new Editor(old,prefs,nav.SelectedItem as string)) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var next = new List<Entry>(entries);
            if (old == null) next.Add(dialog.Result); else next[next.IndexOf(old)] = dialog.Result;
            Commit(next);
        }
    }
    void Commit(List<Entry> next) {
        try { Library.Save(path,next); }
        catch (Exception ex) { MessageBox.Show(this,"Не удалось сохранить изменения.\n" + ex.Message); return; }
        entries = next; RefreshList();
    }
    void RefreshList() {
        list.BeginUpdate(); list.Items.Clear(); edit.Enabled = remove.Enabled = false;
        foreach (var e in entries.OrderBy(e => e.Priority).ThenBy(e => e.Title)) {
            if (nav.SelectedIndex > 0 && e.Format != (string)nav.SelectedItem) continue;
            if (e.Title.IndexOf(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) < 0) continue;
            list.Items.Add(e);
        }
        list.EndUpdate();
        empty.Visible=list.Items.Count==0;
        if(list.Items.Count==0) empty.BringToFront(); else list.BringToFront();
        empty.Text=entries.Count==0 ? "На полке пока пусто" : "Произведений не найдено";
        status.Text = entries.Count == 0 ? "Список пуст. Добавьте первое произведение." : "Отображено: " + list.Items.Count + " из " + entries.Count;
    }
}

internal class Program {
    [STAThread]
    static void Main() {
        using (var mutex = new Mutex(false, "Local\\PolkaCollectionWindow")) {
            try { if (!mutex.WaitOne(0)) { MessageBox.Show("Полка уже открыта."); return; } }
            catch (AbandonedMutexException) { }
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Polka","library.xml");
                Application.Run(new Shelf(path));
            } catch (Exception ex) { MessageBox.Show("Не удалось открыть коллекцию.\n" + ex.Message, "Полка"); }
            finally { mutex.ReleaseMutex(); }
        }
    }
}


