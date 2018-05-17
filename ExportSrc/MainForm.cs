#region usings

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

#endregion

namespace ExportSrc
{
    public partial class MainForm : Form
    {
        private string _settingPath;

        private Settings _settings = Settings.GetDefault();

        public MainForm()
        {
            this.InitializeComponent();
            this.InitBinding();

            Trace.Listeners.Add(new LabelTraceListener(this.labelProgress));
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        private void dataGridView3_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)

                // TODO handle sln file
                try
                {
                    // csproj
                    var doc = new XmlDocument();
                    doc.Load(file);

                    var xmlnsmgr = new XmlNamespaceManager(doc.NameTable);
                    xmlnsmgr.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

                    var node = doc.SelectSingleNode("//msbuild:ProjectGuid", xmlnsmgr);
                    if (node == null)
                        return;

                    Guid projectGuid;
                    if (Guid.TryParse(node.InnerText, out projectGuid))
                    {
                        var project = new Project();
                        project.Id = projectGuid;
                        project.Name = Path.GetFileNameWithoutExtension(file);
                        this.excludedProjectsBindingSource.Add(project);
                    }
                }
                catch (Exception ex)
                {
                }
        }

        private void dataGridView3_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void DestinationDirectoryButton_Click(object sender, EventArgs e)
        {
            var result = this.folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
                this.textBoxDestination.Text = this.folderBrowserDialog1.SelectedPath;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void InitBinding()
        {
            this.settingsBindingSource.DataSource = this._settings;
            this.filterBindingSource.DataSource = this._settings;
            this.filterBindingSource.DataMember = "Filters";
            this.replacementsBindingSource.DataSource = this._settings;
            this.replacementsBindingSource.DataMember = "Replacements";
            this.excludedProjectsBindingSource.DataSource = this._settings;
            this.excludedProjectsBindingSource.DataMember = "ExcludedProjects";
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var txt = sender as TextBox;
                if (txt != null)
                {
                    txt.Text = file;
                    return;
                }

                if (string.IsNullOrEmpty(this.textBoxSource.Text))
                {
                    this.textBoxSource.Text = file;
                    return;
                }

                if (string.IsNullOrEmpty(this.textBoxDestination.Text))
                {
                    this.textBoxDestination.Text = file;
                    return;
                }

                this.textBoxSource.Text = file;
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            var dialogResult = dialog.ShowDialog(this);
            if (dialogResult == DialogResult.OK || dialogResult == DialogResult.Yes)
                try
                {
                    var settings = Settings.Deserialize(dialog.FileName);
                    this._settings = settings;
                    this.InitBinding();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            var src = this.textBoxSource.Text;
            var dst = this.textBoxDestination.Text;
            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst))
            {
                MessageBox.Show("Please select source and destination directories.");
                return;
            }

            this.button3.Enabled = false;
            ThreadPool.QueueUserWorkItem(
                o =>
                    {
                        var exporter = new Exporter(src, this._settings);
                        var result = exporter.Export(dst);
                        this.BeginInvoke(
                            (Action)(() =>
                                            {
                                                this.button3.Enabled = true;
                                                this.labelProgress.Text = string.Empty;
                                            }));
                    });
        }

        private void Save(string path)
        {
            if (path == null)
            {
                if (this._settingPath == null)
                    return;

                path = this._settingPath;
            }

            try
            {
                var xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.CloseOutput = true;
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;

                using (var writer = XmlWriter.Create(path, xmlWriterSettings))
                {
                    this._settings.Serialize(writer);
                }

                this._settingPath = path;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SaveAs()
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.AutoUpgradeEnabled = true;
            dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";

            var dialogResult = dialog.ShowDialog(this);
            if (dialogResult == DialogResult.OK || dialogResult == DialogResult.Yes) this.Save(dialog.FileName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveAs();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this._settingPath == null) this.SaveAs();
            else this.Save(this._settingPath);
        }

        private void SourceDirectoryButton_Click(object sender, EventArgs e)
        {
            var result = this.folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
                this.textBoxSource.Text = this.folderBrowserDialog1.SelectedPath;
        }
    }
}