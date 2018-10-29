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
        private readonly static string NextClick = "NextClick";
        private readonly static string PreviousClick = "PreviousClick";
        private readonly static string RefreshClick = "RefreshClick";
        private static bool isRefreshing = false;
        private readonly static int layouts = Resource.Layout.widget_large;
        private readonly static int layoutsRefreshing = Resource.Layout.widget_large_progress;
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

        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            if (intent.Action.Equals(NextClick))
            {
                // Next
            }
            if (intent.Action.Equals(PreviousClick))
            {
                // Previous
            }
            if (intent.Action.Equals(RefreshClick))
            {
                // Refresh
            }
        }

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

            // Bind the click intent for the next button on the widget
            Intent nextIntent = new Intent(context, typeof(WidgetLarge));
			nextIntent.SetAction(NextClick);
			nextIntent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
			PendingIntent nextPendingIntent = PendingIntent.GetBroadcast(context, 0, nextIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_next, nextPendingIntent);

            // Bind the click intent for the previous button on the widget
            Intent previousIntent = new Intent(context, typeof(WidgetLarge));
            previousIntent.SetAction(PreviousClick);
			PendingIntent previousPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, 0);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_previous, previousPendingIntent);

            // Bind the click intent for the refresh button on the widget
            Intent refreshIntent = new Intent(context, typeof(WidgetLarge));
            refreshIntent.SetAction(RefreshClick);
            PendingIntent refreshPendingIntent = PendingIntent.GetBroadcast(context, 0, previousIntent, PendingIntentFlags.UpdateCurrent);
            widgetView.SetOnClickPendingIntent(Resource.Id.btn_refresh, refreshPendingIntent);
        }
    }
}