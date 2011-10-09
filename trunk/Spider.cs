using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using System.IO;
using System.Threading;
using System.ComponentModel;

using HtmlAgilityPack;

namespace Spider {

    public class Spider {

        string baseUrl;
        string startUrl;
        int niceness;
        int thread_count;

        List<SpiderPage> masterResults;

        List<SpiderStatus> status;

        List<int[]> _thread_status;

        /* 	Spider() -		creates a new Spider object, which encapsulates the process of 
         *                  spidering a site.
         *  @baseUrl -      the minimum url, e.g. http://www.google.com for google
         *  @startUrl -     the first page on the site to spider, e.g. /index.html or http://www.site.com/index.html
         *  @niceness -     time in ms to wait between http requests
		 *	@thread_count -	the maximum number of threads we can have running simultaneously while spidering
         */
        public Spider(string baseUrl, string startUrl, int niceness, int thread_count) {
            this.baseUrl = baseUrl;
            this.startUrl = startUrl;
            this.niceness = niceness;
            this.thread_count = thread_count;

            this.masterResults = new List<SpiderPage>();
            this._thread_status = new List<int[]>();
        }

        /* Spider() -       creates a new spider object which outputs its status to something other
         *                  than the console.
         *  @status -       a list to write status message objects to; SpiderStatus implements INotifyPropertyChanged
         *                  so that the GUI app can legally access the UI thread with status update messages
         */
        public Spider(string baseUrl, string startUrl, int niceness, int thread_count, List<SpiderStatus> status) {
            this.baseUrl = baseUrl;
            this.startUrl = startUrl;
            this.niceness = niceness;
            this.thread_count = thread_count;

            this.masterResults = new List<SpiderPage>();
            this._thread_status = new List<int[]>();

            this.status = status;
        }

        /*	getBaseUrl() -	returns this Spider instances's base URL
        */
        public string getBaseUrl() {
            return this.baseUrl;
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

            /* using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\esayer\test_output.txt", true)) {
                file.WriteLine(msg);
            }*/
        }

        /* getResults() -   return the results of spider(), or null if they're not ready.
         */
        public List<SpiderPage> getResults() {
			// sleep here to make sure getResults() can't be called too early after spider(), and also
			// just so that a while-loop calling getResults() doesn't check so often
            Thread.Sleep(5000);
            for (int i = 0; i < this._thread_status.Count; i++) {
                if (this._thread_status.ElementAt(i)[1] != 0) {
                    return null;
                }
            }
            return this.masterResults;
        }

        /* spider() -       Actually begin spidering- set things up and do the actual work by calling spiderHelper()
         */
        public void spider() {

            this.writeStatus("baseUrl = " + this.baseUrl);
            this.writeStatus("startUrl = " + this.startUrl);
            this.writeStatus("niceness = " + this.niceness);
            this.writeStatus("spider() - starting crawl...");

            ThreadPool.SetMaxThreads(this.thread_count, this.thread_count);
            List<SpiderPage> startLinks = getLinks(new SpiderPage(this.startUrl, this.startUrl), this);

			// breaking up the links on the start page into 5-links-per-thread sets right now, for testing purposes
            int i = 0;
            while (i < startLinks.Count) {
                List<SpiderPage> next_links = new List<SpiderPage>();
                for (int k = i; k < i + 5; k++) {
                    if (k == startLinks.Count) {
                        break;
                    }
                    next_links.Add(startLinks.ElementAt(k));
                }

                ThreadPool.QueueUserWorkItem(new WaitCallback(spiderHelper), new SpiderHelperWrapper(this, next_links));
                i += 5;
            }
        }

