using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Widget;
using static Android.Content.PM.LaunchMode;
using Realms;
using System.Linq;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Forgotten Conqueror")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider_large")]
    class WidgetLarge : AppWidgetProvider
    {
        private readonly static string NextClick = "NextClick";
        private readonly static string PreviousClick = "PreviousClick";
        private readonly static string ReverseClick = "ReverseClick";
        private readonly static string RefreshClick = "RefreshClick";
        private readonly static string BookClick = "BookClick";
        private readonly static int Layout = Resource.Layout.widget_large;
        private readonly static int LayoutRefreshing = Resource.Layout.widget_large_progress;

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            foreach (int appWidgetId in appWidgetIds)
            {
                WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(appWidgetId);
                if (widgetLargeParams == null)
                {
                    // Widget created
                    realm.Write(() =>
                    {
                        widgetLargeParams = new WidgetLargeParams()
                        {
                            ID = appWidgetId,
                            IsRefreshing = true,
                            Book = 0,
                            Descending = false,
                        };
                        realm.Add<WidgetLargeParams>(widgetLargeParams);
                    });
                }
                else if (widgetLargeParams.IsRefreshing)
                {
                    // Already updating
                    return;
                }
                else realm.Write(() => widgetLargeParams.IsRefreshing = true);

                DBController.Instance.ParseBooks(context);

                ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
                appWidgetManager.UpdateAppWidget(appWidgetComponentName, BuildRemoteView(context, appWidgetId, widgetLargeParams));
            }
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            realm.Dispose();
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            string action = intent.Action;
            if (action == null) return;

            int appWidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            if (appWidgetId == AppWidgetManager.InvalidAppwidgetId) return;

            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(appWidgetId);
            if (action.Equals(NextClick))
            {
                // Next
                int bookId = widgetLargeParams.Book;
                int bookCount = realm.All<Book>().Count();
                realm.Write(() => widgetLargeParams.Book = bookId + 1 >= bookCount ? 0 : bookId + 1);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(PreviousClick))
            {
                // Previous
                int bookId = widgetLargeParams.Book;
                int bookCount = realm.All<Book>().Count();
                realm.Write(() => widgetLargeParams.Book = bookId - 1 < 0 ? bookCount - 1 : bookId - 1);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(ReverseClick))
            {
                // Reverse order
                realm.Write(() => widgetLargeParams.Descending = !widgetLargeParams.Descending);
                Redraw(context, appWidgetId);
            }
            if (action.Equals(RefreshClick))
            {
                // Refresh
                Update(context, appWidgetId);
            }
            if (action.Equals(BookClick))
            {
                // Open Book Url
                string url = Realm.GetInstance(DB.RealmConfiguration).Find<Book>(widgetLargeParams.Book).URL;
                Uri uri = Uri.Parse(url);
                Intent browser = new Intent(Intent.ActionView, uri);
                context.StartActivity(browser);
            }
            realm.Dispose();
        }

        private RemoteViews BuildRemoteView(Context context, int appWidgetId, WidgetLargeParams widgetLargeParams)
        {
            RemoteViews widgetView;
            int layout = widgetLargeParams.IsRefreshing ? LayoutRefreshing : Layout;

            widgetView = new RemoteViews(context.PackageName, layout);

            SetView(context, appWidgetId, widgetView, widgetLargeParams);
            
            return widgetView;
        }

        private void SetView(Context context, int appWidgetId, RemoteViews widgetView, WidgetLargeParams widgetLargeParams)
        {
            if (!widgetLargeParams.IsRefreshing)
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);

                // Bind the RemoteViewsService (adapter) for the Chapters list
                Intent intent = new Intent(context, typeof(RemoteChapterAdapter));
			    intent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                intent.PutExtra(RemoteChapterAdapter.ExtraBookId, widgetLargeParams.Book);
                intent.PutExtra(RemoteChapterAdapter.ExtraSortOrder, widgetLargeParams.Descending);
                intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
                widgetView.SetRemoteAdapter(Resource.Id.list_chapters, intent);

                // Set Chapter list click intent template
                Intent chapterClickIntentTemplate = new Intent(Intent.ActionView);
                PendingIntent chapterClickPendingIntentTemplate = TaskStackBuilder.Create(context)
                        .AddNextIntentWithParentStack(chapterClickIntentTemplate)
                        .GetPendingIntent(appWidgetId, PendingIntentFlags.UpdateCurrent);
                widgetView.SetPendingIntentTemplate(Resource.Id.list_chapters, chapterClickPendingIntentTemplate);

                // Set list header to Book title
                string title = realm.Find<Book>(widgetLargeParams.Book).Title;
                widgetView.SetTextViewText(Resource.Id.book_title, title);
                
                Intent bookIntent = new Intent(context, typeof(WidgetLarge));
                bookIntent.SetAction(BookClick);
                bookIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                PendingIntent bookPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, bookIntent, PendingIntentFlags.OneShot);
                widgetView.SetOnClickPendingIntent(Resource.Id.book_title, bookPendingIntent);

                // Bind the click intent for the refresh button on the widget
                Intent refreshIntent = new Intent(context, typeof(WidgetLarge));
                refreshIntent.SetAction(RefreshClick);
                refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
                PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, refreshIntent, PendingIntentFlags.UpdateCurrent);
                widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);

                realm.Dispose();
            }

            // Bind the click intent for the previous button on the widget
            Intent previousIntent = new Intent(context, typeof(WidgetLarge));
            previousIntent.SetAction(PreviousClick);
            previousIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent previousPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, previousIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_previous, previousPendingIntent);

            // Bind the click intent for the next button on the widget
            Intent nextIntent = new Intent(context, typeof(WidgetLarge));
			nextIntent.SetAction(NextClick);
			nextIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
			PendingIntent nextPendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, nextIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_next, nextPendingIntent);

            // Bind the click intent for the reverse button on the widget
            Intent reverseIntent = new Intent(context, typeof(WidgetLarge));
            reverseIntent.SetAction(ReverseClick);
            reverseIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            PendingIntent reversePendingIntent = PendingIntent.GetBroadcast(context, appWidgetId, reverseIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_reverse, reversePendingIntent);
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            base.OnDeleted(context, appWidgetIds);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() =>
            {
                foreach (int appWidgetId in appWidgetIds)
                {
                    WidgetLargeParams widgetParams = realm.Find<WidgetLargeParams>(appWidgetId);
                    realm.Remove(widgetParams);
                }
            });

            realm.Dispose();
        }

        public override void OnDisabled(Context context)
        {
            base.OnDisabled(context);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() => realm.RemoveAll<WidgetLargeParams>());
            realm.Dispose();
        }

        public override void OnRestored(Context context, int[] oldWidgetIds, int[] newWidgetIds)
        {
            base.OnRestored(context, oldWidgetIds, newWidgetIds);
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            realm.Write(() =>
            {
                for (int i = 0; i < oldWidgetIds.Length; i++)
                {
                    WidgetLargeParams widgetLargeParams = realm.Find<WidgetLargeParams>(oldWidgetIds[i]);
                    widgetLargeParams.ID = newWidgetIds[i];
                }
            });

            realm.Dispose();
        }

        public void UpdateAll(Context context)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void Update(Context context, params int[] appWidgetIds)
        {
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public void RedrawAll(Context context)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            ComponentName appWidgetComponentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
            int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(appWidgetComponentName);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeParams>(appWidgetId)));
            }

            realm.Dispose();
        }

        public void Redraw(Context context, params int[] appWidgetIds)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);
            foreach (int appWidgetId in appWidgetIds)
            {
                appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteView(context, appWidgetId, realm.Find<WidgetLargeParams>(appWidgetId)));
            }

            realm.Dispose();
        }
    }

    [Register("forgottenconqueror.WidgetLargeConfigurationActivity", DoNotGenerateAcw = true)]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = false, LaunchMode = SingleTop)]
    [IntentFilter(actions: new[] { "android.appwidget.action.APPWIDGET_CONFIGURE" })]
    public class WidgetLargeConfigurationActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main); // needs layout

            Bundle Extras = Intent.Extras;
            int appWidgetId = Extras != null ? Extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId) : AppWidgetManager.InvalidAppwidgetId;

            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(this);

            Intent result = new Intent();
            result.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            SetResult(Result.Canceled, result);

            // code here

            RemoteViews views = new RemoteViews(PackageName, Resource.Layout.widget_large_progress);
            appWidgetManager.UpdateAppWidget(appWidgetId, views);

            SetResult(Result.Ok, result);
            FinishAndRemoveTask();
        }
    }
}