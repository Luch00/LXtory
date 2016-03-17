//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;

//namespace ScreenShotterWPF
//{
//    class ShellExtension
//    {
//        public async Task<int> RegUnRegDll(string arg)
//        {
//            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
//            string pathTo = Path.Combine(appdata, @"Luch\LXtory");
//            string fullPath = Path.Combine(pathTo, "LXtoryExtension.dll");

//            if (!Directory.Exists(pathTo))
//                Directory.CreateDirectory(pathTo);

//            if (!File.Exists(fullPath))
//            {
//                var extract = await ExtractResource(pathTo, "LXtoryExtension.dll", "ScreenShotterWPF.Properties.Resources.LXtoryExtension.dll");

//                if (!extract)
//                {
//                    return 0;
//                }
//            }

//            var process = new Process
//            {
//                StartInfo =
//                {
//                    CreateNoWindow = true,
//                    ErrorDialog = false,
//                    UseShellExecute = false,
//                    FileName = GetRegAsmPath(),
//                    Arguments = string.Format("\"{0}\" {1}", fullPath, arg)
//                }
//            };
//            int returnCode;
//            using (process)
//            {
//                process.Start();
//                process.WaitForExit();
//                returnCode = process.ExitCode;
//            }
//            return returnCode;
//        }

//        private static string GetRegAsmPath()
//        {
//            string net_base = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), @"..\.."));
//            string reg_path;
//            reg_path = string.Concat(net_base, Environment.Is64BitOperatingSystem ? "\\Framework64\\" : "\\Framework\\", RuntimeEnvironment.GetSystemVersion(), "\\regasm.exe");
//            return reg_path;
//        }

//        private static async Task<bool> ExtractResource(string pathTo, string fileName, string resourceName)
//        {
//            Console.WriteLine(pathTo + @" -- " + fileName + @" -- " + resourceName);
//            try
//            {
//                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(/*resourceName*/ "ScreenShotterWPF.Resources.LXtoryExtension.dll"))
//                {
//                    using (var file = new FileStream(Path.Combine(pathTo, fileName), FileMode.Create, FileAccess.Write))
//                    {
//                        await resource.CopyToAsync(file);
//                        return true;
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                return false;
//            }
//        }
//    }
//}
