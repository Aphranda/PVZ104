# GTS Standardization API (C# version)

- Author: General Test System Inc.
- Version: v0.0.4.1
- Date:  2022.06.07

[toc]



## 引言

在GTS硬件中有多个组件要控制: 转台，链路控制箱，仪表，搅拌器，被测件等。这将由“**Turntable**”，“**LinkCtrlBox**”，”**Instruments**“(暂未实现)，”**blender**“(暂未实现)，”**DUT**“(暂未实现)类分别管理。

## 0. 全局定义

这个类提供了控制转台的实现函数。  

#### 0.1 维度

转台支持的维度

```c#
public enum Dimension
{
    /// <summary>
    /// 以竖直轴Z轴为中心轴，进行旋转运动。
    /// </summary>
    Axis01 = 0,
    /// <summary>
    /// 以水平轴X/Y轴为旋转轴，进行旋转运动
    /// </summary>
    Axis02 = 1,
    /// <summary>
    /// 以X轴为旋转中心，进行平移运动
    /// </summary>
    Axis03 = 2,
    /// <summary>
    /// 以Y轴为旋转中心，进行平移运动
    /// </summary>
    Axis04 = 3,
    /// <summary>
    /// 以Z轴为旋转中心，进行平移运动
    /// </summary>
    Axis05 = 4,
}
```

#### 0.2 错误码

所有函数的返回值都是枚举类型' **E_RESULT** '的值，这实际上是一个错误代码，指示操作是否成功。

“0”表示成功，而其他每个正整数都是一个明显的错误。错误码定义如下:

```c#
public enum E_RESULT
{
    //General status feedback
    E_SUCCESS = 0,							// Success.
    E_FAILED = 1,							// General Failure.
    
    //IO status feedback
    E_IO_EXCEPTION = 20001,					// An I/O error occurs.
    E_PATH_TOO_LONG = 20002,				// A path or fully qualified file name is longer than the system-defined maximum length.
    E_FILE_NOT_FOUND = 20003,				// An attempt to access a file that does not exist on disk fails.
    E_INVALID_ARGUMENT = 20004,				// when a null reference is passed to a method that does not accept it as a valid argument.
    E_ARGUMENT_OUT_OF_RANGE = 20005,		// The value of an argument is outside the allowable range of values.
    E_ABNORMAL_INPUT_FILE = 20006,			// The format or content of the input file is abnormal.  
    
    //Communication status feedback
    E_INVALID_SERIALPORT = 30001,			// Invalid or nonexisted COM port.
    E_ALREADY_DISCONNECTED = 30002,			// The hardware has been disconnected.
    E_TIMEOUT = 30003,						// Timed out. Unable to complete the action or function within the specified time.
    E_BAD_TIMEOUT = 30004,					// The timeout time is less than the time estimated inside the function.
    E_SERIALPORT_TIMEOUT = 30005,			// The operation of the serial port did not complete before the timeout period ended, Or No bytes were read.
    E_ABNORMAL_SERIALPORT = 30006,			// Serial communication is abnormal.
    
    E_ALREADY_CONNECTED = 30007,
    //Authority status feedback
    E_UNAUTHORIZED_ACCESS = 40001,			// Insufficient permissions to access the file or port, the file or port may be occupied.
    E_NO_DONGLE = 40002,					// The driver of the dongle is not installed or the dongle is not inserted.
    E_UNAUTHORIZED = 40003,					// Unauthorized function.
    E_EXPIRED_LICENSE = 40004,				// The authorized function has expired. 
    
    //Function implementation status feedback
    E_NotImplement_Function = 50001,		// The function is not implemented.
    
    //Others
    E_UNDEFINED = 99999						// Undefined or unknown error.

}
```

**Note:**

> 这里只定义已知的返回结果来反馈函数异常或错误的原因。  
>
> 对于未定义或未知的异常，可以参加函数补充返回布尔值或字段。 



## 1. Class `Turntable`

#### 1.1 连接

在发送控制命令之前，必须先连接。 通过IP地址和端口暴露,运动控制器通过DevHandle自行查询。  

```c#
public E_RESULT Connect(string ipAddress, int port) //PLC控制需求IP地址
public E_RESULT Connect() // 运动控制器自己查询DevHandle
```

**parameter:**

- `ipAddress`: 转台的IP地址，例如： `"192.168.1.10"`
- `port`: 转台的IP端口，例如： `4998`

**Returns:**

- A value of the enumeration type `E_RESULT`.

