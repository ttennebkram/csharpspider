using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Spider;

namespace New_Spider {

    class SpiderConsoleApp {

        static void Main(string[] args) {
			if (args.Length > 0) {
				if (args[0] == "help") {
					helpMessage();
				}
				else {
					if (args.Length > 2) {
						doSpider(args);
					}
					else {
						errorMessage();
					}
				}
			}
			else {
				errorMessage();
			}
		}
		
		static void helpMessage() {
            System.Console.WriteLine("\n SpiderConsoleApp.exe [n_threads] [n_ms_niceness] [root_url] [start_url]\n");
            System.Console.WriteLine(" n_threads:     max number of threads to be in use at once");
            System.Console.WriteLine(" n_ms_niceness: niceness factor in ms to wait between http requests");
            System.Console.WriteLine(" root_url:      the root url");
            System.Console.WriteLine(" start_url:     (optional) the starting url; root_url otherwise");
            System.Console.WriteLine("\n example:\n\n    .\\SpiderConsoleApp.exe 20 500 http://www.site.com http://www.site.com/");
		}
		
		static void errorMessage() {
            System.Console.WriteLine("\n **** Incorrect arguments, run 'SpiderConsoleApp.exe help' for help. ****");
		}
		
		static void doSpider(string[] args) {
			bool parse_success = true;
			
			int n_threads = 0;
			int n_ms_timeout = 0;
			string root_url = "";
			string start_url = "";
			try {
				n_threads = Int32.Parse(args[0]);
				n_ms_timeout = Int32.Parse(args[1]);
				root_url = args[2];
				if (args.Length > 3) {
					start_url = args[3];
				}
				else {
					start_url = root_url;
				}
			}
			catch (Exception e) {
				parse_success = false;
				System.Console.WriteLine("ERROR: " + e.Message);
				System.Console.WriteLine("run 'SpiderConsoleApp.exe help' for help.");
			}
			
			if (parse_success) {
            	Spider.Spider s = new Spider.Spider(root_url, start_url, n_ms_timeout, n_threads);
            	s.spider();

            	List<SpiderPage> results = null;
            	do {
                	results = s.getResults();
            	} while (results == null);

            	for (int i = 0; i < results.Count; i++) {
                	SpiderPage curr = results.ElementAt(i);
                	List<string> curr_aliases = curr.getAliasUrls();
                	List<SpiderLink> curr_links = curr.getLinkingToLinks();
                	List<SpiderLink> curr_refs = curr.getReferredByLinks();

                    // make a filename into which we'll ouput this page's content
                    string[] fileparts = (new Uri(curr.getUrl())).Segments;
                    string filename = i + "-";
                    if (fileparts.Length > 1) {
                        filename = filename + fileparts[1].Replace("/", "_");
                    }
                    filename = filename + ".html";
                  
                    System.Console.WriteLine("pages\\" + filename);
                    StreamWriter sw = new StreamWriter("pages\\" + filename);
                    sw.Write(curr.getPageContent());

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
                            	curr_link_url = "< link not followed >";
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
}
