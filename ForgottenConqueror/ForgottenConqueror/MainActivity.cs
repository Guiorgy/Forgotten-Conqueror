using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Realms;
using System.Collections.Generic;
using static Android.Content.PM.LaunchMode;

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

            //List<Chapter> chapters = new List<Chapter>()
            //{
            //    new Chapter()
            //    {
            //        ID = 0,
            //        Title = "Chapter 1",
            //        Count = 1,
            //        URL = "https://www.google.com/?gws_rd=ssl",
            //    },
            //    new Chapter()
            //    {
            //        ID = 1,
            //        Title = "Chapter 2",
            //        Count = 2,
            //        URL = "https://www.google.com/?gws_rd=ssl",
            //    },
            //    new Chapter()
            //    {
            //        ID = 2,
            //        Title = "Chapter 3",
            //        Count = 3,
            //        URL = "https://www.google.com/?gws_rd=ssl",
            //    },
            //    new Chapter()
            //    {
            //        ID = 3,
            //        Title = "Chapter 4",
            //        Count = 4,
            //        URL = "https://www.google.com/?gws_rd=ssl",
            //    },
            //};
            //NotificationManager.Instance.NotifyNewChapters(this, chapters);
            
            FinishAndRemoveTask();
        }
    }
}