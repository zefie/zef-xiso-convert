using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using XBoxISO;

namespace ZefXISOConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            // print version info and attribution
            Assembly assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.FullName.Split(',')[0] + " v" + assembly.FullName.Split('=')[1].Split(',')[0].TrimEnd('0','.') + " by zefie ~ XISO functionality via XDVDMulleter libraries by Badsheepy");

             
#if DEBUG
            string file = "E:\\zefie\\Downloads\\out\\Teen Titans (USA).iso";
            string outfile = "E:\\zefie\\Downloads\\out\\test.iso";
#else
            string outfile = "";
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: zefconvert [redump xboxog iso]");
                return;
            }
            string file = args[0];
            if (args.Length < 2) 
            {
                outfile = args[1];
            }
#endif
            if (File.Exists(file))
            {
                if (outfile.Length == 0)
                {
                    // default name (infile [Redump Extract])
                    List<string> tmp = file.Split('/').Last().Split('.').ToList();
                    tmp.RemoveAt(tmp.Count - 1);
                    outfile = String.Join(".", tmp) + " [Redump Extract].iso";
                }
                Console.WriteLine("Converting XGD Format to XISO Format...");
                convertISO(file, outfile);
            } else
            {
                Console.WriteLine("Could not find file: " + file);
            }
        }

        static int getReadSize(long remain, int max)
        {
            if (remain > max) return max;
            else return (int)remain;
        }

        static string friendlyBytes(long size, int round = 2)
        {
            List<string> sizes = new List<string>() { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = sizes.Count; i > 0; i--) {
                if (i > 0) {
                    double exp = Math.Pow((double)1024,(double)i);
                    if (size > exp) {
                        double result = (size / exp);
                        return Math.Round(result, round).ToString("N" + round) + sizes[i].ToString();
                    }
                }
            }
            return size+"B";
        }


/*
        // incomplete function
        static bool createXISO(string infolder, string outfile)
        {
            XBoxISO.ISOWriter xiso = new ISOWriter();
            xiso.IsXbox360Iso = false;
            xiso.SetFileSource();
            return true;        
        }
*/
        static void convertISO(string infile, string outfile) {
            XboxISOFileSource xisosrc = new XboxISOFileSource(infile);    
            if (xisosrc.GetISOType() != Format.Xbox1)
            {
                Console.Write("This utility only supports ORIGINAL XBox ISOs");
                return;
            }
            ISOWriter xiso = new ISOWriter();
            FileSystemEntry xfs = xisosrc.GetFileSystem();
            List<FileSystemEntry> fslist = xfs.GetFileList();
            FSManipulator xfsm = new FSManipulator();
            FSFileOptimiseComparer xiso_sort = new FSFileOptimiseComparer();
            XBoxISO.CrossLinkChecker clc = new CrossLinkChecker();
            CopyStatus cs = new CopyStatus();
            bool result = false;

            ISOPartitionDetails isop = xisosrc.GetPartitionDetails();

            Console.Write("Optimizing XISO Filesystem Layout...");
            
            if ((long)isop.GamePartitionSize > XBoxDVDReader.DVD5MaxSize) {
                result = xfsm.OptimiseDVD9(xfs, false, true, true, isop);
            } else {
                result = xfsm.OptimiseDVD5(xfs, false, false, true, true, false, 0, false, isop); 
            }
            if (result) Console.WriteLine(" Success.");
            else Console.WriteLine(" Failed... but thats okay, continuing.");

            Console.WriteLine("Checking for crosslinkable files...");
            BackgroundWorker clc_bw = new BackgroundWorker();
            clc_bw.WorkerReportsProgress = true;
            clc_bw.WorkerSupportsCancellation = true;
            clc_bw.ProgressChanged += clc_bw_ProgressChanged;
            clc.CrossLinkCheck(xfs, cs, clc_bw);
            Console.WriteLine();

            Console.WriteLine("Writing ISO to "+outfile+"...");
            BackgroundWorker main_bw = new BackgroundWorker();
            main_bw.WorkerReportsProgress = true;
            main_bw.WorkerSupportsCancellation = true;
            main_bw.ProgressChanged += main_bw_ProgressChanged;
            xiso.writeISO(outfile, xfs, main_bw, false);
            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        public static void ProgressUpdate(string str1, string str2)
        {
            int len = Console.BufferWidth - (str2.Length + 3);
            Console.CursorLeft = 0;
            Console.Write("{0,-" + len + "} {1,0}", str1, str2);
        }

        public static void ProgressUpdate(string str1, string str2, string str3)
        {
            int len = Console.BufferWidth - (str2.Length + str3.Length + 5);
            Console.CursorLeft = 0;
            Console.Write("{0,-" + len + "} {1,3}   {2,0}", str1, str2, str3);
        }

        private static void clc_bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CopyStatus cp = (CopyStatus)e.UserState;
            String leftstr = friendlyBytes(cp.BytesThisFile) + "/" + friendlyBytes(cp.SizeThisFile);
            ProgressUpdate(cp.FileName, leftstr);
        }

        private static void main_bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            CopyStatus cp = (CopyStatus)e.UserState;
            String leftstr = friendlyBytes(cp.BytesThisFile) + "/" + friendlyBytes(cp.SizeThisFile);
            String rightstr = "Total: " + friendlyBytes(cp.TotalBytesWritten) + "/" + friendlyBytes(cp.TotalSizeToWrite);
            ProgressUpdate("Writing " + cp.FileName,leftstr,rightstr);
        }

        // Unused but functional function
        static bool extractISO(string infile, string outfolder = null, int blocksize = 32768)
        {
            if (outfolder == null)
            {
                List<string> tmp = infile.Split('/').Last().Split('.').ToList();
                tmp.RemoveAt(tmp.Count - 1);
                outfolder = String.Join(".", tmp);
                Console.WriteLine(outfolder);
            }

            if (!Directory.Exists(outfolder)) Directory.CreateDirectory(outfolder);

            XboxISOFileSource xisosrc = new XboxISOFileSource(infile);
            FileSystemEntry xfs = xisosrc.GetFileSystem();
            List<FileSystemEntry> fslist = xfs.GetFileList();
            foreach (FileSystemEntry fsentry in fslist)
            {
                if (fsentry.FileName == "XDFS Root") continue;

                if (fsentry.IsFolder)
                {
                    Directory.CreateDirectory(outfolder + "\\" + fsentry.FileName);
                    continue;
                }

                Stream z_in = fsentry.GetStream();
                if (z_in.CanRead)
                {
                    FileStream z_out = File.Create(outfolder + "\\" + fsentry.FullPath);

                    int this_read = 0;
                    int total_read = 0;
                    long left_to_read = z_in.Length;                    

                    while (left_to_read > 0)
                    {
                        int local_bs = getReadSize(left_to_read, blocksize);
                        byte[] data = new byte[local_bs+1];
                        this_read = z_in.Read(data, 0, local_bs);
                        z_out.Write(data, 0, this_read);
                        total_read += this_read;
                        left_to_read -= this_read;
                        Console.Write("\rWriting File: "+ fsentry.FullPath + ":\t" + friendlyBytes((long)total_read) + "/" + friendlyBytes((long)z_in.Length) + "         ");
                    }
                    Console.Write("\n");
                    z_in.Close();
                    z_out.Close();
                }
                else
                {
                    Console.WriteLine("Internal Error: Could not read ISO file " + fsentry.FullPath);
                    z_in.Close();
                    return false;
                }
            }
            return true;
        }
    }
}
