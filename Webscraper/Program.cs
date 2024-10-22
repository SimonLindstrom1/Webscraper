using System;
using System.Collections.Generic;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

class Program
{
    static List<Thread> threads = new List<Thread>();
    static List<List<string>> threadResults = new List<List<string>>();
    static object lockObj = new object();

    static void Main(string[] args)
    {
        for (int i = 0; i < 3; i++)
        {
            
            List<string> threadResult = new List<string>();
            threadResults.Add(threadResult);
            int pageNumber = i + 1;

            Thread scrapeThread = new Thread(new ThreadStart(() => Scrape(pageNumber, threadResult)));
            threads.Add(scrapeThread);
            scrapeThread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
       
        for (int i = 0; i < threadResults.Count; i++)
        {
            Console.WriteLine($"Resultat från tråd {i + 1}:");
            foreach (var result in threadResults[i])
            {
                Console.WriteLine(result);
            }
            Console.WriteLine();
        }
    }

    static void Scrape(int pageNumber, List<string> threadResult)
    {
        string url = $"https://www.myh.se/om-oss/sok-handlingar-i-vart-diarium?katalog=Tillsynsbeslut%20yrkesh%C3%B6gskoleutbildning&p={pageNumber}";
        if (pageNumber == 1)
        {
            url = $"https://www.myh.se/om-oss/sok-handlingar-i-vart-diarium?katalog=Tillsynsbeslut%20yrkesh%C3%B6gskoleutbildning";
        }

        try
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            using (var driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl(url);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                Thread.Sleep(1000);

                var htmlContent = driver.PageSource;
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                var listItemNodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'v-list-item') and contains(@class, 'v-list-item--link')]");
                if (listItemNodes != null)
                {
                    foreach (var listItem in listItemNodes)
                    {
                        var result = "";

                        var diaryNumberNode = listItem.SelectSingleNode(".//div[contains(@class, 'v-list-item__subtitle') and contains(@class, 'letter-space-2')]");
                        if (diaryNumberNode != null)
                        {
                            result += "Diarienummer: " + diaryNumberNode.InnerText.Trim() + "\n";
                        }

                        var reviewNode = listItem.SelectSingleNode(".//div[contains(@class, 'v-list-item__title') and contains(@class, 'myh-h3')]");
                        if (reviewNode != null)
                        {
                            result += "Granskning: " + reviewNode.InnerText.Trim() + "\n";
                        }

                        var actorNode = listItem.SelectSingleNode(".//span[contains(@class, 'v-card') and contains(@class, 'text--primary') and contains(@class, 'myh-body-2')]");
                        if (actorNode != null)
                        {
                            result += "Aktör: " + actorNode.InnerText.Trim() + "\n";
                        }

                        lock (lockObj) 
                        {
                            threadResult.Add(result); 
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            lock (lockObj)
            {
                threadResult.Add("Ett fel inträffade: " + ex.Message);
            }
        }
    }
}

