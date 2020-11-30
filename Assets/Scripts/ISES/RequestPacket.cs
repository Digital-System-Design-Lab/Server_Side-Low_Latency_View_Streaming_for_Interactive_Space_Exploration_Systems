using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace ISESReqPacket
{
    
    public struct RequestPacket
    {
        public int pos_x;
        public int pos_y;
        public int head_dir;
        public int eslevel;
        public int stat;
        public string misslist;
        public int offset_start;
        public int offset_end;
        public bool order; //true : 오름차순, false : 내림차순

        
        public RequestPacket(int pos_x, int pos_y, int head_dir, int eslevel, int stat, string misslist)
        {
            this.pos_x = pos_x;
            this.pos_y = pos_y;
            this.head_dir = head_dir;
            this.eslevel = eslevel;
            this.stat = stat;
            this.misslist = misslist;
            offset_start = 0;
            offset_end = 119;
            order = true;
        }
    }
}
