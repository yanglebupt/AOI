using YLCommon;
using YLCommon.AOI;
using System.Numerics;
using NetPackage = YLCommon.TCPServer<YLCommon.AOI.NetHeader>.NetPackage;
using System;

/// <summary>
/// 全局的后端服务入口
/// </summary>
public class GameGlobal
{
    public static GameGlobal Instance = new GameGlobal();
    private GameGlobal() { }

    public BattleStage battleStage = new();
    public AsyncServer server = new(new ServerConfig { port = 3000, external_handle = true });

    public void Init() {
        server.OnPackage += OnPackage;
        server.Start();
        battleStage.Init();
    }
    
    public void Tick() { 
        battleStage.Tick();
        server.Tick();
    }

    void OnPackage(NetPackage package)
    {
        switch(package.message.header.cmd) {
            case Cmd.ReqLogin:
                LoginStage(package);
                break;
            case Cmd.MovePos:
                MovePos(package);
                break;
            case Cmd.Exit:
                Exit(package);
                break;
        }
    }

    void MovePos(NetPackage package)
    {
        MovePosBody? movePosBody = package.message.GetBody<MovePosBody>();
        if(movePosBody != null)
            battleStage.MoveStage(movePosBody);
    }

    void Exit(NetPackage package)
    {
        ExitBody? exitBody = package.message.GetBody<ExitBody>();
        if (exitBody != null)
            battleStage.ExitStage(exitBody);
    }

    /// <summary>
    /// 登录后进入关卡
    /// </summary>
    void LoginStage(NetPackage package)
    {
        // 创建一个关卡玩家实体
        BattleEntity battleEntity = new BattleEntity {
            entityID = package.session.ID,
            session = package.session,
            targetPos = new Vector3(10, 0, 10),
            playerState = BattleEntity.PlayerState.None,
            entityDriveMode = EntityDriveMode.Client,
        };

        // 进入关卡
        battleStage.EnterStage(battleEntity);

        // 返回响应
        TCPMessage<NetHeader> msg = new()
        {
            header = new NetHeader { cmd = Cmd.ResLogin },
        };
        msg.SetBody(new ResLoginBody { entityID  = battleEntity.entityID });
        battleEntity.session.Send(msg);
    }

    // 生成服务器怪物
    Random rd = new();
    ulong sid = 0;
    public void CreateServerEntity()
    {
        float rdx = rd.Next(-500, 500);
        float rdz = rd.Next(-500, 500);
        BattleEntity battleEntity = new BattleEntity
        {
            entityID = sid++,
            targetPos = new Vector3(rdx, 0, rdz),
            playerState = BattleEntity.PlayerState.None,
            entityDriveMode = EntityDriveMode.Server,
        };
        // 进入关卡
        battleStage.EnterStage(battleEntity);
    }
    
    public void UnInit() {
        battleStage.UnInit();
    }
}