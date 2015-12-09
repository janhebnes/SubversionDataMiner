using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using SharpSvn;

namespace SubversionDataMiner
{
    class Program
    {
        static void Main(string[] args)
        {
        
            var creds = new System.Net.NetworkCredential("SVNUser", "SVNPassword");

            WebClient webClient = new WebClient();
            webClient.Credentials = creds;
            var svnRootHtmlSource = webClient.DownloadString("http://svnserver");
            var links = LinkFinder.Find(svnRootHtmlSource);

            foreach (var svnList in links)
            {
                if (svnList.Href.StartsWith("http://"))
                {
                    continue;
                }
                string sourcePath = string.Format("http://svnserver/{0}", svnList.Href.Replace("/", string.Empty).Trim());
                Console.WriteLine("HarvestSvnRepositoryRevisions {0}", sourcePath);
                try
                {
                    HarvestSvnRepositoryRevisions(creds, sourcePath);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    //throw;
                }
            }
            
            #region Notes, Samples and Trials

            // We use the Sharpsvn for looping all reps and uses logic for getting from the last time we fetched and dump xml or just parse on site 
            // SAMPLE SOURCE: http://sharpsvntips.net/
            // Sample Source: https://sharpsvn.open.collab.net/servlets/ProjectProcess?documentContainer=c4__Samples
            //using (SvnClient client = new SvnClient())
            //{
            //    client.Authentication.DefaultCredentials = new System.Net.NetworkCredential("SVNUSER", "SVNPASSWORD");
            //    string sourcePath = "http://svnserver/project/trunk";

            //    SvnTarget targetUri = new SvnUriTarget(new Uri(sourcePath));
            //    var list = new Collection<SvnListEventArgs>();
            //    client.GetList(targetUri, out list);
            //    Uri target = new Uri(sourcePath);

            //    Collection<SvnLogEventArgs> logitems;
            //    var g = client.GetLog(target, out logitems);
            //    // TODO: Unable to debug due to c dependence and unable to run due to shit implementation and documentation ? ... 
            //    foreach(SvnLogEventArgs logItem in logitems)
            //    {
            //        Console.WriteLine(logItem.LogMessage.ToString());
            //    }

            //    //System.IO.File.WriteAllText("D:\\svnlog.txt", SerializeObject(log));

            //}
            // SAMPLE SOURCE: http://sharpsvntips.net/

            ////using (SvnClient client = new SvnClient())
            ////{

            ////    // Checkout the code to the specified directory
            ////    client.CheckOut(new Uri("http://sharpsvn.googlecode.com/svn/trunk/"),
            ////                            "c:\\sharpsvn");

            ////    // Update the specified working copy path to the head revision
            ////    client.Update("c:\\sharpsvn");
            ////    SvnUpdateResult result;
            ////    client.Update("c:\\sharpsvn", out result);


            ////    client.Move("c:\\sharpsvn\\from.txt", "c:\\sharpsvn\\new.txt");

            ////    // Commit the changes with the specified logmessage
            ////    SvnCommitArgs ca = new SvnCommitArgs();
            ////    ca.LogMessage = "Moved from.txt to new.txt";
            ////    client.Commit("c:\\sharpsvn", ca);
            ////}
            #endregion
        }

        private static void HarvestSvnRepositoryRevisions(NetworkCredential creds, string sourcePath)
        {
            using (SvnClient client = new SvnClient())
            {
                client.Authentication.DefaultCredentials = creds;
                var store = new DataStore();
                
                var args = new SvnLogArgs();
                var maxRevision = store.GetMaxRevision(sourcePath);
                if (maxRevision > 0)
                {
                    args.End = maxRevision;
                    Console.WriteLine("Last harvested revision {0}", maxRevision);
                }

                client.Log(
                    new Uri(sourcePath),
                    args,
                    (o, e) =>
                    {
                        if (maxRevision == e.Revision)
                        {
                            return;
                        }

                        Console.WriteLine("rev {0} author {1} time {2} message {3}", e.Revision, e.Author, e.Time.ToString("g"), e.LogMessage);
                        var query =
                            string.Format(
                                "INSERT INTO SvnRepositoryRevision([SvnRepository],[Revision],[Author],[Time],[LogMessage]) VALUES('{0}','{1}','{2}','{3}','{4}')",
                                sourcePath, e.Revision, e.Author, e.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), SafeParse(e.LogMessage));
                        store.Execute(query);

                        if (e.ChangedPaths == null || e.ChangedPaths.Count > 20)
                        {
                            return;
                        }

                        foreach (var changeItem in e.ChangedPaths)
                        {
                            //Console.WriteLine(
                            //    string.Format(
                            //        "{0} {1} {2} {3}",
                            //        changeItem.Action,
                            //        changeItem.Path,
                            //        changeItem.CopyFromRevision,
                            //        changeItem.CopyFromPath));

                            var query2 =
                            string.Format(
                                "INSERT INTO SvnRepositoryRevisionChangedPaths([SvnRepository],[Revision],[Action],[Path],[CopyFromRevision],[CopyFromPath]) VALUES('{0}','{1}','{2}','{3}','{4}','{5}')",
                                sourcePath, e.Revision, changeItem.Action, changeItem.Path, changeItem.CopyFromRevision, changeItem.CopyFromPath);
                            store.Execute(query2);
                        }
                    });
                store.Flush();
            }
        }

        public static string SafeParse(string input)
        {
            string output = input.Replace("\"", "\"\"");
            output = output.Replace("'", "''");
            return output;
        }

        public static string SerializeObject(object toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());
            StringWriter textWriter = new StringWriter();

            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }

        public struct LinkItem
        {
            public string Href;
            public string Text;

            public override string ToString()
            {
                return Href + "\n\t" + Text;
            }
        }

        static class LinkFinder
        {
            public static List<LinkItem> Find(string file)
            {
                List<LinkItem> list = new List<LinkItem>();

                // 1.
                // Find all matches in file.
                MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                    RegexOptions.Singleline);

                // 2.
                // Loop over each match.
                foreach (Match m in m1)
                {
                    string value = m.Groups[1].Value;
                    LinkItem i = new LinkItem();

                    // 3.
                    // Get href attribute.
                    Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                    if (m2.Success)
                    {
                        i.Href = m2.Groups[1].Value;
                    }

                    // 4.
                    // Remove inner tags from text.
                    string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                    i.Text = t;

                    list.Add(i);
                }
                return list;
            }
        }

    }
}
