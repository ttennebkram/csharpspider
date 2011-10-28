using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spider;

namespace New_Spider {

    class SpiderConsoleApp {

        static void Main(string[] args) {
			if (args.Length > 0) {
				if (args[1] == "help") {
					helpMessage();
				}
				else {
					if (args.Length > 3) {
						string[] s_args = new string[args.Length - 1];
						for (int i = 1; i < args.Length; i++) {
							s_args[i - 1] = args[i];
						}
						doSpider(s_args);
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
			System.Console.WriteLine("helpMessage()");
		}
		
		static void errorMessage() {
			System.Console.WriteLine("errorMessage()");
		}
		
		static void doSpider(string[] args) {
			bool parse_success = true;
			
			int n_threads = 0;
			int n_ms_timeout = 0;
			string root_url = "";
			string start_url = "";
			try {
				n_threads = Int32.ParseInt(args[0]);
				n_ms_timeout = Int32.ParseInt(args[1]);
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
                            	curr_link_url = "< illegal link not followed >";
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
