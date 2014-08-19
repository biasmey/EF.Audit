using System;
using System.Linq;
using EF.Audit;
using EF.Audit.Test;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var con = new MyContext())
            {

                var p = new Person { Age = 10, Name = "Pepe" };
                con.Persons.Add(p);
                p.Dogs.Add(new Dog { Name = "Dinqui" });
                con.SaveChangesAndAudit();

                var dateTime = DateTime.Now;
                var dog = p.Dogs.FirstOrDefault();
                dog.Name = "Campeon";

                p.Age = 20;
                p.Name = "Pepito el loco";
                con.SaveChangesAndAudit();

                //con.Persons.Remove(p);
                //con.SaveChangesAndAudit();

                var snapshot = con.GetSnapshot<Dog, MyContext>(dateTime, con.GetEntityKey(dog));
                var xx = snapshot.ToList();

                var history = con.GetHistory<Person, MyContext>(DateTime.Now, con.GetEntityKey(p));
                var yy = history.ToList();
                var log12 = con.AuditLogs.ToList();
                foreach (var auditLog in log12)
                {
                    Console.WriteLine("{0} - {1}", auditLog.Operation, auditLog.Entity);
                }

                Console.WriteLine("Done!!!");
                Console.ReadKey();
            }
        }
    }
}
