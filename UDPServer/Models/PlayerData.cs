using System.Net;
using System.Numerics;

namespace UDPServer.Models;

public class PlayerData
{
    #region 프로퍼티
    
    public string PlayerID { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public DateTime LastUpdateTime  { get; set; }
    
    #endregion

    #region 생성자

    public PlayerData(string playerID, IPEndPoint endPoint)
    {
        PlayerID = playerID;
        EndPoint = endPoint;
        LastUpdateTime = DateTime.UtcNow;
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
    }

    #endregion

    #region 메서드

    //플레이어의 위치, 회전 정보를 업데이트 하는 메서드
    private void UpdateTransform(Vector3 position, Vector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    #endregion
    
    
    
}
















