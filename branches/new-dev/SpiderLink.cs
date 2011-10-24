using System;
using System.Collections.Generic;

namespace Spider {
	
	class SpiderLink {

        bool legal_link;
		string original_link_url;
		string normalized_link_url;
		string referring_page_url;
				
		public SpiderLink(string original_link_url, string normalized_link_url, string referring_page_url) {
            this.legal_link = true;
			this.original_link_url = original_link_url;
			this.normalized_link_url = normalized_link_url;
			this.referring_page_url = referring_page_url;
		}

        public void setIllegalLink() {
            this.legal_link = false;
        }

        public bool isLegalLink() {
            return this.legal_link;
        }

		public string getOriginalUrl() {
			return this.original_link_url;
		}
		
		public string getNormalizedUrl() {
			return this.normalized_link_url;
		}
		
		public string getReferringUrl() {
			return this.referring_page_url;
		}
	}
}