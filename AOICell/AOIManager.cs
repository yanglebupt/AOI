using System;
using System.Collections.Generic;

namespace YLCommon.AOI
{

    /// <summary>
    /// 管理整个全部 AOI 区块
    /// </summary>
    public class AOIManager
    {
        // 全部区块，string 为索引号组成的字符
        private Dictionary<string, AOICell> cells = new();
        public Dictionary<string, AOICell> Cells => cells;
        // 全部实体
        private List<AOIEntity> entities = new();
        // 名字
        public string name = "";
        // AOI cell 的大小 
        public int cellSize = 20;

        public Action<AOIEntity, AOICellUpdateContainer> OnEntityCellViewChange;
        public Action<AOICell, AOICellUpdateContainer> OnCellEntityOPMerge;
#if DEBUG
        public Action<AOICell> OnCreateCell;
#endif

        public AOIEntity Enter(ulong entityID, float x, float z, EntityDriveMode entityDriveMode)
        {
            AOIEntity aoiEntity = new AOIEntity(entityID, this, entityDriveMode);
            // 第一次进入是传送进入的
            aoiEntity.UpdatePos(x, z, EntityOP.TransferEnter);
            entities.Add(aoiEntity);
            return aoiEntity;
        }

        public void Move(AOIEntity aoiEntity, float x, float z)
        {
            aoiEntity.UpdatePos(x, z);
        }

        public void Exit(AOIEntity aoiEntity)
        {
            string cellKey = AOICell.GetCellKey(aoiEntity.xIndex, aoiEntity.zIndex);
            if(cells.TryGetValue(cellKey, out AOICell aoiCell))
            {
                aoiCell.ExitCell(aoiEntity);
                if (!entities.Remove(aoiEntity))
                    Console.WriteLine($"AOIEntity {aoiEntity.entityID} not found");
            }
            else
                Console.WriteLine($"{cellKey} cell not found");
        }

        /// <summary>
        /// 哪个实体在 cell 里面移动
        /// </summary>
        public void MoveInsideCell(AOIEntity aoiEntity)
        {
            string cellKey = AOICell.GetCellKey(aoiEntity.xIndex, aoiEntity.zIndex);
            if (cells.TryGetValue(cellKey, out AOICell aoiCell))
                aoiCell.MoveCell(aoiEntity);
            else
                Console.WriteLine($"{cellKey} cell not found");
        }

        /// <summary>
        /// 哪个实体跨 cell，进入一个新的 cell
        /// </summary>
        public void MoveCrossCell(AOIEntity aoiEntity)
        {
            AOICell aoiCell = GetOrCreateAOICell(aoiEntity.xIndex, aoiEntity.zIndex);
            aoiCell.EnterCell(aoiEntity);

            if (cells.TryGetValue(
                AOICell.GetCellKey(aoiEntity.xLastIndex, aoiEntity.zLastIndex),
                out AOICell lastCell
            )) lastCell.exitEntities.Add(aoiEntity);
        }

        public AOICell? GetCell(int xIndex, int zIndex)
        {
            if (cells.TryGetValue(AOICell.GetCellKey(xIndex, zIndex), out AOICell cell)) return cell;
            return null;
        }

        /// <summary>
        /// 动态创建 cell，确保只有玩家经过的地方才有 cell
        /// </summary>
        public AOICell GetOrCreateAOICell(int xIndex, int zIndex)
        {
            AOICell aoiCell;
            string cellKey = AOICell.GetCellKey(xIndex, zIndex);
            if(!cells.TryGetValue(cellKey, out aoiCell)){
                aoiCell = new AOICell(xIndex, zIndex, this);
#if DEBUG
                OnCreateCell?.Invoke(aoiCell);
#endif
                cells[cellKey] = aoiCell;
            }
            // 创建周围宫格
            if (!aoiCell.IsCalcAround)
            {
                int index = 0;
                // 预计算外面两圈
                for (int x = xIndex - 2; x <= xIndex + 2; x++)
                {
                    for (int z = zIndex - 2; z <= zIndex + 2; z++)
                    {
                        string key = AOICell.GetCellKey(x, z);
                        AOICell cell;
                        if (!cells.TryGetValue(key, out cell))
                        {
                            cell = new AOICell(x, z, this);
#if DEBUG
                            OnCreateCell?.Invoke(cell);
#endif
                            cells[key] = cell;
                        }
                        // 内圈作为九宫格视野
                        if (x > xIndex - 2 && x < xIndex + 2 &&
                           z > zIndex - 2 && z < zIndex + 2)
                            aoiCell.Arounds[index++] = cell;
                    }
                }
                aoiCell.IsCalcAround = true;
            }
            return aoiCell;
        }

        // 驱动全部的实体计算 AOI 视野
        public void CalcAllEntitiesAOIChange()
        {
            for (int i = 0; i < entities.Count; i++)
                entities[i].CalcEntityCellViewChange();

            foreach (var item in cells)
            {
                AOICell aoiCell = item.Value;

                if (aoiCell.exitEntities.Count > 0)
                {
                    aoiCell.entities.ExceptWith(aoiCell.exitEntities);
                    aoiCell.exitEntities.Clear();
                }

                if (aoiCell.enterEntities.Count > 0)
                {
                    // 合并新进入的实体
                    aoiCell.entities.UnionWith(aoiCell.enterEntities);
                    aoiCell.enterEntities.Clear();
                }

                aoiCell.CalcCellOP();
            }
        }

    }
}
