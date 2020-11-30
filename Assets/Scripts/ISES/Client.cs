using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using ISESCache;
using ISESESAS;
using ISESStructure;
using UnityEngine;
using _NS_5DoFVR;


namespace ISESClient
{
    class Client
    {
        #region DllImport
        [DllImport("libjpeg_turbo_Dll")]
        public  static extern int decoding(byte[] input, byte[] output, int size, int width, int height, int bpp);
        [DllImport("libjpeg_turbo_Dll")]
        public static extern void Resize(byte[] src, byte[] dst, int size, int owidth, int oheight, int rwidth, int rheight);
        [DllImport("libjpeg_turbo_Dll")]
        public static extern void Resize_lite(byte[] src, byte[] dst, int size, int owidth, int oheight, int rwidth, int rheight);
        [DllImport("libjpeg_turbo_Dll")]
        public static extern void Resize_Slite(byte[] src, byte[] dst, int size, int owidth, int oheight, int rwidth, int rheight);
        [DllImport("libjpeg_turbo_Dll")]
        private static extern void Resizing_Parall(byte[] input, byte[] output, int size, int owidth, int oheight, int rwidth, int rheight, int threadnum, int order, int bpp);


        #endregion

        int interface_mode = 0; //0 : test mode, 1 : user mode

        #region ProposedScheme Class
        public GDC cache;
        public ESAS esas;
        #endregion

        #region UnityObjects
        
        #endregion

        #region addtionalclass vars
        public XboxController xbox;
        public NS_5DoFVR myVR;
        #endregion

        #region essential vars
        public viewinfo info;
        public Path[] pathlist;
        public List<SubRange> range;
        public black_imgs black_img;
        #endregion

        #region Byte variables
        byte[] temp_viewL;
        byte[] temp_viewF;
        byte[] temp_viewR;
        byte[] temp_viewB;
        byte[] temp_viewOnlyDec;
        byte[] DSsubviewL;
        byte[] DSsubviewF;
        byte[] DSsubviewR;
        byte[] DSsubviewB;
        byte[] DSsubviewOnlyDec;
        byte[] decoded_viewL;
        byte[] decoded_viewF;
        byte[] decoded_viewR;
        byte[] decoded_viewB;
        byte[] decoded_viewOnlyDec;
        byte[] temp;
        byte[] pre_frame;
        byte[] framebuffer;
        #endregion

        #region Tasklist
        public Task[] resizelistL;
        public Task[] resizelistF;
        public Task[] resizelistR;
        public Task[] resizelistB;
        public Thread[] Threadlist;
        #endregion

        public string init(Cacheinfo cacheinfo, ESASinfo esasinfo, viewinfo info)
        {
            this.info = info;
            
            #region initialize objects related schemes
            cache = new GDC();
            esas = new ESAS(esasinfo);
            cache.init(cacheinfo);
            range = calcSubrange(cacheinfo.seg_size);
            black_img.init(info.width / 4, info.height, info.bpp);
            #endregion

            #region initialize additional classes
            myVR = new NS_5DoFVR();
            myVR.parsing_data();
            xbox = new XboxController();
            #endregion

            #region initialize byte arrays
            DSsubviewL = new byte[((info.width / 4) / 2) * ((info.height) / 2) * info.bpp];
            DSsubviewF = new byte[((info.width / 4) / 2) * ((info.height) / 2) * info.bpp];
            DSsubviewR = new byte[((info.width / 4) / 2) * ((info.height) / 2) * info.bpp];
            DSsubviewB = new byte[((info.width / 4) / 2) * ((info.height) / 2) * info.bpp];
            DSsubviewOnlyDec = new byte[((info.width / 4) / 2) * ((info.height) / 2) * info.bpp];
            decoded_viewL = new byte[(info.width / 4) * (info.height) * info.bpp];
            decoded_viewF = new byte[(info.width / 4) * (info.height) * info.bpp];
            decoded_viewR = new byte[(info.width / 4) * (info.height) * info.bpp];
            decoded_viewB = new byte[(info.width / 4) * (info.height) * info.bpp];
            decoded_viewOnlyDec = new byte[(info.width / 4) * (info.height) * info.bpp];
            framebuffer = new byte[info.width * info.height * info.bpp];
            pre_frame = new byte[info.width * info.height * info.bpp];
            #endregion

            #region initialize Task lists
            resizelistL = new Task[8];
            resizelistF=new Task[8];
            resizelistR=new Task[8];
            resizelistB=new Task[8];

            Threadlist = new Thread[4];
            #endregion

            return "[Client] initialization process was completed...";
        }
        #region Positional processes
        public Path receivePos(int time)
        {
            Path currentPos = new Path();
            switch (interface_mode)
            {
                case 0:
                    currentPos = getPath(time);
                    break;
                case 1:

                    break;
                default:
                    UnityEngine.Debug.LogErrorFormat("[Error-seo] Interface mode must be zero or one, current interface mode : {0}", interface_mode);
                    break;
            }
            return currentPos;
        }
        public Loc TransP2L(Path currentPos)
        {
            myVR.classify_Location(currentPos.x, currentPos.y);
            string region = myVR.getCurregion();
            int i =0;
            int iter = 0;
            int Pos_x = myVR.getPos_X(); int Pos_y = myVR.getPos_Y();
            int start_x = 0; int end_x = 0;
            int start_y = 0; int end_y = 0;


            for (i = 0; i < range.Count; i++)
            {
                if((Pos_x>=range[i]._start) && (Pos_x <= range[i]._end) && region.Substring(0,1).Equals("R"))
                {
                    start_x = range[i]._start;
                    end_x = range[i]._end;
                    iter = Pos_x - start_x;
                }
                if ((Pos_y >= range[i]._start) && (Pos_y <= range[i]._end) && region.Substring(0, 1).Equals("C"))
                {
                    start_y = range[i]._start;
                    end_y = range[i]._end;
                    iter = Pos_y - start_y;
                }
            }
            //cache.cacheinfo.cachesize;
            sub_segment_pos seg_pos = new sub_segment_pos(start_x, end_x, start_y, end_y, cache.cacheinfo.seg_size);
            //UnityEngine.Debug.LogError(seg_pos.ToString());
            //UnityEngine.Debug.LogErrorFormat("X : {0}, Y : {1} iter : {2} start : {3} end : {4}", Pos_x, Pos_y, iter, start_y, end_y);
            seg_pos.calcSeg_pos(cache.cacheinfo.seg_size, myVR.getCurregion().Substring(0, 1), currentPos.x, currentPos.y, myVR.getOrigin_X(), myVR.getOrigin_Y());

            Loc loc = new Loc(myVR.getCurregion(), seg_pos, iter);
            loc.setPos_X(Pos_x); loc.setPos_Y(Pos_y);
            //UnityEngine.Debug.LogFormat("Seg x {0} y {1}", seg_pos.seg_pos_x, seg_pos.seg_pos_y);
            //loc.setSeg_pos(seg_pos);

            return loc;
        }

        public int getHeadDirection(int dir)
        {
            int hd = 0;
            if ((dir >= 0 && dir < 30) || (dir >= 330 && dir <= 360))
            {
                hd = 2; //RIGHT
            }
            else if ((dir >= 60 && dir < 120))
            {
                hd = 1; //FRONT
            }
            else if ((dir >= 150 && dir < 210))
            {
                hd = 0; //LEFT
            }
            else if ((dir >= 240 && dir < 300))
            {
                hd = 3; //BACK
            }
            return hd;
        }
        public int calcESL(Path currentPos, int time)
        {
            esas.addTuple(currentPos);
            float es = esas.calc_es();
            int esl = esas.calcesl(es);
            return esl;
        }
        public int calcESL(Path currentPos)
        {
            float es = currentPos.acc;
            int esl = esas.calcesl(es);
            return esl;
        }

        public string getReqViewlist(int esl, int hd)
        {
            string resultlist = esas.calcviewlist(esl, hd);
            return resultlist;
        }
        #endregion

        #region cache processes
        public out_cache_search searchCache(Loc loc, string reqlist)
        {
            out_cache_search result = new out_cache_search();
            int idx = cache.find_records(loc);
            result.setIdx(idx);
            cache.checkStat(idx, reqlist, ref result);
            return result;
        }

