
Connection Pool达到最大限制问题调查
=========================================

1. 使用DebugDiag设定规则, 抓取Dump文件
1. 用WinDbg打开Dump文件, 设置Symbols路径, 并加载SOS扩展
1. 查看Exception详细信息
    ```
    0:038> !PrintException 000000ebe4806eb8
    Exception object: 000000ebe4806eb8
    Exception type:   System.InvalidOperationException
    Message:          Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.
    InnerException:   <none>
    StackTrace (generated):
        SP               IP               Function
        000000ECAEF7BB70 00007FF848EE7FCD System_Data_ni!System.Data.ProviderBase.DbConnectionFactory.TryGetConnection(System.Data.Common.DbConnection, System.Threading.Tasks.TaskCompletionSource`1<System.Data.ProviderBase.DbConnectionInternal>, System.Data.Common.DbConnectionOptions, System.Data.ProviderBase.DbConnectionInternal, System.Data.ProviderBase.DbConnectionInternal ByRef)+0x64c94d
        000000ECAEF7BC30 00007FF848EE7F17 System_Data_ni!System.Data.ProviderBase.DbConnectionInternal.TryOpenConnectionInternal(System.Data.Common.DbConnection, System.Data.ProviderBase.DbConnectionFactory, System.Threading.Tasks.TaskCompletionSource`1<System.Data.ProviderBase.DbConnectionInternal>, System.Data.Common.DbConnectionOptions)+0x64cb47
        000000ECAEF7BC90 00007FF848898FD9 System_Data_ni!System.Data.SqlClient.SqlConnection.TryOpenInner(System.Threading.Tasks.TaskCompletionSource`1<System.Data.ProviderBase.DbConnectionInternal>)+0xe9
        000000ECAEF7BD30 00007FF848898E96 System_Data_ni!System.Data.SqlClient.SqlConnection.TryOpen(System.Threading.Tasks.TaskCompletionSource`1<System.Data.ProviderBase.DbConnectionInternal>)+0x116
        000000ECAEF7BDA0 00007FF84889899F System_Data_ni!System.Data.SqlClient.SqlConnection.Open()+0xef
        000000ECAEF7BE10 00007FF7EFCDA43F EntityFramework!System.Data.Entity.Infrastructure.Interception.InternalDispatcher`1[[System.__Canon, mscorlib]].Dispatch[[System.__Canon, mscorlib],[System.__Canon, mscorlib]](System.__Canon, System.Action`2<System.__Canon,System.__Canon>, System.__Canon, System.Action`3<System.__Canon,System.__Canon,System.__Canon>, System.Action`3<System.__Canon,System.__Canon,System.__Canon>)+0xcf
        000000ECAEF7BF20 00007FF7EFCDA276 EntityFramework!System.Data.Entity.Infrastructure.Interception.DbConnectionDispatcher.Open(System.Data.Common.DbConnection, System.Data.Entity.Infrastructure.Interception.DbInterceptionContext)+0x1b6
        000000ECAEF7BFA0 00007FF7EFCD90BF EntityFramework_SqlServer!System.Data.Entity.SqlServer.DefaultSqlExecutionStrategy+<>c__DisplayClass1.<Execute>b__0()+0xf
        000000ECAEF7BFD0 00007FF7EFCD9067 EntityFramework_SqlServer!System.Data.Entity.SqlServer.DefaultSqlExecutionStrategy.Execute[[System.__Canon, mscorlib]](System.Func`1<System.__Canon>)+0x107
        000000ECAEF7C040 00007FF7EFFA0434 EntityFramework!System.Data.Entity.Core.EntityClient.EntityConnection.Open()+0x1b4

    StackTraceString: <none>
    HResult: 80131509

    ```
    异常Message:Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached. 是说从Connection Pool里获取Connection超时, 连接数达到上限. 据此分析应该是有大量Connection连接没有被Close.
1. 在托管堆上查找SqlConnection对象  
   要准确统计SqlConnection对象的数量, 首先找出SqlConnection类型的MethodTable

    ```
   0:038> !Name2EE *!System.Data.SqlClient.SqlConnection
    Module:      00007ff848731000
    Assembly:    System.Data.dll
    Token:       0000000002000283
    MethodTable: 00007ff84890e958
    EEClass:     00007ff8487582d8
    Name:        System.Data.SqlClient.SqlConnection
    --------------------------------------

    ```
    统计托管堆上的SqlConnection对象
    ```
    0:038> !DumpHeap -stat -MT 00007ff84890e958
    Statistics:
                  MT    Count    TotalSize Class Name
    00007ff84890e958      711       147888 System.Data.SqlClient.SqlConnection
    Total 711 objects

    ```
    此时堆上有711个SqlConnection对象, 再确认一下有多少SqlConnection对象是Open状态
    ```
    0:038> .foreach(tarConn {!DumpHeap -short -MT 00007ff84890e958}){!do poi(${tarConn}+a0);}
    Name:        System.Data.SqlClient.SqlInternalConnectionTds
    MethodTable: 00007ff8489172f0
    EEClass:     00007ff84877ee50
    Size:        328(0x148) bytes
    File:        C:\Windows\Microsoft.Net\assembly\GAC_64\System.Data\v4.0_4.0.0.0__b77a5c561934e089\System.Data.dll
    Fields:
                  MT    Field   Offset                 Type VT     Attr            Value Name
    00007ff84c7c3980  40008bd       38         System.Int32  1 instance               33 _objectID
    00007ff84c7c8c48  40008c0       44       System.Boolean  1 instance                0 _allowSetConnectionString
    00007ff84c7c8c48  40008c1       45       System.Boolean  1 instance                1 _hidePassword
    00007ff848910560  40008c2       3c         System.Int32  1 instance                1 _state

    ```
    注意_state字段的value为1对象, 经过统计有200个SqlConnection对象是Open状态的
    ![ff](http://7xk5iv.com1.z0.glb.clouddn.com/2016-04-20%2017_01_12-Store.png)
1. 查看SqlConnection对象的引用根
    ```
    0:038> .foreach(tarConn {!DumpHeap -short -MT 00007ff84890e958}){!gcroot ${tarConn};}
    Found 0 unique roots (run '!GCRoot -all' to see all roots).
    Found 0 unique roots (run '!GCRoot -all' to see all roots).
    Found 0 unique roots (run '!GCRoot -all' to see all roots).
    Found 0 unique roots (run '!GCRoot -all' to see all roots).
    Found 0 unique roots (run '!GCRoot -all' to see all roots).

    ```
    发现大量的SqlConnection对象没有引用根, 也就说这些对象是可以回收的.
1. 查FinalizeQueue中的SqlConnection(注: CLR中对于实现的析构函数的类型, 在创建其实例会在FinalizeQueue队列中增加该对象引用)
    在FinalizeQueue中发现有602百多个SqlConnection对象, 表明这些对象没有调用过其Dispose方法(注:一般在Dispose中应该调用GC.SupressFinalize()方法, 用于将对象在FinalizeQueue中移除)
    ```
    0:038> !FinalizeQueue
    
    00007ff7ef639ac8      601        81736 System.Data.Entity.Core.EntityClient.EntityConnection
    00007ff7ef6392f8      542        91056 System.Data.Entity.Core.EntityClient.EntityCommand
    00007ff8454dd6f8     2519       100760 System.Web.ApplicationImpersonationContext
    00007ff7ef6b91f8      602       110768 System.Data.Entity.Core.Objects.ObjectContext
    00007ff84890e958      602       125216 System.Data.SqlClient.SqlConnection

    ```
1. 通过检查找码, 发现的确有很多方法中间接的创建了SqlConnection但并未调用Dispose方法来释放, 修复后问题解决.