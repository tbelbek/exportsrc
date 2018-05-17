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
            InitializeComponent();
            InitBinding();

            Trace.Listeners.Add(new LabelTraceListener(labelProgress));
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
                        excludedProjectsBindingSource.Add(project);
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
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
                textBoxDestination.Text = folderBrowserDialog1.SelectedPath;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void InitBinding()
        {
            settingsBindingSource.DataSource = _settings;
            filterBindingSource.DataSource = _settings;
            filterBindingSource.DataMember = "Filters";
            replacementsBindingSource.DataSource = _settings;
            replacementsBindingSource.DataMember = "Replacements";
            excludedProjectsBindingSource.DataSource = _settings;
            excludedProjectsBindingSource.DataMember = "ExcludedProjects";
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

                if (string.IsNullOrEmpty(textBoxSource.Text))
                {
                    textBoxSource.Text = file;
                    return;
                }

                if (string.IsNullOrEmpty(textBoxDestination.Text))
                {
                    textBoxDestination.Text = file;
                    return;
                }

                textBoxSource.Text = file;
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
                    _settings = settings;
                    InitBinding();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            var src = textBoxSource.Text;
            var dst = textBoxDestination.Text;
            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst))
            {
                MessageBox.Show("Please select source and destination directories.");
                return;
            }

            button3.Enabled = false;
            ThreadPool.QueueUserWorkItem(
                o =>
                    {
                        var exporter = new Exporter(src, _settings);
                        var result = exporter.Export(dst);
                        BeginInvoke(
                            (Action)(() =>
                                            {
                                                button3.Enabled = true;
                                                labelProgress.Text = string.Empty;
                                            }));
                    });
        }

        private void Save(string path)
        {
            if (path == null)
            {
                if (_settingPath == null)
                    return;

                path = _settingPath;
            }

            try
            {
                var xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.CloseOutput = true;
                xmlWriterSettings.Indent = true;
                xmlWriterSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;

                using (var writer = XmlWriter.Create(path, xmlWriterSettings))
                {
                    _settings.Serialize(writer);
                }

                _settingPath = path;
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
            if (dialogResult == DialogResult.OK || dialogResult == DialogResult.Yes) Save(dialog.FileName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_settingPath == null) SaveAs();
            else Save(_settingPath);
        }

        private void SourceDirectoryButton_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
                textBoxSource.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}