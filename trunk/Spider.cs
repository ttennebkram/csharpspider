using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using HtmlAgilityPack;

namespace Spider {

    public class Spider {

        string baseUrl;
        string startUrl;
        int niceness;
        List<SPage> masterResults;

        bool results_available;

        List<SpiderStatus> status;

        /* Spider() -       creates a new Spider object, which encapsulates the process of 
         *                  spidering a site.
         *  @baseUrl -      the minimum url, e.g. http://www.google.com for google
         *  @startUrl -     the first page on the site to spider, e.g. /index.html or http://www.site.com/index.html
         *  @niceness -     time in ms to wait between http requests
         */
        public Spider(string baseUrl, string startUrl, int niceness) {
            this.baseUrl = baseUrl;
            this.startUrl = startUrl;
            this.niceness = niceness;
            this.masterResults = new List<SPage>();

            this.results_available = false;
        }

        /* Spider() -       creates a new spider object which outputs its status to something other
         *                  than the console.
         *  @status -       a list to write status message objects to; SpiderStatus implements INotifyPropertyChanged
         *                  so that the GUI app can legally access the UI thread with status update messages
         */
        public Spider(string baseUrl, string startUrl, int niceness, List<SpiderStatus> status) {
            this.baseUrl = baseUrl;
            this.startUrl = startUrl;
            this.niceness = niceness;
            this.masterResults = new List<SPage>();

            this.results_available = false;

            this.status = status;
        }

        /* writeStatus() -  output a new status message
         *  @msg -          the message to be output 
         */
        public void writeStatus(string msg) {
            if (this.status != null) {
                this.status.Add(new SpiderStatus(msg));
            }
            else {
                System.Console.WriteLine(msg);
            }
        }

        /* getResults() -   return the results of spider(), or null if they're not ready.
         */
        public List<SPage> getResults() {
            if (results_available) {
                return this.masterResults;
            }
            else {
                return null;
            }
        }

        /* spider() -       Actually begin spidering; getResults() will return null as implemented
         *                  currently until this is done.
         */
        public void spider() {
            this.results_available = false;

            this.writeStatus("baseUrl = " + this.baseUrl);
            this.writeStatus("startUrl = " + this.startUrl);
            this.writeStatus("niceness = " + this.niceness);
            this.writeStatus("spider() - starting crawl...");

            List<SPage> startLinks = getLinks(new SPage(this.startUrl, this.startUrl), this.baseUrl, this);
            this.spiderHelper(startLinks);

            this.results_available = true;
        }

        /* spiderHelper -   Actual recursive method for spidering a site, not to be called explicitly
         *                  other than from within spider().
         *  @pages -        A list of SPage objects representing the pages to scan on this pass.
         */
        public void spiderHelper(List<SPage> pages) {

            // basecase - we'll eventually call spiderHelper() with an emptly list because
            // all the pages from the last pass had been found already
            if (pages.Count > 0) {
                //all the new links we find to call spiderHelper() with next time
                List<SPage> n_pages = new List<SPage>();


                this.writeStatus("spiderHelper() - links found in this iteration: " + pages.Count);
                for (int i = 0; i < pages.Count; i++) {
                    bool found = false;
                    SPage curr_page = pages.ElementAt(i);
                    for (int j = 0; j < this.masterResults.Count; j++) {
                        // check to see if curr_page is already in the master results
                        if (curr_page.getUrl() == this.masterResults.ElementAt(j).getUrl()) {
                            found = true;
                            // if curr_page has already been visited, just add all the referring URLs from this curr_page
                            // object to the master results
                            List<string> curr_page_ref_urls = curr_page.getReferencedByUrls();
                            for (int g = 0; g < curr_page_ref_urls.Count; g++) {
                                if (!this.masterResults.ElementAt(j).getReferencedByUrls().Contains(curr_page_ref_urls.ElementAt(g))) {
                                    this.masterResults.ElementAt(j).addReferencedByUrl(curr_page_ref_urls.ElementAt(g));
                                }
                            }
                        }
                    }

                    // if this is a new page...
                    if (!found) {
                        List<SPage> temp_n_pages = getLinks(curr_page, this.baseUrl, this);
                        for (int k = 0; k < temp_n_pages.Count; k++) {
                            // add a all the links on this page to its linking pages list
                            curr_page.addLinkingToUrl(temp_n_pages.ElementAt(k).getUrl());
                        }

                        // add this page to the master results
                        this.masterResults.Add(curr_page);
                        // add all the links on this page to the list of links to be spidered in the next pass
                        n_pages.AddRange(temp_n_pages);
                    }
                }

                this.spiderHelper(n_pages);
            }
        }

        /* getLinks() -     find all the links on a given page
         *  @startp -       the page to be scanned for links, represented as an SPage object (which has a referring 
         *                  page)
         *  @basep -        the base URL for this spider object
         */
        public static List<SPage> getLinks(SPage startp, string basep, Spider s) {
            List<string> pre_pages = new List<string>();

            s.writeStatus("here 1");
            HtmlWeb webGet = new HtmlWeb();
            s.writeStatus("here 1.5");
            HtmlDocument doc = null;
            lock (webGet) {
                try {
                    doc = webGet.Load(startp.getUrl());
                    s.writeStatus("here 2");
                }
                catch (WebException e) {
                    s.writeStatus("here error!");
                    s.writeStatus(e.Message);
                }
            }
            
            var linksOnPage = from lnks in doc.DocumentNode.Descendants()
                              where lnks.Name == "a" &&
                                    lnks.Attributes["href"] != null &&
                                    lnks.InnerText.Trim().Length > 0
                              select new {
                                  Url = lnks.Attributes["href"].Value,
                              };

            foreach (var link in linksOnPage) {
                if (link.Url.StartsWith("/")) {
                    if (link.Url.EndsWith("/")) {
                        pre_pages.Add(basep + link.Url);
                    }
                    else {
                        pre_pages.Add(basep + link.Url + "/");
                    }
                }
            };

            List<string> distinct_pre_pages = pre_pages.Distinct().ToList();
            List<SPage> pages = new List<SPage>();
            for (int m = 0; m < distinct_pre_pages.Count; m++) {
                pages.Add(new SPage(distinct_pre_pages.ElementAt(m), startp.getUrl()));
            }

            return pages;
        }

        static void Main(string[] args) {
            string baseUrl = "http://www.ideaeng.com";
            string startUrl = "http://www.ideaeng.com/";

            Spider s = new Spider(baseUrl, startUrl, 500);
            s.spider();

            List<SPage> pages = s.getResults();

            for (int i = 0; i < pages.Count; i++) {
                System.Console.WriteLine(pages.ElementAt(i).getUrl());
                for (int j = 0; j < pages.ElementAt(i).getReferencedByUrls().Count; j++) {
                    s.writeStatus("\t referenced at - " + pages.ElementAt(i).getReferencedByUrls().ElementAt(j));
                }
                s.writeStatus("\t-------------------------------------------------------\n");
                for (int k = 0; k < pages.ElementAt(i).getLinkingToUrls().Count; k++) {
                    s.writeStatus("\t links to - " + pages.ElementAt(i).getLinkingToUrls().ElementAt(k));
                }
            }
        }
    }
}
