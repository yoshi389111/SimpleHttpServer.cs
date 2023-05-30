using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

public class SimpleHttpServer
{
    private readonly static string DirSep = Path.DirectorySeparatorChar.ToString();

    private static string s_root = "./";
    private static string s_prefix = null;

    public static void Main(string[] args)
    {
        try
        {
            ParseOptions(args);
            string prefixPath = WebUtility.UrlDecode(
                Regex.Replace(s_prefix, "https?://[^/]*", ""));

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(s_prefix);
                listener.Start();
                Console.WriteLine("Listening on " + s_prefix);

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    using (HttpListenerResponse response = context.Response)
                    {
                        string rawPath = WebUtility.UrlDecode(
                            Regex.Replace(request.RawUrl, "[?;].*$", "")
                            ).Substring(prefixPath.Length-1);

                        if (rawPath == "") {
                            rawPath = "/";
                        }

                        string path = Regex.Replace(s_root + rawPath, "/+", DirSep);

                        if (path.EndsWith(DirSep) && File.Exists(path + "index.html"))
                        {
                            path += "index.html";
                        }

                        byte[] content = {};

                        if (!request.HttpMethod.Equals("GET"))
                        {
                            response.StatusCode = 501; // NotImplemented
                        }
                        else if (path.Contains(DirSep + ".." + DirSep)
                            || path.EndsWith(DirSep + ".."))
                        {
                            response.StatusCode = 400; // BadRequest
                        }
                        else if (path.EndsWith(DirSep)
                            && Directory.Exists(path.Substring(0, path.Length-1)))
                        {
                            string indexPage = CreateIndexPage(path, rawPath);
                            content = Encoding.UTF8.GetBytes(indexPage);
                            response.ContentType = "text/html";
                            response.OutputStream.Write(content, 0, content.Length);
                        }
                        else if (Directory.Exists(path))
                        {
                            string[] hosts = request.Headers.GetValues("Host");
                            string thisHost = (hosts != null)
                                ? hosts[0]
                                : request.UserHostAddress;
                            response.Headers.Set("Location",
                                string.Format("http{0}://{1}{2}/",
                                    request.IsSecureConnection ? "s" : "",
                                    thisHost,
                                    request.RawUrl));
                            response.StatusCode = 301; // MovedPermanently
                        }
                        else if (!File.Exists(path))
                        {
                            response.StatusCode = 404; // NotFound
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
                                response.StatusCode = 403; // Forbidden
                            }
                        }

                        Console.WriteLine(
                            "{0} - - [{1}] \"{2} {3} HTTP/{4}\" {5} {6}",
                            request.RemoteEndPoint.Address,
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss K"),
                            request.HttpMethod,
                            request.RawUrl,
                            request.ProtocolVersion,
                            response.StatusCode,
                            content.Length);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private static void ParseOptions(string[] args)
    {
        string port = "8000";
        string host = "+";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-t"))
            {
                s_prefix = "http://+:80/Temporary_Listen_Addresses/";
            }
            else if (args[i].Equals("-p") && i+1 < args.Length)
            {
                port = args[++i];
            }
            else if (args[i].Equals("-b") && i+1 < args.Length)
            {
                host = args[++i];
            }
            else if (args[i].Equals("-r") && i+1 < args.Length)
            {
                s_root = args[++i];
                if (!Directory.Exists(s_root))
                {
                    throw new DirectoryNotFoundException(s_root);
                }
            }
            else if (args[i].Equals("-P") && i+1 < args.Length)
            {
                s_prefix = args[++i];
            }
            else
            {
                Console.Error.WriteLine(
                    "usage: {0} [-r DIR] [-p PORT] [-b ADDR]\n" +
                    "    or {0} [-r DIR] [-t]\n" +
                    "    or {0} [-r DIR] [-P PREFIX]",
                    AppDomain.CurrentDomain.FriendlyName);
                Environment.Exit(0);
            }
        }

        if (s_prefix == null)
        {
            s_prefix = string.Format("http://{0}:{1}/", host, port);
        }
    }

    private static string CreateIndexPage(string path, string urlPath)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<html><head><meta charset=\"UTF-8\" /></head>\n");
        sb.AppendFormat("<body><h1>List of {0}</h1><ul>\n",
            WebUtility.HtmlEncode(urlPath));

        if (urlPath != "/")
        {
            sb.Append("<li><a href=\"..\">..</a></li>\n");
        }

        foreach (string file in Directory.GetFileSystemEntries(path))
        {
            string basename = Path.GetFileName(file);
            sb.AppendFormat("<li><a href=\"{0}{2}\">{1}{2}</a></li>\n",
                WebUtility.UrlEncode(basename),
                WebUtility.HtmlEncode(basename),
                Directory.Exists(file) ? "/" : "");
        }

        sb.Append("</ul></body></html>\n");
        return sb.ToString();
    }
}
