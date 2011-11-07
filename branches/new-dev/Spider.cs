using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using System.IO;
using System.Threading;

using HtmlAgilityPack;

namespace Spider {
	
	class Spider {
		
		string rootUrl;
        string startUrl;
        int niceness;
        int thread_count;

        Object _fetch_lock;
		List<int> _thread_status;
		
		List<SpiderPage> _master_results;
		List<_SpiderPageCandidate> _candidate_pages;
		
		List<SpiderStatus> status_messages;
		
        /*  Spider()            - creates a new Spider object
		 *
         *      @rootUrl        - the root URL of the site to be spidered
         *      @startUrl       - the initial URL of the site to be spidered
         *      @niceness       - time to wait in between HTTP requests (not currently respected, for testing)
         *      @thread_count   - the max number of work-item threads to spawn
         */
		public Spider(string rootUrl, string startUrl, int niceness, int thread_count) {
            // get rid of a trailing "/" on the root URL
            if (rootUrl.EndsWith("/")) {
                this.rootUrl = rootUrl.Substring(0, rootUrl.Count() - 1);
            }
            else {
                this.rootUrl = rootUrl;
            }

	      	this.startUrl = startUrl;
	       	this.niceness = niceness;
	      	this.thread_count = thread_count;

            this._fetch_lock = new Object();

            this._thread_status = new List<int>();
	      	this._master_results = new List<SpiderPage>();
	
			this._candidate_pages = new List<_SpiderPageCandidate>();

			ThreadPool.SetMaxThreads(this.thread_count, this.thread_count);
		}
		
		/*	Spider()				- creates a new Spider object *with* a status message queue
		 *		
		 *		@rootUrl ...		- same as above
		 *		@status_messages	- list of SpiderStatus objects to use for status messages
		 */
		public Spider(string rootUrl, string startUrl, int niceness, int thread_count, List<SpiderStatus> status_messages) 
            : this(rootUrl, startUrl, niceness, thread_count) {
			
			this.status_messages = status_messages;
		}
		
        /*  spider()            - public-facing method for actually beginning to spider with
         *                        this spider object
         */
		public void spider() {
			this.writeStatus("rootUrl = " + this.rootUrl);
            this.writeStatus("startUrl = " + this.startUrl);
            this.writeStatus("niceness = " + this.niceness);
            this.writeStatus("spider() - starting crawl...");

            // make the very first _SpiderPageCandidate out of this.startUrl
            _SpiderPageCandidate spc = new _SpiderPageCandidate(new SpiderLink(this.startUrl, 
                                                                    this.normalizeUrl(this.startUrl, ""), this.startUrl));
            // add the first _SpiderPageCandidate to the _candidate_pages queue, make a thread to process it, and
            // also make a thread to run spiderProcess()
            this._candidate_pages.Add(spc);
            this.addThreadStatus();
            this.addThreadStatus();
            ThreadPool.QueueUserWorkItem(new WaitCallback(fetchPage), new _SpiderWorkItemDataWrapper(this, 0));
            ThreadPool.QueueUserWorkItem(new WaitCallback(spiderProcess), this);
		}

        /*  writeStatus()       - output a message about our status (to use SpiderStatus objects eventually...)
         *      @message        - the message string to output
         */
        public void writeStatus(string message) {
            System.Console.WriteLine(message);
        }

        /*  addNewPage()        - adds a new page to the _master_results, i.e. a new officially vetted page
         *      @new_page       - the SpiderPage object to add
         */
		void addNewPage(SpiderPage new_page) {
			this._master_results.Add(new_page);
		}

        /*  getRootUrl()        - return this Spider object's root URL
         */
        public string getRootUrl() {
            return this.rootUrl;
        }

        /*  getResults()        - if the spidering is done, return the results in a list of SpiderPage objects,
         *                        otherwise return null; sleep for 7 seconds on each call so that a while loop 
         *                        doesn't check too often
         */
        public List<SpiderPage> getResults() {
            Thread.Sleep(7000);
            if (this._thread_status.Contains(1)) {
                return null;
            }
            return this._master_results;
        }

