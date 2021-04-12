using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ISESStructure;
using System.Runtime.InteropServices;
using UnityEditor;
using ISESServer;
using _NS_5DoFVR;
using ISESESAS;
using Born2Code.Net;
using System.Diagnostics;

public class Server_driver : MonoBehaviour
{
    #region Network variables
    [Header("Must be the same in sender and receiver")]
    public int messageByteLength = 24;
    TcpListener tcpserver = null;
    TcpClient tcpclient = null;
    NetworkStream stream = null;
    private int Port = 11000;

    DataPacket receivePacket;
    DataPacket tempPacket;

    Request packet;
    Client_info client_info;

    byte[] frameBytesLength;
    #endregion

    #region positional data
    Pos cur_pos;
    Loc cur_loc;
    out_cache_search cur_result;
    #endregion

    #region 기타 변수들
    int time = 0;
    int sync = 0;
    int prev_seg_x;
    int prev_seg_y;

    sub_segment_pos seg_pos;
    #endregion

    #region 클래스 변수
    Server server;
    NS_5DoFVR myVR;
    #endregion

    #region 클라이언트 변수
    public List<SubRange> range;
    int seg_size;
    #endregion

    #region MesurementExecutionTime
    Stopwatch recvSW;
    Stopwatch loadSW;
    int sumOfBufferWrite=0;
    #endregion

    #region byte변수
    byte[] Lsubview;
    byte[] Fsubview;
    byte[] Rsubview;
    byte[] Bsubview;
    byte[] viewtemp;

    server_buffer buffer;
    #endregion

