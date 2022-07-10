using System;
using HtmlAgilityPack;
using CsvHelper;
using System.Collections.Generic;
using System.Linq;

/*
Instructions for the future you who is undoubtably too stupid to remeber how to run this
1. dotnet build
2. dotnet run

*/

namespace GRVASWebScraper
{
    class Program
    {
        
        static void Main(string[] args)
        {
            string month = DateTime.Now.ToString("MMMM");
            string rwjUrl = "https://trainingcentertechnologies.com/rwjbh/CourseEnrollment.aspx";
            string mountainsideUrl = "https://mountainsidehosp.com/services/emergency-services/ems-education-cprfirst-aid";
            PrintEmailIntro(month);
            PrintRWJClasses(rwjUrl, month);
            PrintMountainsideClasses(mountainsideUrl, month);
            PrintEmailClosing();
        }

        public static void setFileStreamWriter(){
            FileStream ostrm;
    StreamWriter writer;
    TextWriter oldOut = Console.Out;
    try
    {
        ostrm = new FileStream ("./Redirect.txt", FileMode.OpenOrCreate, FileAccess.Write);
        writer = new StreamWriter (ostrm);
    }
    catch (Exception e)
    {
        Console.WriteLine ("Cannot open Redirect.txt for writing");
        Console.WriteLine (e.Message);
        return;
    }
    Console.SetOut (writer);
        }

        public static void PrintEmailIntro(string month)
        {
            string intro = @"
            Hey guys, 

            Here are the free or very inexpensive CEU options for the month of May. Please reach out if you have any questions!";
            Console.WriteLine(intro);
        }

        public static void PrintEmailClosing()
        {
            Console.WriteLine("\nBest,");
        }

