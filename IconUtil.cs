using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2
{
    public static class IconUtil
    {
        // System.Drawing.Icon.ExtractAssociatedIcon() only returns a 32x32 icon.
        // This code allows us to get the 16x16 icon instead.
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/24914e74-8e5a-4d1c-88e9-4a9fc88359a9/32x3216x16-formicon-iconextractassociatediconapplicationexecutablepath?forum=clr
        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx  SHGetFileInfo 
        [StructLayout(LayoutKind.Sequential)]
        struct SHFILEINFO
        {
            public IntPtr handle;
            public IntPtr index;
            public uint attr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string display;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string type;
        };
        const uint SHGFI_ICON = 0x100;
        const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        const uint SHGFI_USEFILEATTRIBUTES = 0x10;  // Does not need to be a valid filename. Can just use the extension
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        [DllImport("shell32.dll")]
        static extern IntPtr SHGetFileInfo(string path, uint fattrs, ref SHFILEINFO sfi, uint size, uint flags);


        static Dictionary<string, Icon> smallIconCache = new Dictionary<string, Icon>();

        public enum IconSize { SmallIcon, LargeIcon };
        public enum PathType { File, Directory };
        public static Icon GetIconForPath(string path, IconSize size, PathType? pathType = null, bool useCache = true)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

            if (pathType == PathType.Directory || string.IsNullOrWhiteSpace(ext))
                useCache = false;

            // See if icon is already in cache
            Icon icon = null;
            if (useCache)
            {
                if (smallIconCache.TryGetValue(ext, out icon))
                {
                    return icon;
                }
            }

            // Look up icon
            uint flags = 0;
            switch (size)
            {
                case IconSize.LargeIcon:
                    flags |= SHGFI_LARGEICON;
                    break;
                default:
                    flags |= SHGFI_SMALLICON;
                    break;
            }
            icon = GetIconForPath(path, SHGFI_USEFILEATTRIBUTES | SHGFI_ICON | flags, pathType);

            // Add to cache
            if (useCache)
                smallIconCache[ext] = icon;

            return icon;
        }
        public static Task<Icon> GetIconForPathAsync(string path, IconSize size, PathType? pathType = null, bool useCache = true)
        {
            // Windows API recommends that you run SHGetFileInfo from a background thread
            return Task.Factory.StartNew<Icon>(() =>
            {
                return GetIconForPath(path, size, pathType, useCache);
            }, TaskCreationOptions.LongRunning);
        }

        private static Icon GetIconForPath(string path, uint flags = SHGFI_USEFILEATTRIBUTES | SHGFI_ICON | SHGFI_SMALLICON, PathType? pathType = null)
        {
            try
            {
                uint attr = 0;
                if (pathType.HasValue)
                {
                    attr = (pathType.Value == PathType.Directory) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                }

                SHFILEINFO info = new SHFILEINFO();
                SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf(info), flags);
                return Icon.FromHandle(info.handle);
            }
            catch
            {
                return null;
            }
        }
    }
}
