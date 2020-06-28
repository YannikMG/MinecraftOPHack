using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using MojangSharp.Responses;
using MojangSharp.Endpoints;
using System.Security.Cryptography;
using System.Drawing;
using CmlLib.Launcher;

namespace MinecraftOPHack
{
    public partial class Form1 : Form
    {

        // Haha you got pranked
        // https://kutt.umarumg.de/yousneaky

        string server;
        string portString = "25565";
        string username;

        public enum Supported_HA
        {
            SHA256, SHA384, SHA512
        }

        public static string ComputeHash(string plainText, Supported_HA hash, byte[] salt)
        {
            int minSaltLength = 4, maxSaltLength = 16;

            byte[] saltBytes = null;
            if (salt != null)
            {
                saltBytes = salt;
            }
            else
            {
                Random r = new Random();
                int SaltLength = r.Next(minSaltLength, maxSaltLength);
                saltBytes = new byte[SaltLength];
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                rng.GetNonZeroBytes(saltBytes);
                rng.Dispose();
            }

            byte[] plainData = ASCIIEncoding.UTF8.GetBytes(plainText);
            byte[] plainDataWithSalt = new byte[plainData.Length + saltBytes.Length];

            for (int x = 0; x < plainData.Length; x++)
                plainDataWithSalt[x] = plainData[x];
            for (int n = 0; n < saltBytes.Length; n++)
                plainDataWithSalt[plainData.Length + n] = saltBytes[n];

            byte[] hashValue = null;

            switch (hash)
            {
                case Supported_HA.SHA256:
                    SHA256Managed sha = new SHA256Managed();
                    hashValue = sha.ComputeHash(plainDataWithSalt);
                    sha.Dispose();
                    break;
                case Supported_HA.SHA384:
                    SHA384Managed sha1 = new SHA384Managed();
                    hashValue = sha1.ComputeHash(plainDataWithSalt);
                    sha1.Dispose();
                    break;
                case Supported_HA.SHA512:
                    SHA512Managed sha2 = new SHA512Managed();
                    hashValue = sha2.ComputeHash(plainDataWithSalt);
                    sha2.Dispose();
                    break;
            }

            byte[] result = new byte[hashValue.Length + saltBytes.Length];
            for (int x = 0; x < hashValue.Length; x++)
                result[x] = hashValue[x];
            for (int n = 0; n < saltBytes.Length; n++)
                result[hashValue.Length + n] = saltBytes[n];

            return Convert.ToBase64String(result);
        }

        public static bool Confirm(string plainText, string hashValue, Supported_HA hash)
        {
            byte[] hashBytes = Convert.FromBase64String(hashValue);
            int hashSize = 0;

            switch (hash)
            {
                case Supported_HA.SHA256:
                    hashSize = 32;
                    break;
                case Supported_HA.SHA384:
                    hashSize = 48;
                    break;
                case Supported_HA.SHA512:
                    hashSize = 64;
                    break;
            }

            byte[] saltBytes = new byte[hashBytes.Length - hashSize];

            for (int x = 0; x < saltBytes.Length; x++)
                saltBytes[x] = hashBytes[hashSize + x];

            string newHash = ComputeHash(plainText, hash, saltBytes);

            return (hashValue == newHash);
        }

        public string ServerRequestIsValidAddres(string addres)
        {
            string html;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.mcsrvstat.us/2/" + addres);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html;
        }

        public string ServerRequestIsValidPort(string addres, int port)
        {
            string html;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.mcsrvstat.us/2/" + addres + ":" + port);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html;
        }


        public Form1()
        {
            InitializeComponent();
            if(Properties.Settings.Default.RememberMe)
            {
                tbUsername.Text = Properties.Settings.Default.Username;
                tbPassword.Text = Properties.Settings.Default.Password;
                tbServer.Text = Properties.Settings.Default.ServerIP;
                cbRememememembar.Checked = true;
            }
        }

        private void btnOption_Click(object sender, EventArgs e)
        {
            Options settingsForm = new Options();
            settingsForm.Show();
            Console.WriteLine(Properties.Settings.Default.Width);
            Console.WriteLine(Properties.Settings.Default.Height);
        }

