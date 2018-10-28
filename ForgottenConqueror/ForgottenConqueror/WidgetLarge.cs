using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System;
using System.Threading;
using Uri = Android.Net.Uri;

namespace ForgottenConqueror
{
    [BroadcastReceiver(Label = "Forgotten Conqueror")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider_large")]
    class WidgetLarge : AppWidgetProvider
    {
        private static bool isRefreshing = false;
        private static int layouts = Resource.Layout.widget_large;
        private static int layoutsRefreshing = Resource.Layout.widget_large_progress;
        private static int bookCount = 0;

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
                db.UpdateBooks();
                isRefreshing = false;
                Data.Instance.Write(context, Data.LastUpdate, DateTime.Now.Ticks);
                ComponentName provider = new ComponentName(context, Java.Lang.Class.FromType(typeof(Widget)).Name);
                appWidgetManager.UpdateAppWidget(provider, BuildRemoteViews(context, appWidgetIds));
            }).Start();

            base.OnUpdate(context, appWidgetManager, appWidgetIds);
            ComponentName me = new ComponentName(context, Java.Lang.Class.FromType(typeof(WidgetLarge)).Name);
            appWidgetManager.UpdateAppWidget(me, BuildRemoteViews(context, appWidgetIds));
        }

        //public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        //{
        //    Bundle options = appWidgetManager.GetAppWidgetOptions(appWidgetId);

        //    // Get min width and height.
        //    int minWidth = options.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
        //    int minHeight = options.GetInt(AppWidgetManager.OptionAppwidgetMinHeight);

        //    // Obtain appropriate widget and update it.
        //    appWidgetManager.UpdateAppWidget(appWidgetId, BuildRemoteViews(context, new int[] { appWidgetId }, minWidth));
        //    base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
        //}

        //private readonly static string OpenChapterClick = "OpenChapterClick";
        //public override void OnReceive(Context context, Intent intent)
        //{
        //    base.OnReceive(context, intent);

        //    // Check if the click is to open chapter in browser
        //    if (OpenChapterClick.Equals(intent.Action))
        //    {
        //        var pm = context.PackageManager;
        //        try
        //        {
        //            //read last chapter url
        //            string url = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration).All<Chapter>().Last().URL;

        //            // launch in browser
        //            Uri uri = Uri.Parse(url);
        //            Intent browser = new Intent(Intent.ActionView, uri);
        //            context.StartActivity(browser);
        //        }
        //        catch
        //        {
        //            // Something went wrong :(
        //        }
        //    }
        //}

        private RemoteViews BuildRemoteViews(Context context, int[] appWidgetIds)
        {
            RemoteViews widgetView;
            int layout = isRefreshing ? layoutsRefreshing : layouts;

            widgetView = new RemoteViews(context.PackageName, layout);

            if (!isRefreshing)
            {
                SetView(context, appWidgetIds, widgetView);
            }

            return widgetView;
        }

        private void SetView(Context context, int[] appWidgetIds, RemoteViews widgetView)
        {
            Intent intent = new Intent(context, typeof(RemoteViewsService));
			intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
			intent.SetData(Uri.Parse(intent.ToUri(IntentUriType.Scheme)));
            widgetView.SetRemoteAdapter(Resource.Id.view_flipper, intent);

			//// Bind the click intent for the next button on the widget
			//final Intent nextIntent = new Intent(context,
   //                 WidgetProvider.class);
			//nextIntent.setAction(WidgetProvider.NEXT_ACTION);
			//nextIntent.putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, id);
			//final PendingIntent nextPendingIntent = PendingIntent
   //                 .getBroadcast(context, 0, nextIntent,
			//				PendingIntent.FLAG_UPDATE_CURRENT);
			//rv.setOnClickPendingIntent(R.id.next, nextPendingIntent);

			//// Bind the click intent for the refresh button on the widget
			//final Intent refreshIntent = new Intent(context,
   //                 WidgetProvider.class);
			//refreshIntent.setAction(WidgetProvider.REFRESH_ACTION);
			//final PendingIntent refreshPendingIntent = PendingIntent
   //                 .getBroadcast(context, 0, refreshIntent,
			//				PendingIntent.FLAG_UPDATE_CURRENT);
			//rv.setOnClickPendingIntent(R.id.refresh, refreshPendingIntent);
        }
        
        private PendingIntent GetPendingSelfIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(WidgetLarge));
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, 0);
        }
    }
}