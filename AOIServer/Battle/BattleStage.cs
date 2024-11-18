using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using YLCommon;
using YLCommon.AOI;
/// <summary>
/// 战斗关卡
/// </summary>
public class BattleStage
{
    public int stageID;
    public string stageName;
    public AOIManager aoiManager;

    // 当前关卡的全部玩家实体
    private ConcurrentDictionary<ulong, BattleEntity> entities = new();
    // 那些玩家退出了关卡
    private ConcurrentQueue<BattleEntity> exitStageEntities = new();
    // 那些玩家进入了关卡
    private ConcurrentQueue<BattleEntity> enterStageEntities = new();
    // 那些玩家在关卡里面移动
    private ConcurrentQueue<BattleEntity> moveStageEntities = new();

    public void Init() {
        // 加载关卡配置
        stageID = 101;
        stageName = "长安城";
        // 创建一个 AOI 管理器来管理这个关卡
        aoiManager = new AOIManager() { 
            name = stageName,
            cellSize = 20,
        };
        aoiManager.OnEntityCellViewChange += OnEntityCellViewChange;
        aoiManager.OnCellEntityOPMerge += OnCellEntityOPMerge;
#if DEBUG
        aoiManager.OnCreateCell += OnCreateCell;
#endif
        hisTime = DateTime.Now;
        lastTickDate = DateTime.Now;
        this.Info("Stage {0} Init", stageID);
    }
    public void Tick() {
        
        RandomServerAI();

        while (exitStageEntities.TryDequeue(out BattleEntity entity))
        {
            aoiManager.Exit(entity.aoiEntity);
            if (entities.TryRemove(entity.entityID, out BattleEntity _))
                entity.OnExitStage();
            else
                this.Warn($"entity: {entity.entityID} not in stage {stageID}");
        }

        while (enterStageEntities.TryDequeue(out BattleEntity entity))
        {
            ulong entityID = entity.entityID;
            if (entities.ContainsKey(entityID))
                this.Warn($"entity: {entityID} has in stage {stageID}");
            else
            {
                // 进入关卡后，需要创建一个对应的 AOI 实体
                entity.aoiEntity = aoiManager.Enter(entity.entityID, entity.targetPos.X, entity.targetPos.Z, entity.entityDriveMode);
                if (entities.TryAdd(entityID, entity))
                {
                    entity.OnEnterStage();
#if DEBUG
                    if(entity.entityDriveMode == EntityDriveMode.Client)
                    {
                        TCPMessage<NetHeader> msg = new()
                        {
                            header = new NetHeader { cmd = Cmd.CreateCell },
                        };
                        Dictionary<string, AOICell> cells = aoiManager.Cells;
                        int cellCount = cells.Count, index = 0;
                        CreateCellBody createCellBody = new() { cells = new CreateCell[cellCount] };
                        foreach (var item in aoiManager.Cells)
                            createCellBody.cells[index++] = new CreateCell { xIndex = item.Value.xIndex, zIndex = item.Value.zIndex };
                        msg.SetBody(createCellBody);
                        entity.UpdateStage(msg);
                    }
#endif
                }
                else
                    this.Warn($"entity: {entityID} has in stage {stageID}");
            }
        }

        while (moveStageEntities.TryDequeue(out BattleEntity entity))
        {
            aoiManager.Move(entity.aoiEntity, entity.targetPos.X, entity.targetPos.Z);
            entity.OnMoveStage();
        }

        aoiManager.CalcAllEntitiesAOIChange();
    }

    public void UnInit() {
        entities.Clear();
        enterStageEntities.Clear();
        exitStageEntities.Clear();
        moveStageEntities.Clear();
    }

    public void EnterStage(BattleEntity battleEntity)
    {
        ulong entityID = battleEntity.entityID;
        if (entities.ContainsKey(entityID))
            this.Warn($"entity: {entityID} has in stage {stageID}");
        else
        {
            enterStageEntities.Enqueue(battleEntity);
            this.Info($"entity: {entityID} entered stage {stageID}");
        }
    }
    
    public void MoveStage(MovePosBody movePosBody)
    {
        if(entities.TryGetValue(movePosBody.entityId, out BattleEntity battleEntity))
        {
            battleEntity.targetPos = new Vector3(movePosBody.posX, 0, movePosBody.posZ);
            MoveStage(battleEntity);
        }
    }
    public void MoveStage(BattleEntity battleEntity)
    {
        ulong entityID = battleEntity.entityID;
        if (entities.ContainsKey(entityID))
            moveStageEntities.Enqueue(battleEntity);
        else
            this.Warn($"entity: {entityID} not in stage {stageID}");
    }
    public void ExitStage(ExitBody exitBody)
    {
        if (entities.TryGetValue(exitBody.entityId, out BattleEntity battleEntity))
            ExitStage(battleEntity);
    }
    public void ExitStage(BattleEntity battleEntity)
    {
        ulong entityID = battleEntity.entityID;
        if (entities.ContainsKey(entityID))
        {
            exitStageEntities.Enqueue(battleEntity);
            this.Info($"entity: {entityID} exited stage {stageID}");
        }
        else
            this.Warn($"entity: {entityID} not in stage {stageID}");
    }

#if DEBUG
    public void OnCreateCell(AOICell aoiCell)
    {
        TCPMessage<NetHeader> msg = new()
        {
            header = new NetHeader { cmd = Cmd.CreateCell },
        };
        CreateCellBody createCellBody = new() { cells = new CreateCell[1] };
        createCellBody.cells[0] = new CreateCell { xIndex = aoiCell.xIndex, zIndex = aoiCell.zIndex };
        msg.SetBody(createCellBody);
        foreach (var item in entities)
            item.Value.UpdateStage(msg);
    }
#endif

