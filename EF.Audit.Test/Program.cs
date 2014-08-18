using System;
using System.Linq;

namespace EF.Audit.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var con = new MyContext())
            {
                var p =  new Person {Age = 10, Name = "Pepe"};
                con.Persons.Add(p);
                p.Dogs.Add(new Dog{Name = "Dinqui"});
                con.SaveChanges();
                var dateTime = DateTime.Now;
                var dog = p.Dogs.FirstOrDefault();
                dog.Name = "Campeon";

                p.Age = 20;
                p.Name = "Pepito el loco";
                con.SaveChanges();

                con.Persons.Remove(p);
                con.SaveChanges();

                //var snapshot = con.GetSnapshot<Dog,MyContext>(dateTime);
                //var log12 = con.AuditLogs.ToList();
                //foreach (var auditLog in log12)
                //{
                //    Console.WriteLine("{0} - {1}", auditLog.Operation, auditLog.Entity);
                //}
                Console.ReadKey();
            }
        }
    }
}
