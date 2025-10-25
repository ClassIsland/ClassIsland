# Cyrene_MSP

![Banner_昔涟](https://res.classisland.tech/banners/cyrene_msp/banner.webp)

Cyrene_MSP（Cyrene Management Server Protocol） 是 ClassIsland 集控服务器与客户端用 GRPC 交流的协议规范。

协议名：`Cyrene_MSP`

版本：`2.0.0.0`

## 定义

在开始之前，我们先明确以下概念的定义：

| 概念 | 定义 |
| --- | --- |
| 客户端 | 指与集控服务器连接的 ClassIsland 应用本体，从集控服务器接收对应的资料和命令 |
| 服务端 | 指[集控服务器](https://github.com/ClassIsland/ManagementServer)的服务端软件，用于发放集控信息。 |

## 目录

- [命令流](command_flow.md)

## 对等端识别

### 客户端

客户端识别基于以下要素：

- CUID（客户端唯一 ID，基于 GUID 格式）
- MAC

在客户端向服务器注册时，服务器会将以上两种要素进行绑定，并在后续连接时进行验证。如果二者不符，将拒绝连接。

### 服务端

服务端识别基于以下要素：

- 服务端私钥

服务端在首次设置时会生成一个 GPG 私钥。首次与服务端连接时，客户端将获取服务端的公钥。在客户端与服务端建立连接时会生成一个随机的挑战令牌，并用保存的公钥加密，要求服务器使用其私钥进行解密并返回解密结果。如果无法解密，将拒绝连接。

在建立连接后会在各个命令的 Headers 里使用 SessionId 来标识连接的会话。

以上识别的具体实现详见文章[命令流](command_flow.md)。

## 请求头

向集控服务器发送的请求头包括以下信息：

| 属性     | 描述       |
|--------|----------|
| `cuid` | 客户端唯一 ID |
| `protocol_name` | 协议名称     |
| `protocol_version` | 协议版本     |
| `session` | 会话 ID    |
