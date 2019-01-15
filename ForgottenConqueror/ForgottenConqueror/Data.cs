using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Android.Content;
using Android.Preferences;

#if DEBUG
using System.Collections.Concurrent;
using System.Threading;
#endif

namespace ForgottenConqueror
{
    class Data
    {
        private static object thislock = new object();
        private static Data instance;
        private Data() { }

        public static Data Instance
        {
            get
            {
                if(instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new Data();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }

        #region SharedPreferences
        public void Write(ref Context context, in string key, in string value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutString(key, value);
            editor.Apply();
        }

        public void Write(ref Context context, in string key, in bool value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean(key, value);
            editor.Apply();
        }

        public void Write(ref Context context, in string key, float value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutFloat(key, value);
            editor.Apply();
        }

        public void Write(ref Context context, in string key, int value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutInt(key, value);
            editor.Apply();
        }

        public void Write(ref Context context, in string key, long value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutLong(key, value);
            editor.Apply();
        }

        public void Write(ref Context context, in string key, in ICollection<string> value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutStringSet(key, value);
            editor.Apply();
        }

        public string Read(ref Context context, in string key, in string defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetString(key, defaultValue);
        }

        public string Read(ref Context context, in string key)
        {
            return Read(ref context, key, null);
        }

        public bool ReadBoolean(ref Context context, in string key, bool defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetBoolean(key, defaultValue);
        }

        public bool ReadBoolean(ref Context context, in string key)
        {
            return ReadBoolean(ref context, key, false);
        }

        public float ReadFloat(ref Context context, in string key, float defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetFloat(key, defaultValue);
        }

        public float ReadFloat(ref Context context, in string key)
        {
            return ReadFloat(ref context, key, 0f);
        }

        public int ReadInt(ref Context context, in string key, int defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetInt(key, defaultValue);
        }

        public int ReadInt(ref Context context, in string key)
        {
            return ReadInt(ref context, key, 0);
        }

        public long ReadLong(ref Context context, in string key, long defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetLong(key, defaultValue);
        }

        public long ReadLong(ref Context context, in string key)
        {
            return ReadLong(ref context, key, 0L);
        }

        public ICollection<string> ReadStrings(ref Context context, in string key, in ICollection<string> defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetStringSet(key, defaultValue);
        }

        public ICollection<string> ReadStrings(ref Context context, in string key)
        {
            return ReadStrings(ref context, key, null);
        }
        
        public readonly static string LastUpdateTime = "LastUpdateTime";
        public readonly static string PreviouslyLastChapterId = "PreviouslyLastChapterId";
        #endregion

        #region String compression
        // Thank you @xanatos (https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp)
        private void CopyTo(in Stream src, in Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public byte[] Compress(in string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public byte[] Zip(in byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public string Decompress(in byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public byte[] Unzip(in byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
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

        public static void Debug(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Debug(message);
#endif
        }

        public static void Debug(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Debug(message);
#endif
        }

        public static void Debug(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Debug(exception, message);
#endif
        }

        public static void Debug(ref Exception exception, string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Debug: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Debug(exception, message);
#endif
        }

        public static void Error(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Error(message);
#endif
        }

        public static void Error(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Error(message);
#endif
        }

        public static void Error(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Error(exception, message);
#endif
        }

        public static void Error(ref Exception exception, string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Error: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Error(exception, message);
#endif
        }

        public static void Fatal(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Fatal(message);
#endif
        }

        public static void Fatal(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Fatal(message);
#endif
        }

        public static void Fatal(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Fatal(exception, message);
#endif
        }

        public static void Fatal(ref Exception exception, string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Fatal: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Fatal(exception, message);
#endif
        }

        public static void Information(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Information(message);
#endif
        }

        public static void Information(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Information(message);
#endif
        }

        public static void Information(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Information(exception, message);
#endif
        }

        public static void Information(ref Exception exception, string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Information: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Information(exception, message);
#endif
        }

        public static void Verbose(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Verbose(message);
#endif
        }

        public static void Verbose(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Verbose(message);
#endif
        }

        public static void Verbose(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Verbose(exception, message);
#endif
        }

        public static void Verbose(ref Exception exception, string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Verbose: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Verbose(exception, message);
#endif
        }

        public static void Warning(ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Warning: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Warning(message);
#endif
        }

        public static void Warning(string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Warning: {message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Warning(message);
#endif
        }

        public static void Warning(ref Exception exception, ref string message)
        {
#if DEBUG
            ConsoleQueue.Add($"Warning: {exception.StackTrace}\n{message}");
#endif
#if LOGGED_RELEASE
            Serilog.Log.Warning(exception, message);
#endif
        }

        public static void Warning(ref Exception exception, string message)
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