        /*  normalizeUrl()      - fix the given URL by normalization standards to be added later...
         *      @url            - the URL string to normalize
         *      @base_url       - the base URL of the link that this URL comes from
         */
        public string normalizeUrl(string url, string base_url) {
            // remove any whitespace
            url = url.TrimStart(' ');
            url = url.TrimEnd(' ');
            // replace &amp; with '&'
            url = url.Replace("&amp;", "&");
            // ignore anchors...
            if (url.StartsWith("#")) {
                return "";
            }
            // ignore PDFs...
            if (url.EndsWith(".pdf")) {
                return "";
            }
            // trailing "/"???
            if (!url.EndsWith("/")) {
                if (url.LastIndexOf('.') < url.LastIndexOf('/') &&
                    url.LastIndexOf('#') < url.LastIndexOf('/')) {
                    url = url + "/";
                }
            }

            if (url.StartsWith("/")) {
                return this.getRootUrl() + url;
            }
            if (url.StartsWith(this.getRootUrl())) {
                return url;
            }
            else {
                try {
                    /*
                    Uri uri = new Uri(url);
                    if (uri.IsAbsoluteUri) {
                        return "";
                    }*/
                    // return nothing for absolute URLs outside this site
                    if (url.StartsWith("http://") || url.StartsWith("https://")) {
                        return "";
                    }
                    if (url.StartsWith("javascript:") ||
                        url.StartsWith("mailto:") ||
                        url.StartsWith("news:") ||
                        url.StartsWith("ftp:")) {
                        return "";
                    }
                    // relative URL and the base URL is *not* a distinct page, e.g. http://www.site.com/
                    if (base_url.EndsWith("/")) {
                        return base_url + url;
                    }
                   // need to parse out the real base URL, e.g. http://www.site.com/index.html
                    if (base_url.Contains("/")) {
                        string[] parts = base_url.Split('/');
                        string new_base_url = parts[0];
                        // keep all but the last URL segment, as split with "/", to get the base URL
                        for (int i = 1; i < parts.Length - 1; i++) {
                            new_base_url = new_base_url + "/" + parts[i];
                        }
                        return new_base_url + "/" + url;
                    }
                    // lastly the http://www.site.com case, where we just need to add a "/"
                    return base_url + "/" + url;
                }
                catch(UriFormatException e) {
                    this.writeStatus("normalizeUrl(): " + e.Message + "; a link to " + url + " is illegal.\n" + 
                                        "\treferring-page: " + base_url);
                    return "";
                }
            }
        }

        /*  findPageIndex()     - find the integer index of the page in _master_results with the same URL
         *                        as the URL given as input, or -1 if it isn't found
         *      @url            - the URL to search for
         */
		public int findPageIndex(string url) {
            int ret = -1;
            // need to lock here because we're depending on _master_results.Count
            lock (this._master_results) {
                for (int i = 0; i < this._master_results.Count; i++) {
                    List<string> search_urls = new List<string>();
                    // put the page's aliases and its own URL in search_urls- the URLs to compare against
                    search_urls.Add(this._master_results.ElementAt(i).getUrl());
                    search_urls.AddRange(this._master_results.ElementAt(i).getAliasUrls());

                    for (int k = 0; k < search_urls.Count; k++) {
                        if (url == search_urls.ElementAt(k)) {
                            // found the page, return its index, i
                            ret = i;
                            break;
                        }
                    }
                }
            }
			return ret;
		}
		
        /*  findCandidatePageIndex()    - find the integer index of a candidate page in _candidate_pages with
         *                                same URL as the URL given as input, or -1 if it isn't found
		 *								  (NOT CURRENTLY BEING USED; esayer 10.23.11)
         *      @url                    - the URL to search for
         */
		public int findCandidatePageIndex (string url) {
            int ret = -1;
            // need to lock here because we're depending on _candidate_pages.Count
            lock (this._candidate_pages) {
                for (int i = 0; i < this._candidate_pages.Count; i++) {
                    if (url == _candidate_pages.ElementAt(i)._candidate_getUrl()) {
                        // we found the page so return its index
                        ret = i;
                        break;
                    }
                }
            }
			return ret;
		}
		
        /*  getPageAtIndex()    - return the page in _master_results at the given index
         *      @index          - integer index of the page we want (generally found with findPageIndex())
         */
		public SpiderPage getPageAtIndex(int index) {
			return this._master_results.ElementAt(index);
		}

