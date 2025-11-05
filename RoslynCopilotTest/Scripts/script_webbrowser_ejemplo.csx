using System.Windows.Forms;

var form = new Form();
form.Text = "Navegador clásico";
form.Width = 900;
form.Height = 600;

var browser = new WebBrowser();
browser.Dock = DockStyle.Fill;
browser.Url = new System.Uri("https://es.wikipedia.org/wiki/Revit");
form.Controls.Add(browser);

form.ShowDialog();

return "✅ WebBrowser clásico mostrado";