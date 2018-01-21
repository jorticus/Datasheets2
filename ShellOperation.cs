using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2
{
    public static class ShellOperation
    {
        private const FILEOP_FLAGS DEFAULT_FLAGS =
              FILEOP_FLAGS.FOF_ALLOWUNDO          // Allow the user to undo the operation in Explorer
            //| FILEOP_FLAGS.FOF_RENAMEONCOLLISION  // Automatically rename on filename collision
            | FILEOP_FLAGS.FOF_NOCONFIRMMKDIR;    // Silently create directories if necessary

        public enum FileOperation : uint
        {
            Move = 0x1, // FO_MOVE
            Copy = 0x2, // FO_COPY
            Delete = 0x3, // FO_DELETE 
            Rename = 0x4 // FO_RENAME
        }

        [Flags]
        public enum FILEOP_FLAGS : ushort
        {
            FOF_MULTIDESTFILES = 0x1,
            FOF_CONFIRMMOUSE = 0x2,
            /// <summary>
            /// Don't create progress/report
            /// </summary>
            FOF_SILENT = 0x4,
            FOF_RENAMEONCOLLISION = 0x8,
            /// <summary>
            /// Don't prompt the user.
            /// </summary>
            FOF_NOCONFIRMATION = 0x10,
            /// <summary>
            /// Fill in SHFILEOPSTRUCT.hNameMappings.
            /// Must be freed using SHFreeNameMappings
            /// </summary>
            FOF_WANTMAPPINGHANDLE = 0x20,
            FOF_ALLOWUNDO = 0x40,
            /// <summary>
            /// On *.*, do only files
            /// </summary>
            FOF_FILESONLY = 0x80,
            /// <summary>
            /// Don't show names of files
            /// </summary>
            FOF_SIMPLEPROGRESS = 0x100,
            /// <summary>
            /// Don't confirm making any needed dirs
            /// </summary>
            FOF_NOCONFIRMMKDIR = 0x200,
            /// <summary>
            /// Don't put up error UI
            /// </summary>
            FOF_NOERRORUI = 0x400,
            /// <summary>
            /// Dont copy NT file Security Attributes
            /// </summary>
            FOF_NOCOPYSECURITYATTRIBS = 0x800,
            /// <summary>
            /// Don't recurse into directories.
            /// </summary>
            FOF_NORECURSION = 0x1000,
            /// <summary>
            /// Don't operate on connected elements.
            /// </summary>
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            /// <summary>
            /// During delete operation,
            /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
            /// </summary>
            FOF_WANTNUKEWARNING = 0x4000,
            /// <summary>
            /// Treat reparse points as objects, not containers
            /// </summary>
            FOF_NORECURSEREPARSE = 0x8000
        }

        // https://msdn.microsoft.com/en-us/library/bb759795(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public FileOperation wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public FILEOP_FLAGS fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;  // Only used with FOF_WANTMAPPINGHANDLE
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;  // Only used with FOF_SIMPLEPROGRESS
        }

        // https://msdn.microsoft.com/en-us/library/bb762164(v=vs.85).aspx
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

        public static Task SHFileOperationAsync(FileOperation operation, string srcpath, string destpath, FILEOP_FLAGS flags = DEFAULT_FLAGS)
        {
            return Task.Factory.StartNew(() =>
            {
                SHFILEOPSTRUCT fFileOpStruct = new SHFILEOPSTRUCT
                {
                    hwnd = IntPtr.Zero,
                    wFunc = operation,
                    pFrom = srcpath,
                    pTo = destpath,
                    fFlags = flags,
                    fAnyOperationsAborted = false,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = null
                };

                // Implicit flags
                flags |= FILEOP_FLAGS.FOF_ALLOWUNDO; // Allow the user to undo the operation in Explorer
                flags |= FILEOP_FLAGS.FOF_NOCONFIRMMKDIR; // Silently create directories if necessary
                                                          //flags |= FILEOP_FLAGS.FOF_RENAMEONCOLLISION; // Automatically rename on filename collision

                int hr = SHFileOperation(ref fFileOpStruct);

                // The error code is a standard Win32 error, with the exception of a handful of codes which mean specific things.
                // See https://msdn.microsoft.com/en-us/library/bb762164(v=vs.85).aspx for more info.
                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);

                // You can check fFileOpStruct.fAnyOperationsAborted to check if any operations were aborted
            }, TaskCreationOptions.LongRunning);
        }

        public static Task SHFileOperationAsync(FileOperation operation, string[] srcfiles, string destdir, FILEOP_FLAGS flags = DEFAULT_FLAGS)
        {
            var src = String.Concat(srcfiles.Select(f => f + '\0'));
            return SHFileOperationAsync(operation, src, destdir, flags);
        }

        public static Task SHFileOperationAsync(FileOperation operation, string[] srcfiles, string[] destfiles, FILEOP_FLAGS flags = DEFAULT_FLAGS)
        {
            var src = String.Concat(srcfiles.Select(f => f + '\0'));
            var dest = String.Concat(destfiles.Select(f => f + '\0'));

            flags |= FILEOP_FLAGS.FOF_MULTIDESTFILES; // Dest is a 1:1 mapping of src, instead of a directory

            return SHFileOperationAsync(operation, src, dest, flags);
        }

        /// <summary>
        /// Shell execute the command/file.
        /// WARNING: Take care if command is from an untrusted source
        /// </summary>
        /// <param name="command">Command or path to file</param>
        public static void ShellExecute(string command)
        {
            if (!String.IsNullOrEmpty(command))
            {
                Process.Start(command);
            }
        }
        
        /// <summary>
        /// Open folder in explorer
        /// </summary>
        /// <param name="path">Path to folder</param>
        public static void ShellOpenFolder(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                // Ensure path is a directory (don't allow opening of abitrary files)
                if (System.IO.Directory.Exists(path))
                {
                    Process.Start("explorer", path);
                }
            }
        }

        /// <summary>
        /// Open URI in the default browser
        /// </summary>
        /// <param name="uri">The HTTP/HTTPS URI to open</param>
        public static void ShellOpenUri(Uri uri)
        {
            if (uri != null)
            {
                // Ensure URI is a web URL. Other schemes may be able to invoke system behaviour.
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    Process.Start(uri.AbsoluteUri);
                }
            }
        }
    }
}
