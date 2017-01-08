using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace GitGooey.History
{
    class GitGooeyOptions
    {
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        private static string GetFilename()
        {
            var dataPath = Environment
                .ExpandEnvironmentVariables("%appdata%");

            if (dataPath == null)
            {
                return null;
            }

            var appDir = Path.Combine(dataPath, "GitGooey");

            try
            {
                Directory.CreateDirectory(appDir);
            }
            catch
            {
                return null;
            }

            return Path.Combine(appDir, "settings.json");
        }

        public static GitGooeyOptions Load()
        {
            var filename = GetFilename();

            if (filename == null)
            {
                return new GitGooeyOptions();
            }

            try
            {
                return JsonConvert.DeserializeObject<GitGooeyOptions>(
                    File.ReadAllText(GetFilename()));
            }
            catch
            {
                return new GitGooeyOptions();
            }
        }

        public void Save()
        {
            var filename = GetFilename();

            if (filename == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(filename,
                    JsonConvert.SerializeObject(this));
            }
            catch { }
        }
    }
}
