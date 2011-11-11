using System;
using System.Collections.Generic;

namespace Spider {

	class SpiderPage {
	
		string _url;
		
		List<string> _alias_urls;
		
		List<SpiderLink> _linking_to_links;
		List<SpiderLink> _referred_by_links;
		
		string _page_content;
		
		public SpiderPage(string url, List<SpiderLink> linking_to_links, List<SpiderLink> referred_by_links) {
			this._url = url;
            this._alias_urls = new List<string>();
			this._linking_to_links = linking_to_links;
			this._referred_by_links = referred_by_links;
		}

        public SpiderPage() {
            this._url = "";
            this._alias_urls = new List<string>();
            this._linking_to_links = new List<SpiderLink>();
            this._referred_by_links = new List<SpiderLink>();
			this._page_content = "";
        }

		public void setUrl(string url) {
			this._url = url;
		}
		
		public string getUrl() {
			return this._url;
		}
		
		public void addAliasUrl(string url) {
			this._alias_urls.Add(url);
		}
		
		public List<string> getAliasUrls() {
			return this._alias_urls;
		}
		
		public List<SpiderLink> getLinkingToLinks() {
			return this._linking_to_links;
		}
		
		public List<SpiderLink> getReferredByLinks() {
			return this._referred_by_links;
		}
		
		public void addLinkingToLink(SpiderLink new_link) {
			this._linking_to_links.Add(new_link);
		}
		
		public void addReferredByLink(SpiderLink new_link) {
			this._referred_by_links.Add(new_link);
		}
		
		public void setPageContent(string page_content) {
			this._page_content = page_content;
		}
		
		public void getPageContent() {
			return this._page_content;
		}
	}
}