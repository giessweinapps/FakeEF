using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FakeEF
{
    public abstract class InMemoryTableBase
    {
        protected static List<InMemoryTableBase> allDatabases = new List<InMemoryTableBase>();

        public IEnumerable<InMemoryTableBase> AllDatabases { get { return allDatabases.AsEnumerable(); } } 

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

        public abstract IEnumerable GetData();
    }
}