using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UDPServer.Models;

namespace UDPServer.Core;

public class UDPGameServer
{
    private readonly ServerConfig _config;
    private Socket? _socket;
    private readonly ConcurrentDictionary<int, PlayerData> _players;
    private bool _isRunning;
    private int _nextPlayerId;

    public UDPGameServer(ServerConfig serverConfig)
    {
        _config = serverConfig;
        _players = new ConcurrentDictionary<int, PlayerData>();
        _nextPlayerId = 0;
        _isRunning = false;
    }
    
    //서버 초기화
    public void Initialize()
    {
        try
        {
            //UDP 소켓 생성
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //소켓 옵션 설정
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(_config.ServerIP), _config.ServerPort);
            _socket.Bind(serverEndPoint);
            Console.WriteLine($"[서버] 초기화 완료: {_config.ServerIP}:{_config.ServerPort}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 초기화 실패 :  {e.Message}");
            throw;
        }
    }
}