using System; // build from the 4th of May of 2026
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

class CoolPhotoViewer : Form
{
    Bitmap currentBitmap;
    float zoom = 1.0f;
    string currentFile = null;

    public CoolPhotoViewer(string fileToOpen = null)
    {
        this.Text = "Cool Photo Viewer";
        this.Width = 900;
        this.Height = 700;
        this.DoubleBuffered = true;

        this.MouseWheel += OnMouseWheel;

        CreateMenu();

        if (!string.IsNullOrEmpty(fileToOpen) && File.Exists(fileToOpen))
        {
            LoadImage(fileToOpen);
        }
    }

    void CreateMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("Open", null, (s, e) => OpenFile());
        fileMenu.DropDownItems.Add("Save As", null, (s, e) => SaveAs());

        var editMenu = new ToolStripMenuItem("Edit");
        editMenu.DropDownItems.Add("Edit In MS Paint", null, (s, e) => OpenExternal("mspaint.exe", false));
        editMenu.DropDownItems.Add("Edit In Photos", null, (s, e) => OpenExternal("ms-photos:", false));
        editMenu.DropDownItems.Add("Edit In Notepad", null, (s, e) => OpenExternal("notepad.exe", true));
        editMenu.DropDownItems.Add("Edit In Visual Studio Code", null, (s, e) => OpenExternal("code", true));

        var viewMenu = new ToolStripMenuItem("View");
        viewMenu.DropDownItems.Add("Zoom In (10%)", null, (s, e) => { zoom *= 1.1f; ClampZoom(); Invalidate(); });
        viewMenu.DropDownItems.Add("Zoom Out (-10%)", null, (s, e) => { zoom *= 0.9f; ClampZoom(); Invalidate(); });

        menu.Items.Add(fileMenu);
        menu.Items.Add(editMenu);
        menu.Items.Add(viewMenu);

        this.MainMenuStrip = menu;
        this.Controls.Add(menu);
    }

    void OpenFile()
    {
        var ofd = new OpenFileDialog();
        ofd.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.jfif;*.txi;*.txia|All files|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            LoadImage(ofd.FileName);
        }
    }

    void LoadImage(string path)
    {
        currentFile = path;

        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".txi" || ext == ".txia")
            currentBitmap = LoadTXI(path);
        else
            currentBitmap = new Bitmap(path);

        FitImage();
        Invalidate();
    }

    Bitmap LoadTXI(string path)
    {
        string[] lines = File.ReadAllLines(path);

        if (lines.Length < 3)
            throw new Exception("Invalid TXI file.");

        string header = lines[0].Trim();
        string footer = lines[lines.Length - 1].Trim();

        bool hasAlpha;

        if (header == "!TXI" && footer == "TXI!")
            hasAlpha = false;
        else if (header == "!TXIA" && footer == "TXIA!")
            hasAlpha = true;
        else
            throw new Exception("TXI header/footer mismatch.");

        var rows = lines.Skip(1).Take(lines.Length - 2)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray();

        int height = rows.Length;
        int width = rows[0].Trim()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < height; y++)
        {
            var pixels = rows[y].Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int x = 0; x < width; x++)
            {
                string px = pixels[x];

                if (!hasAlpha && px.Length == 6)
                    px += "FF";

                int r = Convert.ToInt32(px.Substring(0, 2), 16);
                int g = Convert.ToInt32(px.Substring(2, 2), 16);
                int b = Convert.ToInt32(px.Substring(4, 2), 16);
                int a = Convert.ToInt32(px.Substring(6, 2), 16);

                bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            }
        }

        return bmp;
    }

    void SaveAs()
    {
        if (currentBitmap == null) return;

        var sfd = new SaveFileDialog();
        sfd.Filter = "PNG|*.png|JPG|*.jpg|BMP|*.bmp|TXI|*.txi|TXIA|*.txia";

        if (sfd.ShowDialog() != DialogResult.OK) return;

        string ext = Path.GetExtension(sfd.FileName).ToLower();

        if (ext == ".png")
            currentBitmap.Save(sfd.FileName, ImageFormat.Png);
        else if (ext == ".jpg")
            currentBitmap.Save(sfd.FileName, ImageFormat.Jpeg);
        else if (ext == ".bmp")
            currentBitmap.Save(sfd.FileName, ImageFormat.Bmp);
        else if (ext == ".txi" || ext == ".txia")
            SaveTXI(sfd.FileName, ext == ".txia");
    }

    void SaveTXI(string path, bool withAlpha)
    {
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.WriteLine(withAlpha ? "!TXIA" : "!TXI");

            for (int y = 0; y < currentBitmap.Height; y++)
            {
                for (int x = 0; x < currentBitmap.Width; x++)
                {
                    Color c = currentBitmap.GetPixel(x, y);

                    if (withAlpha)
                        sw.Write(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.R, c.G, c.B, c.A));
                    else
                        sw.Write(string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B));

                    if (x < currentBitmap.Width - 1)
                        sw.Write(" ");
                }
                sw.WriteLine();
            }

            sw.WriteLine(withAlpha ? "TXIA!" : "TXI!");
        }
    }

    void OpenExternal(string app, bool txiOnly)
    {
        if (currentFile == null) return;

        string ext = Path.GetExtension(currentFile).ToLower();
        bool isTXI = (ext == ".txi" || ext == ".txia");

        if (txiOnly && !isTXI) return;
        if (!txiOnly && isTXI) return;

        try
        {
            Process.Start(app, "\"" + currentFile + "\"");
        }
        catch
        {
            MessageBox.Show("Could not open external program.");
        }
    }

    void FitImage()
    {
        if (currentBitmap == null) return;

        float scaleX = (float)this.ClientSize.Width / currentBitmap.Width;
        float scaleY = (float)this.ClientSize.Height / currentBitmap.Height;

        zoom = Math.Min(scaleX, scaleY);
        ClampZoom();
    }

    void ClampZoom()
    {
        if (zoom < 0.05f) zoom = 0.05f;
        if (zoom > 20f) zoom = 20f;
    }

    void OnMouseWheel(object sender, MouseEventArgs e)
    {
        if (e.Delta > 0)
            zoom *= 1.1f;
        else
            zoom *= 0.9f;

        ClampZoom();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (currentBitmap != null)
        {
            int w = (int)(currentBitmap.Width * zoom);
            int h = (int)(currentBitmap.Height * zoom);

            int x = (this.ClientSize.Width - w) / 2;
            int y = (this.ClientSize.Height - h) / 2 + 15;

            e.Graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            e.Graphics.DrawImage(currentBitmap, x, y, w, h);
        }
    }

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.Run(new CoolPhotoViewer(args.Length > 0 ? args[0] : null));
    }
}