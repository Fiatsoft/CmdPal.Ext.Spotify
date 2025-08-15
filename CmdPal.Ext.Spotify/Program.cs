using CmdPal.Ext.Spotify.Helpers;
using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;

namespace CmdPal.Ext.Spotify
{
    public class Program
    {
        private const string AppKey = "CmdPal.Ext.Spotify";
        private static readonly string CurrentUser = WindowsIdentity.GetCurrent().Name.Replace("\\", "_");
        private static string PrimaryMutexName => $@"Local\{AppKey}_{Environment.MachineName}_{Environment.UserName}_Primary";
        private static string ExitEventNameForPid(int pid) => 
            $@"Local\{AppKey}_{Environment.MachineName}_{Environment.UserName}_Exit-{pid}";

        [MTAThread]
        public static async Task Main(string[] args)
        {
            // 1) Publish our own exit event & hook a listener to shut down gracefully if asked.
            using var exitEvent = new EventWaitHandle(
                false, EventResetMode.ManualReset, ExitEventNameForPid(Environment.ProcessId));

            var extensionDisposedEvent = new ManualResetEvent(false);

            // Background waiter: if another instance signals our exit event, we stop the extension.
            _ = Task.Run(() =>
            {
                exitEvent.WaitOne();
                try
                {
                    extensionDisposedEvent.Set(); // triggers graceful shutdown below
                }
                catch { /* best effort */ }
            });

            // 2) Try to become the primary instance (bias toward THIS process).
            using var primaryMutex = new Mutex(initiallyOwned: false, name: PrimaryMutexName);
            if (!primaryMutex.WaitOne(0))
            {
                // Someone else (same user) is primary. Tell them to exit; then we take over.
                RetireOtherInstances(Environment.ProcessId, politelyMs: 1500, thenKillMs: 1500);
                // Try again to take ownership
                primaryMutex.WaitOne(); // will block until we own it
            }

            // From here on, THIS process is the “primary” for this user.
            try
            {
                if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
                {
                    await using var server = new ComServer();
                    var extensionInstance = new SpotifyExtension(extensionDisposedEvent);

                    server.RegisterClass<SpotifyExtension, IExtension>(() => extensionInstance);
                    server.Start();

                    // Wait until we’re told to exit (either by host or by another instance).
                    extensionDisposedEvent.WaitOne();
                }
                else
                {
                    Journal.Append($"Not being launched as an Extension… exiting; args: {Newtonsoft.Json.JsonConvert.SerializeObject(args)}");
                }
            }
            finally
            {
                // Release primary (so the next instance can become primary instantly).
                try { primaryMutex.ReleaseMutex(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// For the same user, asks other instances to exit (gracefully), then force-kills any still running.
        /// </summary>
        private static void RetireOtherInstances(int currentPid, int politelyMs, int thenKillMs)
        {
            var procName = Process.GetCurrentProcess().ProcessName;
            var siblings = Process.GetProcessesByName(procName)
                                  .Where(p => p.Id != currentPid)
                                  .Where(p => IsSameUser(p, CurrentUser))
                                  .ToList();

            if (siblings.Count == 0) return;

            // 1) Signal each sibling’s named exit event (graceful shutdown request).
            foreach (var p in siblings)
            {
                try
                {
                    if (EventWaitHandle.TryOpenExisting(ExitEventNameForPid(p.Id), out var wh))
                    {
                        using (wh) wh.Set();
                    }
                }
                catch { /* sibling may not have created its event yet; best effort */ }
            }

            // 2) Give them a moment to exit cleanly.
            WaitForExit(siblings, politelyMs);

            // 3) Anything still alive? Nudge again, wait briefly, then kill.
            var stubborn = siblings.Where(IsAlive).ToList();
            if (stubborn.Count == 0) return;

            WaitForExit(stubborn, thenKillMs);

            foreach (var p in stubborn.Where(IsAlive))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* last resort */ }
            }
        }

        private static void WaitForExit(System.Collections.Generic.IEnumerable<Process> procs, int timeoutMs)
        {
            var deadline = Environment.TickCount64 + timeoutMs;
            foreach (var p in procs)
            {
                var remaining = Math.Max(0, (int)(deadline - Environment.TickCount64));
                if (remaining == 0) break;
                try { p.WaitForExit(remaining); } catch { /* ignore */ }
            }
        }

        private static bool IsAlive(Process p)
        {
            try { return !p.HasExited; } catch { return false; }
        }

        private static bool IsSameUser(Process p, string normalizedUser)
        {
            try
            {
                var owner = GetProcessOwner(p.Id);
                return string.Equals(owner.Replace("\\", "_"), normalizedUser, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string GetProcessOwner(int pid)
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Handle, ProcessId FROM Win32_Process WHERE ProcessId = {pid}");
            using var results = searcher.Get();
            var mo = results.Cast<ManagementObject>().FirstOrDefault();
            if (mo == null) return string.Empty;

            var args = new object[] { string.Empty, string.Empty }; // domain, user
            var ret = Convert.ToInt32(mo.InvokeMethod("GetOwner", args));
            if (ret == 0)
            {
                var user = (string)args[1];
                var domain = (string)args[0];
                return string.IsNullOrEmpty(domain) ? user : $"{domain}\\{user}";
            }
            return string.Empty;
        }
    }
}