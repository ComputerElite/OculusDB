using System.Threading;
using System.Threading.Tasks;

namespace ComputerUtils.Timing
{
    public class TimeDelay
    {
        public static async Task DelayWithoutThreadBlock(int ms)
        {
            await Task.Delay(ms);
        }
    }
}