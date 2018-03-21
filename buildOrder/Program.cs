using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buildOrder
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1)
                return;
            String path = args[0];
            CSln solution = new CSln();
            solution.Load(path);
            // write updated solution file
            solution.Update();
            // write missing projects list
            solution.Write();
        }
    }
}