        public out_cache_search searchCache_view(Loc loc, string reqlist)
        {
            out_cache_search result = new out_cache_search();
            result.setStat(0);
            int idx = -1;
            result.setIdx(idx);
            //cache.checkStat(idx, reqlist, ref result);
            result.setRenderlist(reqlist);
            result.setHitlist("0000");
            result.setMisslist(reqlist);
            return result;
        }

        int prev_x = 0;int prev_y = 0;


        public void updateCache(RequestPacket packet, subseg_container subcontainer, int iter, int time)
        {
            cache.pathcalc.updatepath(packet.pos.getX(), packet.pos.getY(), prev_x, prev_y);
            cache.updateCache(packet, subcontainer, iter, time);
            cache.recordPrevPacket(packet);
            prev_x = packet.pos.getX();
            prev_y = packet.pos.getY();
            //cache.printCache(time);
        }
        #endregion


        #region network processes
        public RequestPacket requestView(Pos pos, out_cache_search result_cache, Loc loc)
        {
            RequestPacket reqPacket = new RequestPacket(pos, result_cache, loc);
            //network을 통해서 전송을 하는 경우 networkstream으로 전송해야한다.
            return reqPacket;
        }

        public void receiveView() // 이 함수도 local에선 server의 load process와 통합
        {

        }
        #endregion

        #region Display processes

        public byte[] RenderView(RequestPacket packet, pre_renderViews preRenderViews, subseg_container subcontainer, view_container container, int iter)
        {
            //iter는 sub-segment내의 view의 순서이다. 0~seg_size
            //decoding & up-sampling
            //UnityEngine.Debug.Log("Cache stat : " + packet.result_cache.getStat().ToString());
            byte[] empty = { 0 };
            switch (packet.result_cache.getStat())
            {
                //각 encoded image을 view_container로 담아야한다. 
                case 0:
                case 3:
                    //subseg_container으로만 rendering 작업을 한다.
                    rendersubviews(subcontainer, container, packet.result_cache.getRenderlist(), iter, packet);
                    //return Rendering(subcontainer, container, iter, packet.result_cache.getRenderlist());
                    break;
                case 1:
                    //cache에 있는 subview로만 rendering한다.
                    //UnityEngine.Debug.LogErrorFormat("Cache count : {0} / view iter : {1}", cache.cntRecords(packet.result_cache.getIdx()), iter);
                    assignView(subcontainer, packet.result_cache.getHitlist(), packet.result_cache.getIdx(), iter);
                    DateTime render_start = DateTime.Now;
                    rendersubviews(subcontainer, container, packet.result_cache.getRenderlist(), iter, packet);
                    double result = (DateTime.Now - render_start).TotalMilliseconds;
                    //UnityEngine.Debug.LogErrorFormat("Rendersubviews function's delay : {0:f3}", result);
                    break;
                //return Rendering(subcontainer, container, iter, packet.result_cache.getRenderlist());
                //임시로
                //rendersubviews(subcontainer, container, packet.result_cache.getRenderlist(), iter);
                case 2:
                    //cache에 있는 것 먼저 decoding
                    UnityEngine.Debug.LogErrorFormat("Cache count : {0} / view iter : {1}", cache.cntRecords(packet.result_cache.getIdx()), iter);
                    assignView(container, preRenderViews, packet.result_cache.getHitlist(), iter);
                    //subseg_container으로만 rendering 작업
                    UnityEngine.Debug.Log("[PARTIAL HIT] Rendering List : " + packet.result_cache.getRenderlist() + " Hit list : " + packet.result_cache.getHitlist());
                    rendersubviews(subcontainer, container, packet.result_cache.getRenderlist(), packet.result_cache.getHitlist(), iter, packet);
                    break;
                    //return Rendering(subcontainer, container, iter, packet.result_cache.getRenderlist());
            }
            return render_frame(container);
        }
        public bool waitfilling(RequestPacket packet, int iter)
        {
            if(cache.cntRecords(packet.result_cache.getIdx()) < iter)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion




        #region other methods
        public void read_path(string filename)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            if (textValue.Length > 0)
            {
                pathlist = new Path[textValue.Length];
                for(int iter = 0; iter < textValue.Length;iter++)
                {
                    myString = textValue[iter].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    pathlist[iter].x = Convert.ToInt32(myString[0]);
                    pathlist[iter].y = Convert.ToInt32(myString[1]);
                    pathlist[iter].hd = Convert.ToInt32(myString[2]);
                    pathlist[iter].acc = (float)Convert.ToDouble(myString[3]);
                }
            }

        }
        public void read_path(string filename, List<int> list)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            Path[] backlist;
            Path[] golist = new Path[cache.cacheinfo.seg_size];
            int cnt = 0;
            if (textValue.Length > 0)
            {
                pathlist = new Path[textValue.Length+(2*list.Count)*cache.cacheinfo.seg_size];
                UnityEngine.Debug.LogErrorFormat("pathlist size : {0}", pathlist.Length);
                int offset = 0;
                for (int iter = 0; iter < textValue.Length; iter++)
                {
                    if ((iter / (cache.cacheinfo.seg_size)) == list[cnt] && (iter % (cache.cacheinfo.seg_size)) == 0)
                    {
                        backlist = Reverse(golist);
                        //for(int k = 0; k < backlist.Length; k++)
                        //{
                        //    UnityEngine.Debug.LogErrorFormat("Backlist {0} {1},{2}", k, backlist[k].x, backlist[k].y);
                        //}
                        //UnityEngine.Debug.Log("iter " + iter + " reverse idx " + (iter / (cache.cacheinfo.seg_size)) + " backlist size : " + backlist.Length + " " +offset);
                        for (int i = 0; i < cache.cacheinfo.seg_size; i++)
                        {
                            pathlist[offset+i] = backlist[i];
                            pathlist[offset + cache.cacheinfo.seg_size + i] = golist[i];
                        }
                        offset += (2*cache.cacheinfo.seg_size);
                        if (cnt < list.Count - 1)
                        {
                            cnt++;
                        }
                        offset++;
                        myString = textValue[iter].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        pathlist[offset].x = Convert.ToInt32(myString[0]);
                        pathlist[offset].y = Convert.ToInt32(myString[1]);
                        pathlist[offset].hd = Convert.ToInt32(myString[2]);
                        pathlist[offset].acc = (float)Convert.ToDouble(myString[3]);

                        golist[iter % (cache.cacheinfo.seg_size)].x = Convert.ToInt32(myString[0]);
                        golist[iter % (cache.cacheinfo.seg_size)].y = Convert.ToInt32(myString[1]);
                        golist[iter % (cache.cacheinfo.seg_size)].hd = Convert.ToInt32(myString[2]);
                        golist[iter % (cache.cacheinfo.seg_size)].acc = (float)Convert.ToDouble(myString[3]);
                        //UnityEngine.Debug.Log("golist idx : " + iter % (cache.cacheinfo.seg_size));
                    }
                    else
                    {
                        myString = textValue[iter].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        pathlist[offset].x = Convert.ToInt32(myString[0]);
                        pathlist[offset].y = Convert.ToInt32(myString[1]);
                        pathlist[offset].hd = Convert.ToInt32(myString[2]);
                        pathlist[offset].acc = (float)Convert.ToDouble(myString[3]);

                        golist[iter % (cache.cacheinfo.seg_size)].x = Convert.ToInt32(myString[0]);
                        golist[iter % (cache.cacheinfo.seg_size)].y = Convert.ToInt32(myString[1]);
                        golist[iter % (cache.cacheinfo.seg_size)].hd = Convert.ToInt32(myString[2]);
                        golist[iter % (cache.cacheinfo.seg_size)].acc = (float)Convert.ToDouble(myString[3]);
                        //UnityEngine.Debug.Log("golist idx : " + iter % (cache.cacheinfo.seg_size));
                        offset++;

                    }
                }
            }

            for(int i = 0; i < pathlist.Length; i++)
            {
                UnityEngine.Debug.LogFormat("{0} {1}, {2}", i, pathlist[i].x, pathlist[i].y);
            }

        }
        
