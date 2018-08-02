using DQP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace DQPTest
{
    public class UnitTest
    {
        /// <summary>
        /// Test basic tallying of completed work under a batch load.
        /// </summary>
        [Fact]
        public void BatchTest()
        {
            var harness = new DQPTestHarness();
            harness.AddItem(new Item("1", ItemValue.Win));
            harness.AddItem(new Item("1", ItemValue.Win)); // Duplicate should not be processed.
            harness.AddItem(new Item("2", ItemValue.Fail));
            harness.AddItem(new Item("3", ItemValue.Win));
            harness.AddItem(new Item("4", ItemValue.Win));
            harness.AddItem(new Item("5", ItemValue.Succeed));
            harness.AddItem(new Item("6", ItemValue.Fail));
            harness.AddItem(new Item("7", ItemValue.Succeed));
            harness.AddItem(new Item("8", ItemValue.Succeed));
            harness.AddItem(new Item("9", ItemValue.Succeed));

            harness.WaitForCompletion();

            Assert.Equal(2, harness.errors);
            Assert.Equal(3, harness.processed[ItemValue.Win]);
            Assert.Equal(4, harness.processed[ItemValue.Succeed]);

            int expected = 100;
            for (int i = 0; i < expected; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }
            harness.WaitForCompletion();
            Assert.Equal(3 + expected, harness.processed[ItemValue.Win]);
        }

        /// <summary>
        /// Tests a bizarre workload with strange adjustments to the parallelization.
        /// </summary>
        [Fact]
        public void MaliciousTest()
        {
            var harness = new DQPTestHarness();
            int expected = 100;
            harness.Parallelization = 12;
            for (int i = 0; i < expected; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }

            harness.Parallelization = 1;
            for (int i = expected; i < expected * 2; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }

            harness.Parallelization = 0;
            for (int i = expected * 2; i < expected * 3; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }

            for (int i = expected * 10; i < expected * 11; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Fail));
            }

            System.Threading.Thread.Sleep(50);

            for (int i = expected * 3; i < expected * 4; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }
            for (int i = expected * 3; i < expected * 4; i++)
            {
                harness.AddItem(new Item(i.ToString(), ItemValue.Win));
            }
            harness.Parallelization = 1;
            harness.AddItem(new Item("???", ItemValue.Win));

            harness.WaitForCompletion();
            Assert.Equal((expected * 4) + 1, harness.processed[ItemValue.Win]);
            Assert.Equal(expected, harness.errors);
        }

        /// <summary>
        /// Tests tallying of results from a typical somewhat intermittent workload.
        /// </summary>
        [Fact]
        public void TypicalTest()
        {
            var harness = new DQPTestHarness();
            harness.AddItem(new Item("1", ItemValue.Win));
            System.Threading.Thread.Sleep(1);
            harness.AddItem(new Item("2", ItemValue.Fail));
            System.Threading.Thread.Sleep(5);
            harness.AddItem(new Item("3", ItemValue.Win));
            System.Threading.Thread.Sleep(0);
            harness.AddItem(new Item("4", ItemValue.Win));
            System.Threading.Thread.Sleep(0);
            harness.AddItem(new Item("5", ItemValue.Succeed));
            harness.AddItem(new Item("6", ItemValue.Fail));
            System.Threading.Thread.Sleep(5);
            harness.AddItem(new Item("7", ItemValue.Succeed));
            harness.AddItem(new Item("8", ItemValue.Succeed));
            System.Threading.Thread.Sleep(1);
            harness.AddItem(new Item("9", ItemValue.Succeed));

            harness.WaitForCompletion();

            Assert.Equal(2, harness.errors);
            Assert.Equal(3, harness.processed[ItemValue.Win]);
            Assert.Equal(4, harness.processed[ItemValue.Succeed]);
        }

        /// <summary>
        /// Ensures the interface has the correct types.
        /// </summary>
        [Fact]
        public void TypesTest()
        {
            var harness = new DQPTestHarness();
            var a = new ReadOnlyDictionary<string, Item>(new ConcurrentDictionary<string, Item>());
            Assert.Equal(a.GetType(), harness.ItemsQueued.GetType());
            Assert.Equal(a.GetType(), harness.ItemsProcessing.GetType());
        }

        /// <summary>
        /// Tests stalling behavior caused by setting Parallelization to zero before adding items.
        /// </summary>
        [Fact]
        public void StallTest()
        {
            var harness = new DQPTestHarness
            {
                Parallelization = 0
            };

            harness.AddItem(new Item("1", ItemValue.Win));
            harness.AddItem(new Item("2", ItemValue.Win));
            harness.AddItem(new Item("3", ItemValue.Win));

            // No items should be processed after waiting.
            System.Threading.Thread.Sleep(5);
            Assert.Equal(0, harness.processed[ItemValue.Win]);

            // Still no items should be run even with a worker allowed because no workers are started.
            harness.Parallelization = 1;
            System.Threading.Thread.Sleep(5);
            Assert.Equal(0, harness.processed[ItemValue.Win]);

            // After starting a worker items should be complete.
            harness.ManualStartWorker();
            harness.WaitForCompletion();
            Assert.Equal(3, harness.processed[ItemValue.Win]);

            // Cause another stall.
            harness = new DQPTestHarness
            {
                Parallelization = 0
            };
            harness.AddItem(new Item("1", ItemValue.Win));

            // Expecting no items again.
            System.Threading.Thread.Sleep(5);
            Assert.Equal(0, harness.processed[ItemValue.Win]);

            // Adding an item should start a worker.
            harness.Parallelization = 1;
            harness.AddItem(new Item("2", ItemValue.Win));

            // Work should be done after wait.
            harness.WaitForCompletion();
            Assert.Equal(2, harness.processed[ItemValue.Win]);
        }

        /// <summary>
        /// Ensures wait for completion expiration functions as expected.
        /// </summary>
        [Fact]
        public void WaitTest()
        {
            var harness = new DQPTestHarness
            {
                Parallelization = 0 // Start stalled
            };
            harness.AddItem(new Item("1", ItemValue.Win));

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            harness.WaitForCompletion(TimeSpan.FromMilliseconds(20));
            Assert.True(sw.ElapsedMilliseconds > 10);
            Assert.True(harness.ItemsQueued.Count > 0);

            // Enable processing.
            harness.Parallelization = 1;
            harness.ManualStartWorker();

            harness.WaitForCompletion(TimeSpan.FromMinutes(1));
            Assert.True(harness.ItemsQueued.Count == 0);
        }

        /// <summary>
        /// Does a basic test on ActionQueue to ensure it processes items as expected.
        /// </summary>
        [Fact]
        public void ActionQueueTest()
        {
            int success = 0;
            int failure = 0;

            var actionQueue = new ActionQueue<bool>(
                new Action<bool>(x =>
                {
                    if (x) { System.Threading.Interlocked.Increment(ref success); } else { throw new ArgumentException(); }
                }),
                new Action<bool, Exception>((x, ex) =>
                {
                    System.Threading.Interlocked.Increment(ref failure);
                }));

            actionQueue.AddItem(true);
            actionQueue.AddItem(false);
            actionQueue.WaitForCompletion();
            actionQueue.AddItem(true);
            actionQueue.WaitForCompletion();

            Assert.Equal(2, success);
            Assert.Equal(1, failure);
        }
    }
}