        /*  getCandidatePageAtIndex()       - return the page in_candidate_pages at the given index
         *      @index                      - integer index of the page we want (generally found with
         *                                    getCandidatePageAtIndex())
         */
        public _SpiderPageCandidate getCandidatePageAtIndex(int index) {
            return this._candidate_pages.ElementAt(index);
        }

        /*  getLastPageIndex()              - return the index of the last page in this spider object's
         *                                    _master_results list
         */
        public int getLastPageIndex() {
            return this._master_results.Count - 1;
        }

        /*  addThreadStatus()                - create a new entry in _thread_status; run on the creation
         *                                    of a new worker thread
         */
        void addThreadStatus() {
            lock (this._thread_status) {
                this._thread_status.Add(1);
            }
        }

        /*  removeThreadStatus()            - remove an entry from _thread_status; run when a worker thread
         *                                    is finished
         */
        void removeThreadStatus() {
            lock (this._thread_status) {
                this._thread_status.RemoveAt(this._thread_status.FindIndex(1, delegate(int i) { return i == 1; }));
            }
        }

        /*  acquireFetchLock()              - lock the _fetch_lock object and make the current worker
         *                                    thread sleep for the amount specified as this spider's
         *                                    niceness factor
         */
        void acquireFetchLock() {
            if (this.niceness > 0) {
                lock (this._fetch_lock) {
                    Thread.Sleep(this.niceness);
                }
            }
        }

        /*  checkThreads()          - checks to see if the execution of all the worker threads has
         *                            completed
         */
        bool checkWorkerThreads() {
            Thread.Sleep(5000);
            bool ret = true;
            // need to lock so that nobody can change _thread_status while we're checking
            lock (this._thread_status) {
                if (this._thread_status.FindIndex(1, delegate(int i) { return i == 1; }) > 0) {
                    ret = false;
                }
            }
            // output for testing...
            this.writeStatus("\ncheckThreads() - " + ret + "\n");
            return ret;
        }

