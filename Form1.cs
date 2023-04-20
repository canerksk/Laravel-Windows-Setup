using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Caching;

namespace Laravel_Setup
{

    public partial class Form1 : Form
    {

        private static StringBuilder output = new StringBuilder();
        private const string BaseUrl = "https://api.github.com";
        private const string Owner = "laravel";
        private const string Repo = "laravel";
        public static string LocalReleaseFile = Path.Combine(Application.StartupPath, "releases.json");


        public Form1()
        {
            InitializeComponent();
        }

        // Uzak Urlden Releaseleri getir.
        private static List<Release> GetAllReleases()
        {
            var releasesDiffList = new List<Release>();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
            var page = 1;
            while (true)
            {
                var url = $"{BaseUrl}/repos/{Owner}/{Repo}/releases?page={page}&per_page=1000";
                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var pageReleases = JsonConvert.DeserializeObject<List<Release>>(json);
                    if (pageReleases.Count == 0)
                    {
                        break;
                    }
                    releasesDiffList.AddRange(pageReleases);
                    page++;
                }
                else
                {
                    throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            return releasesDiffList;
        }

        // Yerel Dosyadan Releaseleri getir.
        private static List<Release> GetReleasesFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Release>>(json);
        }


        // Release Json List Elements
        public class Release
        {
            public string Name { get; set; }
            public string Tag_Name { get; set; }
            public string Body { get; set; }
            public DateTimeOffset Published_At { get; set; }
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {

            installdir_textbox.Text = Application.StartupPath;


            if (!File.Exists(LocalReleaseFile))
            {
                Console.WriteLine("Uzaktan getir.");

                var releasesDiff = GetAllReleases();
                var json = JsonConvert.SerializeObject(releasesDiff);
                File.WriteAllText(LocalReleaseFile, json);
            }
            else
            {
                Console.WriteLine("Yerelden getir.");
                var releasesLocal = GetReleasesFromFile(LocalReleaseFile);
                foreach (var release in releasesLocal)
                {
                    if (!string.IsNullOrEmpty(release.Tag_Name))
                    {
                        string VersionName = release.Tag_Name.ToString().Substring(1, release.Tag_Name.ToString().Length - 1);
                        comboBox1.Items.Add(VersionName);
                        //Console.WriteLine($"Name: {release.Name}, Tag Name: {release.Tag_Name}, Published At: {release.Published_At}");
                    }
                }
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

            string AppServiceProviderPath = Path.Combine(Application.StartupPath, projectname_textbox.Text, "app", "Provides", "AppServiceProvider.php");
            if (File.Exists(AppServiceProviderPath))
            {

                StreamWriter writer = new StreamWriter(AppServiceProviderPath);
                StreamReader reader = new StreamReader(AppServiceProviderPath);

                // Use
                int satirNoUse = 6;
                string yeniIcerikUse = "use Illuminate\\Support\\Facades\\Schema;";

                string lineUse;
                for (int i = 1; i < satirNoUse; i++)
                {
                    lineUse = reader.ReadLine();
                }
                writer.WriteLine("\n");
                writer.WriteLine(yeniIcerikUse);
                writer.WriteLine("\n");
                writer.Close();
                reader.Close();

                // Code
                int satirNoCode = 22;
                string yeniIcerikCode = " Schema::defaultStringLength(" + 191 + ");";
                string lineCode;
                for (int i = 1; i < satirNoCode; i++)
                {
                    lineCode = reader.ReadLine();
                }
                writer.WriteLine("\n");
                writer.WriteLine(yeniIcerikCode);
                writer.WriteLine("\n");
                writer.Close();
                reader.Close();

            }



        }

        private void refreshversion_button_Click(object sender, EventArgs e)
        {
            if (File.Exists(LocalReleaseFile))
            {
                File.Delete(LocalReleaseFile);
                listBox1.Items.Add("Yerel dosya silindi...");

                Console.WriteLine("Uzaktan getir.");

                var releasesDiff = GetAllReleases();
                var json = JsonConvert.SerializeObject(releasesDiff);
                File.WriteAllText(LocalReleaseFile, json);
                listBox1.Items.Add("Veriler uzak bağlantıdan getiriliyor.");

                Console.WriteLine("Yerelden getir.");
                var releasesLocal = GetReleasesFromFile(LocalReleaseFile);
                foreach (var release in releasesLocal)
                {
                    if (!string.IsNullOrEmpty(release.Tag_Name))
                    {
                        string VersionName = release.Tag_Name.ToString().Substring(1, release.Tag_Name.ToString().Length - 1);
                        comboBox1.Items.Add(VersionName);
                    }
                }
                listBox1.Items.Add("Veriler yerel dosyaya işleniyor.");

            }
            else
            {
                MessageBox.Show("Release listesi zaten yok.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

        }



    }
}
