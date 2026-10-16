using System.Runtime.ExceptionServices;

namespace RubyMarshalCS.Tests;

/// <summary>
/// Reading, writing and describing a value call themselves once for every level of nesting, and the files with a chain of 1500 levels do not fit
/// in the stack of 1 MB that a thread has on Windows (less in a build for debugging). The work on such data is done in a thread with a big stack.
/// </summary>
internal static class BigStack
{
    public static void Run(Action action, int megabytes = 256)
    {
        ExceptionDispatchInfo? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                error = ExceptionDispatchInfo.Capture(e);
            }
        }, megabytes * 1024 * 1024);

        thread.Start();
        thread.Join();
        error?.Throw();
    }
}
