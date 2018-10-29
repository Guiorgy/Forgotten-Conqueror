using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ForgottenConqueror
{
    class WidgetLargeService : RemoteViewsService
    {
        private new static readonly string PackageName = "Za1d3.ForgottenConqueror";

        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(intent);
        }

        private class ViewFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
        {
            private List<int> Layouts = new List<int>() { Resource.Layout.widget_large_list, Resource.Layout.widget_large_list, Resource.Layout.widget_large_list, };
            private int InstanceId = AppWidgetManager.InvalidAppwidgetId;

            public ViewFactory(Intent intent)
            {
                InstanceId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
            }

            public void OnCreate()
            {
                // Created
            }
            
            public RemoteViews GetViewAt(int position)
            {
                RemoteViews page = new RemoteViews(PackageName, Layouts[position]);

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public void OnDataSetChanged()
            {
                // Dataset changed
            }

            public new void Dispose()
            {
                // Disposed
            }

            public void OnDestroy()
            {
                // Destroyed
            }

            public int Count => Layouts.Count;

            public bool HasStableIds => true;

            public RemoteViews LoadingView =>  new RemoteViews(PackageName, Resource.Layout.widget_large_progress);

            public int ViewTypeCount => 1;

            public new IntPtr Handle => throw new NotImplementedException();
        }
    }
}