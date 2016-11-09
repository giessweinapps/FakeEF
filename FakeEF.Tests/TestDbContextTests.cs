using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FakeEF.Tests.TestDatabase;
using NUnit.Framework;

namespace FakeEF.Tests
{
    [TestFixture]
    public class Mytests
    {
        [SetUp]
        public void Setup()
        {
            InMemoryTableBase.ClearAll();
        }

        [Test]
        public void QueryNestedPropertyWithProxy()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void QueryNestedPropertyWithNoProxy()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(0));
            }
        }


        [Test]
        public void QueryNoProxyEnsureId()
        {
            int id = 0;
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
                id = p1.Addresses.FirstOrDefault().Id;
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();
                var p = ctx.Persons.Include(x => x.Addresses).FirstOrDefault();
                Assert.That(p.Addresses.FirstOrDefault().Id, Is.EqualTo(id));
            }
        }

        [Test]
        public void QueryNestedPropertyWithNoProxyAndInclude()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                p1.Addresses.Add(new Adresse()
                {
                    Name = "Myaddress"
                });
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.Addresses).FirstOrDefault();
                Assert.That(p.Addresses.Count, Is.EqualTo(1));
            }
        }
        [Test]
        public void QueryNestedPropertyWithNoProxyAndIncludeOneLevel()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person
                {
                    Name = "Hallo1",
                    ChildPerson = new Person
                    {
                        Name = "Hallo2",
                        ChildPerson = new Person
                        {
                            Name = "Hallo3"
                        }
                    }
                };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.ChildPerson).FirstOrDefault();
                Assert.That(p.ChildPerson, Is.Not.Null);
            }
        }
        [Test]
        public void QueryNestedPropertyWithNoProxyAndIncludeTwoLevel()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person
                {
                    Name = "Hallo1",
                    ChildPerson = new Person
                    {
                        Name = "Hallo2",
                        Addresses = new List<Adresse>()
                        {
                          new Adresse()
                          {
                              Name = "Adr"
                          }  
                        },
                        ChildPerson = new Person
                        {
                            Name = "Hallo3"
                        }
                    }
                };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTestDbContext())
            {
                ctx.Configuration.ProxyCreationEnabled = false;
                ctx.SetupAsTestDbContext();

                var p = ctx.Persons.Include(x => x.ChildPerson.Addresses).FirstOrDefault();
                Assert.That(p.ChildPerson.Addresses.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void AddTwoUniqueEntries()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Hallo" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p2 = new Person { Name = "Jürgen" };
                ctx.Persons.Add(p2);
                ctx.SaveChanges();

                Assert.That(p1.Id, Is.EqualTo(1));
                Assert.That(p2.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void UseTwoDifferentContexts()
        {
            using (var ctx1 = new MyTestDbContext())
            {
                ctx1.SetupAsTestDbContext();

                var p1 = new Person { Name = "Jürgen" };
                p1.Addresses.Add(new Adresse { Name = "AlterWert1" });
                p1.Addresses.Add(new Adresse { Name = "AlterWert2" });
                ctx1.Persons.Add(p1);
                ctx1.SaveChanges();
            }

            using (var ctx2 = new MyTestDbContext())
            {
                ctx2.SetupAsTestDbContext();
                var persons = ctx2.Persons;

                Assert.AreEqual(2, persons.First().Addresses.Count());
            }

            Person p2;
            using (var ctx3 = new MyTestDbContext())
            {
                ctx3.SetupAsTestDbContext();
                p2 = ctx3.Persons.FirstOrDefault(x => x.Id == 1);
                p2.Name = "ÜberschriebenerWert";
                p2.Addresses.First().Name = "Hallo";
            }

            using (var ctx4 = new MyTestDbContext())
            {
                ctx4.SetupAsTestDbContext();
                ctx4.Entry(p2).State = EntityState.Modified;
                ctx4.SaveChanges();
            }

            using (var ctx5 = new MyTestDbContext())
            {
                ctx5.SetupAsTestDbContext();
                var p3 = ctx5.Persons.FirstOrDefault(x => x.Id == 1);


                Assert.That(p2, Is.Not.EqualTo(p3));
                Assert.That(p3.Name, Is.EqualTo("ÜberschriebenerWert"));
                Assert.That(p3.Addresses.First().Name, Is.EqualTo("AlterWert1"));
            }
        }

        [Test]
        public void Performance()
        {
            for (int i = 0; i < 100; i++)
            {
                using (var ctx5 = new MyTestDbContext())
                {
                    ctx5.SetupAsTestDbContext();
                    var p3 = ctx5.Persons.FirstOrDefault(x => x.Id == 1);
                }
            }
        }

        [Test]
        public void FindMethod()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Jürgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p = ctx.Persons.Find(1);
                Assert.That(p.Name, Is.EqualTo(p1.Name));
            }
        }

        [Test]
        public void DeleteItem()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var p1 = new Person { Name = "Jürgen" };
                ctx.Persons.Add(p1);
                ctx.SaveChanges();

                var p = ctx.Persons.FirstOrDefault(x => x.Id == 1);
                ctx.Persons.Remove(p);
                ctx.SaveChanges();

                Assert.That(ctx.Persons.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void AddItemWithRequiredParentAndExpectException()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                ctx.Adresses.Add(new Adresse
                {
                    Name = "test",
                    Person = null //Das dar nicht sein!
                });
                Assert.Throws<DbEntityValidationException>(() => ctx.SaveChanges());
            }
        }

        [Test]
        public void TestJoinOperation()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();
                var person = new Person
                {
                    Name = "Eintrag"
                };

                foreach (var adr in Enumerable.Range(0, 10).Select(x => new Adresse { Name = x.ToString() }))
                    person.Addresses.Add(adr);

                ctx.Persons.Add(person);
                ctx.SaveChanges();

                var query = from p in ctx.Persons
                            from a in ctx.Adresses
                            where p.Id == a.Id
                            select a;

                Assert.That(ctx.Persons.Count(), Is.EqualTo(1));
                Assert.That(ctx.Adresses.Count(), Is.EqualTo(10));
                Assert.That(query.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void GetItemTwice()
        {
            using (var ctx = new MyTestDbContext())
            {
                ctx.SetupAsTestDbContext();

                var person = new Person
                {
                    Name = "Eintrag"
                };
                ctx.Persons.Add(person);
                ctx.SaveChanges();

                var dbEntry = ctx.Persons.FirstOrDefault(x => x.Id == 1);
                Assert.That(person, Is.SameAs(dbEntry));
                Assert.That(person.Id, Is.EqualTo(dbEntry.Id));
            }
        }
    }
}