# ActionKit



由 QFramework 团队官方维护的独立工具包（不依赖 QFramework）。

## 环境要求

* Unity 2018.4LTS

## 安装

* PackageManager
    * add from package git url：https://github.com/liangxiegame/ActionKit.git 
    * 或者国内镜像仓库：https://gitee.com/liangxiegame/ActionKit.git
* 或者直接复制[此代码](ActionKit.cs)到自己项目中的任意脚本中



## 快速开始

* chainning style(Driven by MonoBehaviour or Update)

``` csharp
this.Sequence()
	.Delay(1.0f)
	.Event(()=>Log.I("Delayed 1 second"))
	.Until(()=>something is done)
	.Begin();
```

* object oriented style

``` csharp
var sequenceNode = new SequenceNode();
sequenceNode.Append(DelayAction.Allocate(1.0f));
sequenceNode.Append(EventAction.Allocate(()=>Log.I("Delayed 1 second"));
sequenceNode.Append(UntilAction.Allocate(()=>something is true));

this.ExecuteNode(sequenceNode);
```



## 更多

* QFramework 地址: https://github.com/liangxiegame/qframework