#### 1.2 断开连接

```c#
public E_Result DisConnect()
```

**Returns:**

- A value of the enumeration type `E_Result`.


#### 1.3 检查转台状态

在连接到转台成功后，或控制转台执行任何功能前，**建议使用一次此方法检查转台状态是否正常（有无报警等）**

```c# 
public E_Result GetStatus(out enum E_Turntable_Status, out int alarmId)
```

**Parameter：**

- `E_Turntable_Status`: 查询/并获取到的转台状态，通过Flag枚举表示，状态码定义如下:

  ```C#
  [System.Flags]
  public enum E_Turntable_Status
  {
      alarm = 0,
      moving = 1,
      ready = 2,
      stop = 3,
  }
  ```
- `alarmId`:查询/并获取到转台错误吗，返回int16

```C#
[System.Flags]
[ 0000 0000 0000 0000 ]== [驱动器报警，限位报警，超时报警，相对定位报警，绝对定位报警，原点复位报警，复位报警，使能报警，未连接报警，保留字~]
```
  
**Returns:**

- A value of the enumeration type `E_Result`.


#### 1.4 转台初始化

在连接到转台成功后，控制转台执行功能之前，必须使用此方法进行转台指定的参数初始化。如不需要设置特定参数，此方法可为空方法直接返回E_RESULT = E_SUCCESS

```c# 
public E_RESULT Init(enum dimension, bool servo, Uint32 ServoResetTimeDelay)
```

**Parameter：**

- `dimension`:设置是转台的哪个维度进行复位
- `servo`:设置是否进行伺服初始化 True确认伺服初始化
- `ServoResetTimeDelay`:伺服驱动器初始化时间(`ms`)


**Returns:**

- A value of the enumeration type `E_RESULT`.


#### 1.5 转台寻零

在连接到转台成功后，通过此方法执行在合理的超时时间内对转台的**物理零点寻零**逻辑

##### 15.1 单轴寻零

对单轴进行带偏置的原点复位

```c#
public E_RESULT Home(enum dimension, double speed, double offset, int timeout = -1)
```

**parameter:**

- `dimension`:设置是转台的哪个维度寻零

- `speed`: 设置转台转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
  
- `offset`: 设置转台原点复位时的偏移值. 范围: `(?)`. 旋转单位 `°`. 平移单位`mm`

- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法要在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' E_BAD_TIMEOUT '错误。可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.

##### 1.5.2 单轴快速回零

对于无限旋转的单轴进行快速回零

  ```C#
 public static E_Result Reset(enum dimension, double hightSpeed, double lowSpeed, double offset, int timeout=-1)
  ```

**parameter:**

- `dimension`:设置是转台的哪个维度寻零

- `hightSpeed`: 设置转台高速转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`

- `lowSpeed`: 设置转台低速转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
  
- `offset`: 设置转台原点复位时的偏移值. 范围: `(?)`. 旋转单位 `°`. 平移单位`mm`

- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法要在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' E_BAD_TIMEOUT '错误。可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.
  
##### 1.5.3 多轴同时回零
对多轴同时进行原点复位操作

```C#
public static E_Result HomeAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1)
```

**parameter:**

- `dimensions`:设置是转台回零的数组

- `speed`: 设置转台低速转动或平移速度数组. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
  
- `offset`: 设置转台原点复位时的偏移值数组. 范围: `(?)`. 旋转单位 `°`. 平移单位`mm`

- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法要在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' E_BAD_TIMEOUT '错误。可选参数，如上层程序不设置，则Timeout为转台默认值。

**Returns:**

- A value of the enumeration type `E_RESULT`.

#### 1.6 转台转动/移动

在连接到转台成功后，通过此方法在合理的超时时间内控制转台旋转/移动到指定的角度(位置)。

##### 1.6.1 相对定位
以当前位置为基准，运行输入的距离

```c#
public E_RESULT MoveRelative(enum dimension, double speed, double position, int timeout = -1)
```

**parameter:**

- `dimension`: 设置是转台的哪个维度转动

- `speed`: 设置转台转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`

- `position`: 转台或轴从**当前位置**需要旋转或平移的角度/距离。 旋转范围:[`?°`]，平移范围[`?mm`]。 执行完成时，转台将在提供的角度/位置停止。  **注意位置正负表示方向**
- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法将在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' GTS_E_BAD_TIMEOUT '错误。  可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.

##### 1.6.2 绝对定位
以原点复位之后的零点为基准，运动到指定的位置

