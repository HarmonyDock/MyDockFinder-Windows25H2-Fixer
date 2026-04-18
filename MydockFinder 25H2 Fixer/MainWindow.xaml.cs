using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LnkToExeConverter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private byte[] ExtractResource(string resourceName)
        {
            // 资源名称格式: 默认命名空间.文件夹名.文件名
            // 例如: LnkToExeConverter.Resources.Starter.exe
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }
        string MyDockFinderFolder = "";
        string fixfolder = "";
        private void MydockFolder_Click(object sender, RoutedEventArgs e)
        {

            OpenFolderDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = "请选择MyDockFinder根目录";

            // Show open folder dialog box
            bool? result = dialog.ShowDialog();

            // Process open folder dialog box results
            if (result == true)
            {
                // Get the selected folder
                MyDockFinderFolder = dialog.FolderName;
                Directory.CreateDirectory(MyDockFinderFolder+"");
                fixfolder = MyDockFinderFolder + @"\25H2 Fixer";
                MydockFolder.Visibility = Visibility.Hidden;
                add.Visibility = Visibility.Visible;
                foldertext.Text = MyDockFinderFolder;
            }
        }
        public void AddNewIconEntry(string iniFilePath, string newTag, string newAppName, string newFilePath = "", string newRealPath = "", string newicoPath = "")
        {
            // 读取所有行（使用 UTF-8 编码读取，避免乱码）
            // 如果原文件不是 UTF-8，可能会读到乱码，建议先转换文件编码（见下文）
            string[] lines = File.ReadAllLines(iniFilePath, Encoding.Unicode);

            // 正则匹配节名 [ico数字]
            Regex regex = new Regex(@"^\[ico(\d+)\]$", RegexOptions.IgnoreCase);
            int maxNumber = 0;
            foreach (string line in lines)
            {
                Match match = regex.Match(line.Trim());
                if (match.Success)
                {
                    int num = int.Parse(match.Groups[1].Value);
                    if (num > maxNumber) maxNumber = num;
                }
            }

            int newNumber = maxNumber + 1;

            // 构建要添加的两行（可根据需要增加更多行，但示例要求两行）
            // 通常一个图标项包含 tag、appname、filepath、realpath、icopath 等。
            // 这里仅添加 tag 和 appname 作为演示，你可以按需补全其他行。
            List<string> newLines = new List<string>
    {
        $"[ico{newNumber}]",
        $"tag={newTag}",
        $"appname={newAppName}",
        $"filepath={newFilePath}",
        $"realpath={newRealPath}",
        $"icopath={newicoPath}"
    };

            // 追加到文件末尾，使用 UTF-8 编码（无 BOM 更通用）
            File.AppendAllLines(iniFilePath, newLines, new UnicodeEncoding(false,false));
        }
        public void ConvertToUtf8WithoutBom(string iniFilePath)
        {
            // 尝试用系统默认编码（ANSI）读取，然后保存为 UTF-8 无 BOM
            string content = File.ReadAllText(iniFilePath, Encoding.Default);
            File.WriteAllText(iniFilePath, content, new UnicodeEncoding(false, false));
        }
        private void add_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".lnk"; // Default file extension
            dialog.Filter = "快捷方式 (.lnk)|*.lnk"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                IconHelper.GetShortcutIconWithoutArrow(filename,fixfolder+@"\"+ Path.GetFileNameWithoutExtension(filename)+".png", true);
                File.Copy(filename, fixfolder + @"\" + Path.GetFileName(filename), true);
                byte[] templateData = ExtractResource("MydockFinder_25H2_Fixer.Starter.exe");
                File.WriteAllBytes(fixfolder+@"\"+Path.GetFileNameWithoutExtension(filename)+".exe", templateData);
                AddNewIconEntry(MyDockFinderFolder+@"\ico.ini", Path.GetFileNameWithoutExtension(filename), "myapp.exe", fixfolder + @"\" + Path.GetFileNameWithoutExtension(filename) + ".exe", fixfolder + @"\" + Path.GetFileNameWithoutExtension(filename) + ".exe", fixfolder + @"\" + Path.GetFileNameWithoutExtension(filename) + ".png");
                MessageBox.Show("添加成功！点击重启MyDockFinder！");
                RestartOtherProgram(MyDockFinderFolder+@"\dock_64.exe");
            }
        }
        public static void RestartOtherProgram(string fullExePath)
        {
            if (!File.Exists(fullExePath))
                throw new FileNotFoundException($"找不到可执行文件: {fullExePath}");

            // 1. 从路径中提取进程名（不含 .exe 后缀）
            string processName = Path.GetFileNameWithoutExtension(fullExePath);

            // 2. 终止所有同名进程
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
                process.WaitForExit(); // 等待进程完全退出
            }

            // 3. 给系统一点时间释放资源（可选，但推荐）
            Thread.Sleep(100);

            // 4. 启动新进程
            Process.Start(fullExePath);
        }
    }
}
public static class IconHelper
{
    // 1. 定义 SHFILEINFO 结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;        // 系统图标列表中的索引
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    // 2. SHGetFileInfo 的标志常量
    public const uint SHGFI_SYSICONINDEX = 0x000004000;    // 获取图标索引
    public const uint SHGFI_LARGEICON = 0x000000000;
    public const uint SHGFI_SMALLICON = 0x000000001;
    public const uint SHGFI_OPENICON = 0x000000002;

