using System;
using System.Collections.Generic;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace ForgottenConqueror
{
    class WidgetLargeService : RemoteViewsService
    {
        private new static readonly string PackageName = ForgottenConqueror.Instance.PackageName; //"Za1d3.ForgottenConqueror";

        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(intent);
        }

        private class ViewFactory : Java.Lang.Object, IRemoteViewsFactory
        {
            private List<int> Layouts = new List<int>() { Resource.Layout.widget_large_list, Resource.Layout.widget_large_list, Resource.Layout.widget_large_list, };
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;

            public ViewFactory(Intent intent)
            {
                WidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            }

            public RemoteViews GetViewAt(int position)
            {
                RemoteViews page = new RemoteViews(PackageName, Layouts[position]);

                //page.SetTextViewText(Resource.Id.book_title, $"book {position}"); // TMP!

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public int Count => Layouts.Count;

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(PackageName, Resource.Layout.widget_1cell_progress);

            public int ViewTypeCount => 1;

            public void OnCreate()
            {
                // Created
            }

            public void OnDataSetChanged()
            {
                // Dataset changed
            }

            public void OnDestroy()
            {
                // Destroyed
            }
        }
    }
}