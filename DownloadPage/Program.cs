using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml;
using System.Threading;

using HtmlAgilityPack;

namespace DownloadPage
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> list = GetLinks("http://www.ceskenoviny.cz/archiv/?id_seznam=158&id_rubrika=35&seznam_start=", 0, 1000000);
            File.WriteAllLines("links.txt", list);
            //File.WriteAllLines("links.txt", list);
            // distribuce odkazu
            //SaveData(list);

            //ParseData("data.xml");

            //Xml2Txt("data-clear.xml");

            //Split("slov.txt", 1000);

            //Txt2Dic("data-clear.txt");

            Console.WriteLine();
        }

        static void Split(string file, int linesLimit)
        {
            string[] lines = File.ReadAllLines(file);
            int set = 1;
            TextWriter tw = new StreamWriter(file + "_" + set);
            foreach (string line in lines.Distinct())
            {
                tw.WriteLine(line);
                set++;
                if (set % linesLimit == 0)
                {
                    tw.Close();
                    tw = new StreamWriter(file + "_" + set);
                }
            }
            tw.Close();
        }

        static void Txt2Dic(string source)
        {
            TextReader tr = new StreamReader(source);
            string line = null;
            HashSet<string> alphabet = new HashSet<string>();
            Dictionary<string, int> unigrams = new Dictionary<string, int>();
            Dictionary<string, int> bigrams = new Dictionary<string, int>();
            while ((line = tr.ReadLine()) != null)
            {
                string prevWord = "<s>";
                string[] words = line.ToLower().Split(new string[] { " ", " ","\t" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    foreach (char ch in word)
                        alphabet.Add(ch.ToString());

                    if (!unigrams.ContainsKey(word))
                        unigrams.Add(word, 1);
                    else
                        unigrams[word]++;
                    string bigram = prevWord + "\t" + word;
                    if (!bigrams.ContainsKey(bigram))
                        bigrams.Add(bigram, 1);
                    else
                        bigrams[bigram]++;
                    prevWord = word;
                }
            }
            tr.Close();

            File.WriteAllLines("aplhabet.txt", alphabet.OrderBy(p=>p));
            File.WriteAllLines("unigrams.txt", unigrams.OrderByDescending(p=>p.Value).Select(p => p.Key + "\t" + p.Value));
            File.WriteAllLines("bigrams.txt", bigrams.OrderByDescending(p => p.Value).Select(p => p.Key + "\t" + p.Value));


        }


        static void Xml2Txt(string xmlFilePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);
            TextWriter tw = new StreamWriter(xmlFilePath.Replace(".xml", ".txt"));
            foreach (XmlNode node in doc.DocumentElement.SelectNodes("/articles/article/text"))
            {
                tw.WriteLine(Regex.Replace(node.InnerText.Trim(),@"[^\p{L}\s]"," ").Replace("\r","").Replace("\n"," "));
            }
            tw.Close();

            Console.WriteLine("Velikost trenovacich dat je v kB: "+(new FileInfo(xmlFilePath.Replace(".xml", ".txt")).Length / 1024));
        }

        static void ParseData(string xmlFilePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);
            Regex javaScriptRegex = new Regex(@"<script type='text/javascript'>.*?</script>", RegexOptions.Multiline);
            Regex dateRegex = new Regex(@"itemprop='datePublished'.+?((\d{2}).(\d{2}).(\d{4})), ((\d{2}):(\d{2}))<", RegexOptions.Compiled);
            Regex titleRegex = new Regex(@"class='titulek-clanku'>(.+?)</h1>", RegexOptions.Compiled);
            Regex authorRegex = new Regex(@"<span itemprop='author'>(.*?)</span>", RegexOptions.Compiled);
            Regex textRegex = new Regex(@"<p>(.*?)</p>", RegexOptions.Multiline);
            Regex imgAltRegex = new Regex("<img .*? alt=[\"'](.+?)[\"']");

            List<Regex> cleartext = new List<Regex>();
            cleartext.Add(new Regex("<span class='image-.+?'>.+?</span>"));
            cleartext.Add(new Regex("<img .*?>"));
            cleartext.Add(new Regex("<a href=\"http://disqus.com\" class=\"dsq-brlink\">.*?</a>"));
            cleartext.Add(new Regex("<a href='.+?' style='.+?'>Zjistit víc</a>"));
            cleartext.Add(new Regex("<a .+?></a>"));
            cleartext.Add(new Regex("<.*?>"));

            foreach (XmlNode article in doc.DocumentElement.SelectNodes("//article"))
            {
                string content = article.SelectSingleNode("content").InnerText;

                content = javaScriptRegex.Replace(content, "");

                Match dateMatch = dateRegex.Match(content);
                Console.WriteLine("Vydano: " + dateMatch.Groups[1] + " v " + dateMatch.Groups[5]);

                Match titleMatch = titleRegex.Match(content);
                Console.WriteLine("Nadpis: " + titleMatch.Groups[1]);

                Match authorMatch = authorRegex.Match(content);
                Console.WriteLine("Autor: " + authorMatch.Groups[1]);

                StringBuilder textBuilder = new StringBuilder();
                foreach (Match m in textRegex.Matches(content))
                    textBuilder.AppendLine(m.Groups[1].ToString().Trim());
                
                string text = textBuilder.ToString();
                foreach (Regex r in cleartext)
                    text = r.Replace(text, "");

                XmlNode date = doc.CreateNode(XmlNodeType.Element, "date", null);
                date.InnerText = dateMatch.Groups[1].ToString();

                XmlNode time = doc.CreateNode(XmlNodeType.Element, "time", null);
                time.InnerText = dateMatch.Groups[5].ToString();

                XmlNode dateTime = doc.CreateNode(XmlNodeType.Element, "published", null);
                dateTime.AppendChild(date);
                dateTime.AppendChild(time);
                article.AppendChild(dateTime);

                XmlNode title = doc.CreateNode(XmlNodeType.Element, "title", null);
                title.InnerText = titleMatch.Groups[1].ToString();
                article.AppendChild(title);

                XmlNode author = doc.CreateNode(XmlNodeType.Element, "author", null);
                author.InnerText = authorMatch.Groups[1].ToString();
                article.AppendChild(author);

                XmlNode textNode = doc.CreateNode(XmlNodeType.Element, "text", null);
                textNode.InnerText = text.Replace("\r\n\r\n","\r\n").Replace("\n\n","\n");
                article.AppendChild(textNode);

                XmlNode imgAltsNode = doc.CreateNode(XmlNodeType.Element, "imgAlts", null);
                article.AppendChild(imgAltsNode);
                foreach (Match m in imgAltRegex.Matches(content))
                {
                    XmlNode imgAlt = doc.CreateNode(XmlNodeType.Element, "amgAlt", null);
                    imgAlt.InnerText = m.Groups[1].ToString();
                    imgAltsNode.AppendChild(imgAlt);
                }
            }
            doc.Save("data-clear.xml");

        }

        static List<string> GetLinks(string page, int offsetInicial, int limit)
        {
            List<string> links = new List<string>();
            int count = limit;
            int offset = offsetInicial;
            Regex hrefRegex = new Regex("a href='(.+?)' class");
            while(links.Count<=limit)
            {
                try
                {
                    Uri uri = new Uri("http://www.ceskenoviny.cz/ajax.php?action=nextSeznam&format=CN15_perex_box_li&count=" + count + "&idseznam=1074&start=" + offset);
                    WebClient cl = new WebClient();
                    string pageString = cl.DownloadString(uri);

                    MatchCollection mc = hrefRegex.Matches(pageString);

                    foreach (Match node in mc)
                    {
                        string link = node.Groups[1].ToString();
                        if (!links.Contains(link))
                            links.Add(link);
                    }
                    Thread.Sleep(100);
                    offset = offset + count;
                    Console.WriteLine("pocet linku " + links.Count);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return links;
        }

        static void SaveData(IEnumerable<string> links)
        {
            XmlWriter writer = XmlWriter.Create("data.xml");
            writer.WriteStartElement("articles");
            foreach (string link in links)
            {
                WebClient cl = new WebClient();
                byte[] buffer = cl.DownloadData(link);

                string pageString = Encoding.UTF8.GetString(buffer);

                HtmlDocument doctmp = new HtmlDocument();
                doctmp.LoadHtml(Regex.Replace(pageString,@"\s\s+","\n"));
                HtmlNode articleNode = doctmp.DocumentNode.SelectSingleNode("//div[@class='left']");
                if (articleNode != null)
                {
                    string htmlCode = articleNode.InnerHtml;
                    writer.WriteStartElement("article");
                    writer.WriteElementString("link", link);
                    writer.WriteElementString("content", htmlCode);
                    writer.WriteEndElement();
                }
                Thread.Sleep(50);
            }
            writer.WriteEndElement();
            writer.Close();
        }
    }
}
