CLR集合
==========
CLR中所有集合是在System.Collections命名空间里面，该命名空间中提供一些集合的基类，同时有如下几个命名空间:  

* System.Collections.Generic  该命名空间提供常用的范型集合，如List,HashSet,Dictionary
* System.Collections.Specialized 该命名空间提供一些特殊的集合，如NameValueCollection,BitVector32
* System.Collections.Concurrent 该命名空间提供线程安全的集合，如ConcurrentDictionary,ConcurrentBag
* System.Collections.ObjectModel 该命名空间提供一些集合，这些集合可以用来作为Property或Method的返回值, 如Collection,KeyedCollection,ReadOnlyCollection

**分类**   

* 按存储的数据结构分类，可以分为单值结构和KeyValuePair结构，如果List是存储单值结构的，Dictionary是存储KeyValuePair结构的。
* 按线程安全分类，可以分为线程安全和非线程安全集合。如List是非线程安全的，ConcurrentBag是线程安全的。   
各个集合有各自己不同的特性和用途，使用时需要结合实际的业务场景来选择合适的集合。

**List**   
List是一个使用频率非常高的单值结构的泛型集合，通过Add等方法动态添加元素。List的基本实是，内部使用一个数组来存储元素。当通过Add等方法添加元素时，首先检查这个数组是有否足够的容量来存储新添加的元素，如果数组空间不足，则通过扩容机制来增加数组的长度。  
List通过下列几个骤来完成扩容：   

1. 确定新数组的长度L(如何确新数组长度：如果当前长度为0，新数组的长度为4，否则新数组的长度等于当前长度的2倍);  
2. 声明一个长度为L的新数组;   
3. 调用Array.Copy方法将原数组的元素复制到新声明的数组。    

支持下标访问。 List提供了Indexer(索引索器)，可以像组一样通过下标来访问元素。 
```C#
    //数组可以通过下标访问元素
    var intArray=new int[](1,2,3,4,5);
    var lastItem=intArray[4];

    //List提供了索引属性，可以像数组一样通过下标访问元素
    var intList=new List<int>(){1,2,3,4,5};
    var lastItemInList=intList[4];
```
可以添加重复元素。

> System.Collections命名空间提供一个ArrayList的集合，该集合是在List泛型集合出来以前使用的，是非泛型的List集合。现代的代码建议使用List而不是ArrayList。  

**HashSet**   
HashSet也是一个泛型集合，也提供Add方法动态添加元素。HashSet内部主要使用一个int数组(m_buckets)、嵌套类Slot和Slot数组(m_slots)来实现。   
HashSet的基本实现原理: