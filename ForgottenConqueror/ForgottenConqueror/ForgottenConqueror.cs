using System;
using Android.App;
using Android.Runtime;
using Android;
using Android.OS;
using Android.Support.V7.App;
using Android.Content.PM;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using System.Linq;

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
                    Exception ex = e.ExceptionObject as Exception;
                    Log.Error(ref ex, $"Cought an unhandled exception!   sender: {sender},   terminating: {e.IsTerminating}");
                }
                else
                {
                    Log.Error($"Cought an unhandled exception!   sender: {sender},   terminating: {e.IsTerminating}");
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

            List<string> shouldRequestManually = new List<string>();
            List<string> shouldRequestNormally = new List<string>();

            foreach (string permission in permissions)
            {
                if (activity.CheckSelfPermission(permission) != Permission.Granted)
                {
                    if (!activity.ShouldShowRequestPermissionRationale(permission))
                    {
                        shouldRequestManually.Add(permission);
                    }
                    else
                    {
                        shouldRequestNormally.Add(permission);
                    }
                }
            }

            if (!shouldRequestManually.IsEmpty())
            {
                void Request(int index)
                {
                    string text = Permissions.ContainsKey(shouldRequestManually[index]) ? Permissions[shouldRequestManually[index]] : shouldRequestManually[index];
                    Snackbar.Make(activity.FindViewById(Resource.Id.root), $"Allow ForgottenConqueror permission to {text}?",
                        Snackbar.LengthIndefinite).SetAction("Allow", new Action<Android.Views.View>(delegate (Android.Views.View obj) {
                            activity.RequestPermission(shouldRequestManually[index], shouldRequestManually[index].GetHashCode());
                            if ((++index) < shouldRequestManually.Count)
                            {
                                Request(index);
                            }
                        })).Show();
                }
            }

            if (!shouldRequestNormally.IsEmpty())
            {
                activity.RequestPermissions(shouldRequestNormally.ToArray(), shouldRequestNormally.Sum(p => p.GetHashCode()));
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
}
