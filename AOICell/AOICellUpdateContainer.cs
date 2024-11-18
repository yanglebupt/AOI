using System.Collections.Generic;

namespace YLCommon.AOI
{
    public class AOICellUpdateContainer
    {
        public List<EnterEvent> enterEvents = new();
        public List<MoveEvent> moveEvents = new();
        public List<ExitEvent> exitEvents = new();

        public bool IsEmpty => enterEvents.Count == 0 && moveEvents.Count == 0 && exitEvents.Count == 0;

        public void Clear()
        {
            enterEvents.Clear();
            moveEvents.Clear();
            exitEvents.Clear();
        }
    }

    public struct EnterEvent
    {
        // 实体 ID
        public ulong id;
        // 实体 x 位置
        public float x;
        // 实体 z 位置
        public float z;
        public EnterEvent(ulong id, float x, float z) { 
            this.id = id;
            this.x = x;
            this.z = z;
        }
    }

    public struct MoveEvent
    {
        public ulong id;
        public float x;
        public float z;
        public MoveEvent(ulong id, float x, float z)
        {
            this.id = id;
            this.x = x;
            this.z = z;
        }
    }

    public struct ExitEvent
    {
        public ulong id;
        public ExitEvent(ulong id)
        {
            this.id = id;
        }
    }
}
