# PCR会战 在线分刀系统

* QQ群 1045702612

部署
----
ASP.NET Core发布出来的程序基本上直接启动就对了（Windows和Linux都是），不用安装任何其他软件包。

默认监听本地5000和5001端口，需要对外网开放的话需要设置环境变量（ASPNETCORE_URLS），可以参考[这个链接](https://www.cnblogs.com/rabbityi/p/7020216.html)。当然也可以套反向代理。

简单的使用说明可以参考release下载页面中的pdf和[Github wiki](https://github.com/acaly/PcrBattleChannel/wiki/%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E)。

项目结构
----
* 基本上所有页面都使用的Razor Page，html模板和服务器代码都在Pages文件夹里。
* Layout（包括导航栏）在Views/Shared里。
* 数据库模型在Models文件夹里。
* 比较复杂的计算分离到了Algorithm文件夹。
