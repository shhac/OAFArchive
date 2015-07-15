/*
 * Created by SharpDevelop.
 * User: Paul
 * Date: 15/07/2015
 * Time: 01:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using OAFArchive;
using System.IO;

namespace OAFDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            // TODO: Implement Functionality Here
            
            if (File.Exists("foo.oaf"))
            {
                Console.WriteLine("Deleting old foo.oaf");
                File.Delete("foo.oaf");
            }
            
            using (OAFArchiveWriter aw = new OAFArchiveWriter("foo.oaf")) {
                Console.WriteLine("Adding: hello world.txt");
                FileInfo info = new FileInfo("hello world.txt");
                using (FileStream file = File.OpenRead("hello world.txt")) {
                    aw.Write("archive/path/hello world.txt", info, file);
                }
                
                Console.WriteLine("Adding: lorem ipsum.txt");
                info = new FileInfo("lorem ipsum.txt");
                using (FileStream file = File.OpenRead("lorem ipsum.txt")) {
                    aw.Write("archive/path/lorem ipsum.txt", info, file);
                }
            }
            
            using (OAFArchiveReader ar = new OAFArchiveReader("foo.oaf")) {
                for (int i = 0; i < ar.headers.Count; ++i)
                {
                    Console.WriteLine("Extracting: " + ar.headers[i].path);
                    ar.ExtractToPath(i, "out_" + ar.headers[i].path);
                }
            }
            
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }
    }
}