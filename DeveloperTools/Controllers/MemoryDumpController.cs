﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeveloperTools.Models;
using EPiServer.Web;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperTools.Controllers
{
    public class NativeMethods
    {
        [DllImport("dbghelp.dll",
             EntryPoint = "MiniDumpWriteDump",
             CallingConvention = CallingConvention.StdCall,
             CharSet = CharSet.Unicode,
             ExactSpelling = true,
             SetLastError = true)]
        public static extern bool MiniDumpWriteDump(IntPtr hProcess,
                                                    uint processId,
                                                    IntPtr hFile,
                                                    uint dumpType,
                                                    ref MiniDumpExceptionInformation expParam,
                                                    IntPtr userStreamParam,
                                                    IntPtr callbackParam);

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
        public static extern IntPtr GetCurrentProcess();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MiniDumpExceptionInformation
    {
        public uint ThreadId;
        public IntPtr ExceptionPointers;
        [MarshalAs(UnmanagedType.Bool)] public bool ClientPointers;
    }

    [Flags]
    public enum DumpType : uint
    {
        MiniDumpNormal = 0x00000000,
        MiniDumpWithDataSegs = 0x00000001,
        MiniDumpWithFullMemory = 0x00000002,
        MiniDumpWithHandleData = 0x00000004,
        MiniDumpFilterMemory = 0x00000008,
        MiniDumpScanMemory = 0x00000010,
        MiniDumpWithUnloadedModules = 0x00000020,
        MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
        MiniDumpFilterModulePaths = 0x00000080,
        MiniDumpWithProcessThreadData = 0x00000100,
        MiniDumpWithPrivateReadWriteMemory = 0x00000200,
        MiniDumpWithoutOptionalData = 0x00000400,
        MiniDumpWithFullMemoryInfo = 0x00000800,
        MiniDumpWithThreadInfo = 0x00001000,
        MiniDumpWithCodeSegs = 0x00002000,
        MiniDumpWithoutAuxiliaryState = 0x00004000,
        MiniDumpWithFullAuxiliaryState = 0x00008000,
        MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
        MiniDumpIgnoreInaccessibleMemory = 0x00020000,
        MiniDumpValidTypeFlags = 0x0003ffff
    }

    public sealed class MiniDump
    {
        public static void WriteDump(string fileName, DumpType typeOfdumpType)
        {
            MiniDumpExceptionInformation info;
            info.ThreadId = NativeMethods.GetCurrentThreadId();
            info.ClientPointers = false;
            info.ExceptionPointers = Marshal.GetExceptionPointers();

            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var processId = (uint) Process.GetCurrentProcess().Id;
                var processHandle = Process.GetCurrentProcess().Handle;
                // Feel free to specify different dump types
                //uint dumpType = (uint) (DumpType.MiniDumpNormal | DumpType.MiniDumpWithDataSegs);
                var dumpType = (uint) typeOfdumpType;
                NativeMethods.MiniDumpWriteDump(processHandle,
                                                processId,
                                                fs.SafeFileHandle.DangerousGetHandle(),
                                                dumpType,
                                                ref info,
                                                IntPtr.Zero,
                                                IntPtr.Zero);
            }
        }
    }

    public class MemoryDumpController : DeveloperToolsController
    {
        public ActionResult Index()
        {
            return View(new MemoryDumpModel());
        }

        [HttpPost, ActionName("Index")]
        public ActionResult DumpMemory(MemoryDumpModel memoryDumpModel)
        {
            if(string.IsNullOrEmpty(memoryDumpModel.FilePath))
            {
                memoryDumpModel.FilePath = VirtualPathUtilityEx.RebasePhysicalPath("[appDataPath]\\Dumps");
            }
            if(!Directory.Exists(memoryDumpModel.FilePath))
            {
                Directory.CreateDirectory(memoryDumpModel.FilePath);
            }

            var timeforfileName = DateTime.Now.ToString().Replace('/', '_').Replace(':', '_');
            var name = string.Concat(memoryDumpModel.FilePath.LastIndexOf('/') == -1 ? memoryDumpModel.FilePath + '\\' : memoryDumpModel.FilePath,
                                     Process.GetCurrentProcess().ProcessName + "_" + timeforfileName,
                                     ".dmp");
            MiniDump.WriteDump(name, memoryDumpModel.SelectedDumpType);
            memoryDumpModel.Name = name;

            return View(memoryDumpModel);
        }
    }
}
