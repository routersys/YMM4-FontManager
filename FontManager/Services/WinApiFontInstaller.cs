using FontManager.Services.Interfaces;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace FontManager.Services
{
    public partial class WinApiFontInstaller : IFontInstaller
    {
        [LibraryImport("gdi32.dll", EntryPoint = "AddFontResourceExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        [LibraryImport("gdi32.dll", EntryPoint = "RemoveFontResourceExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial int RemoveFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_FONTCHANGE = 0x001D;
        private const int HWND_BROADCAST = 0xffff;

        public async Task<bool> InstallFontAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            return await Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\Fonts", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                    if (!File.Exists(targetPath))
                        File.Copy(filePath, targetPath, true);

                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true);
                    key?.SetValue($"{Path.GetFileNameWithoutExtension(fileName)} (TrueType)", targetPath);

                    if (AddFontResourceEx(targetPath, 0, IntPtr.Zero) > 0)
                    {
                        PostMessage((IntPtr)HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public async Task<bool> UninstallFontAsync(string familyName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", true);
                    if (key == null) return false;

                    var valueName = key.GetValueNames().FirstOrDefault(n => n.StartsWith(familyName, StringComparison.OrdinalIgnoreCase));
                    if (valueName == null) return false;

                    string? path = key.GetValue(valueName) as string;
                    if (path != null && File.Exists(path))
                    {
                        RemoveFontResourceEx(path, 0, IntPtr.Zero);
                        key.DeleteValue(valueName);

                        try { File.Delete(path); } catch { }

                        PostMessage((IntPtr)HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                        return true;
                    }
                    return false;
                }
                catch { return false; }
            });
        }

        public bool IsFontInstalled(string familyName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", false);
            return key?.GetValueNames().Any(n => n.Contains(familyName, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        public IEnumerable<string> GetInstalledFontNames()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts", false);
            return key?.GetValueNames() ?? Array.Empty<string>();
        }
    }
}