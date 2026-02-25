using System.IO.Compression;
using System.Net;

namespace e2eReportTrace;

public partial class Form1 : Form
{
    private HttpListener _listener;
    private const string UrlPrefix = "http://localhost:5000/";

    public Form1()
    {
        InitializeComponent();

        this.AllowDrop = true;
        this.DragEnter += Form1_DragEnter;
        this.DragDrop += Form1_DragDrop;

        StartServer();
        InitializeWebView();
    }

    private void InitializeWebView()
    {
        webView21.Dock = DockStyle.Fill;
        webView21.CoreWebView2InitializationCompleted += (s, e) =>
        {
            webView21.CoreWebView2.Navigate(UrlPrefix);
        };
        webView21.EnsureCoreWebView2Async(null);
    }

    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        ExtractZip(files);
    }

    private void ExtractZip(string[] files)
    {
        foreach (var file in files)
        {
            if (Path.GetExtension(file).ToLower() == ".zip")
            {
                string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                if (Directory.Exists(wwwrootPath))
                    Directory.Delete(wwwrootPath, true);

                ZipFile.ExtractToDirectory(file, wwwrootPath);
                webView21.CoreWebView2.Reload();
            }
        }
    }

    private void StartServer()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(UrlPrefix);
        _listener.Start();

        Task.Run(async () =>
        {
            while (true)
            {
                var context = await _listener.GetContextAsync();
                var requestPath = context.Request.Url.AbsolutePath.TrimStart('/');

                if (string.IsNullOrEmpty(requestPath))
                    requestPath = "index.html";

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", requestPath);

                if (File.Exists(filePath))
                {
                    byte[] content = File.ReadAllBytes(filePath);
                    context.Response.ContentType = GetContentType(filePath);
                    context.Response.ContentLength64 = content.Length;
                    await context.Response.OutputStream.WriteAsync(content, 0, content.Length);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    byte[] msg = System.Text.Encoding.UTF8.GetBytes("404 - Not Found");
                    await context.Response.OutputStream.WriteAsync(msg, 0, msg.Length);
                }

                context.Response.OutputStream.Close();
            }
        });
    }

    private string GetContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _listener?.Stop();
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Open file dialog to select a zip file
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            // file ends in -report.zip or -report (*).zip
            openFileDialog.Filter = "Zip files (*-report.zip;*-report (*).zip)|*-report.zip;*-report (*).zip|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExtractZip(new string[] { openFileDialog.FileName });
            }
        }
    }

    private void homeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // goto home page
        webView21.CoreWebView2.Navigate(UrlPrefix);
    }

    private void toolStripMenuItem1_Click(object sender, EventArgs e)
    {
        //go back a page
        if (webView21.CoreWebView2.CanGoBack)
            webView21.CoreWebView2.GoBack();
    }

    private void toolStripMenuItem2_Click(object sender, EventArgs e)
    {
        //go forward a page
        if (webView21.CoreWebView2.CanGoForward)
            webView21.CoreWebView2.GoForward();
    }

    private void toolStripMenuItem3_Click(object sender, EventArgs e)
    {
        //reload the page
        webView21.CoreWebView2.Reload();
    }

    private void closeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // close the application
        Close();
    }
}
