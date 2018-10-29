using System;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Last Chapter")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    class Widget : AppWidgetProvider
    {
        private readonly static string OpenChapterClick = "OpenChapterClick";
        private readonly static string RefreshClick = "RefreshClick";
        private static bool isRefreshing = false;
        private static int[] layouts = {
            Resource.Layout.widget,
            Resource.Layout.widget_1cell,
            Resource.Layout.widget_2cell,
            Resource.Layout.widget_3cell,
        };
        private static int[] layoutsRefreshing = {
            Resource.Layout.widget_progress,
            Resource.Layout.widget_1cell_progress,
            Resource.Layout.widget_2cell_progress,
            Resource.Layout.widget_3cell_progress,
        };
        private static int cells = 3;

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            if (isRefreshing)
            {
                return;
            }
            isRefreshing = true;
            
            new Thread(() =>
            {
                // update
                DB db = DB.Instance;
                bool isFirstUpdate = Data.Instance.ReadBoolean(context, Data.IsFirstUpdate, true);
                if (isFirstUpdate)
                {
                    db.UpdateBooks();
                }
                else
                {
                    Book book = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration).All<Book>().Last();
                    db.UpdateBook(book);
                    Data.Instance.Write(context, Data.IsFirstUpdate, false);
                }
                isRefreshing = false;
                Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                ComponentName provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                appWidgetManager.UpdateAppWidget(provider, BuildRemoteViews(context, appWidgetIds, 0));
            }).Start();

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            ComponentName me = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
            appWidgetManager.UpdateAppWidget(me, BuildRemoteViews(context, appWidgetIds, 0));
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            Bundle options = appWidgetManager.GetAppWidgetOptions(appWidgetId);

            // Get min width and height.
            int minWidth = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            //int minHeight = options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight);

            // Obtain appropriate widget and update it.
            appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteViews(context, new int[] { appWidgetId }, minWidth));
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            // Check if the click is to open chapter in browser
            if (OpenChapterClick.Equals(intent.Action))
            {
                var pm = context.PackageManager;
                try
                {
                    //read last chapter url
                    string url = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration).All<Chapter>().Last().URL;

                    // launch in browser
                    Uri uri = Uri.Parse(url);
                    Intent browser = new Intent(Intent.ActionView, uri);
                    context.StartActivity(browser);
                }
                catch
                {
                    // Something went wrong :(
                }
            }
        }

        private RemoteViews BuildRemoteViews(Context context, int[] appWidgetIds, int width)
        {
            RemoteViews widgetView;

            cells = width == 0 ? 0 : GetCellsForSize(width);

            int i = cells > 3 ? 3 : cells;
            int layout = isRefreshing ? layoutsRefreshing[i] : layouts[i];
            layouts[0] = layouts[i];
            layoutsRefreshing[0] = layoutsRefreshing[i];

            widgetView = new RemoteViews(context.PackageName, layout);

            if (!isRefreshing)
            {
                SetView(context, appWidgetIds, widgetView);
            }

            return widgetView;
        }

        private static int GetCellsForSize(int size)
        {
            int n = 1;
            while (70 * n - 30 < size)
            {
                ++n;
            }
            return n - 1;
        }

        private void SetView(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            // Set TextViews
            string title = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration).All<Chapter>().Last().Title;
            long lastUpdate = Data.Instance.ReadLong(context, Data.LastUpdate);

            widgetView.SetTextViewText(Resource.Id.chapter_title, title);
            widgetView.SetTextViewText(Resource.Id.last_update, string.Format("{0:MM/dd/yy H:mm:ss}", new DateTime(lastUpdate)));
            
            // Bind the click intent for the chapter on the widget
            Intent chapterIntent = new Intent(context, typeof(Widget));
            chapterIntent.SetAction(OpenChapterClick);
            chapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
            PendingIntent chapterPendingIntent = PendingIntent.GetBroadcast(context, 0, chapterIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.root, chapterPendingIntent);
            
            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(Widget));
            refreshIntent.SetAction(RefreshClick);
            refreshIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, 0, chapterIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);
        }
    }
}