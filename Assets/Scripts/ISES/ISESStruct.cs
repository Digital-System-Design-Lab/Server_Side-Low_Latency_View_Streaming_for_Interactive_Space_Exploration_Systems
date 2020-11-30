using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ISESStructure
{
    class Printer
    {
        public static void LogPrint(string msg)
        {
            //UnityEngine.Debug.Log(msg);
        }
        public static void WarnPrint(string msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }
        public static void ErrorPrint(string msg)
        {
            UnityEngine.Debug.LogError(msg);
        }
    }

    public enum Policy
    {
        LRU = 0,
        DR = 1,     //Dead Reckoning
        GDC = 2
    }

    public enum CacheStatus
    {
        MISS = 0,           //cache miss
        HIT = 1,            //cache hit
        PARTIAL_HIT = 2,    //partial hit
        FULL = 3            //cache full
    }
    public enum ESLevel
    {
        SLOW = 0,
        FAST = 1,
        SUPER_FAST = 2
    }

    public enum SYNC
    {
        START = 0,
        END = 1,
        ERROR = 2
    }
    public enum QUALITY
    {
        EMPTY = 0, DS = 1, ORIGINAL = 2
    }
    public struct Client_info
    {
        public int sub_segment_size;
        public int pathlength;

        public Client_info(int size, int length)
        {
            sub_segment_size = size;
            pathlength = length;
        }

    }
    public class server_buffer
    {
        public List<byte[]> lviews;
        public List<byte[]> fviews;
        public List<byte[]> rviews;
        public List<byte[]> bviews;
        public int seg_size;

        public server_buffer(int seg_size)
        {
            this.seg_size = seg_size;
            lviews = new List<byte[]>();
            fviews = new List<byte[]>();
            rviews = new List<byte[]>();
            bviews = new List<byte[]>();
            init();
        }

        public void init()
        {
            for (int i = 0; i < seg_size; i++)
            {
                byte[] ltemp = { 0 };
                byte[] ftemp = { 0 };
                byte[] rtemp = { 0 };
                byte[] btemp = { 0 };

                lviews.Add(ltemp);
                fviews.Add(ftemp);
                rviews.Add(rtemp);
                bviews.Add(btemp);
            }
        }

        public void setlview(byte[] lview, int iter) { lviews[iter] = lview; }
        public void setfview(byte[] fview, int iter) { fviews[iter] = fview; }
        public void setrview(byte[] rview, int iter) { rviews[iter] = rview; }
        public void setbview(byte[] bview, int iter) { bviews[iter] = bview; }

        public byte[] getlview(int idx) { return lviews[idx]; }
        public byte[] getfview(int idx) { return fviews[idx]; }
        public byte[] getrview(int idx) { return rviews[idx]; }
        public byte[] getbview(int idx) { return bviews[idx]; }


        public byte[] getView(int digit, int iter)
        {
            byte[] result = null;
            switch (digit)
            {
                case 0:
                    result = lviews[iter];
                    break;
                case 1:
                    result = fviews[iter];
                    break;
                case 2:
                    result = rviews[iter];
                    break;
                case 3:
                    result = bviews[iter];
                    break;
            }
            return result;
        }

    }

    #region Data packetize
    public struct Request
    {
        public Pos pos;
        public Qualitylist misslist; // from client
        public sub_segment_pos sub_seg_info;

        public Request(Pos pos, Qualitylist misslist, sub_segment_pos sub_seg_info)
        {
            this.pos = pos;
            this.misslist = misslist;
            this.sub_seg_info = sub_seg_info;
        }

        public byte[] StructToBytes(object obj)
        {
            int iSize = Marshal.SizeOf(obj);

            byte[] arr = new byte[iSize];

            IntPtr ptr = Marshal.AllocHGlobal(iSize);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, arr, 0, iSize);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public T ByteToStruct<T>(byte[] buffer) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            if (size > buffer.Length)
            {
                throw new Exception();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return obj;
        }
    }
    public struct Qualitylist
    {
        public QUALITY Left;
        public QUALITY Front;
        public QUALITY Right;
        public QUALITY Back;

        public Qualitylist(QUALITY L, QUALITY F, QUALITY R, QUALITY B)
        {
            Left = L;
            Front = F;
            Right = R;
            Back = B;
        }

        public string convertLIST2STR(QUALITY target)
        {
            string result = "";
            if (target == QUALITY.EMPTY)
            {
                result = "0";
            }
            else if (target == QUALITY.DS)
            {
                result = "1";
            }
            else
            {
                result = "2";
            }
            return result;
        }
        public string createStringType()
        {
            string result;
            string L = convertLIST2STR(Left);
            string F = convertLIST2STR(Front);
            string R = convertLIST2STR(Right);
            string B = convertLIST2STR(Back);
            result = L + F + R + B;
            return result;
        }
        public void convertSTR2LIST(string target)
        {
            decideQuality(ref Left, target.Substring(0, 1));
            decideQuality(ref Front, target.Substring(1, 1));
            decideQuality(ref Right, target.Substring(2, 1));
            decideQuality(ref Back, target.Substring(3, 1));
        }
        void decideQuality(ref QUALITY dst, string list)
        {
            if (list.Equals("0"))
            {
                dst = QUALITY.EMPTY;
            }
            else if (list.Equals("1"))
            {
                dst = QUALITY.DS;
            }
            else if (list.Equals("2"))
            {
                dst = QUALITY.ORIGINAL;
            }
        }
    }


    public struct Test
    {
        public int x;
        public int y;
        public SYNC mysync;
        public Test(int x, int y, SYNC mysync)
        {
            this.x = x;
            this.y = y;
            this.mysync = mysync;
        }
        public byte[] StructToBytes(object obj)
        {
            int iSize = Marshal.SizeOf(obj);

            byte[] arr = new byte[iSize];

            IntPtr ptr = Marshal.AllocHGlobal(iSize);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, arr, 0, iSize);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public T ByteToStruct<T>(byte[] buffer) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            if (size > buffer.Length)
            {
                throw new Exception();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return obj;
        }
    }
    public struct DataPacket
    {
        public Pos pos;
        public Loc loc;
        public out_cache_search result;

        public DataPacket(Pos pos, Loc loc, out_cache_search result)
        {
            this.pos = pos;
            this.loc = loc;
            this.result = result;
        }

        public byte[] StructToBytes(object obj)
        {
            int iSize = Marshal.SizeOf(obj);

            byte[] arr = new byte[iSize];

            IntPtr ptr = Marshal.AllocHGlobal(iSize);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, arr, 0, iSize);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public T ByteToStruct<T>(byte[] buffer) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));

            if (size > buffer.Length)
            {
                throw new Exception();
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return obj;
        }
    }
    #endregion  


    public struct Velocity
    {
        private double velo_x;
        private double velo_y;

        public Velocity(double velo_x, double velo_y)
        {
            this.velo_x = velo_x;
            this.velo_y = velo_y;
        }

        public double getVelo_x() { return velo_x; }
        public double getVelo_y() { return velo_y; }

        public void setVelo_x(double velo_x) { this.velo_x = velo_x; }
        public void setVelo_y(double velo_y) { this.velo_y = velo_y; }

        public void setVelo(double velo_x, double velo_y)
        {
            this.velo_x = velo_x;
            this.velo_y = velo_y;
        }
    }
    public struct Pos
    {
        int X;
        int Y;
        int head_dir;
        int eslevel;

        public Pos(int X, int Y, int head_dir, int eslevel)
        {
            this.X = X;
            this.Y = Y;
            this.head_dir = head_dir;
            this.eslevel = eslevel;
        }
        #region Set methods
        public void setX(int X) { this.X = X; }
        public void setY(int Y) { this.Y = Y; }
        public void setHead_dir(int head_dir) { this.head_dir = head_dir; }
        public void setEslevel(int eslevel) { this.eslevel = eslevel; }
        #endregion

        #region Get methods
        //Get methods
        public int getX() { return X; }
        public int getY() { return Y; }
        public int getHead_dir() { return head_dir; }
        public int convertHD(int dir)
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

        public string getHDstr(int hd)
        {
            string[] ori = { "LEFT", "FRONT", "RIGHT", "BACK" };
            return ori[hd];
        }
        public int getEslevel() { return eslevel; }
        #endregion
    }
    public struct ESASinfo
    {
        public float threshold1;
        public float threshold2;
        public int window_size;

        public ESASinfo(int window_size, float threshold1, float threshold2)
        {
            this.window_size = window_size;
            this.threshold1 = threshold1;
            this.threshold2 = threshold2;
        }
    }
    public struct Cacheinfo
    {
        public int cachesize;
        public int seg_size;
        public int row_size;
        public int col_size;
        public Policy policy;
        public int predict_cnt;

        public Cacheinfo(int cachesize, int seg_size, int row_size, int col_size, Policy policy, int predict_cnt)
        {
            this.cachesize = cachesize;
            this.seg_size = seg_size;
            this.row_size = row_size;
            this.col_size = col_size;
            this.policy = policy;
            if(policy == Policy.DR)
            {
                this.predict_cnt = predict_cnt;
            }
            else
            {
                this.predict_cnt = 0;
            }
        }
    }
    public struct Loc
    {
        string path;
        public string dir;
        int pos_x;
        int pos_y;
        sub_segment_pos seg_pos;
        public int iter;
        
        bool order;

        public Loc(string path, sub_segment_pos seg_pos, int iter)
        {
            this.path = path;
            this.seg_pos = seg_pos;
            dir = path.Substring(0, 1);
            this.order=true;
            this.iter = iter;
            pos_x = -1;
            pos_y = -1;
        }
        #region Set methods
        public void setPath(string path) { this.path = path; }
        public void setOrder(bool order) { this.order = order; }
        public void setPos_X(int x) { pos_x = x; }
        public void setPos_Y(int y) { pos_y = y; }
        public void setSeg_pos(sub_segment_pos seg_pos) { this.seg_pos = seg_pos; }
        #endregion

        #region Get methods
        public string getPath() { return path; }
        public sub_segment_pos get_seg_pos() { return seg_pos; }
        public bool getOrder() { return order; }

        public int getPos_x() { return pos_x; }
        public int getPos_y() { return pos_y; }
        #endregion
    }
    public struct SubRange
    {
        public int _start;
        public int _end;
        public SubRange(int _start, int _end)
        {
            this._start = _start;
            this._end = _end;
        }

        public void setStart(int s)
        {
            _start = s;
        }
        public void setEnd(int e)
        {
            _end = e;
        }

        public override string ToString()
        {
            return string.Format("start : {0} end : {1}", _start, _end);
        }
    }
    public struct sub_segment_pos
    {
        public int start_x;
        public int end_x;
        public int start_y;
        public int end_y;
        public int seg_pos_x;
        public int seg_pos_y;

        public sub_segment_pos(int start_x, int end_x, int start_y, int end_y, int seg_size)
        {
            this.start_x = start_x;
            this.end_x = end_x;
            this.start_y = start_y;
            this.end_y = end_y;
            seg_pos_x = 0;
            seg_pos_y = 0;
        }
        public void calcSeg_pos(int seg_size, string dir, int cur_x, int cur_y, int origin_x, int origin_y)
        {
            if (dir.Equals("R"))
            {
                seg_pos_x = ((cur_x - origin_x) / seg_size) + 1 +((origin_x / seg_size)+(origin_x/121));
                seg_pos_y = (cur_y / seg_size) + (origin_y % seg_size);
            }
            else if (dir.Equals("C"))
            {
                seg_pos_x = (cur_x / seg_size) + (origin_x % seg_size);
                seg_pos_y = ((cur_y - origin_y) / seg_size) + 1 + ((origin_y/seg_size)+(origin_y/121));
            }
            UnityEngine.Debug.LogWarningFormat("Current pos : {4} {5} segment pos : {0}, {1} start pos : {2}, {3}", seg_pos_x, seg_pos_y, start_x, start_y, cur_x, cur_y);

        }
        public override string ToString()
        {
            return string.Format("[X] start {0} end {1} [Y] start {2} end {3}", start_x, end_x, start_y, end_y);
        }
    }
    public class jpeg_container
    {
        public byte[] lview;
        public byte[] fview;
        public byte[] rview;
        public byte[] bview;

        public jpeg_container()
        {
            lview = null;
            fview = null;
            rview = null;
            bview = null;
        }

        public void setView(byte[] temp, int dir)
        {
            switch (dir)
            {
                case 0:
                    lview = temp;
                    break;
                case 1:
                    fview = temp;
                    break;
                case 2:
                    rview = temp;
                    break;
                case 3:
                    bview = temp;
                    break;
            }
        }

        public byte[] getView(int dir)
        {
            byte[] temp = null;
            switch (dir)
            {
                case 0:
                    temp = lview;
                    break;
                case 1:
                    temp = fview;
                    break;
                case 2:
                    temp = rview;
                    break;
                case 3:
                    temp = bview;
                    break;
            }
            return temp;
        }

    }


    public class subseg_container
    {
        public List<byte[]> lviews;
        public List<byte[]> fviews;
        public List<byte[]> rviews;
        public List<byte[]> bviews;
        public int segsize;
        public int offset_s;
        public int offset_e;

        
        public subseg_container(int segsize)
        {
            this.segsize = segsize;
            lviews = new List<byte[]>();
            fviews = new List<byte[]>();
            rviews = new List<byte[]>();
            bviews = new List<byte[]>();
            offset_s = 0;
            offset_e = 0;
            init();
        }

        public void init()
        {
            for(int i = 0; i < segsize; i++)
            {
                byte[] ltemp = { 0 };
                byte[] ftemp = { 0 };
                byte[] rtemp = { 0 };
                byte[] btemp = { 0 };

                lviews.Add(ltemp);
                fviews.Add(ftemp);
                rviews.Add(rtemp);
                bviews.Add(btemp);

            }
        }


        public void setlview(byte[] lview, int iter){lviews[iter] = lview;}
        public void setfview(byte[] fview, int iter){fviews[iter] = fview;}
        public void setrview(byte[] rview, int iter){rviews[iter] = rview;}
        public void setbview(byte[] bview, int iter){bviews[iter] = bview;}

        public void setlview(byte[] lview) { lviews.Add(lview); }
        public void setfview(byte[] fview) { fviews.Add(fview); }
        public void setrview(byte[] rview) { rviews.Add(rview); }
        public void setbview(byte[] bview) { bviews.Add(bview); }

        public byte[] getView(int digit, int iter)
        {

            byte[] result= null;
            switch (digit)
            {
                case 0:
                    result = lviews[iter];
                    break;
                case 1:
                    result = fviews[iter];
                    break;
                case 2:
                    result = rviews[iter];
                    break;
                case 3:
                    result = bviews[iter];
                    break;
            }
            return result;
        }

        public void setView_C2S(int digit, byte[] temp, int iter)
        {
            switch (digit)
            {
                case 0:
                    setlview(temp, iter);
                    break;
                case 1:
                    setfview(temp, iter);
                    break;
                case 2:
                    setrview(temp, iter);
                    break;
                case 3:
                    setbview(temp, iter);
                    break;
            }
        }

        public void setView(int digit, byte[] temp, int iter)
        {
            switch (digit)
            {
                case 0:
                    setlview(temp, iter);
                    break;
                case 1:
                    setfview(temp, iter);
                    break;
                case 2:
                    setrview(temp, iter);
                    break;
                case 3:
                    setbview(temp, iter);
                    break;
            }
            
        }
        public void setView(int digit, List<byte[]> temp)
        {
            switch (digit)
            {
                case 0:
                    lviews = temp;
                    break;
                case 1:
                    fviews = temp;
                    break;
                case 2:
                    rviews = temp;
                    break;
                case 3:
                    bviews = temp;
                    break;
            }
            if(offset_e <segsize-1) offset_e++;
        }
    }

    public struct pre_renderViews
    {
        public List<byte[]> lsubview;
        public List<byte[]> fsubview;
        public List<byte[]> rsubview;
        public List<byte[]> bsubview;

        public int owidth;
        public int swidth;
        public int oheight;
        public int sheight;
        public int bpp;

        public pre_renderViews(view_container container, int seg_size)
        {
            owidth = container.owidth;
            oheight = container.oheight;
            swidth = container.swidth;
            sheight = container.sheight;
            bpp = container.bpp;

            
            lsubview = new List<byte[]>();
            fsubview = new List<byte[]>();
            rsubview = new List<byte[]>();
            bsubview = new List<byte[]>();

            for(int i=0;i< seg_size; i++)
            {
                byte[] l_temp = new byte[owidth*oheight*bpp];
                byte[] f_temp = new byte[owidth*oheight*bpp];
                byte[] r_temp = new byte[owidth*oheight*bpp];
                byte[] b_temp = new byte[owidth * oheight * bpp];

                lsubview.Add(l_temp);
                fsubview.Add(f_temp);
                rsubview.Add(r_temp);
                bsubview.Add(b_temp);
            }
        }

        public void setView(int digit, byte[] temp, int iter)
        {
            switch (digit)
            {
                case 0:
                    System.Buffer.BlockCopy(temp, 0, lsubview[iter], 0, temp.Length);
                    break;
                case 1:
                    System.Buffer.BlockCopy(temp, 0, fsubview[iter], 0, temp.Length);
                    break;
                case 2:
                    System.Buffer.BlockCopy(temp, 0, rsubview[iter], 0, temp.Length);
                    break;
                case 3:
                    System.Buffer.BlockCopy(temp, 0, bsubview[iter], 0, temp.Length);
                    break;
            }
        }

        public void setViewBackup(int digit, byte[] temp, int iter)
        {
            switch (digit)
            {
                case 0:
                    lsubview[iter] = temp;
                    break;
                case 1:
                    fsubview[iter] = temp;
                    break;
                case 2:
                    rsubview[iter] = temp;
                    break;
                case 3:
                    bsubview[iter] = temp;
                    break;
            }
        }

        public List<byte[]> getViews(int digit)
        {
            List<byte[]> result = null;
            switch (digit)
            {
                case 0:
                    result = lsubview;
                    break;
                case 1:
                    result = fsubview;
                    break;
                case 2:
                    result = rsubview;
                    break;
                case 3:
                    result = bsubview;
                    break;
            }
            return result;

        }
        public byte[] getView(int digit, int iter)
        {
            byte[] result = null;
            switch (digit)
            {
                case 0:
                    result = lsubview[iter];
                    break;
                case 1:
                    result = fsubview[iter];
                    break;
                case 2:
                    result = rsubview[iter];
                    break;
                case 3:
                    result = bsubview[iter];
                    break;
            }
            return result;
        }
    }


    public class view_container
    {
        //decoding, up-sampling이 끝난 후의 view 데이터를 담는 구조체

        public int width;
        public int height;
        public int numDir;
        public int swidth;
        public int sheight;
        public int bpp;

        public int owidth;
        public int oheight;

        public byte[] framebuffer;
        public byte[] lsubview;
        public byte[] fsubview;
        public byte[] rsubview;
        public byte[] bsubview;

        public view_container(int width, int height, int numDir, int scalefactor, int bpp)
        {
            this.width = width;     //전체 뷰의 width
            this.height = height;   //전체 뷰의 height
            this.numDir = numDir;
            owidth = (width / numDir);  //sub-view의 width
            oheight = height;           //sub-view의 height
            swidth = (width / numDir) / scalefactor;    //down-sampled sub-view width
            sheight= (height) / scalefactor;            //down-sampled sub-view width
            this.bpp = bpp;

            framebuffer = new byte[width * height * bpp];
            lsubview = new byte[(owidth) * (oheight) * bpp];
            fsubview = new byte[(owidth) * (oheight) * bpp];
            rsubview = new byte[(owidth) * (oheight) * bpp];
            bsubview = new byte[(owidth) * (oheight) * bpp];

        }
        
        public byte[] getFrame() { return framebuffer; }
        public void setFrame(byte[] temp) { framebuffer = temp; }


        public void setView(int digit, byte[] temp)
        {
            switch (digit)
            {
                case 0:
                    lsubview = (temp);
                    break;
                case 1:
                    fsubview = (temp);
                    break;
                case 2:
                    rsubview = (temp);
                    break;
                case 3:
                    bsubview = (temp);
                    break;
            }
        }
        public byte[] getView(int digit)
        {
            byte[] result = null;
            switch (digit)
            {
                case 0:
                    result = lsubview;
                    break;
                case 1:
                    result = fsubview;
                    break;
                case 2:
                    result = rsubview;
                    break;
                case 3:
                    result = bsubview;
                    break;
            }
            //UnityEngine.Debug.Log("[getView function] result size " + result.Length);
            return result;
        }
    }



    public struct out_cache_search
    {
        int idx;
        int stat;
        string misslist;
        string hitlist;
        string renderlist;

        public out_cache_search(int idx, int stat, string misslist, string hitlist, string renderlist)
        {
            this.idx = idx;
            this.stat = stat;
            this.misslist = misslist;
            this.hitlist = hitlist;
            this.renderlist = renderlist;
        }

        #region Set methods
        public void setStat(int stat) { this.stat = stat; }
        public void setMisslist(string misslist) { this.misslist = misslist; }
        public void setHitlist(string hitlist) { this.hitlist = hitlist; }
        public void setRenderlist(string renderlist) { this.renderlist = renderlist; }
        public void setIdx(int idx) { this.idx = idx; }
        #endregion
        #region Get methods
        public int getStat() { return this.stat; }
        public string getMisslist() { return this.misslist; }
        public string getHitlist() { return this.hitlist; }
        public string getRenderlist() { return this.renderlist; }
        public int getIdx() { return idx; }
        #endregion
    }

    public struct RequestPacket
    {
        public Pos pos;
        public out_cache_search result_cache;
        public Loc loc;

        public RequestPacket(Pos pos, out_cache_search result_cache, Loc loc)
        {
            this.pos = pos;
            this.result_cache = result_cache;
            this.loc = loc;
        }
    }
    struct candidate_
    {
        public float candidate_dist;
        public int candidate_index;
    }

    public struct black_imgs
    {
        public byte[] front_img;
        public byte[] left_img;
        public byte[] right_img;
        public byte[] back_img;


        public void init(int owidth, int oheight, int bpp)
        {
            front_img = new byte[owidth * oheight * bpp];
            left_img = new byte[owidth * oheight * bpp];
            back_img = new byte[owidth * oheight * bpp];
            right_img = new byte[owidth * oheight * bpp];

            setbytes();
        }

        public void setbytes()
        {
            set_black_val(front_img);
            set_black_val(left_img);
            set_black_val(right_img);
            set_black_val(back_img);
        }

        public byte[] getView(int digit)
        {
            byte[] temp = null;
            switch (digit)
            {
                case 0:
                    temp = left_img;
                    break;
                case 1:
                    temp = front_img;
                    break;
                case 2:
                    temp = right_img;
                    break;
                case 3:
                    temp = back_img;
                    break;
            }
            return temp;
        }

        public void set_black_val(byte[] buffer)
        {
            int length = buffer.Length;
            for (int idx = 0; idx < length; idx++)
            {
                buffer[idx] = 0;
            }
        }
    }

    public struct Record
    {
        public int seg_x;       //sub-segment a x coordinate
        public int seg_y;       //sub-segment a y coordinate
        public int read_time;
        public int path;
        public int seg_size;

        public int tuple_count;

        public string viewlist; //0000 left front right back
        public List<byte[]> Fsubview;
        public List<byte[]> Rsubview;
        public List<byte[]> Bsubview;
        public List<byte[]> Lsubview;

        byte[] temp;

        public Record(Loc loc, int seg_size)
        {
            sub_segment_pos seg_pos = loc.get_seg_pos();
            seg_x = seg_pos.seg_pos_x;
            seg_y = seg_pos.seg_pos_y;
            path = 0;
            tuple_count = 0;
            viewlist = "";
            read_time = 1;

            Fsubview = new List<byte[]>();
            Rsubview = new List<byte[]>();
            Bsubview = new List<byte[]>();
            Lsubview = new List<byte[]>();
            this.seg_size = seg_size;
            temp = null;
            init();
        }

        public void init()
        {
            for (int i = 0; i < seg_size; i++)
            {
                byte[] ltemp = { 0 };
                byte[] ftemp = { 0 };
                byte[] rtemp = { 0 };
                byte[] btemp = { 0 };

                Lsubview.Add(ltemp);
                Fsubview.Add(ftemp);
                Rsubview.Add(rtemp);
                Bsubview.Add(btemp);

            }
        }
        public void print()
        {
            UnityEngine.Debug.LogErrorFormat("{0} {1} {2}", seg_x, seg_y, read_time);
        }

        public void setViewlist(string list) { viewlist = list; }
        public void setReadtime(int read_time) { this.read_time = read_time; }

        public void setViews(string misslist, subseg_container subcontainer, int iter)
        {
            int digit = -1;
            for (int i = 0; i < 4; i++)
            {
                temp = subcontainer.getView(i, iter);
                digit = Convert.ToInt32(misslist.Substring(i, 1));
                if (digit != 0)
                {
                    switch (i)
                    {
                        case 0:
                            //writeLsubview(temp);
                            Lsubview[iter] = temp;
                            break;
                        case 1:
                            //writeFsubview(temp);
                            Fsubview[iter] = temp;
                            break;
                        case 2:
                            //writeRsubview(temp);
                            Rsubview[iter] = temp;
                            break;
                        case 3:
                            //writeBsubview(temp);
                            Bsubview[iter] = temp;
                            break;
                    }
                }
            }
        }

        public void setViews(string misslist, subseg_container subcontainer)
        {
            int digit = -1;
            for(int iter = 0; iter < subcontainer.segsize; iter++)
            {
                for (int i = 0; i < 4; i++)
                {
                    byte[] temp = subcontainer.getView(i, iter);
                    digit = Convert.ToInt32(misslist.Substring(i, 1));
                    if (digit != 0)
                    {
                        switch (i)
                        {
                            case 0:
                                writeLsubview(temp);
                                break;
                            case 1:
                                writeFsubview(temp);
                                break;
                            case 2:
                                writeRsubview(temp);
                                break;
                            case 3:
                                writeBsubview(temp);
                                break;
                        }
                    }
                }
            }
        }

        public void writeFsubview(byte[] sub_view) { Fsubview.Add(sub_view); }
        public void writeRsubview(byte[] sub_view) { Rsubview.Add(sub_view); }
        public void writeBsubview(byte[] sub_view) { Bsubview.Add(sub_view); }
        public void writeLsubview(byte[] sub_view) { Lsubview.Add(sub_view); }

        public List<byte[]> getView(int digit)
        {
            List<byte[]> result = null;
            switch (digit)
            {
                case 0:
                    result = Lsubview;
                    break;
                case 1:
                    result = Fsubview;
                    break;
                case 2:
                    result = Rsubview;
                    break;
                case 3:
                    result = Bsubview;
                    break;
            }
            return result;
        }
    }

    public struct Point 
    {
        public int x;
        public int y;
        public int x_dist;
        public int y_dist;
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;

            x_dist = 0;
            y_dist = 0;
        }

        public void Add_x_dist(int x_dist){this.x_dist += x_dist;}
        public void Add_y_dist(int y_dist){this.y_dist += y_dist;}
    }
    public struct Point2D
    {
        private int x;
        private int y;

        public Point2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int getX() { return x; }
        public int getY() { return y; }

        public void setX(int x) { this.x = x; }
        public void setY(int y) { this.y = y; }

        public void setPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public struct Pathcalculator
    {
        public List<Point> Var_x;
        public List<Point> Var_y;

        public void init()
        {
            Var_x = new List<Point>();
            Var_y = new List<Point>();
        }

        public void updatepath(int cur_x, int cur_y, int prev_x, int prev_y)
        {
            int x_diff = cur_x-prev_x;
            int y_diff = cur_y-prev_y;

            int x_idx = -1;
            int y_idx = -1;


            //x 좌표 기준 y 차분값 계산
            for(int iter = 0; iter < Var_x.Count; iter++)
            {
                if(Var_x[iter].y == cur_y)
                {
                    Var_x[iter].Add_x_dist(x_diff);
                    x_idx = iter;
                }
            }
            for(int iter = 0; iter < Var_y.Count; iter++)
            {
                if(Var_y[iter].x == cur_x)
                {
                    Var_y[iter].Add_y_dist(y_diff);
                    y_idx = iter;
                }
            }

            if(x_idx == -1)
            {
                Point p = new Point(0, cur_y);
                Var_x.Add(p);
            }
            if(y_idx == -1)
            {
                Point p = new Point(cur_x, 0);
                Var_y.Add(p);
            }
        }

        public int calc_path()
        {
            int path = 0;
            int sum_x_var = 0;
            int sum_y_var = 0;

            for(int iter = 0; iter < Var_x.Count; iter++)
            {
                sum_x_var += Math.Abs(Var_x[iter].x_dist);
            }
            for (int iter = 0; iter < Var_y.Count; iter++)
            {
                sum_y_var += Math.Abs(Var_y[iter].y_dist);
            }
            path = sum_x_var + sum_y_var;
            return path;
        }
    }

}
