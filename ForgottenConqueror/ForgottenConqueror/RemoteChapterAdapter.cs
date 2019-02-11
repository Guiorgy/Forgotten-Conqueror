using System.Linq;
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
    class RemoteChapterAdapter : RemoteViewsService
    {
        public static readonly string ExtraBookId = "ExtraBookId";
        public static readonly string ExtraSortOrder = "ExtraSortOrder";
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new ViewFactory(ApplicationContext, intent);
        }

        private class ViewFactory : Java.Lang.Object, IRemoteViewsFactory
        {
            private static readonly int ItemLayout = Resource.Layout.widget__large_chapter_listitem;
            private Context context;
            private int WidgetId = AppWidgetManager.InvalidAppwidgetId;
            private int BookId;
            private bool Descending;
            private int FirstChapterId;

            public ViewFactory(Context context, Intent intent)
            {
                this.context = context;
                WidgetId = intent.GetIntExtra(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);
                BookId = intent.GetIntExtra(ExtraBookId, 0);
                Descending = intent.GetBooleanExtra(ExtraSortOrder, false);
            }

            public RemoteViews GetViewAt(int position)
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);

                RemoteViews page = new RemoteViews(context.PackageName, ItemLayout);

                Chapter chapter = Descending ? realm.Find<Chapter>(FirstChapterId + Count - 1 - position)
                    : realm.Find<Chapter>(FirstChapterId + position);
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

            public int Count { get; private set; }

            public bool HasStableIds => true;

            public RemoteViews LoadingView => new RemoteViews(context.PackageName, Resource.Layout.widget__small_1cell_progress);

            public int ViewTypeCount => 1;
            
            public void OnCreate()
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                FirstChapterId = realm.Find<Book>(BookId).Chapters.FirstOrDefault().ID;
                Count = realm.Find<Book>(BookId).Chapters.Count();
            }

            public void OnDataSetChanged()
            {
                Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                FirstChapterId = realm.Find<Book>(BookId).Chapters.FirstOrDefault().ID;
                Count = realm.Find<Book>(BookId).Chapters.Count();
            }

            public void OnDestroy()
            {
                Realm.GetInstance(DB.RealmConfiguration).Dispose();
            }
        }
    }
}