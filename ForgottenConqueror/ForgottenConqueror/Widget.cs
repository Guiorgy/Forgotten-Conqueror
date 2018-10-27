using System;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using HtmlAgilityPack;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Forgotten Conqueror")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    class Widget : AppWidgetProvider
    {
        private bool isRefreshing = false;
        private int layout = Resource.Layout.widget;

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            if (isRefreshing)
            {
                return;
            }
            isRefreshing = true;

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            ComponentName me = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
            appWidgetManager.UpdateAppWidget(me, BuildRemoteViews(context, appWidgetIds, 0));
        }

        private RemoteViews BuildRemoteViews(Context context, int[] appWidgetIds, int width)
        {
            RemoteViews widgetView;

            int cells = GetCellsForSize(width);

            switch (cells)
            {
                case 0:
                    widgetView = new RemoteViews(context.PackageName, layout);
                    break;
                case 1:
                    widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_1cell);
                    layout = Resource.Layout.widget_1cell;
                    break;
                case 2:
                    widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_2cell);
                    layout = Resource.Layout.widget_2cell;
                    break;
                case 3:
                    widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget_3cell);
                    layout = Resource.Layout.widget_3cell;
                    break;
                default:
                    widgetView = new RemoteViews(context.PackageName, Resource.Layout.widget);
                    layout = Resource.Layout.widget;
                    break;

            }

            SetTextViewText(context, widgetView, cells == 0);
            RegisterClicks(context, appWidgetIds, widgetView);

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

        private void SetTextViewText(Context context, RemoteViews widgetView, bool update)
        {
            if (!update)
            {
                if (layout != Resource.Layout.widget_1cell)
                {
                    // Read last chapter title from SharedPreference
                    string lastChapter = Data.Instance.Read(context, Data.LastChapterTitle);
                    if (lastChapter == null)
                    {
                        lastChapter = "";
                    }
                    // Read last chapter count from SharedPreference
                    int chapterCount = Data.Instance.ReadInt(context, Data.LastChapterCount);
                    long lastUpdate = Data.Instance.ReadLong(context, Data.LastUpdate);

                    widgetView.SetTextViewText(Resource.Id.chapter_title, lastChapter);
                    widgetView.SetTextViewText(Resource.Id.last_update, string.Format("{0:MM/dd/yy H:mm:ss}", new DateTime(lastUpdate)));
                }
            }
            else
            {
                widgetView.SetViewVisibility(Resource.Id.btn_refresh, ViewStates.Invisible);
                widgetView.SetViewVisibility(Resource.Id.progress_container, ViewStates.Visible);

                // Read last chapter title from SharedPreference
                string lastChapter = Data.Instance.Read(context, Data.LastChapterTitle);
                if (lastChapter == null)
                {
                    lastChapter = "";
                }
                // Read last chapter count from SharedPreference
                int chapterCount = Data.Instance.ReadInt(context, Data.LastChapterCount);
                long lastUpdate = Data.Instance.ReadLong(context, Data.LastUpdate);

                // Get html document
                try
                {
                    string url = "http://forgottenconqueror.com/book-3/";
                    HtmlWeb web = new HtmlWeb();
                    HtmlDocument doc = web.Load(url);

                    HtmlNode container = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']/p[last()]");
                    HtmlNodeCollection chapters = container.SelectNodes("./a");

                    int count = chapters.Count();
                    string title = HtmlEntity.DeEntitize(chapters.Last().InnerText);
                    string chapterURL = chapters.Last().Attributes["href"].Value;
                    Data.Instance.Write(context, Data.LastChapterURL, chapterURL);

                    // Compare
                    if (chapterCount != count)
                    {
                        Data.Instance.Write(context, Data.LastChapterCount, count);
                    }
                    if (!lastChapter.Equals(title))
                    {
                        Data.Instance.Write(context, Data.LastChapterTitle, title);
                    }
                    long now = DateTime.Now.Ticks;
                    Data.Instance.Write(context, Data.LastUpdate, now);

                    if (layout != Resource.Layout.widget_1cell)
                    {
                        widgetView.SetTextViewText(Resource.Id.chapter_title, title);
                        widgetView.SetTextViewText(Resource.Id.last_update, string.Format("{0:MM/dd/yy H:mm:ss}", new DateTime(now)));
                    }
                }
                catch (Exception e)
                {
                    // Something went wrong :(
                    Toast.MakeText(context, "Failed to update!", ToastLength.Short).Show();
                }

                isRefreshing = false;
                widgetView.SetViewVisibility(Resource.Id.btn_refresh, ViewStates.Visible);
                widgetView.SetViewVisibility(Resource.Id.progress_container, ViewStates.Invisible);
            }
        }

        private void RegisterClicks(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            Intent intent = new Intent(context, typeof(Widget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);

            // Click to open chapter in browser
            widgetView.SetOnClickPendingIntent(Resource.Id.root, GetPendingSelfIntent(context, OpenChapterClick));

            // Refresh button click
            PendingIntent piRefresh = PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, piRefresh);
        }

        private PendingIntent GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(Widget));
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, 0);
        }
        
        private readonly static string OpenChapterClick = "OpenChapterClick";

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            // Check if the click is to open chapter in browser
            if (OpenChapterClick.Equals(intent.Action))
            {
                var pm = context.PackageManager;
                try
                {
                    // read last chapter url
                    string url = Data.Instance.Read(context, Data.LastChapterURL);

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
    }
}