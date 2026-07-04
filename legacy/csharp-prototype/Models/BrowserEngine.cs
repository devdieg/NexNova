using System;
using System.Threading.Tasks;

namespace NexNova.Models
{
    public interface IBrowserEngine
    {
        Task Navigate(string url);
        void GoBack();
        void GoForward();
        void Refresh();
        Task<string> ExecuteScript(string script);
        string GetTitle();

        event EventHandler<string> PageLoaded;
        event EventHandler<string> TitleChanged;
    }
}