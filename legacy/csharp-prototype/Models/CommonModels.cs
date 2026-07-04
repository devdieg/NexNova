using System;

namespace NexNova.Models
{
    public class NovaDocumentParsedEventArgs : EventArgs
    {
        public NovaHtmlDocument Document { get; }
        public NovaDocumentParsedEventArgs(NovaHtmlDocument document) => Document = document;
    }

    public class NovaResourceLoadedEventArgs : EventArgs
    {
        public string Url { get; }
        public string ContentType { get; }
        public long Size { get; }
        public NovaResourceLoadedEventArgs(string url, string contentType, long size)
        {
            Url = url; ContentType = contentType; Size = size;
        }
    }

    public class NovaScriptExecutedEventArgs : EventArgs
    {
        public string Script { get; }
        public string Result { get; }
        public NovaScriptExecutedEventArgs(string script, string result)
        {
            Script = script; Result = result;
        }
    }
}