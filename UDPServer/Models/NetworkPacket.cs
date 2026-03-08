using System;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UDPServer.Utils;

namespace UDPServer.Models;

//패킷 타입 정의
public enum PacketType
{
    PlayerJoin = 1, //플레이어 접속 요청
    PlayerLeave = 2, //플레이어 접속 종료
    PlayerUpdate = 3, //플레이어 위치 및 상태 업데이트
    PlayerSpawn = 4, //플레이어 스폰 요청, 다른 플레이어에게 새 플레이어 접속 알림
    PlayerDespawn = 5, //플레이어 디스폰 요청, 다른 플레이어에게 플레이어 접속 종료 알림
    PlayerFire = 6, //플레이어 발사 이벤트
    Heartbeat = 7, //하트비트
    Timeout = 8 //플레이어 일정시간 응답 없을 시, 강제종료
}

public class NetworkPacket
{
    public PacketType Type { get; set; }
    public int PlayerId { get; set; }
    //Json 파싱 오류로 인해 별도의 컨버터 사용
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Position { get; set; }
    [JsonConverter(typeof(Vector3Converter))]
    public Vector3 Rotation { get; set; }
    
    //패킷 생성 시간
    public DateTime Timestamp { get; set; }

    public NetworkPacket()
    {
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
        Timestamp = DateTime.UtcNow;
    }
    
    //패킷을 전송 : NetworkPacket -> Json -> byte[] --> UDP
    //패킷을 수신 : UDP --> byte[] -> Json -> NetworkPacket

    public string ToJson()
    {
        //패킷을 직렬화해서 JSON 문자열로 반환 
        return JsonSerializer.Serialize(this);
    }

    public byte[] ToBytes()
    {
        //객체를 JSON문자열로 변환(인코딩)
        string json = ToJson();
        return Encoding.UTF8.GetBytes(json);
    }

    public static NetworkPacket? FromJson(string json)
    {
        return JsonSerializer.Deserialize<NetworkPacket>(json);
    }

    public static NetworkPacket? FromBytes(byte[] bytes, int bufferSize)
    {
        string json = Encoding.UTF8.GetString(bytes, 0, bufferSize);
        return FromJson(json);
    }
    
}