        /*  spiderProcess()         - master spider process:
         * 
         *                            PART 1:     process the candidate pages that the fetchPage() threads
         *                                        crawled from PART 2 last round, generate a list of new
         *                                        links for PART 2
         *                            PART 2:     make new fetchPage() threads to crawl the new candidate pages
         *                                        found in the links from PART 1
         *
         */
        static void spiderProcess(object o) {

            // cast our argument back to a Spider object
            Spider spider_object = (Spider) o;

            // loop spiderProcess() until we're done processing candidate pages
            do {
                // wait for all the worker threads to be done before starting each round of spiderProcess()
                bool ready = false;
                do {
                    ready = spider_object.checkWorkerThreads();
                } while (!ready);

                // all of this is dependent on _master_pages and _candidate_pages, need the spider object locked
                lock (spider_object) {

                    // PART 1:  process the candidate pages that were crawled by the worker threads created in
                    //          the last round of spiderProcess()
                    
                    // list of all the links found in the candidate pages we process
                    List<SpiderLink> new_links_found = new List<SpiderLink>();
                    // list of all the candidate page URLs that we add to the master results this round
                    List<string[]> added_candidate_urls = new List<string[]>();

                    int candidate_page_count = spider_object._candidate_pages.Count;
                    for (int i = 0; i < candidate_page_count; i++) {
                        bool found = false;
                        _SpiderPageCandidate current_candidate_page = spider_object.getCandidatePageAtIndex(i);

                        // make sure this candidate page was crawled by fetchPage(), should be true for every
                        // candidate page that didn't return a 404 or some error, etc.
                        if (current_candidate_page._candidate_isDone()) {
                            // see if this candidate page went to the same final URL as a page that we've already
                            // added in this round of spiderProcess()
                            int already_added_candidate_index = added_candidate_urls.FindIndex(delegate(string[] s) {
                                return s[0] == current_candidate_page.getUrl();
                            });

                            // two tests of whether this candidate page *could* already be in the master results: 1) if this page's 
                            // final URL is in the already-added-list (then it's certainly in the master results), or 2) it was an 
                            // alias candidate (i.e. a redirect to a different final url); otherwise we're guaranteed that this
                            // candidate page is a new page, and therefore not already in the master results, and all of this
                            // will be skipped
                            if (already_added_candidate_index > -1 || current_candidate_page._candidate_isAliasCandidate()) {
                                int real_page_index = -1;
                                if (already_added_candidate_index > -1) {
                                    real_page_index = Int32.Parse(added_candidate_urls.ElementAt(already_added_candidate_index)[1]);
                                }
                                else {
									spider_object.writeStatus("running findPageIndex()");
                                    real_page_index = spider_object.findPageIndex(current_candidate_page.getUrl());
                                }

                                // was it an existing page after all?  if so, add any referring links that have been added to this 
                                // candidate page (i.e. links to its alias address that were found in PART 2 of spiderProcess()
                                // last time), and add this alias URL to the existing page's list of alias URLs (if it was an alias,
                                // it's also possible that the link that generated this candidate page was found after a link that 
                                // went to an alias of this page, in which case this one could not be an alias)
                                if (real_page_index > -1) {
                                    found = true;
                                    SpiderPage real_page = spider_object.getPageAtIndex(real_page_index);
                                    List<SpiderLink> current_candidate_referred_links = current_candidate_page.getReferredByLinks();
                                    for (int k = 0; k < current_candidate_referred_links.Count; k++) {
                                        real_page.addReferredByLink(current_candidate_referred_links.ElementAt(k));
                                    }
                                    if (current_candidate_page._candidate_isAliasCandidate()) {
                                        real_page.addAliasUrl(current_candidate_page._candidate_getUrl());
                                    }
                                }
                            }

                            // this candidate page was a real new page- add it to the master results, add its links to the
                            // new links found this round, and add it to the list of pages added this round
                            if (!found) {
                                SpiderPage new_page = current_candidate_page._candidate_makeNewSpiderPage();
                                new_links_found.AddRange(new_page.getLinkingToLinks());
                                spider_object.addNewPage(new_page);
                                added_candidate_urls.Add(new string[] { new_page.getUrl(), spider_object.getLastPageIndex().ToString() });
                            }

                            // this candidate page is done being processed- remove it from the list
                            spider_object._candidate_pages.RemoveAt(i);
                            candidate_page_count--;
                            i--;
                        }
                    }

                    // PART 2:  make new candidate pages from the new links that go to pages we haven't seen before, 
                    //          create new fetchPage() worker threads to crawl them

                    List<_SpiderPageCandidate> new_candidate_pages = new List<_SpiderPageCandidate>();
                    for (int j = 0; j < new_links_found.Count; j++) {
                        SpiderLink current_link = new_links_found.ElementAt(j);

                        if (current_link.isLegalLink()) {
                            // see if we've made a new candidate page for this link already
                            int link_index = -1;
                            for (int y = 0; y < new_candidate_pages.Count; y++) {
                                if (new_candidate_pages.ElementAt(y)._candidate_getUrl() == current_link.getNormalizedUrl()) {
                                    link_index = y;
                                    break;
                                }
                            }

                            // if we have made a new candidate page already, just add a referred-by link to the
                            // candidate page we already made
                            if (link_index > -1) {
                                new_candidate_pages.ElementAt(link_index).addReferredByLink(current_link);
                            }
                            // otherwise, search the master results to see if we need to create a new candidate 
                            // page or not
                            else {
                                int real_page_index = spider_object.findPageIndex(current_link.getNormalizedUrl());
                                // if this link's URL exists in the master results already, just add a referred-by link
                                if (real_page_index > -1) {
                                    SpiderPage real_page = spider_object.getPageAtIndex(real_page_index);
                                    real_page.addReferredByLink(current_link);
                                }
                                // otherwise, make a new candidate page from this link
                                else {
                                    new_candidate_pages.Add(new _SpiderPageCandidate(current_link));
                                }
                            }
                        }
                    }

                    // create a new fetchPage() worker thread for every new candidate page we made
                    for (int p = 0; p < new_candidate_pages.Count; p++) {
                        spider_object._candidate_pages.Add(new_candidate_pages.ElementAt(p));
                        spider_object.addThreadStatus();
                        ThreadPool.QueueUserWorkItem(new WaitCallback(fetchPage),
                                                      new _SpiderWorkItemDataWrapper(spider_object, spider_object._candidate_pages.Count - 1));
                    }
                }
            }

            // loop spiderProcess() until there are either no candidate pages in the list or there are only
            // error candidate pages left
            while (spider_object._candidate_pages.Count > 0 &&
                    spider_object._candidate_pages.Any(delegate(_SpiderPageCandidate spc) { return !spc._candidate_isError(); }));

            // we're done spidering now, clear our _thread_status (the 0-index _thread_status is reserved for
            // spiderProcess(), worker threads are indices > 0)
            spider_object._thread_status.RemoveAt(0);
        }
		
