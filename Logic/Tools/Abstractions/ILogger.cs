using System.Threading.Tasks;

namespace Trakov.Backend.Logic.Scanner.MarketScanner
{
    public interface ILogger<T>
    {
        public enum Levels
        {
            TRACE, INFO, WARNING, ERROR, CRITICAL
        }

        public Task writeData(string message, Levels level);
    }
}
