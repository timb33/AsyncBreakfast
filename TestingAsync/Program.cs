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


        static void Main(string[] args)
        {
            Foo2();

            #region hide
            //Console.WriteLine("both are now running, but same 1111");
            //await Test1Async("a"); //start thread/Task now. thread is blocked.
            //await Test2Async("b"); //start thread/Task now. thread is blocked.
            //Console.WriteLine("longer hand, but same 22222");

            //Console.WriteLine("longer hand, but same thing");
            //var tskA1 = Test1Async("a");//this starts the thread without blocking, and returns a handle to the thread
            //Console.WriteLine("21");
            //var tskB1 = Test2Async("b");//this starts the thread without blocking
            //Console.WriteLine("23");
            //await tskA1;//do this,
            //Console.WriteLine("25");
            //await tskB1;//then this
            //Console.WriteLine("27");

            //Console.WriteLine("Now run both at same time");
            //var tskA = Test1Async("a"); //setup and start a thread/Task. return handle. run async.
            //var tskB = Test2Async("b"); //setup and start a thread/Task

            //await Task.WhenAll(tskA, tskB);//This is synchonisation/join
            ////var tskC = Task.WhenAll(tskA, tskB);//this is a third task, which completes when the other two complete. A task completes when it returns, not when it's finished doing its job 
            //Console.WriteLine("wait for everything to finish");
            #endregion

            Console.Read();
        }

        private static void Foo2()
        {
            const int iWait_ms = 5000;

            UseDishWasher("pre-breakfast").Wait(iWait_ms); //this is now sync. Timeout is short so we only wash some of the plates
            AddMsg("Pots are probably clean. The longest we allow the dishwasher to run is 5s.");

            //start breakfast...
            var t1 = MakeScrambledEggsAsync("eggz");
            var t2 = MakeToastAsync("tost");

            var tasks = new Task[] {
                t1,t2
            };

            Task.WhenAll(tasks).Wait(); //wait for both breakfast tasks to complete

            //breakfast eaten...
            UseDishWasher("post-breakfast").Wait(); //this is now sync. no timeout so we guarantee cleaning cycle finishes.
            AddMsg("Pots are clean...");
        }

        const int iDelay_ms = 1000;

        private static async Task UseDishWasher(string id)
        {
            Console.WriteLine($"UseDishWasher[{id}] - started. tid:{Thread.CurrentThread.ManagedThreadId}");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine($"UseDishWasher[{id}] - washing plate:{i}. tid:{Thread.CurrentThread.ManagedThreadId}"); ;
                    Thread.Sleep(iDelay_ms);
                }
            });
            Console.WriteLine($"this is the continuation! UseDishWasher [{id}] - done (Pots clean). tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        private static async Task MakeScrambledEggsAsync(string id)
        {
            Console.WriteLine($"MakeScrambledEggsAsync[{id}] - started. tid:{Thread.CurrentThread.ManagedThreadId}");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (int i = 0; i < 6; i++)
                {
                    Console.WriteLine($"MakeScrambledEggsAsync[{id}] - egg:{i}. tid:{Thread.CurrentThread.ManagedThreadId}"); ;
                    Thread.Sleep(iDelay_ms);
                }
            });
            Console.WriteLine($"this is the continuation! MakeScrambledEggsAsync [{id}] - done. tid:{Thread.CurrentThread.ManagedThreadId}");
        }


        private static async Task MakeToastAsync(string id)
        {
            Console.WriteLine($"MakeToast[{id}] - started. tid:{Thread.CurrentThread.ManagedThreadId}");
            //_ = Task.Delay(10); //pause calling thread
            await Task.Run(() => //run this thread/Task now
            {
                for (int i = 0; i < 7; i++)
                {
                    Console.WriteLine($"MakeToast[{id}] - slice:{i}. tid:{Thread.CurrentThread.ManagedThreadId}"); ;
                    Thread.Sleep(iDelay_ms);
                }
            });
            Console.WriteLine($"this is the continuation! MakeToast [{id}] - done. tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        private static void AddMsg(string v)
        {
            Console.WriteLine(v);
        }
    }
}
