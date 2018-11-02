using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Realms;
using System.Collections.Generic;
using System.Linq;
using static ForgottenConqueror.DB;
using static Android.Content.PM.LaunchMode;
using Android.Content;

namespace ForgottenConqueror
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, LaunchMode = SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toast.MakeText(this, "Long-press the homescreen to add the widget", ToastLength.Long).Show();

            Realm realm = Realm.GetInstance(RealmConfiguration.DefaultConfiguration);
            //realm.Write(() => realm.RemoveAll());
            //Data.Instance.Write(this, Data.IsFirstUpdate, true);

            List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > 114).ToList();
            NotificationManager.Instance.NotifyNewChapters(this, chapters);

            NotificationManager.Instance.NotifySimple(this, "title", "blah blah blah blah...", Resource.Drawable.new_chapter_icon);

            //FinishAndRemoveTask();
        }

        protected override void OnNewIntent(Intent intent)
        {
            NotificationManager.Instance.HandleIntent(this, intent);
        }
    }
}