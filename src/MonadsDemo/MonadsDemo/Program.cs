using System;
using System.Collections.Generic;
using System.Linq;
using System.Monads;
using System.Text;
using System.Threading.Tasks;

namespace MonadsDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            MonadsForObjects.DoDemo();

            var data = new Dictionary<int, string>();
            //data.Add(1, "My Name");
            //data.With(1).Do(_ => Console.WriteLine($"已找到Key为1的结果：{_}"));
            data.With(1).Return(_ => $"已找到Key为1的结果：{_}", "未找到Key为1的结果").Do(_ => Console.WriteLine(_));
           data.Return(1, _ =>
           {
               Console.WriteLine("Key1不存在，添加Key1");
               _.
           })
            //data.TryWith(1)
            Console.Read();
        }
    }
}
