using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ISESStructure;

namespace ISESServer { 
    class Server
    {
        byte[] viewtemp;
        #region load processes
        public void loadSubSeg(RequestPacket packet, ref subseg_container container)
        {
            string region = packet.loc.getPath();
            int start = -1; int end = -1;
            if (region.Substring(0,1).Equals("R"))
            {
                start = packet.loc.get_seg_pos().start_x;
                end = packet.loc.get_seg_pos().end_x;
            }
            else if(region.Substring(0, 1).Equals("C"))
            {
                start = packet.loc.get_seg_pos().start_y;
                end = packet.loc.get_seg_pos().end_y;
            }

            UnityEngine.Debug.Log("lodSubSeg : " + packet.result_cache.getStat());
            UnityEngine.Debug.Log("Misslist : " + packet.result_cache.getMisslist());

            for (int i=start; i <= end; i++)
            {
                //Task load_task = Task.Run(() =>
                //{
                //    load_view(container, packet.result_cache.getMisslist(), i, 10.0f, region);
                //});
                //UnityEngine.Debug.LogWarningFormat("load view num {0}", i);
                //UnityEngine.Debug.LogWarningFormat("misslist : {0}", packet.result_cache.getMisslist());
                load_view(ref container, packet.result_cache.getMisslist(), i, 3.0f, region, start);

            }


            //08.17 오늘은 여기까지...
        }

        public void load_view(ref subseg_container container, string misslist, int iter, float delay, string region, int start)
        {
            DateTime temp = DateTime.Now;
            for(int dir = 0; dir < 4; dir++)
            {
                int missdigit = Convert.ToInt32(misslist.Substring(dir, 1));
                if(missdigit != 0)
                {
                    viewtemp = File.ReadAllBytes(setdirectory(missdigit, iter, region, dir));
                    container.setView(dir, viewtemp, (iter - start));
                    //UnityEngine.Debug.LogFormat("Iter : {0} pos_x : {1} view size : {2}", iter, (iter - start), viewtemp.Length);
                }
            }
            if (container.offset_e < container.segsize-1)
            {
                container.offset_e++;
                //UnityEngine.Debug.Log("Call setView function");
            }

            //dodelay(rnd.Next(3, 10));
            //dodelay(delay);
            float loadtime = (DateTime.Now - temp).Milliseconds;
            
            //UnityEngine.Debug.LogWarningFormat("{1} view load end to end delay : {0:f4} ms || view cnt : {2} ", loadtime, iter, container.fviews.Count);
        }

        public void load_singleview(jpeg_container container, string misslist, int iter, float delay, string region)
        {
            DateTime temp = DateTime.Now;
            for (int dir = 0; dir < 4; dir++)
            {
                int missdigit = Convert.ToInt32(misslist.Substring(dir, 1));
                if (missdigit != 0)
                {
                    viewtemp = File.ReadAllBytes(setdirectory(missdigit, iter, region, dir));
                    container.setView(viewtemp, dir);
                    //UnityEngine.Debug.LogFormat("Iter : {0} pos_x : {1} view size : {2}", iter, (iter - start), viewtemp.Length);
                }
            }
            //dodelay(rnd.Next(3, 10));
            dodelay(delay);

            //UnityEngine.Debug.LogWarningFormat("{1} view load end to end delay : {0:f4} ms || view cnt : {2} ", loadtime, iter, container.fviews.Count);
        }

        public void dodelay(float target_delay)
        {
            DateTime temp = DateTime.Now;
            float excution_time = 0.0f;
            while (target_delay > excution_time)
            {
                excution_time = (DateTime.Now - temp).Milliseconds;
            }
            //UnityEngine.Debug.LogWarningFormat("Delay time : {0:f3}", excution_time);
        }
        public string setdirectory(int digit, int pos_x, string region, int direction)
        {
            string[] ori = { "LEFT", "FRONT", "RIGHT", "BACK" };
            string dir = "";
            //string quality = digit == 1 ? "1" : "4";
            string quality = "";
            if(digit == 1)
            {
                quality = "1";
            }
            else if (digit == 2)
            {
                quality = "4";
            }
            if (pos_x < 9)
                dir = "S:\\LFDATA\\1012\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "000" + (pos_x + 1).ToString() + ".jpg";
            else if (pos_x < 99)
                dir = "S:\\LFDATA\\1012\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "00" + (pos_x + 1).ToString() + ".jpg";
            else if (pos_x < 999)
                dir = "S:\\LFDATA\\1012\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "0" + (pos_x + 1).ToString() + ".jpg";

            Printer.LogPrint(dir);

            return dir;
        }
        #endregion


        #region sendprocesses
        //Network의 경우는 stream 통해서 전달을 해야한다.
        //local의 경우는 load process랑 통합.
        #endregion
    }
}
