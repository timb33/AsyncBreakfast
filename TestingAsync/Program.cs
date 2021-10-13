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

                    AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Press C to cancel breakfast");
                    char ch = Console.ReadKey().KeyChar;
                    if (ch == 'c' || ch == 'C')
                    {
                        cts.Cancel();
                        AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Task cancellation requested.");
                    }

                    //cbTask.Wait();//wait has no time param, so wait endlessly, until this is complete before proceeding. 

                    AddMsg(Thread.CurrentThread.ManagedThreadId, "Main - Breakfast completed###############################");
                    Console.ReadKey();
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
        }

        private static async Task CookBreakfastAsync(CancellationToken cts)
        {
            try
            {
                const int iWait_ms = 5000;

                //AddMsg(Thread.CurrentThread.ManagedThreadId, $"no timeout - all dishes get washed");
                //UseDishWasherAsync("pre-breakfast").Wait(); 

                #region start tasks running, and get handles...
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - give dishwasher a headstart on breakfast, but start breakfast before it's finished.");
                var t = UseDishWasherAsync("pre-breakfast", cts).Wait(iWait_ms); //.Wait makes this sync. Timeout is short, so we timeout before task's finished, and only wash some of the plates (and proceed), but the dishwasher continues washing plates
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - most pots are probably clean. The longest we delay starting breakfast is: {iWait_ms}ms.");

                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - start breakfast... (dishes will be still washing)");
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - starting eggs...");

                var taskEggs = MakeScrambledEggsAsync("making eggs", cts);
                //_ = Task.Delay(2);
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - starting toast, without waiting for eggs to be finished");
                var tskToast = MakeToastAsync("making toast", cts); 
                #endregion

                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - all tasks are now running");

                #region await tasks...
                //var tasks = new Task[] {
                //    taskEggs,tskToast
                //};
                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - both tasks are now running");
                await tskToast; AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - toast done - await until MakeToastAsync thread finishes");
                await taskEggs; AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - eggs done - await until MakeScrambledEggsAsync thread finishes");
                //Task.WhenAll(tasks).Wait(); //wait for both breakfast tasks to complete
                #endregion

                AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - everything done");

                //breakfast eaten...
                //UseDishWasherAsync("post-breakfast").Wait(); //this is now sync. no timeout so we guarantee cleaning cycle finishes.
                //AddMsg(Thread.CurrentThread.ManagedThreadId, "Pots are clean...");
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - cancelled: " + ex.Message); ;
            }
        }

        private static async Task UseDishWasherAsync(string id, CancellationToken cts)
        {
            int i = 0;
            const int iMaxPlates = 99;

            try
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - started.");
                //_ = Task.Delay(10); //pause calling thread
                await Task.Run(() => //run this thread/Task now
                {
                    for (i = 0; i < iMaxPlates; i++)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync - Cancelled on iteration # {i + 1}");
                            cts.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - washing plate:{i}/{iMaxPlates}."); ;
                        Thread.Sleep(_iDelay_ms);
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync[{id}] - cancelled: " + ex.Message); ;
                //throw; since we're async, this won't get caught by cookBreakfast
            }

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasherAsync's continuation!  [{id}] - done (All plates washed {i}/{iMaxPlates}).");
        }

        private static async Task MakeScrambledEggsAsync(string id, CancellationToken cts)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - started.");
            //_ = Task.Delay(10); //pause calling thread
            try
            {
                await Task.Run(() => //run this thread/Task now
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync - Cancelled on iteration # {i + 1}");
                            cts.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - egg:{i}."); ;
                        Thread.Sleep(_iDelay_ms);
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - cancelled: " + ex.Message); ;
            }

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync's continuation!  [{id}] - done.");
        }

        private static async Task MakeToastAsync(string id, CancellationToken cts)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - started.");

            //_ = Task.Delay(10); //pause calling thread <- we've not awaited the discarded task. So, we're not actually pausing here. http://tomasp.net/blog/csharp-async-gotchas.aspx/ gotcha#2
            //var delay = Task.Delay(_iDelay_ms * 5); await delay; <- you need the await otherwise the delay doesn't pause this function

            try
            {
                await Task.Run(() => //run this thread/Task now
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (cts.IsCancellationRequested)
                        {
                            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync - Cancelled on iteration # {i + 1}");
                            cts.ThrowIfCancellationRequested();
                        }

                        AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - slice:{i}."); ;
                        Thread.Sleep(_iDelay_ms);
                    //var t = Task.Delay(_iDelay_ms); await t; <- error
                }
                });
            }
            catch (OperationCanceledException ex)
            {
                AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - cancelled: " + ex.Message); ;
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

            var delta_ms = (DateTime.Now.Ticks - _iTickStart);
            Console.WriteLine($"{delta_ms}: ".PadLeft(12) + $" [{tid}] ".PadLeft(4) + v);
        }
        #endregion
    }
}
