# 命令流

在客户端与集控服务器建立连接时，会启动一个命令流用于接受服务器下达的指令。

## 握手

在建立命令流连接前，对等端需要握手以验明双方的身份。

```mermaid
sequenceDiagram
  participant C as 客户端
  participant S as 服务端

  Note over C: 生成挑战令牌，并使用服务器公钥加密
  C ->> S: 发送握手信息（CUID、MAC、加密后的挑战令牌）
  Note over S: 验证客户端 CUID、MAC
  Note over S: 使用私钥解密挑战令牌
  S ->> C: 返回解密后的挑战令牌和服务器公钥
  Note over C: 检验挑战令牌是否正确
  C ->> S: 令牌校验通过
  Note over S: 生成与当前通道唯一对应的 SessionId
  S ->> C: 握手完成，返回 SessionId 并正式开始下发命令
  
```

## 心跳包

客户端每 10 秒向服务器发送一个心跳包（Ping），服务器返回一个已接收的命令（Pong）。

```mermaid
sequenceDiagram
  participant C as 客户端
  participant S as 服务端

  C ->> S: Ping
  S ->> C: Pong

```

## 重试

如果心跳包发送异常，或命令流连接断开，客户端会在出现异常开始，每 30 秒重试连接。重试时会再次进行新的握手流程，并接收新的 SessionId.