        /*  fetchPage()             - takes a _SpiderWorkItemDataWrapper object that will be cast from an object
         *                            (because the work method of a C# ThreadPool work item has to take a single
         *                            object argument, and be static/void), and fetches the _SpiderPageCandidate
         *                            at the index specified by the _index field in the _SpiderWorkItemDataWrapper
         *      @o                  - the object argument to be cast into a _SpiderWorkItemDataWrapper
         */
		static void fetchPage(object o) {
            // unpack the _SpiderWorkItemDataWrapper object
            _SpiderWorkItemDataWrapper wi = (_SpiderWorkItemDataWrapper) o;
			Spider spider_object = wi.getSpiderObject();
            _SpiderPageCandidate candidate_page = wi.getCandidatePage();

			List<string> pre_pages = new List<string>();

	        byte[] buf = new byte[8192];
            StringBuilder sb = new StringBuilder();

            HttpWebResponse resp = null;
            try {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(candidate_page._candidate_getUrl());
                //req.Timeout = 1000;
                // sleep for the niceness time of this spider object
                spider_object.acquireFetchLock();
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (Exception e) {
                candidate_page._candidate_setError();
                spider_object.writeStatus("ERROR: " + e.Message);
                spider_object.writeStatus("\tpage - " + candidate_page._candidate_getUrl() + "\n\t\treferred to by:");

                List<SpiderLink> curr_refs = candidate_page.getReferredByLinks();
                for (int i = 0; i < curr_refs.Count; i++) {
                    spider_object.writeStatus("\t\t\t" + curr_refs.ElementAt(i).getReferringUrl());
                }
            }
            if (resp != null) {
                // record the final Url after any redirects from this link
                string normalized_final_url = spider_object.normalizeUrl(resp.ResponseUri.ToString(), "");
                if (normalized_final_url.Count() < 1) {
                    candidate_page._candidate_setError();
                    spider_object.writeStatus("fetchPage(): candidate page " + candidate_page._candidate_getUrl() +
                                                " redirected to an illegal page.");
                }
                candidate_page.setUrl(normalized_final_url);

                spider_object.writeStatus("thread id: " + Thread.CurrentThread.ManagedThreadId +
                                        ", fetchPage(): fetched " + candidate_page._candidate_getUrl() +
                                        "\n\tfetchPage(): normalized final url - " + candidate_page.getUrl());

                if (!candidate_page._candidate_isError()) {
                    // read in the content of the page
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
                    // parse the page for links
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
                        pre_pages.Add(link.Url);
                    };

                    // parse out the distinct links on this page, removing any duplicates, and marking illegal links as such
                    List<string> distinct_pre_pages = pre_pages.Distinct().ToList();
                    for (int m = 0; m < distinct_pre_pages.Count; m++) {
                        string new_url = distinct_pre_pages.ElementAt(m);
                        SpiderLink new_link = new SpiderLink(new_url, spider_object.normalizeUrl(new_url, candidate_page.getUrl()),
                                                                candidate_page.getUrl());
                        if (new_link.getNormalizedUrl().Count() < 1) {
                            new_link.setIllegalLink();
                        }
                        candidate_page.addLinkingToLink(new_link);
                    }
                    // set this candidate page as processed

                    candidate_page._candidate_setDone();
                }
            }

            // mark this thread as done in _thread_status
            spider_object.removeThreadStatus();
		}
	}
	
