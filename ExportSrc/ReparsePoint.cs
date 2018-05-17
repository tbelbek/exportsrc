#region usings

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace ExportSrc
{
    public class ReparsePoint
    {
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        private const int FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;

        private const int FSCTL_GET_REPARSE_POINT = 0x900A8;

        private const int INVALID_HANDLE_VALUE = -1;

        /// <summary>
        ///     If the path "REPARSE_GUID_DATA_BUFFER.SubstituteName"
        ///     begins with this prefix,
        ///     it is not interpreted by the virtual file system.
        /// </summary>
        private const string NonInterpretedPathPrefix = "\\??\\";

        private const int OPEN_EXISTING = 3;

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            SymbolicLinkType dwFlags);

        /// <summary>
        ///     Gets the target directory from a directory link in Windows Vista.
        /// </summary>
        /// <param name="directoryInfo">
        ///     The directory info of this directory
        ///     link
        /// </param>
        /// <returns>
        ///     the target directory, if it was read,
        ///     otherwise an empty string.
        /// </returns>
        public static string GetTargetDir(FileSystemInfo directoryInfo)
        {
            var targetDir = string.Empty;

            try
            {
                // Is it a directory link?
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    // Open the directory link:
                    var hFile = CreateFile(
                        directoryInfo.FullName,
                        0,
                        0,
                        IntPtr.Zero,
                        OPEN_EXISTING,
                        FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                        IntPtr.Zero);
                    if (hFile.ToInt32() != INVALID_HANDLE_VALUE)
                    {
                        // Allocate a buffer for the reparse point data:
                        var outBufferSize = Marshal.SizeOf(typeof(REPARSE_GUID_DATA_BUFFER));
                        var outBuffer = Marshal.AllocHGlobal(outBufferSize);

                        try
                        {
                            // Read the reparse point data:
                            int bytesReturned;
                            var readOK = DeviceIoControl(
                                hFile,
                                FSCTL_GET_REPARSE_POINT,
                                IntPtr.Zero,
                                0,
                                outBuffer,
                                outBufferSize,
                                out bytesReturned,
                                IntPtr.Zero);
                            if (readOK != 0)
                            {
                                // Get the target directory from the reparse 
                                // point data:
                                var rgdBuffer = (REPARSE_GUID_DATA_BUFFER)Marshal.PtrToStructure(
                                    outBuffer,
                                    typeof(REPARSE_GUID_DATA_BUFFER));
                                targetDir = Encoding.Unicode.GetString(
                                    rgdBuffer.PathBuffer,
                                    rgdBuffer.SubstituteNameOffset,
                                    rgdBuffer.SubstituteNameLength);
                                if (targetDir.StartsWith(NonInterpretedPathPrefix))
                                    targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
                            }
                        }
                        catch (Exception)
                        {
                        }

                        // Free the buffer for the reparse point data:
                        Marshal.FreeHGlobal(outBuffer);

                        // Close the directory link:
                        CloseHandle(hFile);
                    }
                }
            }
            catch (Exception)
            {
            }

            return targetDir;
        }

        public static bool IsSymbolicLink(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists)
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    return true;

            if (File.Exists(path)
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) return true;

            return false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int DeviceIoControl(
            IntPtr hDevice,
            int dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        private bool CreateHardLink(string lpFileName, string lpExistingFileName)
        {
            return CreateHardLink(lpFileName, lpExistingFileName, IntPtr.Zero);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct REPARSE_GUID_DATA_BUFFER
        {
            public readonly uint ReparseTag;

            public readonly ushort ReparseDataLength;

            public readonly ushort Reserved;

            public readonly ushort SubstituteNameOffset;

            public readonly ushort SubstituteNameLength;

            public readonly ushort PrintNameOffset;

            public readonly ushort PrintNameLength;

            /// <summary>
            ///     Contains the SubstituteName and the PrintName.
            ///     The SubstituteName is the path of the target directory.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public readonly byte[] PathBuffer;
        }
    }
}