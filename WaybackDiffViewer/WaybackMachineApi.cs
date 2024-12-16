#region header
// WaybackDiffViewer
// Copyright (C)  2024 canadian
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using HtmlToMarkdownService;

namespace WaybackDiffViewer;

public class WaybackMachineApi
{
    private static readonly HttpClientHandler Handler = new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };
    
    private static readonly HttpClient Client = new HttpClient(Handler);
    private static readonly ConversionService MarkdownConverter = new ConversionService();

    private static readonly string SeachUrl = "https://web.archive.org/cdx/search/cdx?url={url}&filter=statuscode:200&collapse=digest";
    private static readonly string PageUrl = "https://web.archive.org/web/{timestamp}id_/{url}";
    
    public static async IAsyncEnumerable<WaybackResult> PerformSearchAsync(String url)
    {
        var stream = await Client.GetStreamAsync(SeachUrl.Replace("{url}", url));
        var reader = new StreamReader(stream);
        
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            var parts = line.Split(' ');
            var result = new WaybackResult()
            {
                Timestamp = DateTime.ParseExact(parts[1], "yyyyMMddHHmmss", null),
                OriginalUrl = new Uri(parts[2]),
                MimeType = parts[3],
                StatusCode = uint.Parse(parts[4]),
                Digest = parts[5],
                Length = uint.Parse(parts[6])
            };
            
            yield return result;
        }
    }

    private static readonly Regex LinkRegex = new Regex(@"\[([^\]]+)\]\((https?://[\s\d\w\.\#\-\/\?\=]+)\)");
    
    private static async Task<String> GetMarkdownForPageAsync(WaybackResult result)
    {
        var res1 = await result.GetHtmlAsync();
        var res2 = HttpUtility.HtmlDecode(MarkdownConverter.ConvertHtmlToMarkdown(res1.ParsedText));
        var res3 = LinkRegex.Replace(res2, "($1: $2)");
        return res3;
    }

    public struct WaybackResult
    {
        public override string ToString() => Timestamp.ToLongDateString();

        public DateTime Timestamp;
        public Uri OriginalUrl;
        public string MimeType;
        public uint StatusCode;
        public string Digest;
        public uint Length;
        
        private HtmlDocument? _html;
        private string? _markdownRepresentation;

        public async Task<string> GetMarkdownRepresentationAsync() => _markdownRepresentation ??= await GetMarkdownForPageAsync(this);

        public async Task<HtmlDocument> GetHtmlAsync()
        {
            if (_html != null) return _html;
            _html = new HtmlDocument();
            _html.Load(await Client.GetStreamAsync(
                PageUrl.Replace("{timestamp}", this.Timestamp.ToString("yyyyMMddHHmmss"))
                    .Replace("{url}", this.OriginalUrl.OriginalString)), true);
            return _html;
            
        }
    }
}