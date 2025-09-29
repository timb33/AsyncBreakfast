using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncBreakfast
{
    /// <summary>
    /// https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md "David Fowl", MVP, wrote SignalR etc. 
    /// http://tomasp.net/blog/csharp-async-gotchas.aspx/ 
    /// https://arghya.xyz/articles/task-cancellation/ 
    /// </summary>
    class Program
    {

        private const int _iDelay_ms = 1000;

        static void Main(string[] args)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, "Main...");

            using (var cts = new CancellationTokenSource())
            {
                try
                {
                    var cbTask = CookBreakfastAsync(cts.Token); //breakfast is now cooking

                    AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - ################Intense cooking started");

                    AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Press C to cancel breakfast (not CTRL+C, which kills the app)");
                    char ch = Console.ReadKey().KeyChar;
                    if (ch == 'c' || ch == 'C')
                    {
                        cts.Cancel();
                        AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Task cancellation requested.");
                    }

                    //cbTask.Wait();//wait has no time param, so wait endlessly, until this is complete before proceeding. 
                    cbTask.GetAwaiter().GetResult(); //wait for everything to close down, even if we're cancelled.


                }
                catch (AggregateException ae)
                {
                    if (ae.InnerExceptions.Any(e => e is TaskCanceledException))
                        AddMsg(Thread.CurrentThread.ManagedThreadId, "Task cancelled exception detected");
                    else
                    {
                        AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Task xxx exception detected");
                        throw;
                    }
                }
                catch (Exception)
                {
                    AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Task yyyy exception detected");
                    throw;
                }
                finally
                {
                    //cts.Dispose();
                }
            }

            Console.WriteLine( "We're really done. Any key to finish");
            Console.ReadKey(); //keep the window open, so we can view the console
        }

        private static async Task CookBreakfastAsync(CancellationToken ct)
        {
            try
            {
                // This is the max time we're willing to wait for the dishwasher headstart
                const int iHeadStart_ms = 5000;

                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - starting dishwasher for pre-breakfast cleanup.");

                // 1. Start the long-running dishwasher task (don't await yet!)
                var dishWasherTask = UseDishWasherAsync("pre-breakfast", ct);

                // 2. Create a non-blocking timeout task
                var delayTask = Task.Delay(iHeadStart_ms);//.ConfigureAwait(false); //be consistent

                // 3. Wait until EITHER the dishwasher finishes OR the delay expires.
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - waiting for max headstart of {iHeadStart_ms}ms.");
                await Task.WhenAny(dishWasherTask, delayTask).ConfigureAwait(false);

                // 4. Continue with breakfast. The dishWasherTask continues running in the background.
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - headstart complete. Starting cooking...");

                var taskEggs = MakeScrambledEggsAsync("making eggs", ct);
                var tskToast = MakeToastAsync("making toast", ct);

                // Wait for all cooking tasks to complete (non-blocking).
                await Task.WhenAll(taskEggs, tskToast).ConfigureAwait(false);
                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - toast and eggs done.");

                // 5. Finally, wait for the dishwasher that started earlier.
                await dishWasherTask.ConfigureAwait(false);
                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - dishwasher done.");

            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - cancelled: " + ex.Message);
                throw;
            }
            finally {
                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - Breakfast completed###############################");
                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - the cancel keyread doesn't know that we've finished");
            }
        }

        private static async Task UseDishWasherAsync(string id, CancellationToken ct)
        {
            int i = 0;
            const int iMaxPlates = 15;

            try
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - started.");
                //_ = Task.Delay(10); //pause calling thread
                //await Task.Run(() => //run this thread/Task now
                {
                    for (i = 0; i < iMaxPlates; i++)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync - Cancelling on iteration # {i + 1}");
                            ct.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - washing plate:{i}/{iMaxPlates}."); ;
                        await Task.Delay(_iDelay_ms, ct);//Thread.Sleep(_iDelay_ms);//this blocks, causing thread to be locked for a full second doing nothing  
                    }
                }//);
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - cancelled: " + ex.Message); ;
                throw; 
            }

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync's continuation!  [{id}] - done (All plates washed {i}/{iMaxPlates}).");
        }

        private static async Task MakeScrambledEggsAsync(string id, CancellationToken ct)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - started.");
            //_ = Task.Delay(10); //pause calling thread
            try
            {
                //await Task.Run(() => //run this thread/Task now
                {
                    const int iEggMax = 6;
                    for (int i = 0; i < 6; i++)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync - Cancelling on iteration # {i + 1}");
                            ct.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - egg:{i}/{iEggMax}"); ;
                        await Task.Delay(_iDelay_ms, ct);//Thread.Sleep(_iDelay_ms);//this blocks
                    }
                }//);
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - cancelled: " + ex.Message);
                throw;
            }

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync's continuation!  [{id}] - done.");
        }

        private static async Task MakeToastAsync(string id, CancellationToken ct)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - started.");

            //_ = Task.Delay(10); //pause calling thread <- we've not awaited the discarded task. So, we're not actually pausing here. http://tomasp.net/blog/csharp-async-gotchas.aspx/ gotcha#2
            //var delay = Task.Delay(_iDelay_ms * 5); await delay; <- you need the await otherwise the delay doesn't pause this function

            try
            {
                //await Task.Run(() => //run this thread/Task now
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync - Cancelling on iteration # {i + 1}");
                            ct.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - slice:{i}."); ;
                        //await Task.Delay(_iDelay_ms, ct);//Thread.Sleep(_iDelay_ms);//this blocks
                        var t = Task.Delay(_iDelay_ms); await t;// <- no error
                    }
                }//);
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - cancelled: " + ex.Message);
                throw;
            }

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync's continuation!  [{id}] - done.");
        }

        #region logging...
        private static bool _bFirst = true;
        private static long _iTickStart = 0;

        private static void AddMsg(int tid, string v)
        {
            if (_bFirst == true)
            {
                _bFirst = false;
                _iTickStart = DateTime.Now.Ticks;
            }

            // Using Ticks for a crude timestamp, better replaced by System.Diagnostics.Stopwatch for real code.
            var delta_ms = (DateTime.Now.Ticks - _iTickStart) / TimeSpan.TicksPerMillisecond;
            Console.WriteLine($"{delta_ms:N0}ms: ".PadLeft(12) + $" [{tid}] ".PadLeft(4) + v);
        }
        #endregion
    }
}