```c#
public E_RESULT MoveAbsolute(enum dimension, double speed, double position, int timeout = -1)
```

**parameter:**

- `dimension`: 设置是转台的哪个维度转动

- `speed`: 设置转台转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`

- `position`: 转台或轴从**位置原点**需要旋转或平移的位置。 旋转范围:[`?°`]，平移范围[`?mm`]。 执行完成时，转台将在提供的角度/位置停止。  **注意位置为绝对值**
- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法将在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' GTS_E_BAD_TIMEOUT '错误。  可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.

##### 1.6.3 多轴绝对定位
以各轴独立的零点为基准，各轴同时运动到指定的位置

```c#
public static E_Result HomeAll(this GTSApi gts, Dimension[] dimensions, double[] speed, double[] offset, int timeout = -1)
```

**parameter:**

- `dimension`: 设置是转台转动的维度数组

- `speed`: 设置转台转动或平移速度数组. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`

- `position`: 转台或轴从**位置原点**需要旋转或平移的位置数组。 旋转范围:[`?°`]，平移范围[`?mm`]。 执行完成时，转台将在提供的角度/位置停止。  **注意位置为绝对值**
- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法将在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' GTS_E_BAD_TIMEOUT '错误。  可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.

##### 1.6.4 JOG运动
以速度模式运行

```c#
public E_RESULT JOG(Dimension dimension, double speed, bool direction, int timeout = -1)
```

**parameter:**

- `dimension`: 设置是转台的哪个维度转动

- `speed`: 设置转台转动或平移速度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`

- `direction`:设置转台JOG得方向，`true`:顺时针，`false`:逆时针。

- `timeout`: 完成动作的最大允许时间 (`ms`). 请注意，此方法将在内部估计超时的下限，因此，如果上位机提供更小的超时，将返回' GTS_E_BAD_TIMEOUT '错误。  可选参数，如上层程序不设置，则Timeout为转台默认值。  

**Returns:**

- A value of the enumeration type `E_RESULT`.


#### 1.7 连续触发功能

在连接到转台成功后，通过此方法控制转台进行连续触发，输出脉冲。

##### 1.7.1 连续触发开始
配置脉冲输出参数，并准备开始输出脉冲

```c#
public E_Result Trigger(Dimension dimension, double start, double stop, double step, int pluseWidth, int timeout = -1);
```

**Parameter：**

- `dimension`: 设置是转台的哪个维度停止转动
- `start`:设置连续触发的开始角度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
- `stop`:设置连续触发的停止角度. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
- `step`:设置连续触发的运动步长. 范围: `(?)`. 旋转单位 `°/S`. 平移单位`mm/s`
- `pluseWidth`:连续触发输出的脉冲宽度. 脉冲宽度单位`us`

**Returns:**

- A value of the enumeration type `E_Result`.

**Note:**

- 函数实现需支持上位机多线程调用（多轴）

##### 1.7.2 连续触发停止
停止输出脉冲，并清空脉冲输出参数
```c#
public E_Result TriggerStop(Dimension dimension);
```

**Parameter：**

- `dimension`: 设置是转台的哪个维度停止转动

**Returns:**

- A value of the enumeration type `E_Result`.

**Note:**

- 连续触发停止时会清空连续触发设置
  

#### 1.8 转台停止转动

在连接到转台成功后，通过此方法控制转台停止移动，如没有移动亦可调用此方法保证转台为停止状态

```c# 
public E_Result Stop(enum dimension)
```

**Parameter：**

- `dimension`: 设置是转台的哪个维度停止转动

**Returns:**

- A value of the enumeration type `E_Result`.

**Note:**

- 函数实现需支持上位机多线程调用（多轴）


#### 1.9 获取转台速度

从转台获取当前维度的转台转速

```c#
public E_Result GetSpeed(enum dimension, out double speed)
```

**parameter:**

- `dimension`: 设置是转台的哪个维度

- `speed`: 从转台获取当前转速. 范围: `(?)`. 单位`°/S`.

**Returns:**

- A value of the enumeration type `E_Result`.


#### 1.10 获取转台角度/位置

```c#
public E_Result GetPosition(enum dimension, out double position)
```

**parameter:**

- `dimension`: 设置是转台的哪个维度

- `position`: 从转台获取当前转台旋转角度/平移位置（**绝对位置**）, 范围:dimension phi[`?°`] dimension thea[`?°`], dimension X Y Z [`?mm`]。

**Returns:**

- A value of the enumeration type `E_Result`.
