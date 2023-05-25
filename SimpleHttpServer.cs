using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

class SimpleHttpServer
{
    static readonly string[,] MIME_TYPES = new string[,] {
        { ".html", "text/html" },
        { ".js", "text/javascript" },
        { ".css", "text/css" },
        { ".txt", "text/plain" },
        { ".xml", "text/xml" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" },
        { ".json", "application/json" },
        { ".zip", "application/zip" },
    };
    static string port = "8000";
    static string host = "+";
    static string root = "./";
    static bool temporaryListenAddress = false;

    static void Main(string[] args)
    {
        ParseOptions(args);
        string prefix = temporaryListenAddress
            ? "http://+:80/Temporary_Listen_Addresses/"
            : string.Format("http://{0}:{1}/", host, port);
        try
        {
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
                        string path = (root + Regex.Replace(request.RawUrl, "[?;].*$", ""))
                            .Replace("//", "/").Replace("/", @"\");
                        if (path.EndsWith(@"\"))
                        {
                            path += "index.html";
                        }
                        byte[] content = null;
                        if (!request.HttpMethod.Equals("GET"))
                        {
                            response.StatusCode = (int) HttpStatusCode.NotImplemented;
                        }
                        else if (path.Contains(@"\..\") || path.EndsWith(@"\.."))
                        {
                            response.StatusCode = (int) HttpStatusCode.BadRequest;
                        }
                        else if (Directory.Exists(path))
                        {
                            var hosts = request.Headers.GetValues("Host");
                            var thisHost = (hosts != null) ? hosts[0] : request.UserHostAddress;
                            response.Headers.Set("Location", string.Format("http://{0}{1}/", thisHost, request.RawUrl));
                            response.StatusCode = (int) HttpStatusCode.MovedPermanently;
                        }
                        else if (!File.Exists(path))
                        {
                            response.StatusCode = (int) HttpStatusCode.NotFound;
                        }
                        else
                        {
                            try
                            {
                                content = File.ReadAllBytes(path);
                                response.ContentType = ContentType(path);
                                response.OutputStream.Write(content, 0, content.Length);
                            }
                            catch (Exception)
                            {
                                response.StatusCode = (int) HttpStatusCode.Forbidden;
                            }
                        }
                        Console.WriteLine(string.Format("{0} - - [{1}] \"{2} {3} HTTP/{4}\" {5} {6}",
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
            if (args[i].Equals("-t")) temporaryListenAddress = true;
            else if (args[i].Equals("-p") && i+1 < args.Length) port = args[++i];
            else if (args[i].Equals("-b") && i+1 < args.Length) host = args[++i];
            else if (args[i].Equals("-r") && i+1 < args.Length) root = args[++i];
            else
            {
                Console.Error.WriteLine(
                    "usage: HttpServer [-p PORT] [-b ADDR] [-r DIR]\n" +
                    "    or HttpServer [-t] [-r DIR]");
                Environment.Exit(0);
            }
        }
    }

    static string ContentType(string path)
    {
        for (int i=0; i<MIME_TYPES.GetLength(0); i++)
        {
            if (path.EndsWith(MIME_TYPES[i, 0]))
            {
                return MIME_TYPES[i, 1];
            }
        }
        return "application/octet-stream";
    }
}
