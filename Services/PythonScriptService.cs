using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dAIlog.Services
{
    public class PythonScriptService
    {
        public void RunPythonScript(string scriptPath, string pythonEnvPath)
        {
            string fileName, activateCommand;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                activateCommand = $"{pythonEnvPath}\\Scripts\\activate";
            }
            else // Assuming Linux/macOS
            {
                fileName = "/bin/bash";
                activateCommand = $"source {pythonEnvPath}/bin/activate";
            }

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = start };
            process.Start();

            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(activateCommand);
                    sw.WriteLine($"python {scriptPath}");
                }
            }

            // Optionally, you can read the output from the script
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                Console.WriteLine(result);
            }

            process.WaitForExit();
        }
    }
}