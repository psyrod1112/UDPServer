using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UDPServer.Models;

namespace UDPServer.Core;

public class UDPGameServer
{
    #region 프로퍼티

    private readonly ServerConfig _config;
    private Socket? _socket;
    private readonly ConcurrentDictionary<int, PlayerData> _players;
    private bool _isRunning;
    private int _nextPlayerId;

    #endregion


    #region 생성자

    public UDPGameServer(ServerConfig serverConfig)
    {
        _config = serverConfig;
        _players = new ConcurrentDictionary<int, PlayerData>();
        _nextPlayerId = 0;
        _isRunning = false;
    }

    #endregion

    #region 초기화 및 시작 메서드

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
            
            //소켓 바인딩
            _socket.Bind(serverEndPoint);
            Console.WriteLine($"[서버] 초기화 완료: {_config.ServerIP}:{_config.ServerPort}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 초기화 실패 :  {e.Message}");
            throw;
        }
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            Console.WriteLine("[서버] 이미 실행 중입니다.");
            return;
        }
        Console.WriteLine("[서버] 서버 시작...");
        _isRunning = true;

        await Task.Run(ReceiveLoop);
    }

    #endregion

    #region 메시지 수신 루프

    public void ReceiveLoop()
    {
        //패킷 수신용 버퍼
        byte[] buffer = new byte[_config.BufferSize];
        //엔드포인트
        EndPoint clientEP = new IPEndPoint(IPAddress.Any, _config.ServerPort);

        while (_isRunning)
        {
            try
            {
                int receiveBytes = _socket.ReceiveFrom(buffer, ref clientEP);
                if (receiveBytes > 0)
                {
                    //수신된 바이트 배열을 실제 데이터 크기만큼 복사
                    byte[] data = new byte[receiveBytes];
                    //버퍼에서 실제 수신된 데이터만 복사
                    Array.Copy(buffer, data, receiveBytes);
                    
                    Console.WriteLine($"[서버] {clientEP} 로부터 {receiveBytes} 바이트 수신");
                    
                    //수신된 데이터 처리 로직 (패킷 파싱)
                    ProcessPacket(data, (IPEndPoint)clientEP);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"[서버] 소켓 오류 발생: {e.Message}");
            }
            catch(Exception e)
            {
                Console.WriteLine($"[서버] 수신 오류 발생: {e.Message}");
            }
        }
        Console.WriteLine("[서버] 수신 루프 종료");
        
    }

    #endregion

    #region 패킷 파싱

    private void ProcessPacket(byte[] data, IPEndPoint clientEP)
    {
        try
        {
            //바이트 배열을 NetworkPacket 객체로 변환
            NetworkPacket? packet = NetworkPacket.FromBytes(data);
            if (packet == null)
            {
                Console.WriteLine($"[서버] 잘못된 패킷 형식 수신 {clientEP}");
            }

            switch (packet?.Type)
            {
                case PacketType.PlayerJoin:
                    Console.WriteLine("[서버] 플레이어 접속 요청");
                    HandlePlayerJoin(packet, clientEP);
                    break;
                case PacketType.PlayerLeave:
                    Console.WriteLine("[서버] 플레이어 접속 종료 요청");
                    HandlePlayerLeave(packet.PlayerID);
                    break;
                case PacketType.PlayerUpdate:
                    Console.WriteLine("[서버] 플레이어 정보 업데이트 요청");
                    HandlePlayerUpdate(packet, clientEP);
                    break;
                case PacketType.PlayerFire:
                    Console.WriteLine("[서버] 플레이어 발사 이벤트 요청");
                    HandlePlayerFire(packet, clientEP);
                    break;
                default:
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 패킷 오류 발생 {e.Message}");
        }
    }

    

    #endregion

    #region 핸들러 메서드

    //새로운 플레이어 접속 처리
    private void HandlePlayerJoin(NetworkPacket packet, IPEndPoint clientEP)
    {
        //최대 플레이어 수 확인(예외처리)
        if (_config.MaxPlayers < _players.Count)
        {
            Console.WriteLine($"[서버] 최대 플레이어 수 초과. 접속 거부: {clientEP}");
            return;
        }
        
        //새로운 플레이어 ID 할당(스레드 안전하게 증가)
        int newPlayerID = Interlocked.Increment(ref _nextPlayerId);
        
        //새로운 플레이어 데이터 생성
        PlayerData newPlayerData = new PlayerData(newPlayerID, clientEP);
        
        //초기 위치와 회전 설정
        newPlayerData.UpdateTransform(packet.Position, packet.Rotation);
        
        //플레이어 등록
        if (_players.TryAdd(newPlayerID, newPlayerData))
        {
            Console.WriteLine($"[서버] 플레이어 {newPlayerID} 접속 성공: {clientEP}");
            
            //플레이어 접속 성공 응답 전송(packetType.PlayerSpawn)
            SendPlayerSpawn(newPlayerData, clientEP);
            //다른 플레이어들에게 새 플레이어 접속 알림 전송
            BroadcastPlayerSpawn(newPlayerData, clientEP);
            //새 플레이어에게 기존 플레이어 정보 전송
            SendExistingPlayers(clientEP);

        }
        else
        {
            Console.WriteLine($"[서버] 플레이어 {newPlayerID} 등록 실패: {clientEP}");
        }
        
        
    }
    
    private void HandlePlayerLeave(int playerID)
    {
        if (_players.TryRemove(playerID, out var playerData))
        {
            Console.WriteLine($"[서버] 플레이어 접속해제 - ID: {playerData.PlayerID}, EP: {playerData.EndPoint}");
            
            //다른 플레이어에게 접속해제 알림 전송
            BroadcastPlayerDespawn(playerID);
        }
        else
        {
            Console.WriteLine($"[서버] 오류! 플레이어 접속해제 실패 - ID: {playerData.PlayerID} 존재하지 않음");
        }
    }

    private void HandlePlayerUpdate(NetworkPacket packet, IPEndPoint clientEP)
    {
        //서버에서 해당 플레이어가 존재하는 지 확인
        if (_players.TryGetValue(packet.PlayerID, out var playerData))
        {
            //클라이언트 검증
            if (playerData.EndPoint.Equals(clientEP))
            {
                //플레이어 위치 회전 정보 갱신
                playerData.UpdateTransform(packet.Position, packet.Rotation);
                Console.WriteLine($"[서버] 플레이어 {packet.PlayerID} 위치/회전 정보 갱신 POS : {packet.Position}, Rot: {packet.Rotation} ");
                //다른 플레이어들에게 위치 업데이트 알림 전송
                BroadcastplayerUpdate(packet, clientEP);
            }
            else
            {
                Console.WriteLine($"[서버] 플레이어 {packet.PlayerID} 정보 갱신 실패 - 클라이언트 EP불일치");
                return;
            }
        }
        else
        {
            Console.WriteLine($"[서버] 플레이어 {packet.PlayerID} 정보 갱신 실패 - 플레이어 존재하지 않음");
        }
    }

    private void HandlePlayerFire(NetworkPacket packet, IPEndPoint clientEP)
    {
        //서버에서 해당 플레이어 정보 조회
        if (_players.TryGetValue(packet.PlayerID, out var playerData))
        {
            //클라이언트 주소 검증
            if (playerData.EndPoint.Equals(clientEP))
            {
                Console.WriteLine($"[서버] 플레이어 {playerData.PlayerID} 발사 이벤트 처리 완료");
                
                //발사 이벤트를 다른 플레이어들에게 전송
                BroadcastPlayerFire(packet, clientEP);
            }
            else
            {
                Console.WriteLine($"[서버] 플레이어 {playerData.PlayerID} 발사 이벤트 실패!");
            }
        }
    }
    
    #endregion

    #region 패킷 전송 메서드

    //PlayerSpawn 패킷 전송
    private void SendPlayerSpawn(PlayerData playerData, IPEndPoint clientEP)
    {
        try
        {
            //플레이어 스폰 패킷 생성
            NetworkPacket packet = new NetworkPacket
            {
                Type = PacketType.PlayerSpawn,
                PlayerID = playerData.PlayerID,
                Position = playerData.Position,
                Rotation = playerData.Rotation,
                Timestamp = DateTime.UtcNow
            };

            //패킷을 바이트 배열로 변환
            byte[] data = packet.ToBytes();

            //클라이언트에게 패킷 전송
            _socket?.SendTo(data, clientEP);

            Console.WriteLine($"[서버] PlayerSpawn 패킷 전송 성공 to {clientEP}");

        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] PlayerSpawn 패킷 전송 오류 : {e.Message}");
            
        }
    }

    private void SendExistingPlayers(IPEndPoint newClientEP)
    {
        int sentCount = 0;
        foreach (var existingPlayer in _players)
        {
            if (!existingPlayer.Value.EndPoint.Equals(newClientEP))
            {
                SendPlayerSpawn(existingPlayer.Value, newClientEP);
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Console.WriteLine($"[서버] 기존 플레이어(총 {sentCount}명) 정보 전송 to {newClientEP} ");
        }
        else
        {
            Console.WriteLine($"[서버] 기존 플레이어가 없어 전송 생략 (총 {sentCount}명)");
        }
    }
    
    #endregion

    
    #region 브로드캐스트 메소드

    //기존에 접속했던 player들에게 새로운 플레이어의 생성을 알림
    private void BroadcastPlayerSpawn(PlayerData newPlayer, IPEndPoint clientEP)
    {
        int sentCount = 0;
        foreach (var existingPlayer in _players)
        {
            if (!existingPlayer.Value.EndPoint.Equals(clientEP))
            {
                SendPlayerSpawn(newPlayer, existingPlayer.Value.EndPoint);
                sentCount++;
            }
        }

        if (sentCount > 0)
        {
            Console.WriteLine($"[서버] 기존 플레이어들에게 새로운 플레이어 {newPlayer.PlayerID} 스폰 알림 전송 완료 (총 {sentCount}명)");
        }
        else
        {
            Console.WriteLine($"[서버] 오류! 기존 플레이어가 없어 새로운 플레이어 {newPlayer.PlayerID} 스폰 알림 전송 생략");
        }
    }
    
    private void BroadcastPlayerDespawn(int playerID)
    {
        try
        {
            NetworkPacket? newPacket = new NetworkPacket
            {
                Type = PacketType.PlayerDespawn,
                PlayerID = playerID,
                Timestamp = DateTime.UtcNow
            };
            
            byte[] data = newPacket.ToBytes();
            int sentCount = 0;
            foreach (var existingPlayer in _players)
            {
                if (existingPlayer.Value.PlayerID != playerID)
                {
                    _socket.SendTo(data, existingPlayer.Value.EndPoint);
                    sentCount++;
                }
            }
            Console.WriteLine($"[서버] 플레이어 {playerID} 접속 해제 알림 전송 완료 (총 {sentCount}명)");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 플레이어 접속 해제 알림 전송 오류! {e.Message}");
        }
    }
    
    //모든 플레이어에게 특정 플레이어의 위치/회전 값 전송
    private void BroadcastplayerUpdate(NetworkPacket packet, IPEndPoint clientEP)
    {
        try
        {
            //플레이어 업데이트 패킷 생성
            var newPacket = new NetworkPacket
            {
                Type = PacketType.PlayerUpdate,
                PlayerID = packet.PlayerID,
                Position = packet.Position,
                Rotation = packet.Rotation,
                Timestamp = DateTime.UtcNow
            };
            
            byte[] data = newPacket.ToBytes();
            int  sentCount = 0;
            foreach (var existingPlayer in _players)
            {
                if (!existingPlayer.Value.EndPoint.Equals(clientEP))
                {
                    _socket.SendTo(data, existingPlayer.Value.EndPoint);
                    sentCount++;
                }
            }
            Console.WriteLine($"[서버] 플레이어 {packet.PlayerID} 위치/회전 업데이트 전송 완료 (총 {sentCount}명)");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 플레이어 위치/회전 업데이트 전송 오류: {e.Message}");
        }
    }

    private void BroadcastPlayerFire(NetworkPacket packet, IPEndPoint clientEP)
    {
        try
        {
            //플레이어 발사 패킷 생성
            var newPacket = new NetworkPacket
            {
                Type = PacketType.PlayerFire,
                PlayerID = packet.PlayerID,
                Position = packet.Position,
                Rotation = packet.Rotation,
                Timestamp = DateTime.UtcNow
            };
            byte[] data = newPacket.ToBytes();
            int sentCount = 0;
            foreach (var existingPlayer in _players)
            {
                if (!existingPlayer.Value.EndPoint.Equals(clientEP))
                {
                    _socket.SendTo(data, existingPlayer.Value.EndPoint);
                    sentCount++;
                }
            }
            Console.WriteLine($"[서버] 플레이어 {newPacket.PlayerID} 발사 이벤트 전송 완료 (총 {sentCount}명)");
        }
        catch (Exception e)
        {
            
        }
    }


    #endregion
    
}