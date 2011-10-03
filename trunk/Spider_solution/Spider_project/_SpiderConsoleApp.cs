using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

using Spider;

namespace SpiderConsoleApp {

    class SpiderConsoleApp {

        static void Main(string[] args) {
            string startUrl = "http://www.ideaeng.com";
            string baseUrl = "http://www.ideaeng.com";
            Spider.Spider s = new Spider.Spider(startUrl, baseUrl, 500);
            
            s.spider();

            Thread.Sleep(120000);
            List<SpiderPage> results = s.getResults();

            for (int i = 0; i < results.Count; i++) {
                SpiderPage curr = results.ElementAt(i);
                List<string> curr_links = curr.getLinkingToUrls();
                List<string> curr_refs = curr.getReferencedByUrls();

                System.Console.WriteLine("\t" + curr.getUrl() + " links to " + curr_links.Count + " page(s):");
                for (int k = 0; k < curr_links.Count; k++) {
                    System.Console.WriteLine("\t\t" + curr_links.ElementAt(k));
                }

                System.Console.WriteLine("\t" + curr.getUrl() + " is referred to by " + curr_refs.Count + " page(s):");
                for (int g = 0; g < curr_refs.Count; g++) {
                    System.Console.WriteLine("\t\t" + curr_refs.ElementAt(g));
                }

                System.Console.WriteLine("------------------------------------------------------------------------------------");
            }
        }
    }
}
