using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

class SimpleHttpServer
{
    static string port = "8000";
    static string host = "+";
    static string root = "./";
    static string prefix = null;

    static void Main(string[] args)
    {
        ParseOptions(args);
        if (prefix == null)
        {
            prefix = string.Format("http://{0}:{1}/", host, port);
        }
        try
        {
            string prefixPath = WebUtility.UrlDecode(
                Regex.Replace(prefix, @"https?://[^/]*", ""));
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(prefix);
                listener.Start();
                Console.WriteLine("Listening on " + prefix);
                while (true)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    using (var response = context.Response)
                    {
                        string rawPath = WebUtility.UrlDecode(
                            Regex.Replace(request.RawUrl, "[?;].*$", ""));
                        if (0 < prefixPath.Length && rawPath.StartsWith(prefixPath))
                        {
                            rawPath = rawPath.Substring(prefixPath.Length-1);
                        }
                        string path = (root + rawPath).Replace("//", "/").Replace("/", @"\");
                        if (path.EndsWith(@"\") && File.Exists(path + "index.html"))
                        {
                            path += "index.html";
                        }
                        byte[] content = null;
                        if (!request.HttpMethod.Equals("GET"))
                        {
                            response.StatusCode = (int) HttpStatusCode.NotImplemented; // 501
                        }
                        else if (path.Contains(@"\..\") || path.EndsWith(@"\.."))
                        {
                            response.StatusCode = (int) HttpStatusCode.BadRequest; // 400
                        }
                        else if (path.EndsWith(@"\")
                            && Directory.Exists(path.Substring(0, path.Length-1)))
                        {
                                string indexPage = CreateIndexPage(path, rawPath);
                                content = Encoding.UTF8.GetBytes(indexPage);
                                response.ContentType = "text/html";
                                response.OutputStream.Write(content, 0, content.Length);
                        }
                        else if (Directory.Exists(path))
                        {
                            var hosts = request.Headers.GetValues("Host");
                            var thisHost = (hosts != null) ? hosts[0] : request.UserHostAddress;
                            response.Headers.Set(
                                "Location",
                                string.Format("http://{0}{1}/", thisHost, request.RawUrl));
                            response.StatusCode = (int) HttpStatusCode.MovedPermanently; // 301
                        }
                        else if (!File.Exists(path))
                        {
                            response.StatusCode = (int) HttpStatusCode.NotFound; // 404
                        }
                        else
                        {
                            try
                            {
                                content = File.ReadAllBytes(path);
                                response.ContentType = MimeMapping.GetMimeMapping(path);
                                response.OutputStream.Write(content, 0, content.Length);
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                                response.StatusCode = (int) HttpStatusCode.Forbidden; // 403
                            }
                        }
                        Console.WriteLine(
                            string.Format("{0} - - [{1}] \"{2} {3} HTTP/{4}\" {5} {6}",
                            request.RemoteEndPoint.Address,
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss K"),
                            request.HttpMethod, request.RawUrl, request.ProtocolVersion,
                            response.StatusCode,
                            (content == null ? 0 : content.Length)));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    static void ParseOptions(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-t")) prefix = "http://+:80/Temporary_Listen_Addresses/";
            else if (args[i].Equals("-p") && i+1 < args.Length) port = args[++i];
            else if (args[i].Equals("-b") && i+1 < args.Length) host = args[++i];
            else if (args[i].Equals("-r") && i+1 < args.Length) root = args[++i];
            else if (args[i].Equals("-P") && i+1 < args.Length) prefix = args[++i];
            else
            {
                Console.Error.WriteLine(
                    "usage: SimpleHttpServer [-r DIR] [-p PORT] [-b ADDR]\n" +
                    "    or SimpleHttpServer [-r DIR] [-t]" +
                    "    or SimpleHttpServer [-r DIR] [-P PREFIX]");
                Environment.Exit(0);
            }
        }
    }

    static string CreateIndexPage(string path, string urlPath)
    {
        string[] files = Directory.GetFileSystemEntries(path);
        string indexPage = string.Format(
            "<html><head><meta charset=\"UTF-8\" /></head>\n" +
            "<body><h1>List of {0}</h1><ul>\n",
            WebUtility.HtmlEncode(urlPath));
        if (urlPath != "/")
        {
            indexPage += "<li><a href=\"..\">..</a></li>\n";
        }
        foreach (string file in files) {
            string basename = Path.GetFileName(file);
            string link = string.Format(
                "<li><a href=\"{0}{2}\">{1}{2}</a></li>\n",
                WebUtility.UrlEncode(basename),
                WebUtility.HtmlEncode(basename),
                Directory.Exists(file) ? "/" : "");
            indexPage += link;
        }
        indexPage += "</ul></body></html>\n";
        return indexPage;
    }
}
