using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace buildOrder
{
    class CCSProj
    {
        private List<string> m_lines;
        public string ProjectLine = "";
        public string ID = "";
        public string Name = "";
        public List<string> Dependencies;

        public CCSProj()
        {
            m_lines = new List<string>();
            Dependencies = new List<string>();
        }

        public bool Load(string filename)
        {
            if (!File.Exists(filename))
                return false;
            try
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    while (!sr.EndOfStream)
                        m_lines.Add(sr.ReadLine());
                }
                foreach (String line in m_lines)
                {
                    string cleanline = line.Trim();
                    if (cleanline.StartsWith("<Reference Include="))
                    {
                        if (line.Contains(','))
                            continue;
                        string name = line.Remove(0, line.IndexOf('=') + 2);
                        name = name.Remove(name.IndexOf('\"'));
                        if (name.StartsWith("System") || name.StartsWith("Microsoft"))
                            continue;
                        Dependencies.Add(name);
                    }
                    if (cleanline.StartsWith("<ProjectReference Include="))
                    {
                        if (line.Contains(','))
                            continue;
                        string name = line.Remove(0, line.IndexOf('=') + 2);
                        name = name.Remove(name.IndexOf('\"'));
                        string[] nameparts = name.Split('\\');
                        name = nameparts[nameparts.Length - 1].Replace(".csproj", "");
                        Dependencies.Add(name);
                    }
                    if (cleanline.StartsWith("<Content Include=") && (cleanline.Contains(".exe") || cleanline.Contains(".dll")))
                    {
                        if (line.Contains(','))
                            continue;
                        string name = line.Remove(0, line.IndexOf('=') + 2);
                        name = name.Remove(name.IndexOf('\"'));
                        string[] nameparts = name.Split('\\');
                        name = nameparts[nameparts.Length - 1].Replace(".dll", "");
                        name = name.Replace(".exe", "");
                        Dependencies.Add(name);
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = "Can't load project file " + filename + ":" + Environment.NewLine + ex.Message;
                if (ex.InnerException != null)
                    msg += Environment.NewLine + ex.InnerException.Message;
                Console.WriteLine(msg);
                return false;
            }
            return true;
        }
    }
}