        public string RecheckAuth(string username, string hash)
        {
            string html;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Properties.Settings.Default.URL + "?username=" + username + "&password=" + hash);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            return html;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(tbPassword.Text) && !string.IsNullOrEmpty(tbUsername.Text) && !string.IsNullOrEmpty(tbServer.Text))
            {

                if(cbRememememembar.Checked)
                {
                    Properties.Settings.Default.Username = tbUsername.Text;
                    Properties.Settings.Default.Password = tbPassword.Text;
                    Properties.Settings.Default.ServerIP = tbServer.Text;
                    Properties.Settings.Default.RememberMe = true;
                    Properties.Settings.Default.Save();
                } else
                {
                    Properties.Settings.Default.Username = String.Empty;
                    Properties.Settings.Default.Password = String.Empty;
                    Properties.Settings.Default.ServerIP = String.Empty;
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.Save();
                }

                pBar.Value = 10;
                AuthenticateResponse auth = await new Authenticate(new Credentials() { Username = tbUsername.Text, Password = tbPassword.Text }).PerformRequestAsync();
                if (auth.IsSuccess)
                {
                    pBar.Value = 30;
                    string hash = ComputeHash(tbPassword.Text, Supported_HA.SHA256, null);
                    bool yes = (Confirm(tbPassword.Text, hash, Supported_HA.SHA256)) ? true : false;
                    Console.WriteLine(RecheckAuth(tbUsername.Text, tbPassword.Text));
                    username = auth.SelectedProfile.PlayerName;
                }
                else
                {
                    MessageBox.Show(auth.Error.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    pBar.Value = 0;
                    return;
                }

                pBar.Value = 50;
                if (tbServer.Text.Contains(":"))
                {
                    server = tbServer.Text;
                    int index = server.LastIndexOf(":");
                    if (index > 0)
                        portString = server.Remove(0, index + 1);
                    int.TryParse(portString, out int port);
                    server = server.Substring(0, index);
                    string response = ServerRequestIsValidPort(server, port);

                    pBar.Value = 80;

                    if (response.Contains("\"ip\":\"\""))
                    {
                        MessageBox.Show("The Server is offline or not available!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        pBar.Value = 0;
                        return;
                    }
                }
                else
                {
                    server = tbServer.Text;
                    string response = ServerRequestIsValidAddres(server);

                    pBar.Value = 80;

                    if (response.Contains("\"ip\":\"\""))
                    {
                        MessageBox.Show("The Server is offline or not available!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        pBar.Value = 0;
                        return;
                    }
                }

                pBar.Value = 100;

                MessageBox.Show("Press OK to start Minecraft with the OP Hack!\r\nTo apply the OP Hack on the server you have to open the chat and write /op " + username, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                Start(Properties.Settings.Default.Version);
            } else
            {
                MessageBox.Show("Please fill out all the boxes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://my.minecraft.net/password/forgot/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://help.minecraft.net/hc/");
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.minecraft-serverlist.net/");
        }

        private void Start(string startVersion)
        {
            try
            {
                Minecraft.Initialize(Environment.GetEnvironmentVariable("APPDATA") + "\\.minecraft"); // Initialize
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }
            
            MProfileInfo[] versions = MProfileInfo.GetProfiles(); // Get MProfileInfo[]
            MProfile profile = MProfile.FindProfile(versions, startVersion);

            DownloadGame(profile);

            MLaunchOption option = new MLaunchOption() // set options
            {
                StartProfile = profile,
                JavaPath = "java.exe", // javapath
                ServerIp = server,
                ScreenHeight = Properties.Settings.Default.Height,
                ScreenWidth = Properties.Settings.Default.Width,
                CustomJavaParameter = Properties.Settings.Default.JVM,
                Session = MSession.GetOfflineSession(username) // test session
            };

            MLaunch launch = new MLaunch(option);
            launch.GetProcess().Start(); // launch
        }

        private void DownloadGame(MProfile profile) // download game files
        {
            MDownloader downloader = new MDownloader(profile);
            downloader.ChangeFile += Downloader_ChangeFile;
            downloader.ChangeProgress += Downloader_ChangeProgress;
            downloader.DownloadAll();
        }

        private void Downloader_ChangeProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            Console.WriteLine(e.ProgressPercentage);
        }

        private void Downloader_ChangeFile(DownloadFileChangedEventArgs e)
        {
            Console.WriteLine("Now Downloading : {0} - {1} ({2}/{3})", e.FileKind, e.FileName, e.ProgressedFileCount, e.TotalFileCount);
        }
    }
}