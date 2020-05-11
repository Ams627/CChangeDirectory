using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CChangeDirectory
{
    class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "-i")
                {
                    var indexManager = new IndexManager();
                }
                else
                {
                    var path = args[0];
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine($"{path}");
                    }
                    else if (File.Exists(path))
                    {
                        Console.WriteLine($"{Path.GetDirectoryName(path)}");
                    }
                    else
                    {
                        var indexManager = new IndexManager();
                        indexManager.Lookup(path);
                    }
                }
            }
            catch (Exception ex)
            {
                var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                var progname = Path.GetFileNameWithoutExtension(fullname);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
