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

        bool results_available;

        List<SpiderStatus> status;

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

            this.results_available = false;
        }

        /* Spider() -       creates a new spider object which outputs its status to something other
         *                  than the console.
         *  @status -       a list to write status message objects to; SpiderStatus implements INotifyPropertyChanged
         *                  so that the GUI app can legally access the UI thread with status update messages
         */
        public Spider(string baseUrl, string startUrl, int niceness, int thread_count, List<SpiderStatus> status) {
          	: this(baseUrl, startUrl, niceness, thread_count);

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
            if (results_available) {
                return this.masterResults;
            }
            else {
                return null;
            }
        }

        /* spider() -       Actually begin spidering- set things up and do the actual work by calling spiderHelper()
         */
        public void spider() {
            this.results_available = false;

            this.writeStatus("baseUrl = " + this.baseUrl);
            this.writeStatus("startUrl = " + this.startUrl);
            this.writeStatus("niceness = " + this.niceness);
            this.writeStatus("spider() - starting crawl...");

            ThreadPool.SetMaxThreads(this.n_theads);
			List<SpiderPage> startLinks = getLinks(new SpiderPage(this.startUrl, this.startUrl), this.getBaseUrl());
			
			int i = 0;
            while (i < startLinks.Count) {
                List<SpiderPage> next_links = new List<SpiderPage>();
                for (int k = i; k < i + 5; k++) {
                    if (k == startLinks.Count) {
                     	break;
                    }
                    next_links.Add(startLinks.ElementAt(k));
                }

				ThreadPool.QueueUserWorkItem(new WaitCallBack(spiderHelper), new SpiderHelperWrapper(this, next_links));
                i += 5;
            }

			this.results_available = true;
        }

        /* spiderHelper -   Actual recursive method for spidering a site, not to be called explicitly
         *                  other than from within spider().
         *  @pages -        A list of SpiderPage objects representing the pages to scan on this pass.
         */
        static void spiderHelper(Object args) {

			SpiderHelperWrapper wrapper = (SpiderHelperWrapper) args;
			Spider spider_obj = wrapper.getSpiderObject();
			List<SpiderPages> pages = wrapper.getNewPages();

		  	//all the new links we find to call spiderHelper() with next time
		   	List<SpiderPage> n_pages = new List<SpiderPage>();

		  	this.writeStatus("spiderHelper() - links found in this iteration: " + pages.Count);
		
		   	for (int i = 0; i < pages.Count; i++) {
		    	bool found = false;
		    	int new_page_found_index = 0;
		    	SpiderPage curr_page = pages.ElementAt(i);
		
				private Object lock_obj = new Object();
		    	lock (lock_obj) {
		        	for (int j = 0; j < spider_obj.masterResults.Count; j++) {
		            	// check to see if curr_page is already in the master results
		            	if (curr_page.getUrl() == spider_obj.masterResults.ElementAt(j).getUrl()) {
		                	found = true;
		                	// if curr_page has already been visited, just add all the referring URLs from this curr_page
		                	// object to the master results
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
		        	List<SpiderPage> temp_n_pages = getLinks(spider_obj.masterResults.ElementAt(new_page_found_index), spider_obj.getBaseUrl());
		        	for (int k = 0; k < temp_n_pages.Count; k++) {
		            	// add all the links on this page to its linking pages list
		            	spider_obj.masterResults.ElementAt(new_page_found_index).addLinkingToUrl(temp_n_pages.ElementAt(k).getUrl());
		        	}

		        	// add all the links on this page to the list of links to be spidered in the next pass
		        	n_pages.AddRange(temp_n_pages);
		    	}
			}
			
			ThreadPool.QueueUserWorkItem(new WaitCallBack(spiderHelper), new SpiderHelperWrapper(spider_obj, n_pages));
		}
		
        /* getLinks() 	-	find all the links on a given page
         *  @startp 	-	the page to be scanned for links, represented as an SpiderPage object (which has a referring 
         *             		page)
         *  @baseurl	- 	the base URL
         */
        public static List<SpiderPage> getLinks(SpiderPage startp, string base_url) {
            List<string> pre_pages = new List<string>();
            List<SpiderPage> new_pages = new List<SpiderPage>();

            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];

			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(startp.getUrl());
          	//req.Timeout = 1000;

           	HttpWebResponse resp = null;
         	try {
           		resp = (HttpWebResponse) req.GetResponse();
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
                            pre_pages.Add(base_url + link.Url);
                        }
                        else {
                            pre_pages.Add(base_url + link.Url + "/");
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

	private class SpiderHelperWrapper {
		
		Spider spider_obj;
		List<SpiderPage> new_pages;
		
		public SpiderHelperWrapper(Spider spider_obj, List<SpiderPage> new_pages) {
			this.spider_obj = spider_obj;
			this.new_pages = new_pages;
		}
		
		Spider getSpiderObj() {
			return this.spider_obj;
		}
		
		List<SpiderPage> getNewPages() {
			return this.new_pages;
		}
	}
}