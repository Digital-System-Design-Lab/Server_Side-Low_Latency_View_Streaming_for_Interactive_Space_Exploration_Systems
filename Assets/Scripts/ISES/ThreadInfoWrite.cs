using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;

namespace ISESThreadInfoWrite
{
    class ThreadInfoWrite
    {

        public void Threadinfo()
        {
            Process proc = Process.GetCurrentProcess();
            ProcessThreadCollection ptc = proc.Threads;
            int i = 1;
            foreach(ProcessThread pt in ptc)
            {
                UnityEngine.Debug.LogErrorFormat("************{0} 번째 Thread info ************", i);
                UnityEngine.Debug.LogErrorFormat("ThreadId : {0} ", pt.Id);
                UnityEngine.Debug.LogErrorFormat("시작 시간 : {0}", pt.StartTime);
                UnityEngine.Debug.LogErrorFormat("우선 순위 : {0}", pt.BasePriority);
                UnityEngine.Debug.LogErrorFormat("상태 : {0}", pt.ThreadState);
                UnityEngine.Debug.LogErrorFormat("");

            }
        }
    }
}
