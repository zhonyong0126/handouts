using System;
using System.Monads;

namespace MonadsDemo
{
    class MonadsForObjects
    {
        public static void DoDemo()
        {
            var name = "gao";
            if (null != name)
            {
                Console.WriteLine($"[常规] name不为Null, 所以打印name:{name}");
            }
            name.Do(_ => Console.WriteLine($"[Monads] name不为Null, 所以打印name:{_}"));
        }
    }
}