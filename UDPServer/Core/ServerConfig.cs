using System;

namespace UDPServer.Core;

public class ServerConfig //서버 정보 클래스
{
    #region 프로퍼티

    public string ServerIP {get; set;}
    public int ServerPort {get; set;}
    public int MaxPlayers {get; set;}
    public int BufferSize { get; set; }
    public int WorkerThreadCount { get; set; }
    public int PlayerTimeoutSeconds { get; set; }

    #endregion

    #region 생성자

    public ServerConfig()
    {
        ServerIP = "127.0.0.1";
        ServerPort = 7777;
        MaxPlayers = 100;
        BufferSize = 1024;
        WorkerThreadCount = Environment.ProcessorCount;
        PlayerTimeoutSeconds = 30;
    }

    #endregion
    
}