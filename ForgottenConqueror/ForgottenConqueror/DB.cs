using HtmlAgilityPack;
using Realms;
using System.Collections.Generic;
using System.Linq;

namespace ForgottenConqueror
{
    class DB
    {
        private static object thislock = new object();
        private static DB instance;
        private DB() { }

        public static DB Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (thislock)
                    {
                        if (instance == null)
                        {
                            instance = new DB();
                        }
                    }
                }

                return instance;
            }
            private set { }
        }

        // Entities
        public class Chapter : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public int Count { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }

            public Book Book { get; set; }
        }

        public class Book : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public int Count { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }

            [Backlink(nameof(Chapter.Book))]
            public IQueryable<Chapter> Chapters { get; }
        }

        public class WidgetParams : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public bool IsRefreshing { get; set; }
            public int Cells { get; set; }
        }

        public class WidgetLargeParams : RealmObject
        {
            [PrimaryKey]
            public int ID { get; set; }
            //public RealmInteger<int> Counter { get; set; }
            public bool IsRefreshing { get; set; }
            public int Book { get; set; }
        }

        // Methods
        public void UpdateBook(Realm realm = null, Book book = null, int id = 0)
        {
            if(realm == null) realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");
            
            if(book == null)
            {
                book = realm.Find<Book>(id);
            }
            Chapter first = book.Chapters.First();
            int count = first == null ? 0 : first.ID;

            List<Chapter> chapters = new List<Chapter>();
            foreach(HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach(HtmlNode node in nodes)
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
                foreach(Chapter chapter in chapters)
                {
                    realm.Add<Chapter>(chapter, true);
                }
            });
        }

        public void UpdateBooks(Realm realm = null)
        {
            if(realm == null) realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load("http://forgottenconqueror.com/");

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/h1/strong/a");

            List<Book> books = new List<Book>();
            List<Chapter> chapters = new List<Chapter>();
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

                UpdateBooks_Book(realm, book, chapters);
            }

            realm.Write(() =>
            {
                foreach(Book book in books)
                {
                    realm.Add<Book>(book, true);
                }
                foreach (Chapter chapter in chapters)
                {
                    realm.Add<Chapter>(chapter, true);
                }
            });
        }

        private void UpdateBooks_Book(Realm realm, Book book, List<Chapter> chapters)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");
            
            foreach (HtmlNode container in containers)
            {
                HtmlNodeCollection nodes = container.SelectNodes("./a");
                foreach (HtmlNode node in nodes)
                {
                    string title = HtmlEntity.DeEntitize(node.InnerText);
                    string url = node.Attributes["href"].Value;

                    Chapter chapter = new Chapter()
                    {
                        ID = chapters.Count,
                        Count = book.Chapters.Count() + 1,
                        Title = title,
                        URL = url,
                        Book = book,
                    };

                    chapters.Add(chapter);
                }
            }
        }
    }
}