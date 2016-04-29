### 现象   
NextPms站点CPU利用率长时间保持在100%
### 抓取dump文件
抓取2个Dmp文件, 之间相隔约10s. 使用的工具是Windows自带的"任务管理器".
### 分析
1. 用WinDbg(X64)打开第一个w3wp.DMP文件. 设置好Symbols路径,并load sos.
1. 执行!runaway命令来查看各个线程的执行时间信息. 但在输入!runaway命令回车后, 提示如下错误, 命令无法执行.
    ![](http://7xk5iv.com1.z0.glb.clouddn.com/2016-04-28%2023_04_10-%E5%BC%80%E5%A7%8B.png)
1. 由于!runaway命令无法执行, 只能寻找其它方法. 执行!threads命令列出所有线程, 任意进入一个线程并执行.ttime命令, 可以正常显示.
    ```
    0:104> .ttime
    Created: Wed Apr 27 14:12:59.999 2016 (UTC + 8:00)
    Kernel:  0 days 0:00:00.671
    User:    0 days 0:16:56.625

    ``` 
由于.ttime命令可以看到线程执行的时间, 由于是用~*e .printf "Thread is: 0x%x\n", @@c++(@$teb->ClientId.UniqueThread);.ttime命令打印所有线程的OSID和执行时间.
    ```
    Thread is: 0x145c
    Created: Wed Apr 27 14:21:50.078 2016 (UTC + 8:00)
    Kernel:  0 days 0:00:00.093
    User:    0 days 0:00:00.187
    Thread is: 0x5b4
    Created: Wed Apr 27 14:22:09.702 2016 (UTC + 8:00)
    Kernel:  0 days 0:00:00.015
    User:    0 days 0:00:00.000
    Thread is: 0xcec
    Created: Wed Apr 27 14:22:14.499 2016 (UTC + 8:00)
    Kernel:  0 days 0:00:00.000
    User:    0 days 0:00:00.000
    Thread is: 0x1b0c
    Created: Wed Apr 27 14:22:19.249 2016 (UTC + 8:00)
    Kernel:  0 days 0:00:00.031
    User:    0 days 0:00:00.000

    ```
1. 通过观察以上结果, 确定有104, 173等线程执行时间都在15s以上, 执行!clrstack命令,显示当前CPU正在执行HashSet.Contians方法.
    ```
    0:104> !clrstack
    OS Thread Id: 0x173c (104)
            Child SP               IP Call Site
    000000eca40a9e50 00007ffe70b817dc System.Collections.Generic.HashSet`1[[System.__Canon, mscorlib]].Contains(System.__Canon)
    000000eca40a9ec0 00007ffe552e6040 *** WARNING: Unable to verify checksum for NextPms.Logic.DLL
    NextPms.Logic.Util.CallContextHelper.SetToCallContext[[System.Int32, mscorlib]](Int32, System.String) [c:\BuildAgent\work\7d65603fe91bbc4d\NextPms.Logic\Util\CallContextHelper.cs @ 99]
    000000eca40a9f60 00007ffe552e5fb4 PostSharp.BusinessLogAttribute.set_Depth(Int32) [c:\BuildAgent\work\7d65603fe91bbc4d\NextPms.Logic\Aop\BusinessLogAttribute.cs @ 57]
    000000eca40a9f90 00007ffe552e5269 PostSharp.BusinessLogAttribute.OnEntry(PostSharp.Aspects.MethodExecutionArgs) [c:\BuildAgent\work\7d65603fe91bbc4d\NextPms.Logic\Aop\BusinessLogAttribute.cs @ 134]

    ```
1. 查看w3wp(2).dmp文件的104号线程, 确定当前的stack还在是HashSet.Contains方法中.
1. 查看所有线程, 发现有很多线程都是执行HashSet.Contains方法.
1. 通过查看源代码, 发现一个HashSet类型的变量是静态的, 但在操作时并保证线程安全.

### 结论
多线程环境中, 对HashSet的非线程安全操作容易导致死循环, 从而造成CPU利用率高.参考[High CPU in .NET app using a static Generic.Dictionary](https://blogs.msdn.microsoft.com/tess/2009/12/21/high-cpu-in-net-app-using-a-static-generic-dictionary)
