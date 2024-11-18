using System.Numerics;
using YLCommon;
using YLCommon.AOI;
using NetSession = YLCommon.TCPServer<YLCommon.AOI.NetHeader>.NetSession;

/// <summary>
/// 战斗关卡里面的一个玩家实体
/// </summary>
public class BattleEntity
{
    public enum PlayerState
    {
        None = 0,
        Online,
        Offline,
        Mandate
    }

    public ulong entityID;
    public NetSession? session;
    public PlayerState playerState;
    public EntityDriveMode entityDriveMode;
    public Vector3 targetDir;
    public Vector3 targetPos;

    // 用来处理 AOI 逻辑的实体对象
    public AOIEntity aoiEntity;

    public void OnEnterStage()
    {
        playerState = PlayerState.Online;
    }

    public void OnMoveStage()
    {

    }

    public void OnExitStage()
    {
        playerState = PlayerState.Offline;
    }

    // 在线过程中发送消息
    public void UpdateStage(TCPMessage<NetHeader> message)
    {
        if (entityDriveMode == EntityDriveMode.Client && playerState == PlayerState.Online && session != null)
            session.Send(message);
    }

    public void UpdateStage(byte[] message)
    {
        if (entityDriveMode == EntityDriveMode.Client && playerState == PlayerState.Online && session != null)
            session.Send(message);
    }
}