    public void OnEntityCellViewChange(AOIEntity aoiEntity, AOICellUpdateContainer updateContainer)
    {
        TCPMessage<NetHeader> msg = new()
        {
            header = new NetHeader { cmd = Cmd.ResAOI },
        };
        AOIBody aoiBody = new AOIBody();

        foreach (EnterEvent enterEvent in updateContainer.enterEvents)
            aoiBody.enterEvents.Add(new EnterEventBody { entityId = enterEvent.id, posX = enterEvent.x, posZ = enterEvent.z });
        foreach (ExitEvent exitEvent in updateContainer.exitEvents)
            aoiBody.exitEvents.Add(new ExitEventBody { entityId = exitEvent.id});

        msg.SetBody(aoiBody);

        // 将 AOI 变化消息发送给客户端
        if (entities.TryGetValue(aoiEntity.entityID, out BattleEntity battleEntity))
            battleEntity.UpdateStage(msg);
    }

    public void OnCellEntityOPMerge(AOICell aoiCell, AOICellUpdateContainer updateContainer)
    {
        TCPMessage<NetHeader> msg = new()
        {
            header = new NetHeader { cmd = Cmd.ResAOI },
        };
        AOIBody aoiBody = new AOIBody();

        foreach (EnterEvent enterEvent in updateContainer.enterEvents)
            aoiBody.enterEvents.Add(new EnterEventBody { entityId = enterEvent.id, posX = enterEvent.x, posZ = enterEvent.z });
        foreach (MoveEvent moveEvent in updateContainer.moveEvents)
            aoiBody.moveEvents.Add(new MoveEventBody { entityId = moveEvent.id, posX = moveEvent.x, posZ = moveEvent.z });
        foreach (ExitEvent exitEvent in updateContainer.exitEvents)
            aoiBody.exitEvents.Add(new ExitEventBody { entityId = exitEvent.id });

        msg.SetBody(aoiBody);

        byte[]? bytes = NetworkConfig.SerializePack(msg);
        if (bytes == null) return;

        // 将 AOI 变化消息发送给该 cell 内的全部客户端
        foreach (var aoiEntity in aoiCell.entities)
        {
            if (entities.TryGetValue(aoiEntity.entityID, out BattleEntity battleEntity))
                battleEntity.UpdateStage(bytes);
        }
    }


    // 服务端怪兽随机移动
    Random rd = new();
    DateTime hisTime;
    DateTime lastTickDate;
    void RandomServerAI()
    {
        // 1s 变一次方向
        if(DateTime.Now > lastTickDate.AddSeconds(1))
        {
            lastTickDate = DateTime.Now;
            foreach (var item in entities)
            {
                BattleEntity battleEntity = item.Value;
                // 一半的概率改变方向
                if(battleEntity.entityDriveMode == EntityDriveMode.Server && rd.Next(0, 100) < 50)
                {
                    if(Math.Abs(battleEntity.targetPos.X) >= 500 || Math.Abs(battleEntity.targetPos.Z) >= 500)
                    {
                        // 超过边界，回来
                        float rdx = rd.Next(-500, 500);
                        float rdz = rd.Next(-500, 500);
                        // 朝向内部移动
                        battleEntity.targetDir = Vector3.Normalize(new Vector3(rdx, 0, rdz) * 0.8f - battleEntity.targetPos);
                    }
                    else
                    {
                        // 超过边界，回来
                        float rdx = rd.Next(-100, 100);
                        float rdz = rd.Next(-100, 100);
                        rdx = rdx == 0 ? 1 : rdx;
                        rdz = rdz == 0 ? 1 : rdz;
                        // 朝向内部移动
                        battleEntity.targetDir = Vector3.Normalize(new Vector3(rdx, 0, rdz));
                    }
                }
            }
        }

        // 移动
        DateTime nowTime = DateTime.Now;
        float delta = (float)((nowTime - hisTime).TotalMilliseconds / 1000f);
        hisTime = nowTime;
        foreach (var item in entities)
        {
            BattleEntity battleEntity = item.Value;
            if(battleEntity.entityDriveMode == EntityDriveMode.Server)
            {
                battleEntity.targetPos += battleEntity.targetDir * 40 * delta;
                MoveStage(battleEntity);
            }
        }
    }
}