    /*  class _SpiderCandidatePage      - _SpiderPageCandidate objects represent a page that needs to be
     *                                    fetched, after which we will determine if it's a new page or not;
     *                                    new _SpiderPageCandidate objects are generated by finding links to
     *                                    pages that we haven't seen before, which could be redirects, but
     *                                    we have to grab a page to get its final URL; each _SpiderPageCandidate
     *                                    page in the Spider object's _candidate_pages list will be processed by
     *                                    a worker thread;  _SpiderPageCandidate extends SpiderPage, and a candidate
     *                                    object is simply converted into a SpiderPage object if it's found to
     *                                    be a unique new page
     */
	class _SpiderPageCandidate : SpiderPage {
		
		bool _done;
        bool _error;

		string _candidate_url;
		
        /*  _SpiderPageCandidate()      - makes a new _SpiderPageCandidate object from a SpiderLink
         *      @candidate_page_link    - the link that this _SpiderPageCandidate will be made from,
         *                                using the link's normalized URL as this object's URL and 
         *                                the link itself as this object's first referred-by link
         */
		public _SpiderPageCandidate(SpiderLink candidate_page_link) {
			this._done = false;
            this._error = false;
			
			this._candidate_url = candidate_page_link.getNormalizedUrl();

            this.addReferredByLink(candidate_page_link);
		}
		
        /*  _candidate_isDone()         - true if this object has been processed to completion with 
		 *								  fetchPage(), false otherwise
         */
		public bool _candidate_isDone() {
			return this._done;
		}

        /*  _candidate_setDone()        - set the status of this object as done being processed by
         *                                fetchPage()
         */
        public void _candidate_setDone() {
            this._done = true;
        }

        /*  _candidate_isError()        - return whether fetchPage() marked this candidate page as
         *                                an error page, i.e. it resulted in a 404, etc.
         */
        public bool _candidate_isError() {
            return this._error;
        }

        /*  _candidate_setError()       - sets this candidate page as an error, used by fetchPage()
         *                                when processing a candidate page results in a 404, etc.
         */
        public void _candidate_setError() {
            this._error = true;
        }
		
        /*  _candidate_getUrl()         - return this object's URL, i.e. the normalized URL of the
         *                                link it was made from
         */
		public string _candidate_getUrl() {
			return this._candidate_url;
		}
		
        /*  _candidate_isAliasCandidate()   - returns true if this object's final URL ended up being
         *                                    different than the link's href text that it was made
         *                                    from, i.e. it was a redirect; only called after this
         *                                    object is done being processed by fetchPage()
         */
		public bool _candidate_isAliasCandidate() {
			return !(this._candidate_getUrl() == this.getUrl());
		}
		
        /*  _candidate_makeNewSpiderPage    - makes a new SpiderPage out of this candidate object
         */
		public SpiderPage _candidate_makeNewSpiderPage() {
            SpiderPage s = new SpiderPage(this.getUrl(), this.getLinkingToLinks(), this.getReferredByLinks());
            if (this._candidate_isAliasCandidate()) {
                s.addAliasUrl(this._candidate_getUrl());
            }
            return s;
		}
	}
	
    /*  class _SpiderWorkItemDataWrapper    - encapsulates the spider object and an integer index
     *                                        to pass to fetchPage() for each worker thread; the index
     *                                        is the element number in the _candidate_pages list to be
     *                                        processed
     */
	class _SpiderWorkItemDataWrapper {
		
		int _index;
		Spider _spider;
		
        /*  _SpiderWorkItemDataWrapper()    - make a new _SpiderWorkItemDataWrapper object
         * 
         *      @spider                     - the spider object to wrap
         *      @index                      - the index of the page in _candidate_pages to be processed
         *                                    by the worker thread that uses this wrapper
         */
		public _SpiderWorkItemDataWrapper(Spider spider, int index) {
			this._spider = spider;
            this._index = index;
		}

        /*  getSpiderObject()               - return the spider object inside this wrapper
         */
        public Spider getSpiderObject() {
            return this._spider;
        }

        /*  getCandidatePage()              - return the candidate page at the index inside this wrapper
         */
		public _SpiderPageCandidate getCandidatePage() {
            this.getSpiderObject().writeStatus("getCandidatePage(): " + this._index);
            return this._spider.getCandidatePageAtIndex(this._index);
		}
	}
}