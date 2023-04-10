using System.Net;

namespace ICPDAS_Manager
{
    public static class WebRequests
    {
        public static async Task<HttpWebResponse?> PostAsync(string url, string contentType, string accept, string? content = null, string? referer = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = HttpMethod.Post.ToString();
            request.Accept = accept;
            request.ContentType = contentType;
            request.Referer = referer;

            using Stream requestStream = await request.GetRequestStreamAsync();
            using StreamWriter streamWriter = new StreamWriter(requestStream);
            await streamWriter.WriteAsync(content);
            streamWriter.Flush();
            try
            {
                return (await request.GetResponseAsync()) as HttpWebResponse;
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse httpResponse)
                {
                    return httpResponse;
                }
                throw;
            }
        }
    }
}