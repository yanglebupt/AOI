using MessagePack;
using System.Collections.Generic;

namespace YLCommon.AOI
{
    /**
     * 1. 宫格 cell 自适应生成（非规则宫格），空白的地方不需要生成宫格 
     *    - 根据玩家移动动态创建宫格，确保只有玩家经过的地方才需要创建宫格

     * 2. 动态确保每个玩家实体只能看到周围 9 个宫格内的其他玩家实体
     *    - 玩家实体需要写入增加视野，删除视野
     *    
     * 3. 同一个宫格里面的玩家实体， 视野一样合并处理
     *    - 宫格里面需要写入移动，进入，退出
     *    
     * 4. 剔除无客户端关注的服务端实体数据变化计算
     *    - 实体区分客户端驱动（玩家）和服务端驱动（怪物），怪物是不需要计算视野的，无客户端关注的宫格也是不需要计算的
     */
    public enum Cmd
    {
        ReqLogin,
        ResLogin,

        ResAOI,
        CreateCell,

        MovePos,
        Exit,
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial class NetHeader: TCPHeader
    {
        public Cmd cmd;
    }

    /////////////////////////// Bodys ///////////////////////////
   [MessagePackObject(keyAsPropertyName: true)]
    public partial class ReqLoginBody
    {
        public string account;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial class ResLoginBody
    {
        public ulong entityID;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial class CreateCellBody
    {
        public CreateCell[] cells;
    }
   [MessagePackObject(keyAsPropertyName: true)]
    public partial class CreateCell
    {
        public int xIndex;
        public int zIndex;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial class MovePosBody
    {
        public ulong entityId;
        public float posX;
        public float posZ;
    }


   [MessagePackObject(keyAsPropertyName: true)]
    public partial class ExitBody
    {
        public ulong entityId;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial class AOIBody
    {
        public int type;
        public List<EnterEventBody> enterEvents = new();
        public List<MoveEventBody> moveEvents = new();
        public List<ExitEventBody> exitEvents = new();
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial struct EnterEventBody
    {
        // 实体 ID
        public ulong entityId;
        // 实体 x 位置
        public float posX;
        // 实体 z 位置
        public float posZ;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial struct MoveEventBody
    {
        public ulong entityId;
        public float posX;
        public float posZ;
    }

   [MessagePackObject(keyAsPropertyName: true)]
    public partial struct ExitEventBody
    {
        public ulong entityId;
    }
}