        public Path[] Reverse(Path[] inputArray)
        {
            //for(int i = 0; i < inputArray.Length; i++)
            //{
            //    UnityEngine.Debug.LogErrorFormat("Reverse function {0} {1},{2}", i, inputArray[i].x, inputArray[i].y);
            //}
            Path[] result = new Path[inputArray.Length];
            Array.Copy(inputArray, 0, result, 0, result.Length);
            Array.Reverse(result);
            return result;
        }

        
        public Path getPath(int time)
        {
            if(pathlist.Length <= time)
            {
                return pathlist[pathlist.Length - 1];
            }
            else
            {
                return pathlist[time];
            }
        }
        public List<SubRange> calcSubrange(int seg_size)
        {
            List<SubRange> subrange = new List<SubRange>();
            int numOfrange = 120 / seg_size;
            for(int iter = 0; iter < numOfrange; iter++)
            {
                int start = seg_size * (iter);
                int end = seg_size * (iter+1) - 1;
                SubRange range = new SubRange(start, end);
                subrange.Add(range);
            }
            return subrange;
        }
        public byte[] render_frame(view_container container)
        {
            int one_eighth_length = container.owidth/2 * container.bpp;
            int quater_length = one_eighth_length*2;
            int back_offset = one_eighth_length;
            int front_offset = 0;
            int left_offset = 0;
            int right_offset = 0;
            int offset = 0;

            

            for (int h = 0; h < container.height; h++)
            {
                System.Buffer.BlockCopy(container.bsubview, back_offset, framebuffer, offset, one_eighth_length);
                back_offset -= one_eighth_length;
                offset += one_eighth_length;
                System.Buffer.BlockCopy(container.lsubview, left_offset, framebuffer, offset, quater_length);
                left_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(container.fsubview, front_offset, framebuffer, offset, quater_length);
                front_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(container.rsubview, right_offset, framebuffer, offset, quater_length);
                right_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(container.bsubview, back_offset, framebuffer, offset, one_eighth_length);
                back_offset += (quater_length + one_eighth_length);
                offset += one_eighth_length;
            }
            

            return framebuffer;
            
        }
        public void assignView(subseg_container subcontainer, string list, int idx, int iter)
        {
            int[] digits = new int[4];
            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] != 0)
                {
                    temp = cache.GetRecord(idx).getView(i)[iter];
                    subcontainer.setView_C2S(i, temp, iter);
                }
            }
        }

        public void assignView(view_container container, pre_renderViews preRenderViews, string list, int iter)
        {
            int[] digits = new int[4];
            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] != 0)
                {
                    UnityEngine.Debug.LogFormat("{0} 에서 {1} quality로 보충 view 길이 : {2}", i, digits[i], preRenderViews.getView(i, iter).Length);
                    container.setView(i, preRenderViews.getView(i, iter));
                }
            }
        }
        public void pre_rendersubviews(pre_renderViews preRenderViews, string list, int iter, int idx)
        {
            int[] digits = new int[4];
            bool[] is_created = new bool[4];
            UnityEngine.Debug.LogErrorFormat("Hit list : {0} hit index : {1} iter : {2}", list, idx, iter);
            for (int i = 0; i < 4; i++)
            {
                is_created[i] = false;
            }
            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] == 1)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_lDS(iter, preRenderViews, idx, i));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_fDS(iter, preRenderViews, idx, i));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            //renderpart_fDS(iter, subcontainer, container);
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_rDS(iter, preRenderViews, idx, i));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            //renderpart_rDS(iter, subcontainer, container);
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_bDS(iter, preRenderViews, idx, i));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            //renderpart_bDS(iter, subcontainer, container);
                            break;
                    }
                }
                else if (digits[i] == 2)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_l(iter, preRenderViews, idx, i));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            //renderpart_l(iter, subcontainer, container);
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_f(iter, preRenderViews, idx, i));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_r(iter, preRenderViews, idx, i));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_b(iter, preRenderViews, idx, i));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            break;
                    }
                    //renderthread.IsBackground = true;
                    //renderthread.Start();
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (is_created[i])
                {
                    UnityEngine.Debug.Log(i + " 번째 thread 종료 대기");
                    Threadlist[i].Join();
                    UnityEngine.Debug.Log(i + " 번째 thread 종료");
                }
            }
        }

        public void preRendering(RequestPacket packet, pre_renderViews preRenderViews, viewinfo info)
        {
            UnityEngine.Debug.LogFormat("{0} cached idx // {1}", packet.result_cache.getIdx(), cache.find_records(packet.loc));
            for(int i = 0; i < cache.cacheinfo.seg_size ; i++)
            {
                pre_rendersubviews(preRenderViews, packet.result_cache.getHitlist(), i, packet.result_cache.getIdx());
            }
        }

        public byte[] Rendering(subseg_container subcontainer, view_container container, int iter, string list)
        {
            int[] digit = new int[4];
            bool[] isStartTask = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                digit[i] = Convert.ToInt32(list.Substring(i, 1));
                isStartTask[i] = false;
            }
            Task[] tasklist = new Task[4];
            //Left decoding & resizing
            if(digit[0] == 1)
            {
                tasklist[0] = Task.Run(() =>
                {
                    temp_viewL = subcontainer.getView(0, iter);
                    int err = decoding(temp_viewL,
                                  DSsubviewL,
                                  temp_viewL.Length,
                                  container.swidth,
                                  container.sheight,
                                  container.bpp);
                    Resize_Slite(DSsubviewL,
                           decoded_viewL,
                           DSsubviewL.Length,
                           container.swidth,
                           container.sheight,
                           container.owidth,
                           container.oheight);
                    container.setView(0, decoded_viewL);
                });
                isStartTask[0] = true;
            }
            else if (digit[0] == 2)
            {
                tasklist[0] = Task.Run(() =>
                {
                    temp_viewL = subcontainer.getView(0, iter);
                    int err = decoding(temp_viewL,
                                  decoded_viewL,
                                  temp_viewL.Length,
                                  container.owidth,
                                  container.oheight,
                                  container.bpp);
                    container.setView(0, decoded_viewL);
                });
                isStartTask[0] = true;
            }

            //Front decoding & resizing
            if (digit[1] == 1)
            {
                tasklist[1] = Task.Run(() =>
                {
                    temp_viewF = subcontainer.getView(1, iter);
                    int err = decoding(temp_viewF,
                                  DSsubviewF,
                                  temp_viewF.Length,
                                  container.swidth,
                                  container.sheight,
                                  container.bpp);
                    Resize_Slite(DSsubviewF,
                           decoded_viewF,
                           DSsubviewF.Length,
                           container.swidth,
                           container.sheight,
                           container.owidth,
                           container.oheight);
                    container.setView(1, decoded_viewF);
                });
                isStartTask[1] = true;
            }
            else if (digit[1] == 2)
            {
                tasklist[1] = Task.Run(() =>
                {
                    temp_viewF = subcontainer.getView(1, iter);
                    int err = decoding(temp_viewF,
                                  decoded_viewF,
                                  temp_viewF.Length,
                                  container.owidth,
                                  container.oheight,
                                  container.bpp);
                    container.setView(1, decoded_viewF);
                });
                isStartTask[1] = true;
            }

            //Right decoding & resizing
            if (digit[2] == 1)
            {
                tasklist[2] = Task.Run(() =>
                {
                    temp_viewR = subcontainer.getView(2, iter);
                    int err = decoding(temp_viewR,
                                  DSsubviewR,
                                  temp_viewR.Length,
                                  container.swidth,
                                  container.sheight,
                                  container.bpp);
                    Resize_Slite(DSsubviewR,
                           decoded_viewR,
                           DSsubviewR.Length,
                           container.swidth,
                           container.sheight,
                           container.owidth,
                           container.oheight);
                    container.setView(2, decoded_viewR);
                });
                isStartTask[2] = true;
            }
            else if (digit[2] == 2)
            {
                tasklist[2] = Task.Run(() =>
                {
                    temp_viewR = subcontainer.getView(2, iter);
                    int err = decoding(temp_viewR,
                                  decoded_viewR,
                                  temp_viewR.Length,
                                  container.owidth,
                                  container.oheight,
                                  container.bpp);
                    container.setView(2, decoded_viewR);
                });
                isStartTask[2] = true;
            }

            //Back decoding & resizing
            if (digit[3] == 1)
            {
                tasklist[3] = Task.Run(() =>
                {
                    temp_viewB = subcontainer.getView(3, iter);
                    int err = decoding(temp_viewB,
                                  DSsubviewB,
                                  temp_viewB.Length,
                                  container.swidth,
                                  container.sheight,
                                  container.bpp);
                    Resize_Slite(DSsubviewB,
                           decoded_viewB,
                           DSsubviewB.Length,
                           container.swidth,
                           container.sheight,
                           container.owidth,
                           container.oheight);
                    container.setView(3, decoded_viewB);
                });
                isStartTask[3] = true;
            }
            else if (digit[3] == 2)
            {
                tasklist[3] = Task.Run(() =>
                {
                    temp_viewB = subcontainer.getView(3, iter);
                    int err = decoding(temp_viewB,
                                  decoded_viewB,
                                  temp_viewB.Length,
                                  container.owidth,
                                  container.oheight,
                                  container.bpp);
                    container.setView(3, decoded_viewB);
                });
                isStartTask[3] = true;
            }

            if (isStartTask[0]) { tasklist[0].Wait(); }
            if (isStartTask[1]) { tasklist[1].Wait(); }
            if (isStartTask[2]) { tasklist[2].Wait(); }
            if (isStartTask[3]) { tasklist[3].Wait(); }

            //render to framebuffer

            int one_eighth_length = container.owidth / 2 * container.bpp;
            int quater_length = one_eighth_length * 2;
            int back_offset = one_eighth_length;
            int front_offset = 0;
            int left_offset = 0;
            int right_offset = 0;
            int offset = 0;

            for (int h = 0; h < container.height; h++)
            {
                System.Buffer.BlockCopy(decoded_viewB, back_offset, framebuffer, offset, one_eighth_length);
                back_offset -= one_eighth_length;
                offset += one_eighth_length;
                System.Buffer.BlockCopy(decoded_viewL, left_offset, framebuffer, offset, quater_length);
                left_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(decoded_viewF, front_offset, framebuffer, offset, quater_length);
                front_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(decoded_viewR, right_offset, framebuffer, offset, quater_length);
                right_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(decoded_viewB, back_offset, framebuffer, offset, one_eighth_length);
                back_offset += (quater_length + one_eighth_length);
                offset += one_eighth_length;
            }
            return framebuffer;
        }

        /*
        void SF_rendering(byte[] imagebytes, byte[] left_image, byte[] right_image)
        {
            int err;
            var resize1 = Task.Run(() =>
            {
                err = decoding(imagebytes, resized_image, imagebytes.Length, (width / 4) / 2, height / 2, bpp);
                Resize_Slite(resized_image, part_decoded_image, resized_image.Length, (width / 4) / 2, height / 2, width / 4, height);
            });
            var resize2 = Task.Run(() =>
            {
                err = decoding(left_image, left_resized_image, left_image.Length, (width / 4) / 2, height / 2, bpp);
                Resize_Slite(left_resized_image, left_part_decoded_image, left_resized_image.Length, (width / 4) / 2, height / 2, width / 4, height);
            });
            var resize3 = Task.Run(() =>
            {
                err = decoding(right_image, right_resized_image, right_image.Length, (width / 4), height / 2, bpp);
                Resize_Slite(right_resized_image, right_part_decoded_image, right_resized_image.Length, (width / 4) / 2, height / 2, width / 4, height);
            });

            resize1.Wait();
            resize2.Wait();
            resize3.Wait();
            int back_L_offset = 0;
            int back_R_offset = 0;
            int front_offset = 0;
            int left_offset = 0;
            int right_offset = 0;
            int offset = 0;
            DateTime render_time = DateTime.Now;
            for (int h = 0; h < height; h++)
            {
                System.Buffer.BlockCopy(back_L_img, back_L_offset, frame_buffer, offset, one_eighth_length);
                back_L_offset += one_eighth_length;
                offset += one_eighth_length;
                System.Buffer.BlockCopy(left_part_decoded_image, left_offset, frame_buffer, offset, quater_length);
                left_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(part_decoded_image, front_offset, frame_buffer, offset, quater_length);
                front_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(right_part_decoded_image, right_offset, frame_buffer, offset, quater_length);
                right_offset += quater_length;
                offset += quater_length;
                System.Buffer.BlockCopy(back_R_img, back_R_offset, frame_buffer, offset, one_eighth_length);
                back_R_offset += one_eighth_length;
                offset += one_eighth_length;
            }
            texture.LoadRawTextureData(frame_buffer);
            texture.Apply();
            float render_result = (DateTime.Now - render_time).Milliseconds;
            UnityEngine.Debug.LogErrorFormat("Byte -> Texture render time : {0:f3}", render_result);

        }
        */

        /*
        public void rendersubviews(subseg_container subcontainer, view_container container, string list, int iter)
        {
            int[] digits = new int[4];
            bool[] is_created = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                is_created[i] = false;
            }
            Thread[] Threadlist = new Thread[4];
            Task[] Tasklist = new Task[4];
            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] == 1)
                {
                    switch (i)
                    {
                        case 0:
                            Tasklist[0] = Task.Run(() => renderpart_lDS(iter, subcontainer, container));
                            is_created[0] = true;
                            break;
                        case 1:
                            Tasklist[1] = Task.Run(() => renderpart_fDS(iter, subcontainer, container));
                            is_created[1] = true;
                            break;
                        case 2:
                            Tasklist[2] = Task.Run(() => renderpart_rDS(iter, subcontainer, container));
                            is_created[2] = true;
                            break;
                        case 3:
                            Tasklist[3] = Task.Run(() => renderpart_bDS(iter, subcontainer, container));
                            is_created[3] = true;
                            break;
                    }
                }
                else if (digits[i] == 2)
                {
                    switch (i)
                    {
                        case 0:
                            Tasklist[0] = Task.Run(() => renderpart_l(iter, subcontainer, container));
                            is_created[0] = true;
                            break;
                        case 1:
                            Tasklist[1] = Task.Run(() => renderpart_f(iter, subcontainer, container));
                            is_created[1] = true;
                            break;
                        case 2:
                            Tasklist[2] = Task.Run(() => renderpart_r(iter, subcontainer, container));
                            is_created[2] = true;
                            break;
                        case 3:
                            Tasklist[3] = Task.Run(() => renderpart_b(iter, subcontainer, container));
                            is_created[3] = true;
                            break;
                    }
                }
                else if (digits[i] == 0)
                {
                    container.setView(i, black_img.getView(i));
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (is_created[i]) { Tasklist[i].Wait(); }
            }
            //if (is_created[0]) { tasklist[0].Wait(); }
            //if (is_created[1]) { tasklist[1].Wait(); }
            //if (is_created[2]) { tasklist[2].Wait(); }
            //if (is_created[3]) { tasklist[3].Wait(); }
        }
        */
        /*
        public void rendersubviews(subseg_container subcontainer, view_container container, string list, int iter, RequestPacket packet)
        {
            int[] digits = new int[4];
            bool[] is_created = new bool[4];
            bool[] isRendered = new bool[4];

            for(int i=0;i<4;i++)
            {
                is_created[i] = false;
            }

            DateTime[] stopwatch = new DateTime[4];
            double[] renderingtime = new double[4];

            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] == 1)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_lDS(iter, subcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            stopwatch[0] = DateTime.Now;
                            //renderpart_lDS(iter, subcontainer, container);
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_fDS(iter, subcontainer, container));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            stopwatch[1] = DateTime.Now;
                            //renderpart_fDS(iter, subcontainer, container);
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_rDS(iter, subcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            stopwatch[2] = DateTime.Now;
                            //renderpart_rDS(iter, subcontainer, container);
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_bDS(iter, subcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            stopwatch[3] = DateTime.Now;
                            //renderpart_bDS(iter, subcontainer, container);
                            break;
                    }
                }
                else if(digits[i] == 2)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_l(iter, subcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            //renderpart_l(iter, subcontainer, container);
                            stopwatch[0] = DateTime.Now;
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_f(iter, subcontainer, container, packet));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            //renderpart_f(iter, subcontainer, container);
                            stopwatch[1] = DateTime.Now;
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_r(iter, subcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            //renderpart_r(iter, subcontainer, container);
                            stopwatch[2] = DateTime.Now;
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_b(iter, subcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            //renderpart_b(iter, subcontainer, container);
                            stopwatch[3] = DateTime.Now;
                            break;
                    }
                    //renderthread.IsBackground = true;
                    //renderthread.Start();
                }
                else if(digits[i] == 0)
                {
                    stopwatch[i] = DateTime.Now;
                    container.setView(i, black_img.getView(i));
                }
            }

            for(int i = 0; i < 4; i++)
            {
                if (is_created[i]) {
                    Threadlist[i].Join();
                    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                }
                //else
                //{
                //    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                //}
            }
            //UnityEngine.Debug.LogFormat("Rendering time : {0:f3} {1:f3} {2:f3} {3:f3}",
            //    renderingtime[0], renderingtime[1], renderingtime[2], renderingtime[3]);

            //renderthread.Join();

            //if (is_created[0]) { tasklist[0].Wait(); }
            //if (is_created[1]) { tasklist[1].Wait(); }
            //if (is_created[2]) { tasklist[2].Wait(); }
            //if (is_created[3]) { tasklist[3].Wait(); }
        }
        */

        public void rendersubviews(subseg_container subcontainer, view_container container, string list, int iter, RequestPacket packet)
        {
            int[] digits = new int[4];
            bool[] is_created = new bool[4];
            bool[] isRendered = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                is_created[i] = false;
            }

            DateTime[] stopwatch = new DateTime[4];
            double[] renderingtime = new double[4];

            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));

                // head direction일 때는 thread 쓰지 않고 하자.
                if(i == packet.pos.getHead_dir())
                {
                    if (digits[i] == 1)
                    {
                        switch (i)
                        {
                            case 0:
                                renderpart_lDS(iter, subcontainer, container);
                                stopwatch[0] = DateTime.Now;
                                //renderpart_lDS(iter, subcontainer, container);
                                break;
                            case 1:
                                renderpart_fDS(iter, subcontainer, container);
                                stopwatch[1] = DateTime.Now;
                                //renderpart_fDS(iter, subcontainer, container);
                                break;
                            case 2:
                                renderpart_rDS(iter, subcontainer, container);
                                stopwatch[2] = DateTime.Now;
                                //renderpart_rDS(iter, subcontainer, container);
                                break;
                            case 3:
                                renderpart_bDS(iter, subcontainer, container);
                                stopwatch[3] = DateTime.Now;
                                //renderpart_bDS(iter, subcontainer, container);
                                break;
                        }
                    }
                    else if (digits[i] == 2)
                    {
                        switch (i)
                        {
                            case 0:
                                renderpart_l(iter, subcontainer, container);
                                stopwatch[0] = DateTime.Now;
                                break;
                            case 1:
                                renderpart_f(iter, subcontainer, container, packet);
                                stopwatch[1] = DateTime.Now;
                                break;
                            case 2:
                                renderpart_r(iter, subcontainer, container);
                                stopwatch[2] = DateTime.Now;
                                break;
                            case 3:
                                renderpart_b(iter, subcontainer, container);
                                stopwatch[3] = DateTime.Now;
                                break;
                        }
                    }
                    else if (digits[i] == 0)
                    {
                        stopwatch[i] = DateTime.Now;
                        container.setView(i, black_img.getView(i));
                    }
                }
                else
                {
                    if (digits[i] == 1)
                    {
                        switch (i)
                        {
                            case 0:
                                Threadlist[0] = new Thread(() => renderpart_lDS(iter, subcontainer, container));
                                is_created[0] = true;
                                Threadlist[0].IsBackground = true;
                                Threadlist[0].Start();
                                stopwatch[0] = DateTime.Now;
                                //renderpart_lDS(iter, subcontainer, container);
                                break;
                            case 1:
                                Threadlist[1] = new Thread(() => renderpart_fDS(iter, subcontainer, container));
                                is_created[1] = true;
                                Threadlist[1].IsBackground = true;
                                Threadlist[1].Start();
                                stopwatch[1] = DateTime.Now;
                                //renderpart_fDS(iter, subcontainer, container);
                                break;
                            case 2:
                                Threadlist[2] = new Thread(() => renderpart_rDS(iter, subcontainer, container));
                                is_created[2] = true;
                                Threadlist[2].IsBackground = true;
                                Threadlist[2].Start();
                                stopwatch[2] = DateTime.Now;
                                //renderpart_rDS(iter, subcontainer, container);
                                break;
                            case 3:
                                Threadlist[3] = new Thread(() => renderpart_bDS(iter, subcontainer, container));
                                is_created[3] = true;
                                Threadlist[3].IsBackground = true;
                                Threadlist[3].Start();
                                stopwatch[3] = DateTime.Now;
                                //renderpart_bDS(iter, subcontainer, container);
                                break;
                        }
                    }
                    else if (digits[i] == 2)
                    {
                        switch (i)
                        {
                            case 0:
                                Threadlist[0] = new Thread(() => renderpart_l(iter, subcontainer, container));
                                is_created[0] = true;
                                Threadlist[0].IsBackground = true;
                                Threadlist[0].Start();
                                //renderpart_l(iter, subcontainer, container);
                                stopwatch[0] = DateTime.Now;
                                break;
                            case 1:
                                Threadlist[1] = new Thread(() => renderpart_f(iter, subcontainer, container, packet));
                                is_created[1] = true;
                                Threadlist[1].IsBackground = true;
                                Threadlist[1].Start();
                                //renderpart_f(iter, subcontainer, container);
                                stopwatch[1] = DateTime.Now;
                                break;
                            case 2:
                                Threadlist[2] = new Thread(() => renderpart_r(iter, subcontainer, container));
                                is_created[2] = true;
                                Threadlist[2].IsBackground = true;
                                Threadlist[2].Start();
                                //renderpart_r(iter, subcontainer, container);
                                stopwatch[2] = DateTime.Now;
                                break;
                            case 3:
                                Threadlist[3] = new Thread(() => renderpart_b(iter, subcontainer, container));
                                is_created[3] = true;
                                Threadlist[3].IsBackground = true;
                                Threadlist[3].Start();
                                //renderpart_b(iter, subcontainer, container);
                                stopwatch[3] = DateTime.Now;
                                break;
                        }
                    }
                    else if (digits[i] == 0)
                    {
                        stopwatch[i] = DateTime.Now;
                        container.setView(i, black_img.getView(i));
                    }
                }
                
            }

            for (int i = 0; i < 4; i++)
            {
                if (is_created[i])
                {
                    Threadlist[i].Join();
                    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                }
            }
        }

        public void rendersubviews(subseg_container subcontainer, view_container container, string list, string hit_list, int iter, RequestPacket packet)
        {
            int[] digits = new int[4];
            int hit_digit = 0;
            bool[] is_created = new bool[4];
            bool[] isRendered = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                is_created[i] = false;
            }

            DateTime[] stopwatch = new DateTime[4];
            double[] renderingtime = new double[4];

            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                hit_digit = Convert.ToInt32(hit_list.Substring(i, 1));
                // head direction일 때는 thread 쓰지 않고 하자.
                if (digits[i] == 1 && hit_digit == 0)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_lDS(iter, subcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            stopwatch[0] = DateTime.Now;
                            //renderpart_lDS(iter, subcontainer, container);
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_fDS(iter, subcontainer, container));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            stopwatch[1] = DateTime.Now;
                            //renderpart_fDS(iter, subcontainer, container);
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_rDS(iter, subcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            stopwatch[2] = DateTime.Now;
                            //renderpart_rDS(iter, subcontainer, container);
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_bDS(iter, subcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            stopwatch[3] = DateTime.Now;
                            //renderpart_bDS(iter, subcontainer, container);
                            break;
                    }
                }
                else if (digits[i] == 2 && hit_digit == 0)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_l(iter, subcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            //renderpart_l(iter, subcontainer, container);
                            stopwatch[0] = DateTime.Now;
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_f(iter, subcontainer, container, packet));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            //renderpart_f(iter, subcontainer, container);
                            stopwatch[1] = DateTime.Now;
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_r(iter, subcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            //renderpart_r(iter, subcontainer, container);
                            stopwatch[2] = DateTime.Now;
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_b(iter, subcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            //renderpart_b(iter, subcontainer, container);
                            stopwatch[3] = DateTime.Now;
                            break;
                    }
                }
                else if (digits[i] == 0)
                {
                    stopwatch[i] = DateTime.Now;
                    container.setView(i, black_img.getView(i));
                }

            }

            for (int i = 0; i < 4; i++)
            {
                if (is_created[i])
                {
                    Threadlist[i].Join();
                    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                }
            }
        }

        public void rendersubviews(jpeg_container jpegcontainer, view_container container, string list, int iter, RequestPacket packet)
        {
            int[] digits = new int[4];
            bool[] is_created = new bool[4];
            bool[] isRendered = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                is_created[i] = false;
            }
            Thread[] Threadlist = new Thread[4];
            Task[] Tasklist = new Task[4];

            DateTime[] stopwatch = new DateTime[4];
            double[] renderingtime = new double[4];

            for (int i = 0; i < 4; i++)
            {
                digits[i] = Convert.ToInt32(list.Substring(i, 1));
                if (digits[i] == 1)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_lDS(0, jpegcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            stopwatch[0] = DateTime.Now;
                            //renderpart_lDS(iter, subcontainer, container);
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_fDS(1, jpegcontainer, container));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            stopwatch[1] = DateTime.Now;
                            //renderpart_fDS(iter, subcontainer, container);
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_rDS(2, jpegcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            stopwatch[2] = DateTime.Now;
                            //renderpart_rDS(iter, subcontainer, container);
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_bDS(3, jpegcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            stopwatch[3] = DateTime.Now;
                            //renderpart_bDS(iter, subcontainer, container);
                            break;
                    }
                }
                else if (digits[i] == 2)
                {
                    switch (i)
                    {
                        case 0:
                            Threadlist[0] = new Thread(() => renderpart_l(0, jpegcontainer, container));
                            is_created[0] = true;
                            Threadlist[0].IsBackground = true;
                            Threadlist[0].Start();
                            //renderpart_l(iter, subcontainer, container);
                            stopwatch[0] = DateTime.Now;
                            break;
                        case 1:
                            Threadlist[1] = new Thread(() => renderpart_f(1, jpegcontainer, container, packet));
                            is_created[1] = true;
                            Threadlist[1].IsBackground = true;
                            Threadlist[1].Start();
                            //renderpart_f(iter, subcontainer, container);
                            stopwatch[1] = DateTime.Now;
                            break;
                        case 2:
                            Threadlist[2] = new Thread(() => renderpart_r(2, jpegcontainer, container));
                            is_created[2] = true;
                            Threadlist[2].IsBackground = true;
                            Threadlist[2].Start();
                            //renderpart_r(iter, subcontainer, container);
                            stopwatch[2] = DateTime.Now;
                            break;
                        case 3:
                            Threadlist[3] = new Thread(() => renderpart_b(3, jpegcontainer, container));
                            is_created[3] = true;
                            Threadlist[3].IsBackground = true;
                            Threadlist[3].Start();
                            //renderpart_b(iter, subcontainer, container);
                            stopwatch[3] = DateTime.Now;
                            break;
                    }
                    //renderthread.IsBackground = true;
                    //renderthread.Start();
                }
                else if (digits[i] == 0)
                {
                    stopwatch[i] = DateTime.Now;
                    container.setView(i, black_img.getView(i));
                }
            }


            //for(int i = 0; i < 4; i++)
            //{
            //
            //}

            for (int i = 0; i < 4; i++)
            {
                if (is_created[i])
                {
                    Threadlist[i].Join();
                    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                }
                //else
                //{
                //    renderingtime[i] = (DateTime.Now - stopwatch[i]).TotalMilliseconds;
                //}
            }
            UnityEngine.Debug.LogFormat("Rendering time : {0:f3} {1:f3} {2:f3} {3:f3}",
                renderingtime[0], renderingtime[1], renderingtime[2], renderingtime[3]);

            //renderthread.Join();

            //if (is_created[0]) { tasklist[0].Wait(); }
            //if (is_created[1]) { tasklist[1].Wait(); }
            //if (is_created[2]) { tasklist[2].Wait(); }
            //if (is_created[3]) { tasklist[3].Wait(); }
        }

        int _time = 0;

        public void renderpart_l(int iter, subseg_container subcontainer, view_container container)
        {
            DateTime dec_time;
            int temp_ = 0;

            int _iter = iter;
            temp_viewL = subcontainer.getView(temp_, _iter);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          decoded_viewL,
                          temp_viewL.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;

            //UnityEngine.Debug.LogWarningFormat("left decoding time : {0:f4}", decodingtime);
            container.setView(temp_, decoded_viewL);
        }
        public void renderpart_f(int iter, subseg_container subcontainer, view_container container, RequestPacket packet)
        {
            int temp_ = 1;
            int _iter = iter;
            temp_viewF = subcontainer.getView(temp_, _iter);
            //System.IO.File.WriteAllBytes(string.Format("output/{2} {5} front_{0}_{1} {3}_{4}.jpg", packet.pos.getX(), packet.pos.getY(), _time++, packet.loc.getPos_x(), packet.loc.getPos_y(), packet.loc.getPath()), temp_viewF);

            int err = decoding(temp_viewF,
                          decoded_viewF,
                          temp_viewF.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewF);
        }
        public void renderpart_r(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 2;
            int _iter = iter;
            temp_viewR = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewR,
                          decoded_viewR,
                          temp_viewR.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewR);
        }
        public void renderpart_b(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 3;
            int _iter = iter;
            temp_viewB = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewB,
                          decoded_viewB,
                          temp_viewB.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewB);
        }

        



        public void renderpart_lDS(int iter, subseg_container subcontainer, view_container container)
        {
            DateTime dec_time;
            DateTime resize_time;
            int temp_ = 0;
            int _iter = iter;
            temp_viewL = subcontainer.getView(temp_, _iter);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          DSsubviewL,
                          temp_viewL.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;
            resize_time = DateTime.Now;
            int threadnum = 8;
            resizelistL[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistL[0].Start();

            resizelistL[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistL[1].Start();

            resizelistL[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistL[2].Start();

            resizelistL[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistL[3].Start();

            resizelistL[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistL[4].Start();

            resizelistL[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum,  5, container.bpp);
            });
            resizelistL[5].Start();

            resizelistL[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum,  6, container.bpp);
            });
            resizelistL[6].Start();

            resizelistL[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistL[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistL[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistL[i].Wait();
            }
            double resizingtime = (DateTime.Now - resize_time).TotalMilliseconds;
            UnityEngine.Debug.LogWarningFormat("left decoding : {0:f4} ms, resizing : {1:f4} ms", decodingtime, resizingtime);

            container.setView(temp_, decoded_viewL);
        }
        public void renderpart_fDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 1;
            int _iter = iter;
            temp_viewF = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewF,
                          DSsubviewF,
                          temp_viewF.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistF[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistF[0].Start();

            resizelistF[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistF[1].Start();

            resizelistF[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistF[2].Start();

            resizelistF[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistF[3].Start();

            resizelistF[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistF[4].Start();

            resizelistF[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistF[5].Start();

            resizelistF[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistF[6].Start();

            resizelistF[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistF[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistF[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistF[i].Wait();
            }

            container.setView(temp_, decoded_viewF);
        }
        public void renderpart_rDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 2;
            int _iter = iter;
            temp_viewR = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewR,
                          DSsubviewR,
                          temp_viewR.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistR[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistR[0].Start();

            resizelistR[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistR[1].Start();

            resizelistR[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistR[2].Start();

            resizelistR[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistR[3].Start();

            resizelistR[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistR[4].Start();

            resizelistR[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistR[5].Start();

            resizelistR[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistR[6].Start();

            resizelistR[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistR[7].Start();


            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistR[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistR[i].Wait();
            }
            container.setView(temp_, decoded_viewR);
        }
        public void renderpart_bDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 3;
            int _iter = iter;
            temp_viewB = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewB,
                          DSsubviewB,
                          temp_viewB.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistB[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistB[0].Start();

            resizelistB[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistB[1].Start();

            resizelistB[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistB[2].Start();

            resizelistB[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistB[3].Start();

            resizelistB[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistB[4].Start();

            resizelistB[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistB[5].Start();

            resizelistB[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistB[6].Start();

            resizelistB[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistB[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistB[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistB[i].Wait();
            }
            container.setView(temp_, decoded_viewB);
        }


        public void renderpart_l(int iter, pre_renderViews container, int idx, int digit)
        {
            DateTime dec_time;
            int temp_ = 0;
            int _iter = iter;
            temp_viewL = cache.GetView(idx, _iter, temp_);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          decoded_viewL,
                          temp_viewL.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;

            //UnityEngine.Debug.LogWarningFormat("left decoding time : {0:f4}", decodingtime);
            container.setView(temp_, decoded_viewL, _iter);
        }
        public void renderpart_f(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 1;
            int _iter = iter;
            temp_viewF = cache.GetView(idx, _iter, temp_);
            //System.IO.File.WriteAllBytes(string.Format("output/{2} {5} front_{0}_{1} {3}_{4}.jpg", packet.pos.getX(), packet.pos.getY(), _time++, packet.loc.getPos_x(), packet.loc.getPos_y(), packet.loc.getPath()), temp_viewF);

            int err = decoding(temp_viewF,
                          decoded_viewF,
                          temp_viewF.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewF, _iter);
        }
        public void renderpart_r(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 2;
            int _iter = iter;
            UnityEngine.Debug.LogErrorFormat("cache view size : {0} idx : {1} digit : {2}", temp_viewR.Length, idx, temp_);
            temp_viewR = cache.GetView(idx, _iter, temp_);
            int err = decoding(temp_viewR,
                          decoded_viewR,
                          temp_viewR.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewR, _iter);
        }
        public void renderpart_b(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 3;
            int _iter = iter;
            temp_viewB = cache.GetView(idx, _iter, temp_);
            int err = decoding(temp_viewB,
                          decoded_viewB,
                          temp_viewB.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(temp_, decoded_viewB, _iter);
        }
        public void renderpart_lDS(int iter, pre_renderViews container, int idx, int digit)
        {
            DateTime dec_time;
            DateTime resize_time;
            int temp_ = 0;
            int _iter = iter;
            temp_viewL = cache.GetView(idx, _iter, temp_);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          DSsubviewL,
                          temp_viewL.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;
            resize_time = DateTime.Now;
            int threadnum = 8;
            resizelistL[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistL[0].Start();

            resizelistL[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistL[1].Start();

            resizelistL[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistL[2].Start();

            resizelistL[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistL[3].Start();

            resizelistL[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistL[4].Start();

            resizelistL[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistL[5].Start();

            resizelistL[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistL[6].Start();

            resizelistL[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistL[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistL[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistL[i].Wait();
            }
            double resizingtime = (DateTime.Now - resize_time).TotalMilliseconds;
            UnityEngine.Debug.LogWarningFormat("left decoding : {0:f4} ms, resizing : {1:f4} ms", decodingtime, resizingtime);

            container.setView(temp_, decoded_viewL, _iter);
        }
        public void renderpart_fDS(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 1;
            int _iter = iter;
            temp_viewF = cache.GetView(idx, _iter, temp_);
            int err = decoding(temp_viewF,
                          DSsubviewF,
                          temp_viewF.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistF[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistF[0].Start();

            resizelistF[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistF[1].Start();

            resizelistF[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistF[2].Start();

            resizelistF[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistF[3].Start();

            resizelistF[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistF[4].Start();

            resizelistF[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistF[5].Start();

            resizelistF[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistF[6].Start();

            resizelistF[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistF[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistF[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistF[i].Wait();
            }

            container.setView(temp_, decoded_viewF, _iter);
        }
        public void renderpart_rDS(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 2;
            int _iter = iter;
            temp_viewR = cache.GetView(idx, iter, temp_);
            int err = decoding(temp_viewR,
                          DSsubviewR,
                          temp_viewR.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistR[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistR[0].Start();

            resizelistR[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistR[1].Start();

            resizelistR[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistR[2].Start();

            resizelistR[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistR[3].Start();

            resizelistR[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistR[4].Start();

            resizelistR[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistR[5].Start();

            resizelistR[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistR[6].Start();

            resizelistR[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistR[7].Start();


            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistR[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistR[i].Wait();
            }
            container.setView(temp_, decoded_viewR, _iter);
        }
        public void renderpart_bDS(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 3;
            int _iter = iter;
            temp_viewB = cache.GetView(idx, _iter, temp_);
            int err = decoding(temp_viewB,
                          DSsubviewB,
                          temp_viewB.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            resizelistB[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });
            resizelistB[0].Start();

            resizelistB[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });
            resizelistB[1].Start();

            resizelistB[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });
            resizelistB[2].Start();

            resizelistB[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelistB[3].Start();

            resizelistB[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });
            resizelistB[4].Start();

            resizelistB[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });
            resizelistB[5].Start();

            resizelistB[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });
            resizelistB[6].Start();

            resizelistB[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });
            resizelistB[7].Start();

            //for (int i = 0; i < threadnum; i++)
            //{
            //    resizelistB[i].Start();
            //}

            for (int i = 0; i < threadnum; i++)
            {
                resizelistB[i].Wait();
            }
            container.setView(temp_, decoded_viewB, _iter);
        }

        public void onlydecoding_DS(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 0;
            int _iter = iter;
            temp_viewOnlyDec = cache.GetView(idx, iter, digit);
            int err = decoding(temp_viewOnlyDec,
                          DSsubviewOnlyDec,
                          temp_viewOnlyDec.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            Task[] resizelist = new Task[threadnum];
            resizelist[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });

            resizelist[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });

            resizelist[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });

            resizelist[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelist[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });

            resizelist[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });

            resizelist[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });

            resizelist[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewOnlyDec, decoded_viewOnlyDec, DSsubviewOnlyDec.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });


            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Start();
            }

            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Wait();
            }
            container.setView(temp_, decoded_viewOnlyDec, iter);
        }
        public void onlydecoding(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 0;
            int _iter = iter;
            temp_viewOnlyDec = cache.GetView(idx, iter, digit);
            int err = decoding(temp_viewOnlyDec,
                          decoded_viewOnlyDec,
                          temp_viewOnlyDec.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            container.setView(temp_, decoded_viewOnlyDec, iter);
        }




        public void renderpart_l(int iter, jpeg_container jpegcontainer, view_container container)
        {
            DateTime dec_time;
            int _iter = iter;
            temp_viewL = jpegcontainer.getView(_iter);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          decoded_viewL,
                          temp_viewL.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;

            //UnityEngine.Debug.LogWarningFormat("left decoding time : {0:f4}", decodingtime);
            container.setView(_iter, decoded_viewL);
        }
        public void renderpart_f(int iter, jpeg_container jpegcontainer, view_container container, RequestPacket packet)
        {
            int _iter = iter;
            temp_viewF = jpegcontainer.getView(_iter);
            //System.IO.File.WriteAllBytes(string.Format("output/{2} {5} front_{0}_{1} {3}_{4}.jpg", packet.pos.getX(), packet.pos.getY(), _time++, packet.loc.getPos_x(), packet.loc.getPos_y(), packet.loc.getPath()), temp_viewF);

            int err = decoding(temp_viewF,
                          decoded_viewF,
                          temp_viewF.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(_iter, decoded_viewF);
        }
        public void renderpart_r(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewR = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewR,
                          decoded_viewR,
                          temp_viewR.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(_iter, decoded_viewR);
        }
        public void renderpart_b(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewB = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewB,
                          decoded_viewB,
                          temp_viewB.Length,
                          container.owidth,
                          container.oheight,
                          container.bpp);
            container.setView(_iter, decoded_viewB);
        }


        public void renderpart_lDS(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewL = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewL,
                          DSsubviewL,
                          temp_viewL.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            Task[] resizelist = new Task[threadnum];
            resizelist[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });

            resizelist[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });

            resizelist[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });

            resizelist[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelist[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });

            resizelist[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });

            resizelist[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });

            resizelist[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewL, decoded_viewL, DSsubviewL.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });


            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Start();
            }

            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Wait();
            }
            container.setView(_iter, decoded_viewL);
        }
        public void renderpart_fDS(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewF = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewF,
                          DSsubviewF,
                          temp_viewF.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            Task[] resizelist = new Task[threadnum];
            resizelist[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });

            resizelist[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });

            resizelist[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });

            resizelist[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelist[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });

            resizelist[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });

            resizelist[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });

            resizelist[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewF, decoded_viewF, DSsubviewF.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });


            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Start();
            }

            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Wait();
            }

            container.setView(_iter, decoded_viewF);
        }
        public void renderpart_rDS(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewR = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewR,
                          DSsubviewR,
                          temp_viewR.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            Task[] resizelist = new Task[threadnum];
            resizelist[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });

            resizelist[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });

            resizelist[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });

            resizelist[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelist[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });

            resizelist[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });

            resizelist[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });

            resizelist[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewR, decoded_viewR, DSsubviewR.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });


            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Start();
            }

            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Wait();
            }
            container.setView(_iter, decoded_viewR);
        }
        public void renderpart_bDS(int iter, jpeg_container jpegcontainer, view_container container)
        {
            int _iter = iter;
            temp_viewB = jpegcontainer.getView(_iter);
            int err = decoding(temp_viewB,
                          DSsubviewB,
                          temp_viewB.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            int threadnum = 8;
            Task[] resizelist = new Task[threadnum];
            resizelist[0] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 0, container.bpp);
            });

            resizelist[1] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 1, container.bpp);
            });

            resizelist[2] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 2, container.bpp);
            });

            resizelist[3] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 3, container.bpp);
            });
            resizelist[4] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 4, container.bpp);
            });

            resizelist[5] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 5, container.bpp);
            });

            resizelist[6] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 6, container.bpp);
            });

            resizelist[7] = new Task(() =>
            {
                Resizing_Parall(DSsubviewB, decoded_viewB, DSsubviewB.Length, container.swidth, container.sheight, container.owidth, container.oheight, threadnum, 7, container.bpp);
            });


            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Start();
            }

            for (int i = 0; i < threadnum; i++)
            {
                resizelist[i].Wait();
            }
            container.setView(_iter, decoded_viewB);
        }


        /*
        public void renderpart_lDS(int iter, subseg_container subcontainer, view_container container)
        {
            DateTime dec_time;
            DateTime resize_time;
            int temp_ = 0;
            int _iter = iter;
            temp_viewL = subcontainer.getView(temp_, _iter);
            dec_time = DateTime.Now;
            int err = decoding(temp_viewL,
                          DSsubviewL,
                          temp_viewL.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            double decodingtime = (DateTime.Now - dec_time).TotalMilliseconds;
            resize_time = DateTime.Now;
            Resize_Slite(DSsubviewL,
                   decoded_viewL,
                   DSsubviewL.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            double resizingtime = (DateTime.Now - resize_time).TotalMilliseconds;
            UnityEngine.Debug.LogWarningFormat("left decoding : {0:f4} ms, resizing : {1:f4} ms", decodingtime, resizingtime);

            container.setView(temp_, decoded_viewL);
        }
        public void renderpart_fDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 1;
            int _iter = iter;
            temp_viewF = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewF,
                          DSsubviewF,
                          temp_viewF.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize_Slite(DSsubviewF,
                   decoded_viewF,
                   DSsubviewF.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            container.setView(temp_, decoded_viewF);
        }
        public void renderpart_rDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 2;
            int _iter = iter;
            temp_viewR = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewR,
                          DSsubviewR,
                          temp_viewR.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize_Slite(DSsubviewR,
                   decoded_viewR,
                   DSsubviewR.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            container.setView(temp_, decoded_viewR);
        }
        public void renderpart_bDS(int iter, subseg_container subcontainer, view_container container)
        {
            int temp_ = 3;
            int _iter = iter;
            temp_viewB = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_viewB,
                          DSsubviewB,
                          temp_viewB.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize_Slite(DSsubviewB,
                   decoded_viewB,
                   DSsubviewB.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            container.setView(temp_, decoded_viewB);
        }

        public void onlydecoding_DS(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 0;
            int _iter = iter;
            temp_viewOnlyDec = cache.GetView(idx, iter, digit);
            int err = decoding(temp_viewOnlyDec,
                          DSsubviewOnlyDec,
                          temp_viewOnlyDec.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize_Slite(DSsubviewOnlyDec,
                   decoded_viewOnlyDec,
                   DSsubviewOnlyDec.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            container.setView(temp_, decoded_viewOnlyDec, iter);
        }
        public void onlydecoding(int iter, pre_renderViews container, int idx, int digit)
        {
            int temp_ = 0;
            int _iter = iter;
            temp_viewOnlyDec = cache.GetView(idx, iter, digit);
            int err = decoding(temp_viewOnlyDec,
                          decoded_viewOnlyDec,
                          temp_viewOnlyDec.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            container.setView(temp_, decoded_viewOnlyDec, iter);
        }
        */
        /*
        public void renderpart_lDS(int iter, subseg_container subcontainer, view_container container, ref Task Resize)
        {
            int temp_ = 0;
            int _iter = iter;
            byte[] temp_view = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_view,
                          DSsubview,
                          temp_view.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize = Task.Run(() =>
            {
                Resize_Slite(DSsubview,
                   decoded_view,
                   DSsubview.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            });
            container.setView(temp_, decoded_view);
        }
        public void renderpart_fDS(int iter, subseg_container subcontainer, view_container container, ref Task Resize)
        {
            int temp_ = 1;
            int _iter = iter;
            byte[] temp_view = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_view,
                          DSsubview,
                          temp_view.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize = Task.Run(() =>
            {
                Resize_Slite(DSsubview,
                decoded_view,
                DSsubview.Length,
                container.swidth,
                container.sheight,
                container.owidth,
                container.oheight);
            });
            
            container.setView(temp_, decoded_view);
        }
        public void renderpart_rDS(int iter, subseg_container subcontainer, view_container container, ref Task Resize)
        {
            int temp_ = 2;
            int _iter = iter;
            byte[] temp_view = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_view,
                          DSsubview,
                          temp_view.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize = Task.Run(() =>
            {
                Resize_Slite(DSsubview,
                   decoded_view,
                   DSsubview.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            });
           
            container.setView(temp_, decoded_view);
        }
        public void renderpart_bDS(int iter, subseg_container subcontainer, view_container container, ref Task Resize)
        {
            int temp_ = 3;
            int _iter = iter;
            byte[] temp_view = subcontainer.getView(temp_, _iter);
            int err = decoding(temp_view,
                          DSsubview,
                          temp_view.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize = Task.Run(() =>
            {
                Resize_Slite(DSsubview,
                   decoded_view,
                   DSsubview.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            });
            container.setView(temp_, decoded_view);
        }

        public void onlydecoding_DS(int iter, pre_renderViews container, int idx, int digit, ref  Task Resize)
        {
            int temp_ = 0;
            int _iter = iter;
            byte[] temp_view = cache.GetView(idx, iter, digit);
            int err = decoding(temp_view,
                          DSsubview,
                          temp_view.Length,
                          container.swidth,
                          container.sheight,
                          container.bpp);
            Resize = Task.Run(() =>
            {
                Resize_Slite(DSsubview,
                   decoded_view,
                   DSsubview.Length,
                   container.swidth,
                   container.sheight,
                   container.owidth,
                   container.oheight);
            });
            container.setView(temp_, decoded_view, iter);
        }
        */


        #endregion
    }
}
