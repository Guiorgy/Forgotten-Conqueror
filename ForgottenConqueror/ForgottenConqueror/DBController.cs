using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using HtmlAgilityPack;
using Realms;
using static ForgottenConqueror.DB;

namespace ForgottenConqueror
{
    class DBController
    {
        private static object thislock = new object();
        private static DBController instance;
        private DBController() { }

        public static DBController Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new DBController();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }
        
        private bool CanParse = true;
        public void ParseBooks(Context context)
        {
            if (CanParse)
            {
                CanParse = false;

                void Finished()
                {
                    Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                    realm.Write(() =>
                    {
                        foreach (WidgetParams widgetParams in realm.All<WidgetParams>())
                        {
                            widgetParams.IsRefreshing = false;
                        }

                        foreach (WidgetLargeParams widgetLargeParams in realm.All<WidgetLargeParams>())
                        {
                            widgetLargeParams.IsRefreshing = false;
                        }

                        foreach (WidgetLargeAltParams widgetLargeAltParams in realm.All<WidgetLargeAltParams>())
                        {
                            widgetLargeAltParams.IsRefreshing = false;
                        }
                    });
                    Data.Instance.Write(context, Data.LastUpdateTime, DateTime.Now.Ticks);

                    int lastId = Data.Instance.ReadInt(context, Data.PreviouslyLastChapterId, -1);
                    int currentId = realm.All<Chapter>().OrderBy(c => c.ID).Last().ID;
                    if (lastId != -1 && lastId < currentId)
                    {
                        List<Chapter> chapters = realm.All<Chapter>().Where(c => c.ID > lastId).ToList();
                        NotificationManager.Instance.NotifyNewChapters(context, chapters);
                    }

                    RedrawAllWidgets(context);
                    CanParse = true;
                    realm.Dispose();
                }

                try
                {
                    Task UpdateTask = Task.Run(() =>
                    {
                        Realm realm = Realm.GetInstance(DB.RealmConfiguration);
                        var list = realm.All<Chapter>();
                        if (list.Count() != 0)
                        {
                            var last = list.OrderBy(c => c.ID).Last();
                            if (last != null)
                            {
                                Data.Instance.Write(context, Data.PreviouslyLastChapterId, last.ID);
                            }
                        }
                        UpdateBooks();
                    });
                    UpdateTask.ContinueWith((task) =>
                    {
                        Finished();
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                } catch(Exception e)
                {
                    Log.Error(e, "ParseBooks() failed!");
                    Finished();
                }
            }
        }

        private static object parselock = new object();
        [Obsolete("A lot slower, only use if absolutely needed. Use ParseBooks(Context context, bool onlyLast) instead.")]
        public void ParseBooksSafe(Context context)
        {
            if (CanParse)
            {
                lock (parselock)
                {
                    ParseBooks(context);
                }
            }
        }

        private void UpdateBook(Realm realm = null, Book book = null, int id = 0)
        {
            if (realm == null) realm = Realm.GetInstance(DB.RealmConfiguration);

            if (book == null)
            {
                book = realm.Find<Book>(id);
                if (book == null) return;
            }
            Chapter first = book.Chapters.OrderBy(c => c.ID).First();
            int count = first == null ? 0 : first.ID;

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);
            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");
            
            List<Chapter> chapters = new List<Chapter>();
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach (HtmlNode node in nodes)
                {
                    string title = HtmlEntity.DeEntitize(node.InnerText);
                    string url = node.Attributes["href"].Value;

                    Chapter chapter = new Chapter()
                    {
                        ID = count++,
                        Count = chapters.Count + 1,
                        Title = title,
                        URL = url,
                        Book = book,
                    };

                    chapters.Add(chapter);
                }
            }

            realm.Write(() =>
            {
                foreach (Chapter chapter in chapters)
                {
                    realm.Add<Chapter>(chapter, true);
                }
            });
        }

        private void UpdateBooks()
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load("http://forgottenconqueror.com/");

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/h1/strong/a");

            List<Task<List<Chapter>>> tasks = new List<Task<List<Chapter>>>();
            List<Book> books = new List<Book>();
            foreach (HtmlNode node in nodes)
            {
                string title = HtmlEntity.DeEntitize(node.InnerText);
                string url = node.Attributes["href"].Value;

                Book book = new Book()
                {
                    ID = books.Count,
                    Count = books.Count + 1,
                    Title = title,
                    URL = url,
                };

                books.Add(book);
                tasks.Add(Task<List<Chapter>>.Run(() => UpdateBooks_Chapters(book)));
            }

            Task.WaitAll(tasks.ToArray());
            List<Chapter> chapters = tasks.SelectMany(task => task.Result).ToList();

            realm.Write(() =>
            {
                foreach (Book book in books)
                {
                    realm.Add<Book>(book, true);
                }
                for (int i = 0; i < chapters.Count(); i++)
                {
                    chapters[i].ID = i;
                    realm.Add<Chapter>(chapters[i], true);
                }
            });
        }

        private List<Chapter> UpdateBooks_Chapters(Book book)
        {
            Realm realm = Realm.GetInstance(DB.RealmConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");

            List<Chapter> chapters = new List<Chapter>();
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach (HtmlNode node in nodes)
                {
                    string title = HtmlEntity.DeEntitize(node.InnerText);
                    string url = node.Attributes["href"].Value;

                    Chapter chapter = new Chapter()
                    {
                        Count = chapters.Count() + 1,
                        Title = title,
                        URL = url,
                        Book = book,
                    };

                    chapters.Add(chapter);
                }
            }

            return chapters;
        }

        private void RedrawAllWidgets(Context context)
        {
            // Widget
            Widget widget = new Widget();
            widget.RedrawAll(context);

            // WidgetLarge
            WidgetLarge widgetLarge = new WidgetLarge();
            widgetLarge.RedrawAll(context);

            // WidgetLargeAlt
            WidgetLargeAlt widgetLargeAlt = new WidgetLargeAlt();
            widgetLargeAlt.RedrawAll(context);
        }


        #region String compression
        // Thank you @xanatos (https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp)
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
        #endregion
    }
}
