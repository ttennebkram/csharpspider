using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spider;

namespace New_Spider {

    class SpiderConsoleApp {

        static void Main(string[] args) {

            Spider.Spider s = new Spider.Spider("http://www.ideaeng.com", "http://www.ideaeng.com/", 500, 20);
            s.spider();
            System.Console.WriteLine("BACK FROM CALLING spider()");

            List<SpiderPage> results = null;
            do {
                results = s.getResults();
            } while (results == null);

            for (int i = 0; i < results.Count; i++) {
                SpiderPage curr = results.ElementAt(i);
                List<string> curr_aliases = curr.getAliasUrls();
                List<SpiderLink> curr_links = curr.getLinkingToLinks();
                List<SpiderLink> curr_refs = curr.getReferredByLinks();

                System.Console.WriteLine("--------------------------------------------------------------------");
                System.Console.WriteLine("REAL_PAGE - " + curr.getUrl());
                System.Console.WriteLine("--------------------------------------------------------------------");
                if (curr_aliases.Count > 0) {
                    System.Console.WriteLine("\t" + curr.getUrl() + " has these aliases (non-normalized):");
                    for (int j = 0; j < curr_aliases.Count; j++) {
                        System.Console.WriteLine("\t\t" + curr_aliases.ElementAt(j));
                    }
                }
                else {
                    System.Console.WriteLine("\t0 aliases.");
                }

                System.Console.WriteLine("\t------------------------------------------------------------------------------");
                if (curr_links.Count > 0) {
                    System.Console.WriteLine("\t" + curr_links.Count + " linked-to page(s):");
                    for (int k = 0; k < curr_links.Count; k++) {
                        string curr_link_url = curr_links.ElementAt(k).getNormalizedUrl();
                        if (!curr_links.ElementAt(k).isLegalLink()) {
                            curr_link_url = "< illegal link not followed >";
                        }
                        System.Console.WriteLine("\t\t" + curr_link_url);
                        System.Console.WriteLine("\t\t\t original href text: " + curr_links.ElementAt(k).getOriginalUrl());
                    }
                }
                else {
                    System.Console.WriteLine("\t0 pages linked-to.");
                }

                System.Console.WriteLine("\t------------------------------------------------------------------------------");
                System.Console.WriteLine("\t" + curr_refs.Count + " referred-by page(s):");
                for (int q = 0; q < curr_refs.Count; q++) {
                    System.Console.WriteLine("\t\t" + curr_refs.ElementAt(q).getReferringUrl());
                    System.Console.WriteLine("\t\t\t original href text: " + curr_refs.ElementAt(q).getOriginalUrl());
                }
            }
        }
    }
}
