using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace TrinityConfigsManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private int rowIndex = 0;
        private string pathOfFile;

        private void addRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add(false, "changeme", "changeme", "");
        }

        private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!dataGridView1.Rows[rowIndex].IsNewRow)
            {
                dataGridView1.Rows.RemoveAt(rowIndex);
            }
        }

        private void saveCollectionButton_Click(object sender, EventArgs e)
        {
            try
            {
                string pathFile = comboBox1.Text + ".xml";

                DataTable dt = new DataTable();
                dt.TableName = "ConfigJobs";
                
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    if (dataGridView1.Columns[i].Visible) // Add's only Visible columns (if you need it)
                    {
                        string headerText = dataGridView1.Columns[i].HeaderText;
                        headerText = Regex.Replace(headerText, "[-/, ]", "_");
                
                        DataColumn column = new DataColumn(headerText);
                        dt.Columns.Add(column);
                    }
                }
                
                foreach (DataGridViewRow DataGVRow in dataGridView1.Rows)
                {
                    DataRow dataRow = dt.NewRow();
                    // Add's only the columns that you want
                    dataRow[0] = DataGVRow.Cells[0].Value;
                    dataRow[1] = DataGVRow.Cells[1].Value;
                    dataRow[2] = DataGVRow.Cells[2].Value;
                
                    dt.Rows.Add(dataRow); //dt.Columns.Add();
                }
                DataSet ds = new DataSet();
                ds.Tables.Add(dt);
                
                //Finally the save part:
                XmlTextWriter xmlSave = new XmlTextWriter(pathFile, Encoding.UTF8);
                xmlSave.Formatting = Formatting.Indented;
                ds.DataSetName = "Data";
                ds.WriteXml(xmlSave);
                xmlSave.Close();

                if (!comboBox1.Items.Contains(comboBox1.Text))
                    comboBox1.Items.Add(comboBox1.Text);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string filepath = Directory.GetCurrentDirectory();
                DirectoryInfo d = new DirectoryInfo(filepath);
                foreach (var file in d.GetFiles("*.xml"))
                {
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(file.Name));
                    comboBox1.SelectedIndex = 0;
                }
            }
            catch (Exception exception) {  MessageBox.Show(exception.Message); }

            // focus on last row
            try { dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0]; } catch { }

            label1.Text = "Total: " + dataGridView1.RowCount.ToString() + " configs prepared.";
        }

        private void deleteCollectionButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to delete?", "Confirmation", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                string fileName = comboBox1.SelectedItem.ToString() + ".xml";

                if (File.Exists(fileName))
                    File.Delete(fileName);

                comboBox1.Items.Remove(comboBox1.SelectedItem);
                comboBox1.SelectedIndex = 0;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fileName = comboBox1.SelectedItem.ToString() + ".xml";

            dataGridView1.Rows.Clear();

            XDocument xmlDoc = XDocument.Load(fileName);

            foreach (var coordinate in xmlDoc.Descendants("ConfigJobs"))
            {
                dataGridView1.Rows.Add(coordinate.Element("Check").Value,
                    coordinate.Element("Config_Name").Value,
                    coordinate.Element("Set_Value").Value);
            };
        }

        private void buttonCheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells[0].Value = true;
                row.Cells[0].Selected = true;
            }
        }
        private void buttonUncheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells[0].Value = false;
                row.Cells[0].Selected = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                if ((bool)dataGridView1.Rows[i].Cells[0].FormattedValue)
                {
                    dataGridView1.Rows.RemoveAt(i);
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add(false, "......", "......");

            // focus on last row
            try { dataGridView1.CurrentCell = dataGridView1.Rows[dataGridView1.RowCount - 1].Cells[0]; } catch { }
        }

        private void buttonSelectPathOfFile(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                pathOfFile = openFileDialog1.InitialDirectory + openFileDialog1.FileName;
                textBox1.Text = pathOfFile;
                label1.Text = "Changed file path";
            }
        }

        private async void buttonDoReplaceJob_Click(object sender, EventArgs e)
        {
            int replacesCount = 0;

            DialogResult result = MessageBox.Show("This action will replace specified config values with yours.", "Confirmation", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (File.Exists(pathOfFile))
                {
                    label1.Text = "Running task, please wait...";
                    replacesCount = await Task.Run(() => DoReplaceJob(1000));
                    label1.Text = "Jobs finished: " + replacesCount + " configs affected.";
                }
                else
                    MessageBox.Show("Please select a valid file!");
            }
        }

        private int DoReplaceJob(int sleepTime)
        {
            int replacesCount = 0;
            string text = string.Empty;

            using (StreamReader sr = new StreamReader(pathOfFile))
            {
                int i = 0;
                do
                {
                    i++; string line = sr.ReadLine();

                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (!line.StartsWith("#")) // ignore hashtag lines
                        {
                            if (line.Contains('=')) // if contains our separator
                            {
                                string configName = line.Split('=').First();

                                if (configName.Contains(row.Cells[1].Value.ToString())) // if found the config we are looking for
                                {
                                    string configValue = line.Split('=').Last();

                                    if (!string.IsNullOrEmpty(configValue)) // if string after separator is not null or empty
                                    {
                                        string newline = configName + "= " + row.Cells[2].Value.ToString(); // prepare new line
                                        line = line.Replace(line, newline);
                                        replacesCount++;
                                    }
                                }
                            }
                        }
                    }
                    text = text + line + Environment.NewLine;

                } while (sr.EndOfStream == false);
            }

            if (replacesCount > 0) 
                File.WriteAllText(pathOfFile, text);

            Thread.Sleep(sleepTime);

            return replacesCount;
        }
    }
}
