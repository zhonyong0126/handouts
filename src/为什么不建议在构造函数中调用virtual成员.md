现象
=======
为什么不建议在构造函数中调用virtual成员呢？这个问题各位平时在写代码的时候是否留意过？让我们来看一段代码：
```c#
    abstract class BaseClass
    {
         protected BaseClass()
         {
             Initialize();
         }

        protected abstract void Initialize();
    }

    class SubClass:BaseClass
    {
        private readonly List<string> _items;
        public SubClass()
        {
            _items = new List<string>();

        }
        protected override void Initialize()
        {
            _items.Add("item1");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var sub=new SubClass();
        }
    }
```
上述是一段看似很平常的代码，那么SubClass.Initialize()方法在运行期间会在_items.Add("item1")处抛出NullReferenceException吗？    
答案是肯定的。因为，_items字段此时是_null。如下图：    
![NullReferenceException](http://7xk5iv.com1.z0.glb.clouddn.com/cnblogs-001-3.png)      

另外，如果你安装Resharper插件, 也会得到它的相关提示，如下图：  
![Resharper Prompt](http://7xk5iv.com1.z0.glb.clouddn.com/cnblogs-001-2.png)   

这里，肯定有人会问_items字段不是在SubClass的构造函数中被初始化了吗？为什么会是null呢？   

分析
=======
现在我们先来一起分析一下上面这段代码：
1. BaseClass的构造函数中调了自已定义的一个Initialize方法，且标记为abstract的，因此在子类中必须用override来重载实现它。
2. SubClass继承了BaseClass，且实现了Initialize方法。在Initialize方法中调用_items.Add方法。
3. SubClass在构造函数中对_items进行初始化和赋值。    

从上面的代码，我们很难一下发现异常原因。

既然从C#代码不能有所发现，那么我们就从IL代码入手。

1. 在VS中打开Msiler（一款VS的插件，文章后面有介绍），并编译上面的代码。
2. 在Msiler窗口中找到SubClass的构造函数对应的IL代码。如下图:   
![SubClass.ctor](http://7xk5iv.com1.z0.glb.clouddn.com/cnblogs-001-1.png)   
在上图中，我们可以清晰的看到。在SubClass的构造函数中，首先执行“call     System.Void ConsoleApplication3.BaseClass::.ctor()”,然后执行“newobj   System.Void System.Collections.Generic.List`1<System.String>::.ctor()”。    
BaseClass::.ctor()正是BaseClass的构造函数，在BaseClass的构造函数调用了Initialize方法，而这时调用是在SubClass中实现的Initialize方法，此时_items=new List<string>()代码还没有被执行。
导致了SubClass.Initialize方法在_items=new List<string>()代码之前被执行了，这也正是为上面的代码会抛出NullReferenceException的原因。

总结
=======
在C#中，构造函数执行顺序是这样的。在子类的构造函数内第一行代码即是调用它基类的构造函数，之后才执行子类构造函数中的其它代码。如果在基类构造函数调用virtual/abstract成员，这些成员可能会被子类override，且在子类中方法可能会访问在子类构造函数初始化的字段，因此会造成在子类成员未初始化的情况被调用。

关于Msiler插件
==============
Msiler插件可以在VS中直接查看某段代码生成的IL代码，而不需要额外通过ILSpy之类的工具打开再查看，比较方便。目前支持Visual Studio 2012, 2013, 2015。
可以在VS的Extension中找到，也可以点击链接直接下载[https://marketplace.visualstudio.com/items?itemName=segrived.Msiler](https://marketplace.visualstudio.com/items?itemName=segrived.Msiler)