    #region Task 변수
    Thread sendTask;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        Application.runInBackground = true;
        EstablishConn();                    //클라이언트와의 연결
        receiveClientInfo();                //클라이언트로부터 sub-segment size 정보 수신
        init_Server();                      //클래스 초기화
        recvSW = new Stopwatch();
        loadSW = new Stopwatch();
    }

    // Update is called once per frame
    void Update()
    {
        // 만약 client가 끊긴다면 종료
        if(tcpclient.Connected == false || tcpclient.Available == 1)
        {
            UnityEngine.Debug.Log("Disconnected");
            EditorApplication.isPlaying = false;
        }
        #region Receive a reqeust from client
        ReceivePacket();                    //클라이언트로부터 request packet 수신
        TransP2L();                         //수신받은 packet은 가상공간 좌표로 변환
        #endregion

        sendTask = new Thread(()=>
        {
            loadandSendSubSeg(cur_loc);     //Sub-segment 요청이 들어왔다면 background로 클라이언트에게 전송
        });
        sendTask.Start();
        time++;
    }

    #region 기타 methods
    public void TransP2L()
    {
        myVR.classify_Location(packet.pos.getX(), packet.pos.getY());
        string region = myVR.getCurregion();
        int i = 0;
        int iter = 0;
        int Pos_x = myVR.getPos_X(); int Pos_y = myVR.getPos_Y();
        int start_x = 0; int end_x = 0;
        int start_y = 0; int end_y = 0;


        for (i = 0; i < range.Count; i++)
        {
            if ((Pos_x >= range[i]._start) && (Pos_x <= range[i]._end) && region.Substring(0, 1).Equals("R"))
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
        seg_pos = new sub_segment_pos(start_x, end_x, start_y, end_y, seg_size);
        seg_pos.calcSeg_pos(seg_size, myVR.getCurregion().Substring(0, 1), packet.pos.getX(), packet.pos.getY(), myVR.getOrigin_X(), myVR.getOrigin_Y());

        cur_loc = new Loc(myVR.getCurregion(), seg_pos, iter);
        cur_loc.setPos_X(Pos_x); cur_loc.setPos_Y(Pos_y);
    }
    public List<SubRange> calcSubrange(int seg_size)
    {
        List<SubRange> subrange = new List<SubRange>();
        int numOfrange = 120 / seg_size;
        for (int iter = 0; iter < numOfrange; iter++)
        {
            int start = seg_size * (iter);
            int end = seg_size * (iter + 1) - 1;
            SubRange range = new SubRange(start, end);
            subrange.Add(range);
        }
        return subrange;
    }
    void receiveClientInfo()
    {
        int clientinfoSize = readPosByteSize(messageByteLength);
        readClientInfo(clientinfoSize);
    }
    /// <summary>
    /// Dead Reckoning의 경우 예측이 실패했을 때 서버 응답시간에 패널티를 부여
    /// prediction이 없을 때 2~3ms, prediction 실패의 경우 +30ms
    /// </summary>
    /// <param name="isPredicted">Prediction 성공 여부</param>
    void Response(int isPredicted)
    {
        int delay = 3;
        System.Random rand = new System.Random();
        if (isPredicted == 0)
        {
            //prediction 성공
            delay += rand.Next(-1, 1);
        }
        else
        {
            delay = delay * 30 + rand.Next(-1, 1);
        }
        UnityEngine.Debug.LogWarningFormat("Response time : {0}", delay);
        dodelay(delay);
    }
    public void dodelay(int target_delay)
    {
        DateTime temp = DateTime.Now;
        float excution_time = 0.0f;
        while (target_delay > excution_time)
        {
            excution_time = (DateTime.Now - temp).Milliseconds;
        }
    }
    void init_Server()
    {
        server = new Server();
        myVR = new NS_5DoFVR();
        myVR.parsing_data();
        seg_size = client_info.sub_segment_size;
        buffer = new server_buffer(seg_size);
        range = calcSubrange(seg_size);
        prev_seg_x = -1;
        prev_seg_y = -1;
    }
    #endregion

    #region Disk functions
    void load_view(Loc loc, Qualitylist misslist, int iter, int start)
    {
        if (misslist.Left != QUALITY.EMPTY) buffer.setlview(File.ReadAllBytes(setdirectory(iter, loc.getPath(), 0, misslist.Left)), (iter - start));
        if (misslist.Front != QUALITY.EMPTY) buffer.setfview(File.ReadAllBytes(setdirectory(iter, loc.getPath(), 1, misslist.Front)), (iter - start));
        if (misslist.Right != QUALITY.EMPTY) buffer.setrview(File.ReadAllBytes(setdirectory(iter, loc.getPath(), 2, misslist.Right)), (iter - start));
        if (misslist.Back != QUALITY.EMPTY) buffer.setbview(File.ReadAllBytes(setdirectory(iter, loc.getPath(), 3, misslist.Back)), (iter - start));
    }
    public string setdirectory(int pos_x, string region, int direction, QUALITY digit)
    {
        string[] ori = { "LEFT", "FRONT", "RIGHT", "BACK" };
        string dir = "";
        string quality = "";
        if (digit == QUALITY.DS)
        {
            quality = "1";
        }
        else if (digit == QUALITY.ORIGINAL)
        {
            quality = "4";
        }

        if (pos_x < 9)
            dir = "C:\\LFDATA\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "000" + (pos_x + 1).ToString() + ".jpg";
        else if (pos_x < 99)
            dir = "C:\\LFDATA\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "00" + (pos_x + 1).ToString() + ".jpg";
        else if (pos_x < 999)
            dir = "C:\\LFDATA\\" + quality + "K" + "\\Keyidea2\\" + ori[direction] + "\\" + region + "\\" + ori[direction].ToLower() + "_image_" + "0" + (pos_x + 1).ToString() + ".jpg";

        //UnityEngine.Debug.Log(dir);
        //buffer = File.ReadAllBytes(dir);\
        return dir;
    }
    #endregion

    #region Network functions
    public void loadandSendSubSeg(Loc cur_loc)
    {
        string region = cur_loc.getPath();
        int start = -1; int end = -1;
        if (region.Substring(0, 1).Equals("R"))
        {
            start = cur_loc.get_seg_pos().start_x;
            end = cur_loc.get_seg_pos().end_x;
        }
        else if (region.Substring(0, 1).Equals("C"))
        {
            start = cur_loc.get_seg_pos().start_y;
            end = cur_loc.get_seg_pos().end_y;
        }

        bool reverse = false;
        if (cur_loc.iter == 0)
        {
            reverse = false;
        }
        else if (cur_loc.iter == seg_size - 1)
        {
            reverse = true;
        }
        int isPredicted = packet.isPredicted;
        if (!reverse)
        {
            for (int i = start; i <= end; i++)
            {
                DateTime temp = DateTime.Now;
                Response(isPredicted);                          //Dead Reckoning 일 경우 prediction 실패시 서버 응답시간에 패널티 부여
                load_view(cur_loc, packet.misslist, i, start);  //misslist에 따른 sub-view를 disk로부터 load
                sendView(packet.misslist, (i - start));         //클라이언트에게 packet으로 sub-view 전송
                UnityEngine.Debug.LogFormat("Sending view delay : {0:f3}", (DateTime.Now - temp).TotalMilliseconds);
            }
        }
        else
        {
            for (int i = end; i >= start; i--)
            {
                DateTime temp = DateTime.Now;
                Response(isPredicted);
                load_view(cur_loc, packet.misslist, i, start);
                sendView(packet.misslist, (i - start));
                UnityEngine.Debug.LogFormat("Sending view delay : {0:f3}", (DateTime.Now - temp).TotalMilliseconds);

            }
        }
    }

    void Sending_viewsize(int viewlength)
    {
        frameBytesLength = new byte[messageByteLength];
        byteLengthToFrameByteArray(viewlength, frameBytesLength);
        tcpclient.SendBufferSize = frameBytesLength.Length;
        tcpclient.NoDelay = true;
        tcpclient.Client.NoDelay = true;
        NetworkStream tex_stream = tcpclient.GetStream();
        tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
    }
    void Sending_view(byte[] view)
    {
        tcpclient.SendBufferSize = view.Length;
        tcpclient.NoDelay = true;
        tcpclient.Client.NoDelay = true;
        NetworkStream tex_stream = tcpclient.GetStream();
        tex_stream.Write(view, 0, view.Length);
    }

    void sendView(Qualitylist misslist, int i)
    {
        Sending_viewsize(i);
        if (misslist.Left != QUALITY.EMPTY)
        {
            Sending_viewsize(buffer.getlview(i).Length);
            Sending_view(buffer.getlview(i));
        }
        if (misslist.Front != QUALITY.EMPTY)
        {
            Sending_viewsize(buffer.getfview(i).Length);
            Sending_view(buffer.getfview(i));
        }
        if (misslist.Right != QUALITY.EMPTY)
        {
            Sending_viewsize(buffer.getrview(i).Length);
            Sending_view(buffer.getrview(i));
        }
        if (misslist.Back != QUALITY.EMPTY)
        {
            Sending_viewsize(buffer.getbview(i).Length);
            Sending_view(buffer.getbview(i));
        }
    }
    void ReceivePacket()
    {
        int PosDataSize = readPosByteSize(messageByteLength);
        readFrameByteArray(PosDataSize);
    }
    void EstablishConn()
    {
        tcpserver = new TcpListener(IPAddress.Any, Port);
        tcpserver.Start();
        Task serverThread = new Task(() =>
        {
            UnityEngine.Debug.Log("Wait for Client");
            tcpclient = tcpserver.AcceptTcpClient();
            UnityEngine.Debug.Log("Connection is successful!!");
            stream = tcpclient.GetStream();
        });
        serverThread.Start();
        serverThread.Wait();
    }

    private void readClientInfo(int size)
    {
        bool disconnected = false;
        byte[] clientinfo = new byte[size];
        tcpclient.ReceiveBufferSize = size;
        tcpclient.NoDelay = true;
        NetworkStream pos_stream = tcpclient.GetStream();
        var total = 0;
        do
        {
            var read = pos_stream.Read(clientinfo, total, size - total);
            if (read == 0)
            {
                disconnected = true;
                break;
            }
            total += read;
        } while (total != size);
        UnityEngine.Debug.Log("position data 모두 받았음!");

        client_info = ByteToStruct<Client_info>(clientinfo);
    }

    private void readFrameByteArray(int size)
    {
        bool disconnected = false;

        byte[] PosByte = new byte[size];
        tcpclient.ReceiveBufferSize = size;
        tcpclient.NoDelay = true;
        NetworkStream pos_stream = tcpclient.GetStream();
        var total = 0;
        do
        {
            var read = pos_stream.Read(PosByte, total, size - total);
            if (read == 0)
            {
                disconnected = true;
                break;
            }
            total += read;
        } while (total != size);
        UnityEngine.Debug.Log("position data 모두 받았음!");

        packet = ByteToStruct<Request>(PosByte);
    }
    private int readPosByteSize(int size)
    {
        bool disconnected = false;

        byte[] PosBytesCount = new byte[size];
        tcpclient.ReceiveBufferSize = size;
        tcpclient.NoDelay = true;
        NetworkStream pos_stream = tcpclient.GetStream();
        var total = 0;
        do
        {
            var read = pos_stream.Read(PosBytesCount, total, size - total);
            if (read == 0)
            {
                disconnected = true;
                break;
            }
            total += read;
        } while (total != size);

        int byteLength;

        if (disconnected)
        {
            byteLength = -1;
        }
        else
        {
            byteLength = frameByteArrayToByteLength(PosBytesCount);
        }

        return byteLength;
    }
    //Converts the byte array to the data size and returns the result
    int frameByteArrayToByteLength(byte[] frameBytesLength)
    {
        int byteLength = BitConverter.ToInt32(frameBytesLength, 0);
        return byteLength;
    }

    void byteLengthToFrameByteArray(int byteLength, byte[] fullBytes)
    {
        //Clear old data
        Array.Clear(fullBytes, 0, fullBytes.Length);
        //Convert int to bytes
        byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
        //Copy result to fullBytes
        bytesToSendCount.CopyTo(fullBytes, 0);
    }
    #endregion

    #region Convert struct to byte array
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
    #endregion
    private void OnApplicationQuit()
    {
        UnityEngine.Debug.LogWarningFormat("Avg Buffer Write time : {0:f3}", (double)sumOfBufferWrite/(double)960);
    }
}
