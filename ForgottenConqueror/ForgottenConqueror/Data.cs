using System.Collections.Generic;
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
    }
}