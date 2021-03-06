﻿using System;
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
		List<SpiderPage> _candidate_pages;

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
			this._candidate_pages = new List<SpiderPage>();
			
			ThreadPool.SetMaxThreads(this.thread_count, this.thread_count);
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
			this._candidate_pages = new List<SpiderPage>();
			
			ThreadPool.SetMaxThreads(this.thread_count, this.thread_count);

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

        /* spider() -       Actually begin spidering- set things up and do the actual work by calling spiderProcess()
         */
        public void spider() {

            this.writeStatus("baseUrl = " + this.baseUrl);
            this.writeStatus("startUrl = " + this.startUrl);
            this.writeStatus("niceness = " + this.niceness);
            this.writeStatus("spider() - starting crawl...");

            this._candidate_pages.Add(new SpiderPage(this.startUrl, this.startUrl));

			this._thread_status.Add(new int[]{ (thread_count * thread_count), 0 });
			this.spiderProcess();
        }

		void spiderProcess() {
		
			_thread_status.ElementAt(0)[1] = 1;

            lock (this) {

                int candidate_pages_count = this._candidate_pages.Count;
                SpiderPage current_candidate_page = null;
                SpiderPage current_master_page = null;
                
                for (int i = 0; i < candidate_pages_count; i++) {
                    bool found = false;
                    current_candidate_page = this._candidate_pages.ElementAt(i);

                    if (current_candidate_page.finalUrlNeeded()) {
                        for (int m = 0; m < this.masterResults.Count; m++) {
                            current_master_page = this.masterResults.ElementAt(m);
                            if (!current_master_page.finalUrlNeeded()
                                && current_candidate_page.getUrl() == current_master_page.getFinalUrl()) {
                                
                                found = true;
                                List<string> current_candidate_page_ref_urls = current_candidate_page.getReferencedByUrls();
                                for (int g = 0; g < current_candidate_page_ref_urls.Count; g++) {
                                    if (!current_master_page.getReferencedByUrls().Contains(current_candidate_page_ref_urls.ElementAt(g))) {
                                        this.masterResults.ElementAt(j).addReferencedByUrl(current_candidate_page_ref_urls.ElementAt(g));
                                    }
                                }
                                    


                    for (int j = 0; j < this.masterResults.Count; j++) {
                        current_master_page = this.masterResults.ElementAt(j);

                        if (current_master_page.finalUrlNeeded()) {
                            if (current_candidate_page.getUrl() == current_master_page.getUrl()) {


                        }


                        if (current_candidate_page.getFinalUrl() == this.masterResults.ElementAt(j).getFinalUrl()) {
                            found = true;
							// add an alias entry in the masterResults if we were redirected here
							if (current_candidate_page.getUrl() != current_candidate_page.getFinalUrl()) {
								this.masterResults.ElementAt(j).addAliasUrl(current_candidate_page.getUrl());
							}
                            // add all the linking URLs from the curr_page object to masterResults
                            List<string> current_page_link_urls = current_candidate_page.getLinkingToUrls();
                            for (int q = 0; q < current_page_link_urls.Count; q++) {
                                if (!this.masterResults.ElementAt(j).getLinkingToUrls().Contains(current_page_link_urls.ElementAt(q))) {
                                    this.masterResults.ElementAt(j).addLinkingToUrl(current_page_link_urls.ElementAt(q));
                                }
                            }
                            // add all the referring URLs from the curr_page object to masterResults
                            List<string> current_page_ref_urls = current_candidate_page.getReferencedByUrls();
                            for (int g = 0; g < current_page_ref_urls.Count; g++) {
                                if (!this.masterResults.ElementAt(j).getReferencedByUrls().Contains(current_page_ref_urls.ElementAt(g))) {
                                    this.masterResults.ElementAt(j).addReferencedByUrl(current_page_ref_urls.ElementAt(g));
                                }
                            }
                            break;
                        }
                    }
                    // if this is a new page...
                    if (!found) {
                        this.masterResults.Add(current_candidate_page);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(spiderFetch), new _SpiderDataWrapper_spiderFetch(this, current_candidate_page));
                    }
                }
                // remove all the candidate pages we've just processed
                _candidate_pages.RemoveRange(0, candidate_pages_count);
            }

            Thread.Sleep(30000);
            if (this._candidate_pages.Count > 0) {
                this.spiderProcess();
            }
            this._thread_status.ElementAt(0)[1] = 0;
		}

        /* spiderFetch -   	ThreadPool QueueUserWorkItem method, gets the links on a page and adds them to the 
		 *					candidates list.
         *  @args -        	A _spiderFetch_SpiderDataWrapper object that will be cast back from a normal object.  We
		 *					have to take normal objects as input because = requires a single 
		 *					generic object to give its worker delegate (spiderFetch) as an argument.
         */
        static void spiderFetch(Object args) {

            _SpiderDataWrapper_spiderFetch wrapper = (_SpiderDataWrapper_spiderFetch) args;

            Spider spider_obj = wrapper.getSpiderObject();
            SpiderPage current_page = wrapper.getNewPage();

			// check this thread into _thread_status, a list of int[]s, where [0] is the thread ID and [1] is
			// the status- 0 for not working and 1 for working.  thread_index is used later to change this
			// thread id's status back to not working when it's done
            int thread_index = 0;
            bool thread_found = false;
            for (int i = 0; i < spider_obj._thread_status.Count; i++) {
                if (spider_obj._thread_status.ElementAt(i)[0] == Thread.CurrentThread.ManagedThreadId) {
	                spider_obj._thread_status.ElementAt(i)[1] = 1;
		            thread_index = i;
		            thread_found = true;
					break;
                }
            }
			// need to make a new entry for this thread id in _thread_status...
            if (!thread_found) {
				// lock the thread when performing an operation that depends on _thread_status.Count, using
				// a local lock object
                Object lock_obj = new Object();
                lock (lock_obj) {
                    spider_obj._thread_status.Add(new int[]{ Thread.CurrentThread.ManagedThreadId, 1 });
                    thread_index = spider_obj._thread_status.Count - 1;
                }
            }

            spider_obj.writeStatus("thread id: " + Thread.CurrentThread.ManagedThreadId + ", spiderFetch(): fetching " + current_page.getUrl());
			
			_SpiderDataWrapper_getLinks gl_wrapper = getLinks(current_page, spider_obj);
			string current_page_final_url = gl_wrapper.getFinalUrl();
			List<SpiderPage> current_page_links = gl_wrapper.getNewLinks();
						
			List<string> current_page_link_strings = new List<string>();
			for (int q = 0; q < current_page_links.Count; q++) {
                SpiderPage qth_page = current_page_links.ElementAt(q);
				spider_obj._candidate_pages.Add(qth_page);
				current_page_link_strings.Add(qth_page.getUrl());
			}
			spider_obj._candidate_pages.Add(new SpiderPage(current_page.getUrl(), current_page_final_url, current_page.getReferencedByUrls(), current_page_link_strings));

			// set this thread id's status back to not working in _thread_status
            spider_obj._thread_status.ElementAt(thread_index)[1] = 0;
        }

        /* getLinks() 	-	find all the links on a given page
         *  @startp 	-	the page to be scanned for links, represented as an SpiderPage object (which has a referring 
         *             		page)
         *  @s			- 	the Spider object in use
         */
       	static _SpiderDataWrapper_getLinks getLinks(SpiderPage startp, Spider s) {
            List<string> pre_pages = new List<string>();

			string final_url = "";
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
				// record the final Url after any redirects from this link
                final_url = resp.ResponseUri.AbsoluteUri;

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

            return new _SpiderDataWrapper_getLinks(final_url, new_pages);
        }
    }

	public class _SpiderDataWrapper_spiderFetch {

		Spider spider_obj;
		SpiderPage new_page;

		public _SpiderDataWrapper_spiderFetch(Spider spider_obj, SpiderPage new_page) {
			this.spider_obj = spider_obj;
			this.new_page = new_page;
		}

		public Spider getSpiderObject() {
			return this.spider_obj;
		}

		public SpiderPage getNewPage() {
			return this.new_page;
		}
	}
	
	public class _SpiderDataWrapper_getLinks {
		
		string final_url;
		List<SpiderPage> new_links;
		
		public _SpiderDataWrapper_getLinks(string final_url, List<SpiderPage> new_links) {
			this.final_url = final_url;
			this.new_links = new_links;
		}
		
		public string getFinalUrl() {
			return this.final_url;
		}
		
		public List<SpiderPage> getNewLinks() {
			return this.new_links;
		}
	}
}