    // 3. ImageList_GetIcon 的标志常量
    public const uint ILD_NORMAL = 0x000000000;
    public const uint ILD_TRANSPARENT = 0x000000001;

    // 4. 导入 API 函数
    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags
    );

    [DllImport("Comctl32.dll")]
    public static extern IntPtr ImageList_GetIcon(
        IntPtr himl,    // 系统图标列表句柄
        int i,          // 图标索引
        uint flags      // 绘制标志
    );

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// 获取不带角标的快捷方式图标
    /// </summary>
    /// <param name="shortcutPath">快捷方式文件路径（如 .lnk 文件）</param>
    /// <param name="isLargeIcon">true=大图标(32x32), false=小图标(16x16)</param>
    /// <returns>WPF 可用的 ImageSource，若失败则返回 null</returns>
    public static ImageSource GetShortcutIconWithoutArrow(string shortcutPath, string pp, bool isLargeIcon = true)
    {
        if (string.IsNullOrEmpty(shortcutPath))
            return null;

        SHFILEINFO shinfo = new SHFILEINFO();

        // 设置标志：获取系统图标索引，并指定大/小图标
        uint flags = SHGFI_SYSICONINDEX;
        if (isLargeIcon)
            flags |= SHGFI_LARGEICON;
        else
            flags |= SHGFI_SMALLICON;

        // 1. 获取系统图标列表句柄 和 该快捷方式在图库中的索引
        IntPtr hImageList = SHGetFileInfo(
            shortcutPath,
            0,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            flags
        );

        if (hImageList == IntPtr.Zero || shinfo.iIcon == 0)
            return null;

        // 2. 通过索引从系统图标列表中提取图标句柄（此时已无角标）
        IntPtr hIcon = ImageList_GetIcon(hImageList, shinfo.iIcon, ILD_TRANSPARENT);

        if (hIcon == IntPtr.Zero)
            return null;

        // 3. 转换为 WPF 可用的 ImageSource
        ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
            hIcon,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions()
        );
        if (imageSource == null) throw new ArgumentNullException(nameof(imageSource));
        if (!(imageSource is BitmapSource bitmapSource))
            throw new ArgumentException("ImageSource 必须是 BitmapSource 类型");

        // 编码为 PNG 并保存
        using (FileStream stream = new FileStream(pp, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
        }

        // 4. 清理资源
        DestroyIcon(hIcon);

        return imageSource;
    }
}