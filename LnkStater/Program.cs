using System.Diagnostics;
using System.IO;
string exeNameWithoutExt = Path.ChangeExtension(Process.GetCurrentProcess().MainModule.FileName, ".lnk");
var startInfo = new ProcessStartInfo
{
    FileName = exeNameWithoutExt,
    UseShellExecute = true
};
if (File.Exists(exeNameWithoutExt))
{
    Process.Start(startInfo);
}
Console.ReadKey();
