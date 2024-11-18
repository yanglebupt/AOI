using System;
using System.Collections.Generic;

namespace YLCommon.AOI
{
    // 宫格行为枚举
    public enum CellOP
    {
        EntityEnter,
        EntityExit,
        EntityMove,
    }

    /// <summary>
    /// AOI 单元块
    /// </summary>
    public class AOICell
    {
        public int xIndex, zIndex;
        public AOIManager aoiManager;

        // 内部实体
        public HashSet<AOIEntity> entities = new();
        public HashSet<AOIEntity> enterEntities = new();
        public HashSet<AOIEntity> exitEntities = new();

        // 周围宫格
        public bool IsCalcAround = false;
        public AOICell[] Arounds = new AOICell[9];

        // 多少个实体关注该 cell
        public int clientEntityConcernCount = 0;
        public int serverEntityConcernCount = 0;

        // 哪些实体进入该 cell
        // 哪些实体退出该 cell
        // 哪些实体在该 cell 移动
        public AOICellUpdateContainer updateContainer = new();

        public AOICell(int xIndex, int zIndex, AOIManager aoiManager)
        {
            this.xIndex = xIndex;
            this.zIndex = zIndex;
            this.aoiManager = aoiManager;
        }

        public string GetCellKey()
        {
            return GetCellKey(xIndex, zIndex);
        }
        public static string GetCellKey(int xIndex, int zIndex)
        {
            return $"({xIndex}-{zIndex})";
        }

        /// <summary>
        /// 实体进入该 cell（另一个地图上的实体直接传送到该 cell，或者该地图的实体移动到该 cell）
        /// </summary>
        public void EnterCell(AOIEntity entity) {
            if (entity.entityOP == EntityOP.TransferEnter)
            {
                // 缓存新进入的实体
                if (!enterEntities.Add(entity)) { 
                    Console.WriteLine($"entity {entity.entityID} has in cell {GetCellKey()}");
                    return;
                };
                // 更改视野
                entity.SetAroundCells(Arounds);
                // 通知视野范围内，有人进来了
                for (int i = 0; i < Arounds.Length; i++)
                    Arounds[i].WriteCellOP(CellOP.EntityEnter, entity);
            }
            else if(entity.entityOP == EntityOP.MoveCross)
            {
                // 缓存新进入的实体
                if (!enterEntities.Add(entity))
                {
                    Console.WriteLine($"entity {entity.entityID} has in cell {GetCellKey()}");
                    return;
                };
                // 视野增删
                AOICell? enterCell = aoiManager.GetCell(entity.xIndex, entity.zIndex);
                AOICell? exitCell = aoiManager.GetCell(entity.xLastIndex, entity.zLastIndex);
                if(enterCell != null && exitCell != null)
                {
                    AOICell[] enterArounds = enterCell.Arounds, exitArounds = exitCell.Arounds;
                    HashSet<AOICell> moveAroundSet = new(), enterAroundSet = new(), exitAroundSet = new();
                    for (int i = 0; i < 9; i++)
                    {
                        moveAroundSet.Add(enterArounds[i]);
                        enterAroundSet.Add(enterArounds[i]);
                        exitAroundSet.Add(exitArounds[i]);
                    }
                    moveAroundSet.IntersectWith(exitArounds);
                    enterAroundSet.ExceptWith(moveAroundSet);
                    exitAroundSet.ExceptWith(moveAroundSet);

                    foreach (var enter in enterAroundSet)
                    {
                        entity.AddAroundCell(enter);
                        enter.WriteCellOP(CellOP.EntityEnter, entity);
                    }

                    foreach (var move in moveAroundSet)
                    {
                        move.WriteCellOP(CellOP.EntityMove, entity);
                    }

                    foreach (var exit in exitAroundSet)
                    {
                        entity.RemoveAroundCell(exit);
                        exit.WriteCellOP(CellOP.EntityExit, entity);
                    }
                }
                else Console.WriteLine($"cell {GetCellKey(entity.xIndex, entity.zIndex)} or {GetCellKey(entity.xLastIndex, entity.zLastIndex)} not found");
            }
            else
                Console.WriteLine($"entity {entity.entityID} enter cell with error EntityOP");
        }

        /// <summary>
        /// 实体在该 cell 里面移动
        /// </summary>
        public void MoveCell(AOIEntity entity) {
            for (int i = 0; i < Arounds.Length; i++)
                Arounds[i].WriteCellOP(CellOP.EntityMove, entity);
        }

        public void ExitCell(AOIEntity entity) {
            exitEntities.Add(entity);
            // 通知视野范围内，有人出去了
            for (int i = 0; i < Arounds.Length; i++)
                Arounds[i].WriteCellOP(CellOP.EntityExit, entity);
        }

        // 收集实体操作
        public void WriteCellOP(CellOP cellOP, AOIEntity aoiEntity)
        {
            switch (cellOP)
            {
                case CellOP.EntityEnter:
                    if (aoiEntity.entityDriveMode == EntityDriveMode.Client)
                        clientEntityConcernCount++;
                    else if(aoiEntity.entityDriveMode == EntityDriveMode.Server)
                        serverEntityConcernCount++;
                    updateContainer.enterEvents.Add(new EnterEvent(aoiEntity.entityID, aoiEntity.posX, aoiEntity.posZ));
                    break;
                case CellOP.EntityExit:
                    if (aoiEntity.entityDriveMode == EntityDriveMode.Client)
                        clientEntityConcernCount--;
                    else if (aoiEntity.entityDriveMode == EntityDriveMode.Server)
                        serverEntityConcernCount--;
                    updateContainer.exitEvents.Add(new ExitEvent(aoiEntity.entityID));
                    break;
                case CellOP.EntityMove:
                    updateContainer.moveEvents.Add(new MoveEvent(aoiEntity.entityID, aoiEntity.posX, aoiEntity.posZ));
                    break;
            }
        }

        public void CalcCellOP()
        {
            // 该 cell 有客户端实体关注，并且该 cell 里面有客户端实体，才需要计算视野并发送消息
            bool hasClient = false;
            foreach (var item in entities)
            {
                if(item.entityDriveMode == EntityDriveMode.Client)
                {
                    hasClient = true;
                    break;
                }
            }
            if (!updateContainer.IsEmpty && hasClient)
                aoiManager.OnCellEntityOPMerge?.Invoke(this, updateContainer);
            updateContainer.Clear();
        }
    }
}
