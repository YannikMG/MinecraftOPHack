using CmlLib.Launcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftOPHack
{
    public partial class Options : Form
    {
        public Options()
        {
            InitializeComponent();

            Minecraft.Initialize(Environment.GetEnvironmentVariable("APPDATA") + "\\.minecraft"); // Initialize

            MProfileInfo[] versions = MProfileInfo.GetProfiles(); // Get MProfileInfo[]
            foreach (var version in versions)
            {
                cbVersions.Items.Add(version.Name);
            }
            cbVersions.SelectedIndex = cbVersions.FindString(Properties.Settings.Default.Version);
            cbRes.SelectedIndex = cbRes.FindString(Properties.Settings.Default.Width + "x" + Properties.Settings.Default.Height);
            textBox1.Text = Properties.Settings.Default.JVM;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Version = cbVersions.SelectedItem.ToString();
            Properties.Settings.Default.JVM = textBox1.Text;
            string[] list = cbRes.SelectedItem.ToString().Split('x');
            Properties.Settings.Default.Width = int.Parse(list[0]);
            Properties.Settings.Default.Height = int.Parse(list[1]);
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
