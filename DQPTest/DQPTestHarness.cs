using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DQP;

namespace DQPTest
{
    class Item
    {
        public readonly ItemValue Value;
        public readonly string Name;

        public Item(string name, ItemValue value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => Name;
    }

    enum ItemValue
    {
        Win = 0,
        Succeed = 1,
        Fail = 2
    }

    class DQPTestHarness : DistinctQueueProcessor<Item>
    {
        private readonly object locker = new object();
        public Dictionary<ItemValue, int> processed;
        public int errors;

        readonly Random random = new Random();

        public new int Parallelization
        {
            get
            {
                return base.Parallelization;
            }
            set
            {
                base.Parallelization = value;
            }
        }

        public DQPTestHarness()
        {
            Parallelization = 4;
            processed = new Dictionary<ItemValue, int>(((ItemValue[])Enum.GetValues(typeof(ItemValue))).Select(x => new KeyValuePair<ItemValue, int>(x, 0)));
        }

        public new void ManualStartWorker()
        {
            base.ManualStartWorker();
        }

        protected override void Error(Item item, Exception ex)
        {
            lock (locker)
            {
                errors++;
            }
        }

        protected override void Process(Item item)
        {
            RandomWait();
            if (item.Value == ItemValue.Fail) { throw new ArgumentException("Item.Fail will always throw an exception on processing."); }
            lock (locker)
            {
                processed[item.Value]++;
            }
        }

        private void RandomWait()
        {
            System.Threading.Thread.SpinWait(random.Next(50, 10000));
        }
    }
}
