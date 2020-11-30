using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using ISESReqPacket;

namespace ISESLoader
{
    /// <summary>
    /// Loader class
    ///  ISES 에서 view image를 sub-segment 단위로 읽어온다. 이때 sub-segment를 load하는 작업은 background로 진행한다.
    /// 현재 버전은 local 환경이며 disk에서 memory로의 load이다. 
    /// 지원하는 기능으로는 순차 load, 역순 load, 이어서 load 이다.
    /// </summary>
    public class Loader
    {
        public int offset_start;
        public int offset_end;
        public bool read_dir;
        

        public void init()
        {
            offset_start = 0;
            offset_end = 0;
            read_dir = true; //true : 오름차순, false : 내림차순
        }

        public void loadsubseg(RequestPacket req_view)
        {
           
        }

    }
}
