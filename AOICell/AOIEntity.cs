using System;
using System.Collections.Generic;

namespace YLCommon.AOI
{
    // 实体行为枚举
    public enum EntityOP
    {
        None,
        // 传送进入
        TransferEnter,
        // 传送离开
        TransferExit,
        // 跨越宫格
        MoveCross,
        // 宫格内部移动
        MoveInside
    }

    public enum EntityDriveMode
    {
        None,
        Client,
        Server
    }

    /// <summary>
    /// AOI 实体
    /// </summary>
    public class AOIEntity
    {
        public ulong entityID;
        public AOIManager aoiManager;

        // 当前所在宫格索引
        public int xIndex = int.MaxValue, zIndex = int.MaxValue;
        // 上一个宫格索引
        public int xLastIndex = int.MaxValue, zLastIndex = int.MaxValue;

        // 当前位置
        public float posX, posZ;

        // 移动类型
        public EntityOP entityOP;

        // 实体驱动模式
        public EntityDriveMode entityDriveMode;

        // 当前视野
        private AOICell[]? aroundCells = null;

        private List<AOICell> removeCells = new();
        private List<AOICell> addCells = new();

        public AOICellUpdateContainer updateContainer = new();

        public AOIEntity(ulong entityID, AOIManager aoiManager, EntityDriveMode entityDriveMode)
        {
            this.entityID = entityID;
            this.aoiManager = aoiManager;
            this.entityDriveMode = entityDriveMode;
        }

        // 移动更新位置
        public void UpdatePos(float x, float z, EntityOP op = EntityOP.None)
        {
            posX = x;
            posZ = z;
            entityOP = op;

            // 计算索引
            int _xIndex = (int)Math.Floor(posX / aoiManager.cellSize);
            int _zIndex = (int)Math.Floor(posZ / aoiManager.cellSize);
            if(_xIndex == xIndex && _zIndex == zIndex)
            {
                entityOP = EntityOP.MoveInside;
                // 在同一个区块内移动
                aoiManager.MoveInsideCell(this);
            }
            else
            {
                // 跨区块
                xLastIndex = xIndex;
                zLastIndex = zIndex;

                xIndex = _xIndex;
                zIndex = _zIndex;

                // 传送进来，传送出去，通过参数传入来指定，不指定就认为是 cross 移动
                if(entityOP != EntityOP.TransferEnter && entityOP != EntityOP.TransferExit)
                    entityOP = EntityOP.MoveCross;

                aoiManager.MoveCrossCell(this);
            }
        }

        // 该实体对应的视野改变
        public void CalcEntityCellViewChange()
        {
            // 确保自身是客户端实体，才需要计算视野发送，如果自身是服务端怪物，则没必要计算视野
            if (entityDriveMode == EntityDriveMode.Client)
            {
                // 收集全部视野
                if (aroundCells != null)
                {
                    for (int i = 0; i < aroundCells.Length; i++)
                    {
                        HashSet<AOIEntity> entities = aroundCells[i].entities;
                        foreach (AOIEntity entity in entities)
                            updateContainer.enterEvents.Add(new EnterEvent { id = entity.entityID, x = entity.posX, z = entity.posZ });
                    }
                }

                for (int i = 0; i < removeCells.Count; i++)
                {
                    HashSet<AOIEntity> entities = removeCells[i].entities;
                    foreach (AOIEntity entity in entities)
                        updateContainer.exitEvents.Add(new ExitEvent { id = entity.entityID });
                }

                for (int i = 0; i < addCells.Count; i++)
                {
                    HashSet<AOIEntity> entities = addCells[i].entities;
                    foreach (AOIEntity entity in entities)
                        updateContainer.enterEvents.Add(new EnterEvent { id = entity.entityID, x = entity.posX, z = entity.posZ });
                }

                // 通知管理器我的视野改变了，发送给客户端
                if (!updateContainer.IsEmpty)
                    aoiManager.OnEntityCellViewChange?.Invoke(this, updateContainer);
            }

            updateContainer.Clear();
            addCells.Clear();
            removeCells.Clear();
            aroundCells = null;
        }

        public void SetAroundCells(AOICell[] aroundCells)
        {
            if (entityDriveMode != EntityDriveMode.Client) return;
            this.aroundCells = aroundCells;
        }

        public void AddAroundCell(AOICell aoiCell)
        {
            if (entityDriveMode != EntityDriveMode.Client) return;
            addCells.Add(aoiCell);
        }
        public void RemoveAroundCell(AOICell aoiCell)
        {
            if (entityDriveMode != EntityDriveMode.Client) return;
            removeCells.Add(aoiCell);
        }
    }
}
