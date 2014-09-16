using System.Collections.Generic;

namespace FakeEF
{
    public abstract class InMemoryTableBase
    {
        protected static List<InMemoryTableBase> allDatabases = new List<InMemoryTableBase>();



        protected InMemoryTableBase()
        {
            allDatabases.Add(this);
        }

        public static void ClearAll()
        {
            foreach (var item in allDatabases)
            {
                item.Clear();
            }
        }

        public abstract void Clear();
    }
}