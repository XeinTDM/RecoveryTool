using RecoveryTool.Recovery;

namespace RecoveryTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RecoveryManager.RecoverForAllAsync();
        }
    }
}