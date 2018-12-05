using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Android.Content;
using Android.Preferences;

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
        public void Write(Context context, string key, string value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutString(key, value);
            editor.Apply();
        }

        public void Write(Context context, string key, bool value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean(key, value);
            editor.Apply();
        }

        public void Write(Context context, string key, float value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutFloat(key, value);
            editor.Apply();
        }

        public void Write(Context context, string key, int value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutInt(key, value);
            editor.Apply();
        }

        public void Write(Context context, string key, long value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutLong(key, value);
            editor.Apply();
        }

        public void Write(Context context, string key, ICollection<string> value)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutStringSet(key, value);
            editor.Apply();
        }

        public string Read(Context context, string key, string defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetString(key, defaultValue);
        }

        public string Read(Context context, string key)
        {
            return Read(context, key, null);
        }

        public bool ReadBoolean(Context context, string key, bool defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetBoolean(key, defaultValue);
        }

        public bool ReadBoolean(Context context, string key)
        {
            return ReadBoolean(context, key, false);
        }

        public float ReadFloat(Context context, string key, float defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetFloat(key, defaultValue);
        }

        public float ReadFloat(Context context, string key)
        {
            return ReadFloat(context, key, 0f);
        }

        public int ReadInt(Context context, string key, int defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetInt(key, defaultValue);
        }

        public int ReadInt(Context context, string key)
        {
            return ReadInt(context, key, 0);
        }

        public long ReadLong(Context context, string key, long defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetLong(key, defaultValue);
        }

        public long ReadLong(Context context, string key)
        {
            return ReadLong(context, key, 0L);
        }

        public ICollection<string> ReadStrings(Context context, string key, ICollection<string> defaultValue)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            return prefs.GetStringSet(key, defaultValue);
        }

        public ICollection<string> ReadStrings(Context context, string key)
        {
            return ReadStrings(context, key, null);
        }
        
        public readonly static string LastUpdateTime = "LastUpdateTime";
        public readonly static string PreviouslyLastChapterId = "PreviouslyLastChapterId";
        #endregion


        #region String compression
        // Thank you @xanatos (https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp)
        private void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public byte[] Compress(string str)
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

        public byte[] Zip(byte[] bytes)
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

        public string Decompress(byte[] bytes)
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

        public byte[] Unzip(byte[] bytes)
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
}