        /* spiderHelper -   Actual recursive method for spidering a site, not to be called explicitly
         *                  other than from within spider().
         *  @args -        	A SpiderWrapperHelper object that will be cast back from a normal object.  We
		 *					have to take normal objects as input because QueueUserWorkItem requires a single 
		 *					generic object to give its worker delegate (spideHelper) as an argument.
         */
        static void spiderHelper(Object args) {

            SpiderHelperWrapper wrapper = (SpiderHelperWrapper) args;

            Spider spider_obj = wrapper.getSpiderObject();
            List<SpiderPage> pages = wrapper.getNewPages();

			// check this thread into _thread_status, a list of int[]s, where [0] is the thread ID and [1] is
			// the status- 0 for not working and 1 for working.  thread_index is used later to change this
			// thread id's status back to not working when it's done
            int thread_index = 0;
            bool thread_found = false;
            for (int i = 0; i < spider_obj._thread_status.Count; i++) {    
                if (spider_obj._thread_status.ElementAt(i)[0] == Thread.CurrentThread.ManagedThreadId) {
		            thread_found = true;
		            thread_index = i;
                    spider_obj._thread_status.ElementAt(thread_index)[1] = 1;
					break;
                }
            }
			// need to make a new entry for this thread id in _thread_status...
            if (!thread_found) {
				// lock the thread when performing an operation that depends on _thread_status.Count, using
				// a local lock object
                Object lock_obj = new Object();
                lock (lock_obj) {
                    spider_obj._thread_status.Add(new int[] { Thread.CurrentThread.ManagedThreadId, 1 });
                    thread_index = spider_obj._thread_status.Count - 1;
                }
            }

            if (pages.Count > 0) {
                //all the new links we find to call spiderHelper() with next time
                List<SpiderPage> n_pages = new List<SpiderPage>();

                spider_obj.writeStatus("thread id: " + Thread.CurrentThread.ManagedThreadId + ", spiderHelper() - links found in this iteration: " + pages.Count);

                for (int i = 0; i < pages.Count; i++) {
                    bool found = false;
                    int new_page_found_index = 0;
                    SpiderPage curr_page = pages.ElementAt(i);

					// lock on a local object; masterResults-sensitive code!
                    Object lock_obj = new Object();
                    lock (lock_obj) {
                        for (int j = 0; j < spider_obj.masterResults.Count; j++) {
                            // check to see if curr_page is already in masterResults
                            if (curr_page.getUrl() == spider_obj.masterResults.ElementAt(j).getUrl()) {
                                found = true;
                                // if curr_page has already been visited, just add all the referring URLs from this curr_page
                                // object to masterResults
                                List<string> curr_page_ref_urls = curr_page.getReferencedByUrls();
                                for (int g = 0; g < curr_page_ref_urls.Count; g++) {
                                    if (!spider_obj.masterResults.ElementAt(j).getReferencedByUrls().Contains(curr_page_ref_urls.ElementAt(g))) {
                                        spider_obj.masterResults.ElementAt(j).addReferencedByUrl(curr_page_ref_urls.ElementAt(g));
                                    }
                                }
                                break;
                            }
                        }
                        // if this is a new page...
                        if (!found) {
                            spider_obj.masterResults.Add(curr_page);
                            new_page_found_index = spider_obj.masterResults.Count - 1;
                        }
                    }
                    // if this is a new page (outside thread lock now)...
                    if (!found) {
                        List<SpiderPage> temp_n_pages = getLinks(spider_obj.masterResults.ElementAt(new_page_found_index), spider_obj);
                        for (int k = 0; k < temp_n_pages.Count; k++) {
                            // add all the links on this page to its linking pages list
                            spider_obj.masterResults.ElementAt(new_page_found_index).addLinkingToUrl(temp_n_pages.ElementAt(k).getUrl());
                        }
                        // add all the links on this page to the list of links to be spidered in the next pass
                        n_pages.AddRange(temp_n_pages);
                    }
                }
				// put a new work item into the ThreadPool queue; multi-threaded recursion here since this new work item 
				// will call spiderHelper() with all the new links found in this iteration
                ThreadPool.QueueUserWorkItem(new WaitCallback(spiderHelper), new SpiderHelperWrapper(spider_obj, n_pages));
            }
			// set this thread id's status back to not working in _thread_status
            spider_obj._thread_status.ElementAt(thread_index)[1] = 0;
        }

        /* getLinks() 	-	find all the links on a given page
         *  @startp 	-	the page to be scanned for links, represented as an SpiderPage object (which has a referring 
         *             		page)
         *  @s			- 	the Spider object in use
         */
        public static List<SpiderPage> getLinks(SpiderPage startp, Spider s) {
            List<string> pre_pages = new List<string>();
            List<SpiderPage> new_pages = new List<SpiderPage>();

            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(startp.getUrl());
            //req.Timeout = 1000;

            HttpWebResponse resp = null;
            try {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (Exception e) {
                s.writeStatus("ERROR: " + e.Message);
                s.writeStatus("\tpage - " + startp.getUrl() + "\n\t\treferred to by:");

                List<string> curr_refs = startp.getReferencedByUrls();
                for (int i = 0; i < curr_refs.Count; i++) {
                    s.writeStatus("\t\t\t" + curr_refs.ElementAt(i));
                }
            }

            if (resp != null) {
                Stream resp_stream = resp.GetResponseStream();
                string temp_string = null;
                int count = 0;
                do {
                    count = resp_stream.Read(buf, 0, buf.Length);
                    if (count != 0) {
                        temp_string = Encoding.ASCII.GetString(buf, 0, count);
                        sb.Append(temp_string);
                    }
                }
                while (count > 0);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(sb.ToString());
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
                            pre_pages.Add(s.getBaseUrl() + link.Url);
                        }
                        else {
                            pre_pages.Add(s.getBaseUrl() + link.Url + "/");
                        }
                    }
                };

                List<string> distinct_pre_pages = pre_pages.Distinct().ToList();
                for (int m = 0; m < distinct_pre_pages.Count; m++) {
                    new_pages.Add(new SpiderPage(distinct_pre_pages.ElementAt(m), startp.getUrl()));
                }
            }

            return new_pages;
        }
    }
}