        public static void PrintRWJClasses(String RwjUrl, string month)
        {
            try
            {
                HtmlNodeCollection classNodes = GetRWJClassDetails(RwjUrl);
                var classList = FilterByMonth(GetRWJClasses(classNodes), month);

                Console.WriteLine("\nRWJ Classes");
                Console.WriteLine($"Register at: {RwjUrl}");
                foreach (var mClass in classList)
                {
                    if (!mClass.IsInPerson)
                    {
                        Console.WriteLine(mClass.formClassOutput());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void PrintMountainsideClasses(String MountainsideUrl, String month)
        {
            string mountainsideReminder = "(Reminder: these mountainside classes serve as our monthly training meeting. Try to attend if you can!)";
            try
            {
                HtmlNodeCollection classNodes = GetMountainsideClassDetails(MountainsideUrl);
                var classList = FilterByMonth(GetMountainsideClasses(classNodes), month);

                Console.WriteLine("\nMountainside Classes");
                Console.WriteLine(mountainsideReminder);
                Console.WriteLine($"Register at: {MountainsideUrl}");
                foreach (var mClass in classList)
                {
                    if (!mClass.IsInPerson)
                    {
                        Console.WriteLine(mClass.formClassOutput());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        static HtmlDocument GetDocument(string url)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            return doc;
        }

        static HtmlNodeCollection GetRWJClassDetails(string url)
        {
            HtmlDocument doc = GetDocument(url);
            var tableItemXPath = "//tr[contains(@class,\"bottomborder\")]/td/table/tr[2]";

            HtmlNodeCollection classNodeCollection = doc.DocumentNode.SelectNodes(tableItemXPath);

            return classNodeCollection;
        }

        static List<CeuClass> GetRWJClasses(HtmlNodeCollection classNodes)
        {
            var classes = new List<CeuClass>();
            foreach (var node in classNodes)
            {
                var newClass = new CeuClass();
                try
                {
                    IEnumerable<HtmlNode> nodeCollection = node.Elements("td");
                    List<HtmlNode> childList = nodeCollection.ToList();
                    newClass.Title = childList[1].Element("span").GetDirectInnerText();
                    newClass.Note = childList[1].Elements("span").ToList()[1].Element("i").GetDirectInnerText();
                    newClass.Date = childList[1].Element("div").Element("span").GetDirectInnerText();
                    string[] timeParse = childList[1].Element("div").Elements("span").ToList()[1].GetDirectInnerText().Split("&nbsp;");
                    if (timeParse.Length > 1)
                    {
                        newClass.Time = timeParse[1];
                    }
                    else
                    {
                        newClass.Time = timeParse[0];
                    }

                    newClass.Description = childList[1].Element("a").Element("span").GetDirectInnerText().Trim();

                    //for online classes, locationName is ONLINE LIVE CLASS
                    newClass.LocationName = childList[2].Elements("span").ToList()[0].GetDirectInnerText();
                    //for online classes, streetAddress is the website url
                    newClass.StreetAddress = childList[2].Elements("span").ToList()[1].GetDirectInnerText();
                    newClass.Town = childList[2].Elements("span").ToList()[3].GetDirectInnerText();
                    newClass.State = childList[2].Elements("span").ToList()[5].GetDirectInnerText();
                    newClass.ZipCode = childList[2].Elements("span").ToList()[6].GetDirectInnerText();

                    newClass.Enrolled = Int32.Parse(childList[3].Element("b").Element("span").GetDirectInnerText().Split(", ")[0].Split(": ")[1]);
                    newClass.MaxEnrolled = Int32.Parse(childList[3].Element("b").Element("span").GetDirectInnerText().Split(", ")[1].Split(": ")[1]);
                    newClass.Cost = childList[3].Element("b").Elements("span").ToList()[1].GetDirectInnerText().Split("$")[1];
                    if (newClass.LocationName.ToLower().Contains("online"))
                    {
                        newClass.IsInPerson = false;
                    }
                    else
                    {
                        newClass.IsInPerson = true;
                    }

                    classes.Add(newClass);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
            return classes;
        }

        private static HtmlNodeCollection GetMountainsideClassDetails(String url)
        {
            HtmlDocument doc = GetDocument(url);
            var tableItemXPath = "//div[contains(@class,\"col-md-6 col-lg-6\")]";

            HtmlNodeCollection classNodeCollection = doc.DocumentNode.SelectNodes(tableItemXPath);

            return classNodeCollection;
        }
        public static List<CeuClass> GetMountainsideClasses(HtmlNodeCollection classCollection)
        {
            List<CeuClass> classList = new List<CeuClass>();
            List<HtmlNode> HtmlColumnNodeList = classCollection.ToList();
            List<HtmlNode> HtmlNodeList = HtmlColumnNodeList[0].ChildNodes.ToList();
            HtmlNodeList.AddRange(HtmlColumnNodeList[1].ChildNodes.ToList());
            CeuClass mClass = null;
            foreach (var node in HtmlNodeList)
            {
                if (node.Name.Equals("h3"))
                {
                    if (mClass != null)
                    {
                        classList.Add(mClass);
                    }
                    mClass = new CeuClass();
                    mClass.Cost = "0.00";
                    mClass.Time = "6:30pm";
                    mClass.Description = "6:30pm - Dinner and Drinks, 7pm - Lecture";
                    mClass.Date = node.GetDirectInnerText().Trim();
                }
                else if (mClass != null && node.GetClasses().Contains("bold"))
                {
                    mClass.Title = node.GetDirectInnerText().Trim();
                }
                else if (mClass != null && node.GetClasses().Contains("no-margin"))
                {
                    mClass.LocationName = node.GetDirectInnerText().Trim();
                }
            }

            return classList;
        }

        public static List<CeuClass> FilterByMonth(List<CeuClass> list, string month)
        {
            return list.Where(x => x.Date.Contains(month)).ToList();
        }
    }

    class CeuClass
    {
        public bool IsInPerson { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public int Enrolled { get; set; }
        public int MaxEnrolled { get; set; }
        public string Cost { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Description { get; set; }
        public string LocationName { get; set; }
        public string StreetAddress { get; set; }
        public string Town { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }

        public string formClassOutput()
        {
            return @$"
            Class Title: {Title}
            Time: {Date}, {Time}
            Location: {LocationName}
            Cost: ${Cost}, Open Spots: {(MaxEnrolled==0 ? "Unknown" : MaxEnrolled - Enrolled) }
            Description: {Description}";
        }


    }
}
