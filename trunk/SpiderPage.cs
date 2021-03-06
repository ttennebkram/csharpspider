﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider {

    public class SpiderPage {

        string url;
		string final_url;
        bool final_url_flag;
		List<string> alias_urls;
        List<string> linking_to_urls;
        List<string> referenced_by_urls;

        /* SpiderPage() -   make a new SpiderPage object representing a page visited while spidering a site
         *  @url -          the URL of this page
         *  @ref_url -      the referring URL of this page- all pages have to start with one referring page
         */
        public SpiderPage(string url, string ref_url) {
            this.url = url;
            this.final_url_flag = false;
			this.alias_urls = new List<string>();
            this.linking_to_urls = new List<string>();
            this.referenced_by_urls = new List<string>();

            this.referenced_by_urls.Add(ref_url);
        }

        /* SpiderPage() -  	make a new SpiderPage object with pre-populated lists of linking and referring URLs
         *  @url -          the URL of this page
         *  @ref_url -      a list of strings representing the URLs of pages that refer (i.e. link to) this page
         *  @link_url -     a list of strings representing the URLs that this page links to
         */
        public SpiderPage(string url, string final_url, List<string> ref_url, List<string> link_url) {
            this.url = url;
            this.final_url = final_url;
            this.final_url_flag = true;
            this.linking_to_urls = link_url;
            this.referenced_by_urls = ref_url;
        }

        /* getUrl() -       return this SpiderPage's URL
         */
        public string getUrl() {
            return this.url;
        }

		/* getFinalUrl() -	return this SpiderPage's final URL
		 */
		public string getFinalUrl() {
			return this.final_url;
		}

        /* getAliasUrls() -     return this SpiderPage's list of URLs that are aliases to it
         */
        public List<string> getAliasUrls() {
            return this.alias_urls;
        }
        /* getLinkingToUrls() - return this SpiderPage's list of URLs that it links to
         */
        public List<string> getLinkingToUrls() {
            return this.linking_to_urls;
        }

        /* getReferencedByUrls() -  return this SpiderPage's list of URLs that refer to it (i.e. link to it)
         */
        public List<string> getReferencedByUrls() {
            return this.referenced_by_urls;
        }
		
		/* addAliasUrl() -		add a new alias URL to this SpiderPage's list
		 *	@ new_url -		the new URL to be added
		 */
		public void addAliasUrl(string new_url) {
			this.alias_urls.Add(new_url);
		}

        /* addLinkingToUrl() -  add a new URL to this SpiderPage's list of URLs that it links to
         *  @new_url -          the new URL to add to the list
         */
        public void addLinkingToUrl(string new_url) {
            this.linking_to_urls.Add(new_url);
        }

        /* addReferencedByUrl() -   add a new URL to this SpiderPage's list of URLs that link to it
         *  @new_url -              the new URL to add to the list
         */
        public void addReferencedByUrl(string new_url) {
            this.referenced_by_urls.Add(new_url);
        }

        /* finalUrlNeeded() -       check if this page still needs its final url filled in,
         *                          i.e. it's content hasn't been spidered yet; spiderProcess
         *                          will see this on the next round
         */
        public bool finalUrlNeeded() {
            return this.final_url_flag;
        }
    }
}