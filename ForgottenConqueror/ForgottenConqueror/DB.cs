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

        // Methods
        public void UpdateBooks()
        {
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load("http://forgottenconqueror.com/");

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/h1/strong/a");

            int count = 0;
            foreach(HtmlNode node in nodes)
            {
                string title = HtmlEntity.DeEntitize(node.InnerText);
                string url = node.Attributes["href"].Value;

                Book book = new Book()
                {
                    ID = count++,
                    Count = count,
                    Title = title,
                    URL = url,
                };

                realm.Write(() => realm.Add<Book>(book, true));

                UpdateBook(book);
            }
        }

        public void UpdateBook(Book book)
        {
            Realm realm = Realm.GetInstance(Realms.RealmConfiguration.DefaultConfiguration);
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(book.URL);

            HtmlNodeCollection containers = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p[position()>2]");

            int count = 0;
            var books = realm.All<Book>().Where(b => b.Count < book.Count);
            foreach (Book b in books) count += b.Chapters.Count();

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
    }
}