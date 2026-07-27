PVZ104 运动控制 DLL 集成包

目标框架：.NET Framework 4.8

目录说明：
- x64：64 位软件使用
- x86：32 位软件使用

每个平台目录必须整体放到软件运行目录或 DLL 搜索路径中，至少包含：
- PVZ104.dll
- nmc_lib20.dll
- msvcp100.dll
- msvcr100.dll
- MotionModule.json

基础用法：

GTSApi api = new AutoGCApi();
api.GetConfigs(out string[] configs);
api.ApplyConfig("PT-RZ104V01");
api.Connect(new byte[] { 192, 168, 1, 10 });
api.Init(Dimension.Axis01, false, 3000);

配置接口：
- GetConfigs(out string[] itemNumbers)
- ApplyConfig(string itemNumber)
- GetActiveConfig(out string itemNumber)

注意：
- ApplyConfig 必须在 Connect / Init 前调用。
- MotionModule.json 为现场参数配置文件，外发后请按实际型号选择配置。
- PT-RZ104V01 / PT-RZ104H01 当前配置为无正负硬限位、无软限位。
