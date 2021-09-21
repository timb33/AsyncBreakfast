using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestingAsync
{
    class Program
    {

        private const int _iDelay_ms = 1000;

        static void Main(string[] args)
        {
            CookBreakfastAsync().Wait();//wait has no params, so wait until this is complete before reading the line. 
            Console.Read();
        }

        private async static Task CookBreakfastAsync()
        {
            const int iWait_ms = 5000;

            //AddMsg(Thread.CurrentThread.ManagedThreadId, $"no timeout - all dishes get washed");
            //UseDishWasherAsync("pre-breakfast").Wait(); 

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - give dishwasher a headstart on breakfast, but start breakfast before it's finished.");
            UseDishWasherAsync("pre-breakfast").Wait(iWait_ms); //.Wait makes this sync. Timeout is short, so we timeout before it's finished, and only wash some of the plates, but the dishwasher continues washing plates
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - most pots are probably clean. The longest we delay starting breakfast is: {iWait_ms}ms.");

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - start breakfast... (dishes will be still washing)");
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - starting eggs...");

            var taskEggs = MakeScrambledEggsAsync("making eggs");
            //_ = Task.Delay(2);
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - starting toast, without waiting for eggs to be finished");
            var tskToast = MakeToastAsync("making toast");
   
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"CookBreakfastAsync - both tasks are now running");
            //var tasks = new Task[] {
            //    taskEggs,tskToast
            //};

            AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - both tasks are now running");

            await tskToast; AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - toast done - await until MakeToastAsync thread finishes");

            await taskEggs; AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - eggs done - await until MakeScrambledEggsAsync thread finishes");



            //Task.WhenAll(tasks).Wait(); //wait for both breakfast tasks to complete
            AddMsg(Thread.CurrentThread.ManagedThreadId, "CookBreakfastAsync - everything done");

            //breakfast eaten...
            //UseDishWasherAsync("post-breakfast").Wait(); //this is now sync. no timeout so we guarantee cleaning cycle finishes.
            //AddMsg(Thread.CurrentThread.ManagedThreadId, "Pots are clean...");
        }

        private static async Task UseDishWasherAsync(string id)
        {
            int i = 0;
            const int iMax = 9;

            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasher[{id}] - started.");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (i = 0; i < iMax; i++)
                {
                    AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasher[{id}] - washing plate:{i}/{iMax}."); ;
                    Thread.Sleep(_iDelay_ms);
                }
            });
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"UseDishWasher's continuation!  [{id}] - done (All plates washed {i}/{iMax}).");
        }

        private static async Task MakeScrambledEggsAsync(string id)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - started.");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (int i = 0; i < 6; i++)
                {
                    AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync[{id}] - egg:{i}."); ;
                    Thread.Sleep(_iDelay_ms);
                }
            });
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeScrambledEggsAsync's continuation!  [{id}] - done.");
        }

        private static async Task MakeToastAsync(string id)
        {
            AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - started.");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (int i = 0; i < 3; i++)
                {
                    AddMsg(Thread.CurrentThread.ManagedThreadId, $"MakeToastAsync[{id}] - slice:{i}."); ;
                    Thread.Sleep(_iDelay_ms);
                }
            });
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
