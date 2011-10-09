using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;


namespace spider {
	
	class SpiderHelperWrapper {
		
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