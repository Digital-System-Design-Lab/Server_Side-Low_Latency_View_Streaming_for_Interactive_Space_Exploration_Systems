using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISESStructure;
using System.Runtime.InteropServices;

namespace ISESESAS
{
    public struct Path
    {
        public int x;
        public int y;
        public int hd;
        public float acc;

        public Path(int x, int y, int hd, float acc)
        {
            this.x = x;
            this.y = y;
            this.hd = hd;
            this.acc = acc;
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
    public struct viewinfo
    {
        public int height;
        public int width;
        public int bpp;

        public viewinfo(int height, int width, int bpp)
        {
            this.height = height;
            this.width = width;
            this.bpp = bpp;
        }
    }

    public struct Tuple
    {
        public int pos_x;
        public int pos_y;
        public float acc;

        public Tuple(int x, int y, float acc)
        {
            pos_x = x;
            pos_y = y;
            this.acc = acc;
        }
    }

    class ESAS
    {
        public List<Tuple> tables;
        ESASinfo esasinfo;

        public ESAS(ESASinfo esasinfo)
        {
            this.esasinfo = esasinfo;
            tables = new List<Tuple>();
        }

        public void addTuple(Path currentPos)
        {
            Tuple t = new Tuple(currentPos.x, currentPos.y, currentPos.acc);
            tables.Add(t);
        }

        public void update_tablesize()
        {
            if(tables.Count >= esasinfo.window_size)
            {
                tables.RemoveAt(0);
            }
        }

        public float calc_es()
        {
            float explorespeed = 0.0f;
            for(int i = 0; i < tables.Count; i++)
            {
                explorespeed += tables[i].acc;
            }
            explorespeed = explorespeed / tables.Count;
            return explorespeed;
        }
        public int calcesl(float cur_es)
        {
            int esl = 0;
            if (cur_es > esasinfo.threshold1 && cur_es <= esasinfo.threshold2)
                esl = 1;
            else if (cur_es > esasinfo.threshold2)
                esl = 2;
            else
                esl = 0;

            return esl;
        }

        public string calcviewlist(int cur_esl, int cur_hd)
        {
            string result = "";
            switch (cur_esl)
            {
                case 0: //Slow
                    if (cur_hd == 0) { result = "2202"; }       //LEFT
                    else if (cur_hd == 1) { result = "2220"; }  //FRONT
                    else if (cur_hd == 2) { result = "0222"; }  //RIGHT
                    else if (cur_hd == 3) { result = "2022"; }  //BACK
                    break;
                case 1:
                    if (cur_hd == 0) { result = "2101"; }
                    else if (cur_hd == 1) { result = "1210"; }
                    else if (cur_hd == 2) { result = "0121"; }
                    else if (cur_hd == 3) { result = "1012"; }
                    break;
                case 2:
                    if (cur_hd == 0) { result = "1101"; }
                    else if (cur_hd == 1) { result = "1110"; }
                    else if (cur_hd == 2) { result = "0111"; }
                    else if (cur_hd == 3) { result = "1011"; }
                    break;
            }//end switch
            return result;
        }
    }
}
