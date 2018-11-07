﻿using System.Linq;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Net;
using Android.Widget;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    [Service(Permission = Android.Manifest.Permission.BindRemoteviews, Exported = false)]
    class WidgetLargeService : RemoteViewsService
    {
        public static readonly string ExtraBookId = "ExtraBookId";
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(ApplicationContext, intent);
        }

        private class ViewFactory : Java.Lang.Object, IRemoteViewsFactory
        {
            private Context context;
            private int ItemLayout = Resource.Layout.widget_large_chapter_listitem;
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;
            private Realm RealmInstance;
            private int BookId;
            private int FirstChapterId;

            public ViewFactory(Context context, Intent intent)
            {
                this.context = context;
                WidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                BookId = intent.GetIntExtra(ExtraBookId, 0);
            }

            public RemoteViews GetViewAt(int position)
            {
                Realm other = Realm.GetInstance(DB.RealmConfiguration);
                if (!RealmInstance.IsSameInstance(other))
                {
                    RealmInstance.Dispose();
                    RealmInstance = other;
                }

                RemoteViews page = new RemoteViews(context.PackageName, ItemLayout);

                Chapter chapter = RealmInstance.Find<Chapter>(FirstChapterId + position);
                if (chapter == null) return page;

                page.SetTextViewText(Resource.Id.chapter_title, chapter.Title);

                Intent chapterClick = new Intent();
                chapterClick.SetData(Uri.Parse(chapter.URL));
                page.SetOnClickFillInIntent(Resource.Id.root, chapterClick);

                return page;
            }

            public long GetItemId(int position)
            {
                return position;
            }

            public int Count {
                get
                {
                    Realm other = Realm.GetInstance(DB.RealmConfiguration);
                    if (!RealmInstance.IsSameInstance(other))
                    {
                        RealmInstance.Dispose();
                        RealmInstance = other;
                    }
                    return RealmInstance.Find<Book>(BookId).Chapters.Count();
                }
            }

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(context.PackageName, Resource.Layout.widget_1cell_progress);

            public int ViewTypeCount => 1;
            
            public void OnCreate()
            {
                RealmInstance = Realm.GetInstance(DB.RealmConfiguration);
                FirstChapterId = RealmInstance.Find<Book>(BookId).Chapters.FirstOrDefault().ID;
            }

            public void OnDataSetChanged()
            {
                Realm other = Realm.GetInstance(DB.RealmConfiguration);
                if (!RealmInstance.IsSameInstance(other))
                {
                    RealmInstance.Dispose();
                    RealmInstance = other;
                }
                FirstChapterId = RealmInstance.Find<Book>(BookId).Chapters.FirstOrDefault().ID;
            }

            public void OnDestroy()
            {
                RealmInstance.Dispose();
            }
        }
    }
}