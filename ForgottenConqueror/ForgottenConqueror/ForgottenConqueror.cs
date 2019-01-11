using System;
using Android.App;
using Android.Runtime;
using Android;
using Android.OS;
using Android.Support.V7.App;
using Android.Content.PM;
using Android.Support.Design.Widget;
using System.Collections.Generic;

#if DEBUG
using System.Collections.Concurrent;
using System.Threading;
#endif

#if LOGGED_RELEASE
using Serilog;
using System.IO;
using Serilog.Events;
#endif

namespace ForgottenConqueror
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    class ForgottenConqueror : Application
    {
        public static ForgottenConqueror Instance { get; private set; }

        public ForgottenConqueror(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        { }

        public override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

#if LOGGED_RELEASE
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(config => 
                    config.RollingFile(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "ForgottenConqueror/FC_log-{Date}.txt"),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    fileSizeLimitBytes: 52428800, // 50 Megabybtes
                    retainedFileCountLimit: 5))
                .WriteTo.AndroidLog(restrictedToMinimumLevel: LogEventLevel.Verbose)
                .CreateLogger();
#endif
#if LOGGED_RELEASE || DEBUG
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => {
                if(e.ExceptionObject is Exception)
                {
                    Serilog.Log.Error(e.ExceptionObject as Exception, $"Cought an unhandled exception!   sender: {sender},   terminationg: {e.IsTerminating}");
                }
                else
                {
                    Serilog.Log.Error($"Cought an unhandled exception!   sender: {sender},   terminationg: {e.IsTerminating}");
                }
                if (e.IsTerminating)
                {
                    Process.KillProcess(Process.MyPid());
                }
            };
#endif
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            Instance = null;

#if LOGGED_RELEASE
            Serilog.Log.CloseAndFlush();
#endif
        }

        #region Permissions
        private static readonly Dictionary<string, string> Permissions = new Dictionary<string, string>()
        {
            { Manifest.Permission.ReadExternalStorage, "Read External Storage" },
            { Manifest.Permission.WriteExternalStorage, "Write External Storage" },
        };

        public void RequestPermission(AppCompatActivity activity, params string[] permissions)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;

            foreach (string permission in permissions)
            {
                if (activity.CheckSelfPermission(permission) != Permission.Granted)
                {
                    if (activity.ShouldShowRequestPermissionRationale(permission))
                    {
                        string request = string.Join(", ", permission);
                        Snackbar.Make(activity.FindViewById(Resource.Id.root), $"Allow ForgottenConqueror permission to {request}?",
                            Snackbar.LengthIndefinite).SetAction("Allow", new Action<Android.Views.View>(delegate (Android.Views.View obj) {
                                activity.RequestPermissions(permissions, permission.GetHashCode());
                            })).Show();
                    }
                    else
                    {
                        activity.RequestPermissions(permissions, permission.GetHashCode());
                    }
                }
            }
        }

        public bool CheckForPermissions(params string[] permissions)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return true;

            foreach (string permission in permissions)
            {
                if (Instance.CheckSelfPermission(permission) != Permission.Granted)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }

    #region Log
    public static class Log
    {
#if DEBUG
        private static BlockingCollection<string> ConsoleQueue = new BlockingCollection<string>();
#endif

        static Log()
        {
#if DEBUG
            var thread = new Thread(() =>
            {
                while (true) Console.WriteLine(ConsoleQueue.Take());
            });
            thread.IsBackground = true;
            thread.Start();
#endif
        }

        public static void Debug(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Debug(message);
#endif
        }

        public static void Debug(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Debug(exception, message);
#endif
        }

        public static void Error(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Error(message);
#endif
        }

        public static void Error(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Error(exception, message);
#endif
        }

        public static void Fatal(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Fatal(message);
#endif
        }

        public static void Fatal(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Fatal(exception, message);
#endif
        }

        public static void Information(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Information(message);
#endif
        }

        public static void Information(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Information(exception, message);
#endif
        }

        public static void Verbose(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Verbose(message);
#endif
        }

        public static void Verbose(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Verbose(exception, message);
#endif
        }

        public static void Warning(in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Warning: {message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Warning(message);
#endif
        }

        public static void Warning(ref Exception exception, in string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Warning: {exception.StackTrace}\n{message}");
#endif

#if LOGGED_RELEASE
            Serilog.Log.Warning(exception, message);
#endif
        }
    }
    #endregion
}
