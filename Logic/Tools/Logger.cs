using System;
using System.IO;
using System.Threading.Tasks;

namespace Trakov.Backend.Logic.Scanner.MarketScanner
{
    public class Logger<T> : ILogger<T>
    {
        public Task writeData(string message, ILogger<T>.Levels level)
        {
            lock (typeof(T))
            {
                message = $"{DateTime.Now.ToLongDateString()} | {DateTime.Now.ToLongTimeString()} | {level} | {message}";
                _ = File.AppendAllTextAsync($"{typeof(T).Name}.log", message+'\n');
            }
            return Task.CompletedTask;
        }
    }
}
