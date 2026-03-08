using System.Buffers;
using System.Net;
using UDPServer.Core;

namespace UDPServer.Models;

public class PacketJob : IJob
{
    private readonly UDPGameServer _server;
    private readonly byte[] _buffer;
    private readonly int _bufferSize;
    private readonly IPEndPoint _clientEP;

    public PacketJob(UDPGameServer server, byte[] buffer, int bufferSize, IPEndPoint clientEP)
    {
        _server = server;
        _buffer = buffer;
        _bufferSize = bufferSize;
        _clientEP = clientEP;
    }
    
    public void Execute()
    {
        //수신받은 정보를 PacketJob형태로 받아서 JobQueue안의 큐에 계속 저장해두는 식
        try
        {
            _server.ProcessPacket(_buffer, _bufferSize, _clientEP);
            Console.WriteLine("[PacketJob] 패킷 처리 완료!");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[서버] 패킷 처리 오류 : {e.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}