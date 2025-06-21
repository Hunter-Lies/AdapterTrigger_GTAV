# GTAV 飞智八爪鱼4手柄 Mod

## 功能概述
- **状态检测**：检测玩家当前是否在装弹、驾驶载具、使用载具武器或手持武器。
- **配置驱动**：根据`trigger.ini`配置文件中的设置，为不同状态生成相应的触发指令。
- **UDP通信**：将指令通过UDP发送到本地端口7878（飞智手柄Mod监听端口）。
  
## 安装步骤
### 前置条件
1. 需要 Grand Theft Auto V（GTAV）
2. 需要 Script Hook V http://www.dev-c.com/gtav/scripthookv/
3. 需要 Script Hook V .NET https://github.com/crosire/scripthookvdotnet/releases
4. 飞智八爪鱼4手柄

### 放置文件：
### 将下载的所有文件放入GTA V的scripts文件夹：
```
GTA5 根目录/
├── scripts/
│   ├── AdapterTrigger.dll    # 本脚本
│   ├── AdapterTriggerResource.dll
│   ├── INIFileParser.dl
│   ├── Newtonsoft.Json.dll
│   └── trigger.ini           # 配置文件
```
## 配置参数详解

|    参数    |    说明    |    范围    |
| ------------- | ------------- |------------- |
| `Mode`    | 震动/反馈模式	 | 1-5 (见下表)    |
| `param1`  | 主强度/参数     | 0-255          |
| `param2`  | 持续时间        | 0-255          |
| `param3`  | 自定义参数1     | 0-255          |
| `param4`  | 自定义参数2     | 0-255          |    

## 模式说明 (Mode)

|模式|说明|适用场景|
| ------------- | ------------- |------------- |
| `1`	|单次强震动|	武器射击、爆炸|
| `2`	|持续震动|	载具行驶、装弹|
| `3`	|渐强震动|	车辆加速|
| `4`	|脉冲震动|	直升机螺旋桨|
| `5`	|自定义复杂震动|	特殊效果|
