using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Laravel_Setup
{
    public partial class Form1 : Form
    {

        private static StringBuilder output = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {

            installdir_textbox.Text = Application.StartupPath;

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("laravel", "1"));

            var repo = "laravel/laravel";
            var contentsUrl = $"https://api.github.com/repos/{repo}/releases?per_page=100";
            var contentsJson = await httpClient.GetStringAsync(contentsUrl);
            var contents = (JArray)JsonConvert.DeserializeObject(contentsJson);

            foreach (var file in contents)
            {
                var fileType = (string)file["type"];

                var TagName = file["tag_name"];
                string VersionName = TagName.ToString().Substring(1, TagName.ToString().Length - 1);

                //Console.WriteLine(file["tag_name"]);
                comboBox1.Items.Add(VersionName);
                //if (fileType == "dir")
                //{
                // var directoryContentsUrl = (string)file["url"];
                // Console.WriteLine($"DIR: {directoryContentsUrl}");
                //}
                // else if (fileType == "file")
                //{
                //var downloadUrl = (string)file["download_url"];
                // Console.WriteLine($"DOWNLOAD: {downloadUrl}");
                //}
            }

            //projectname_textbox.Text = "";

            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            bool InstalledPhp = false;
            bool InstalledComposer = false;
            try
            {
                //\wamp64\bin\php\php8.2.1; 
                //\wamp64\composer;
                //\wamp64\bin\php\php8.2.1;
                //\wamp64\bin\php\Composer;
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("Path");
                        if (o != null)
                        {
                            string[] RegValues = o.ToString().Split(';');
                            foreach (var RegValue in RegValues)
                            {
                                if (RegValue.Contains("php"))
                                {
                                    InstalledPhp = true;
                                }
                                if (RegValue.Contains("composer"))
                                {
                                    InstalledComposer = true;
                                }
                                //Console.WriteLine(RegValue);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (InstalledPhp != true)
            {
                buildButton.Enabled = false;
                listBox1.Items.Add("Sisteminizde kurulu PHP bulunamadı");
                return;
            }

            if (InstalledComposer != true)
            {
                buildButton.Enabled = false;
                listBox1.Items.Add("Sisteminizde kurulu Composer bulunamadı");
                return;
            }

        }

        private void buildButton_Click(object sender, EventArgs e)
        {
            string projectnameDirectroyName = projectname_textbox.Text;

            if (string.IsNullOrEmpty(projectnameDirectroyName))
            {
                MessageBox.Show("Proje adı giriniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                //listBox1.Items.Add("Proje adı giriniz.");
                return;
            }
            if (Directory.Exists(projectnameDirectroyName))
            {
                MessageBox.Show("Bu isimde bir proje bulunuyor.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                //listBox1.Items.Add("Bu isimde bir proje bulunuyor.");
                return;
            }

            listBox1.Items.Add("Laravel v." + comboBox1.Text + " " + projectname_textbox.Text + " adıyla  kuruluyor, lütfen bekleyiniz...");
            buildButton.Enabled = false;

            Process process = new Process();
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            // composer create-project laravel/laravel:^9.* project_name
            process.StartInfo.FileName = "cmd.exe";
            //process.StartInfo.WorkingDirectory = Application.StartupPath;
            process.StartInfo.Arguments = "/C composer create-project laravel/laravel:^" + comboBox1.Text + " " + projectname_textbox.Text;
            Console.WriteLine("/C composer create-project laravel/laravel:^" + comboBox1.Text + " " + projectname_textbox.Text);
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;

            process.OutputDataReceived += new DataReceivedEventHandler((ss, ee) =>
            {
                if (!String.IsNullOrEmpty(ee.Data))
                {
                    Console.WriteLine(ee.Data);
                }
            });

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            Console.WriteLine(output);
            process.WaitForExit();
            process.Close();

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadLine();
            listBox1.Items.Add("Laravel " + comboBox1.Text + " " + projectname_textbox.Text + " adıyla başarıyla kuruldu...");
            buildButton.Enabled = true;
        }


    }
}
