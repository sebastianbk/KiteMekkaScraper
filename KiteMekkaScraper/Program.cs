using HtmlAgilityPack;
using System;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace KiteMekkaScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initiating web client...");
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            KiteBuddyEntities db = new KiteBuddyEntities();
            Console.WriteLine("Done!");
            Console.WriteLine("Looking for new kite spots...");

            for (int i = 1; i <= 1000; i++)
            {
                string url = "http://kitemekka.net/Kitespot.aspx?nr=" + i;

                try
                {
                    string page = client.DownloadString(url);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(page);

                    url = doc.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value;
                    if (db.IndexedSpots.SingleOrDefault(x => x.Link == url) != null)
                        continue;

                    string title = doc.DocumentNode.SelectSingleNode("//div[@id='idSpotname']").InnerText.Trim();
                    
                    string mapsUrl = doc.DocumentNode.SelectSingleNode("//a[@id='MainContent_hlShowmap']").Attributes["href"].Value;
                    double latitude = 0;
                    double longitude = 0;
                    if (mapsUrl.Contains("maps.google.com"))
                    {
                        string[] latlong = mapsUrl.Split(new char[] { '=' }).Last().Split(new char[] { ',' });
                        latitude = double.Parse(latlong.First());
                        longitude = double.Parse(latlong.Last());
                    }
                    else if (mapsUrl.Contains("findvej.dk"))
                    {
                        Regex latitudeRegex = new Regex(@"latitude=(\d*\.\d*)");
                        Match latMatch = latitudeRegex.Match(mapsUrl);
                        latitude = double.Parse(latMatch.Groups[1].Value);
                        Regex longitudeRegex = new Regex(@"longitude=(\d*\.\d*)");
                        Match longMatch = longitudeRegex.Match(mapsUrl);
                        longitude = double.Parse(longMatch.Groups[1].Value);
                    }

                    string category = doc.DocumentNode.SelectSingleNode("//div[@id='idedCategory']").InnerText.Trim();
                    string spotType = doc.DocumentNode.SelectSingleNode("//div[@id='idedSpottype']").InnerText.Trim();
                    string spotLevel = doc.DocumentNode.SelectSingleNode("//div[@id='idedSpotlevel']").InnerText.Trim();
                    string toilet = doc.DocumentNode.SelectSingleNode("//div[@id='idedToilet']").InnerText.Trim();
                    string directions = doc.DocumentNode.SelectSingleNode("//div[@id='idDirections']").InnerText.Trim();
                    string description = doc.DocumentNode.SelectSingleNode("//div[@id='idDescription']").InnerText.Trim();

                    IndexedSpot spot = new IndexedSpot();

                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindNE']").InnerHtml.Trim() != "")
                        spot.WindNE = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindE']").InnerHtml.Trim() != "")
                        spot.WindE = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindSE']").InnerHtml.Trim() != "")
                        spot.WindSE = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindS']").InnerHtml.Trim() != "")
                        spot.WindS = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindSV']").InnerHtml.Trim() != "")
                        spot.WindSW = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindV']").InnerHtml.Trim() != "")
                        spot.WindW = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindNV']").InnerHtml.Trim() != "")
                        spot.WindNW = true;
                    if (doc.DocumentNode.SelectSingleNode("//div[@id='idWindN']").InnerHtml.Trim() != "")
                        spot.WindN = true;

                    spot.Title = title;
                    spot.Link = url;
                    spot.Category = category;
                    spot.SpotType = spotType;
                    spot.SpotLevel = spotLevel;
                    spot.ToiletAvailable = toilet;
                    spot.Directions = directions;
                    spot.Description = description;
                    spot.Location = DbGeography.FromText("POINT(" + latitude + " " + longitude + ")", 4326);
                    db.IndexedSpots.Add(spot);
                    db.SaveChanges();

                    Console.WriteLine("Found: " + title);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            Console.WriteLine("Done!");
        }
    }
}
