using YLCommon;
using YLCommon.AOI;

public class AsyncServer: ITCPServer<NetHeader>
{
    public AsyncServer(ServerConfig config) : base(config) { }

    public override void ClientConnected(ulong ID)
    {
        this.Info("Client {0} Connected", ID);
    }

    public override void ClientDisconnected(ulong ID)
    {
        this.Warn("Client {0} Disconnected", ID);
    }
}
