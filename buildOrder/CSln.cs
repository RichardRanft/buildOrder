using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace buildOrder
{
    class CSln
    {
        private List<string> m_solution;
        private Dictionary<string, string> m_projects;
        private List<CCSProj> m_projectList;
        private List<string> m_missingProjects;
        private String m_filename;
        private String m_workPath;

        public CSln()
        {
            m_missingProjects = new List<string>();
            m_projectList = new List<CCSProj>(); 
            m_solution = new List<string>();
            m_projects = new Dictionary<string, string>();
        }

        public bool Load(string filename)
        {
            if (!File.Exists(filename))
                return false;
            m_filename = filename;
            m_workPath = Path.GetDirectoryName(m_filename);
            try
            {
                using(StreamReader sr = new StreamReader(filename))
                {
                    while (!sr.EndOfStream)
                        m_solution.Add(sr.ReadLine());
                }
                foreach(String line in m_solution)
                {
                    if(line.StartsWith("Project("))
                    {
                        // get name and id
                        string[] parts = line.Split(',');
                        string id = parts[parts.Length - 1].Remove(0, parts[parts.Length - 1].IndexOf('{') + 1);
                        id = id.Remove(id.IndexOf('}'));
                        string name = line.Remove(0, line.IndexOf('=') + 3);
                        name = name.Remove(name.IndexOf('\"'));
                        String projfile = line.Remove(0, line.IndexOf(',') + 3);
                        projfile = projfile.Remove(projfile.IndexOf('\"'));
                        if (!projfile.Contains(".csproj"))
                            continue;
                        projfile = m_workPath + "\\" + projfile;
                        if (!m_projects.ContainsKey(name))
                        {
                            CCSProj proj = new CCSProj();

                            if (proj.Load(projfile))
                            {
                                proj.ProjectLine = line;
                                proj.Name = name;
                                proj.ID = id;
                                m_projectList.Add(proj);
                                m_projects.Add(name, id);
                            }
                            else
                                m_missingProjects.Add(projfile);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                string msg = "Can't load solution file " + filename + ":" + Environment.NewLine + ex.Message;
                if (ex.InnerException != null)
                    msg += Environment.NewLine + ex.InnerException.Message;
                Console.WriteLine(msg);
                return false;
            }
            return true;
        }

        public bool Update()
        {
            updateSln();
            string filename = m_filename + ".updated";
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    foreach (String line in m_solution)
                        sw.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                string msg = "Can't write solution file " + filename + ":" + Environment.NewLine + ex.Message;
                if (ex.InnerException != null)
                    msg += Environment.NewLine + ex.InnerException.Message;
                Console.WriteLine(msg);
                return false;
            }
            return true;
        }

        public bool Write()
        {
            if (m_missingProjects.Count < 1)
                return true;
            string filename = m_filename + ".missing";
            try
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    foreach(String line in m_missingProjects)
                        sw.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                string msg = "Can't write solution file " + filename + ":" + Environment.NewLine + ex.Message;
                if (ex.InnerException != null)
                    msg += Environment.NewLine + ex.InnerException.Message;
                Console.WriteLine(msg);
                return false;
            }
            return true;
        }

        private void updateSln()
        {
            foreach (CCSProj project in m_projectList)
                updateDependencies(project);
        }

        private bool updateDependencies(CCSProj project)
        {
            int start = m_solution.IndexOf(project.ProjectLine);
            int end = start;
            do ++end; while (m_solution[end].CompareTo("EndProject") != 0);
            int foundCount = 0;
            List<string> missing = new List<string>();
            bool hasDeps = false;
            foreach(string dependency in project.Dependencies)
            {
                bool found = false;
                for(int i = start; i < end; ++i)
                {
                    if (m_solution[i].Contains("ProjectSection(ProjectDependencies) = postProject"))
                        hasDeps = true;
                    if(m_projects.ContainsKey(dependency) && m_solution[i].Contains(m_projects[dependency]))
                    {
                        ++foundCount;
                        found = true;
                    }
                }
                if (!found)
                    missing.Add(dependency);
            }

            bool hasProjectDep = false;
            foreach(string dep in project.Dependencies)
            {
                if(m_projects.ContainsKey(dep))
                {
                    hasProjectDep = true;
                    break;
                }
            }

            if (foundCount == project.Dependencies.Count || project.Dependencies.Count < 1 || !hasProjectDep)
                return true;

            if(hasDeps)
            {
                // find last dep, then append missing to list
                int index = start;
                for (int i = index; i < end; ++i )
                {
                    if(m_solution[i].Contains("EndProjectSection"))
                    {
                        index = i - 1;
                        break;
                    }
                }
                foreach (string dep in missing)
                {
                    if (m_projects.ContainsKey(dep))
                        insertLine(++index, m_projects[dep]);
                }
            }
            else
            {
                // no deps at all, add full block
                int index = start;
                m_solution.Insert(++index, "\tProjectSection(ProjectDependencies) = postProject");
                foreach(string dep in missing)
                {
                    if (m_projects.ContainsKey(dep))
                        insertLine(++index, m_projects[dep]);
                }
                m_solution.Insert(++index, "\tEndProjectSection");
            }
            return false;
        }

        private void insertLine(int index, string text)
        {
            m_solution.Insert(index, "\t\t{" + text + "} = {" + text + "}");
        }

        private bool missingAllDependencies(CCSProj project)
        {
            int state = 0;
            foreach (string key in project.Dependencies)
            {
                if (m_projects.ContainsKey(key))
                    state++;
            }
            return state < 1;
        }

        private bool insertDependency(string dependant, string dependency)
        {
            if(m_projects.ContainsKey(dependant))
            {
                return true;
            }
            return false;
        }
    }
}
