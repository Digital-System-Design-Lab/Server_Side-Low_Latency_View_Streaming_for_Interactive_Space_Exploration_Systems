﻿using System;
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

public class Server_driver : MonoBehaviour
{
    #region Network variables
    //private string Pos_client_IP = "165.246.39.163";
    private string Pos_client_IP = "192.168.0.3";
    //private string Pos_client_IP = "116.34.184.61";
    private int Pos_port = 12000;

    int BANDWIDTH = 50;

    int Mega = 1000000;

    private string Texture_server_IP = "192.168.0.64";
    private int Texture_port = 11000;

    [Header("Must be the same in sender and receiver")]
    public int messageByteLength = 24;

    TcpClient posClient = null;
    TcpClient texClient = null;
    TcpListener textureServer = null;

    NetworkStream pos_stream = null;
    NetworkStream tex_stream = null;

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
    #endregion

    #region 클래스 변수
    Server server;
    NS_5DoFVR myVR;
    #endregion

    #region 클라이언트 변수
    public List<SubRange> range;
    int seg_size;
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
    Task sendTask;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        EstablishConn();
        //receiveClientInfo();
        //init_Server();
    }

    // Update is called once per frame
    void Update()
    {
        /*
        #region Receive a reqeust from client
        ReceivePacket();
        cur_loc = TransP2L();
        #endregion
        
        sendTask = new Task(()=>
        {
            loadandSendSubSeg(cur_loc);
        });
        sendTask.Start();
        
        UnityEngine.Debug.Log(sync + " 완료");
        if (time == 11040/seg_size)
        {
            EditorApplication.isPlaying = false;
        }
        time++;
        */
    }

    void receiveClientInfo()
    {
        int clientinfoSize = readPosByteSize(messageByteLength);
        readClientInfo(clientinfoSize);
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
    public bool _isSameSeg()
    {
        if ((prev_seg_x == cur_loc.get_seg_pos().seg_pos_x) && (prev_seg_y == cur_loc.get_seg_pos().seg_pos_y))
        {
            return true;
        }
        else
        {
            prev_seg_x = cur_loc.get_seg_pos().seg_pos_x;
            prev_seg_y = cur_loc.get_seg_pos().seg_pos_y;

            return false;
        }
    }
    void load_view(Loc loc)
    {
        int iter = 0;
        if (loc.getPath().Substring(0, 1).Equals("C"))
        {
            iter = loc.getPos_y();
        }
        else
        {
            iter = loc.getPos_x();
        }
        viewtemp = File.ReadAllBytes(setdirectory(iter, loc.getPath()));
    }
    void load_view(Loc loc, Qualitylist misslist)
    {
        int iter = 0;
        if (loc.getPath().Substring(0, 1).Equals("C"))
        {
            iter = loc.getPos_y();
        }
        else
        {
            iter = loc.getPos_x();
        }

        setdirectory(ref Lsubview, iter, loc.getPath(), 0, misslist.Left);
        setdirectory(ref Fsubview, iter, loc.getPath(), 1, misslist.Front);
        setdirectory(ref Rsubview, iter, loc.getPath(), 2, misslist.Right);
        setdirectory(ref Bsubview, iter, loc.getPath(), 3, misslist.Back);
    }

    void load_view(Loc loc, Qualitylist misslist, int iter, int start)
    {
        if (misslist.Left != QUALITY.EMPTY) buffer.setlview(File.ReadAllBytes(setdirectory(ref Lsubview, iter, loc.getPath(), 0, misslist.Left)), (iter-start));
        if (misslist.Front != QUALITY.EMPTY) buffer.setfview(File.ReadAllBytes(setdirectory(ref Fsubview, iter, loc.getPath(), 1, misslist.Front)), (iter - start));
        if (misslist.Right != QUALITY.EMPTY) buffer.setrview(File.ReadAllBytes(setdirectory(ref Rsubview, iter, loc.getPath(), 2, misslist.Right)), (iter - start));
        if (misslist.Back != QUALITY.EMPTY) buffer.setbview(File.ReadAllBytes(setdirectory(ref Bsubview, iter, loc.getPath(), 3, misslist.Back)), (iter - start));
    }

    void send_view()
    {
        frameBytesLength = new byte[messageByteLength];
        byteLengthToFrameByteArray(viewtemp.Length, frameBytesLength);
        tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
        tex_stream.Write(viewtemp, 0, viewtemp.Length);
    }

    public void loadSubSeg(Loc cur_loc)
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

        for (int i = start; i <= end; i++)
        {
            //Task load_task = Task.Run(() =>
            //{
            //    load_view(container, packet.result_cache.getMisslist(), i, 10.0f, region);
            //});
            //UnityEngine.Debug.LogWarningFormat("load view num {0}", i);
            //UnityEngine.Debug.LogWarningFormat("misslist : {0}", packet.result_cache.getMisslist());
            load_view(cur_loc, packet.misslist, i, start);
        }
        //08.17 오늘은 여기까지...
    }

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
        if(cur_loc.iter == 0)
        {
            reverse = false;
        }
        else if(cur_loc.iter == seg_size-1)
        {
            reverse = true;
        }

        if (!reverse)
        {
            for (int i = start; i <= end; i++)
            {
                load_view(cur_loc, packet.misslist, i, start);
                sendView(packet.misslist, (i - start));
            }
        }
        else
        {
            for (int i = end; i >= start; i--)
            {
                load_view(cur_loc, packet.misslist, i, start);
                sendView(packet.misslist, (i - start));
                //Thread.Sleep(4);
            }
        }
        
        
        //08.17 오늘은 여기까지...
    }

    void Sending_viewsize(int viewlength)
    {
        frameBytesLength = new byte[messageByteLength];
        byteLengthToFrameByteArray(viewlength, frameBytesLength);
        texClient.SendBufferSize = frameBytesLength.Length;
        texClient.NoDelay = true;
        NetworkStream tex_stream = texClient.GetStream();
        tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
    }
    void Sending_view(byte[] view)
    {
        texClient.SendBufferSize = view.Length;
        texClient.NoDelay = true;
        NetworkStream tex_stream = texClient.GetStream();
        tex_stream.Write(view, 0, view.Length);
    }
    void sendSubSeg(Qualitylist misslist)
    {
        for (int i = 0; i < buffer.seg_size; i++)
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

    void send_views(Qualitylist misslist)
    {
        if (misslist.Left != QUALITY.EMPTY)
        {
            frameBytesLength = new byte[messageByteLength];
            byteLengthToFrameByteArray(Lsubview.Length, frameBytesLength);
            texClient.SendBufferSize = frameBytesLength.Length;
            texClient.NoDelay = true;
            NetworkStream tex_stream = texClient.GetStream();
            tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
            texClient.SendBufferSize = 65536;
            texClient.NoDelay = true;
            NetworkStream tex_stream1 = texClient.GetStream();
            tex_stream1.Write(Lsubview, 0, Lsubview.Length);
        }
        if (misslist.Front != QUALITY.EMPTY)
        {
            frameBytesLength = new byte[messageByteLength];
            byteLengthToFrameByteArray(Fsubview.Length, frameBytesLength);
            texClient.SendBufferSize = frameBytesLength.Length;
            texClient.NoDelay = true;
            NetworkStream tex_stream = texClient.GetStream();
            tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
            texClient.SendBufferSize = 65536;
            texClient.NoDelay = true;
            NetworkStream tex_stream1 = texClient.GetStream();
            tex_stream1.Write(Fsubview, 0, Fsubview.Length);
        }
        if (misslist.Right != QUALITY.EMPTY)
        {
            frameBytesLength = new byte[messageByteLength];
            byteLengthToFrameByteArray(Rsubview.Length, frameBytesLength);
            texClient.SendBufferSize = frameBytesLength.Length;
            texClient.NoDelay = true;
            NetworkStream tex_stream = texClient.GetStream();
            tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
            texClient.SendBufferSize = 65536;
            texClient.NoDelay = true;
            NetworkStream tex_stream1 = texClient.GetStream();
            tex_stream1.Write(Rsubview, 0, Rsubview.Length);
        }
        if (misslist.Back != QUALITY.EMPTY)
        {
            frameBytesLength = new byte[messageByteLength];
            byteLengthToFrameByteArray(Bsubview.Length, frameBytesLength);
            texClient.SendBufferSize = frameBytesLength.Length;
            texClient.NoDelay = true;
            NetworkStream tex_stream = texClient.GetStream();
            tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
            texClient.SendBufferSize = 65536;
            texClient.NoDelay = true;
            NetworkStream tex_stream1 = texClient.GetStream();
            tex_stream1.Write(Bsubview, 0, Bsubview.Length);
        }
    }
    void displayPos()
    {
        UnityEngine.Debug.LogFormat("Current position : {0},{1}", packet.pos.getX(), packet.pos.getY());
    }
    public Loc TransP2L()
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
        sub_segment_pos seg_pos = new sub_segment_pos(start_x, end_x, start_y, end_y, seg_size);
        seg_pos.calcSeg_pos(seg_size, myVR.getCurregion().Substring(0, 1), packet.pos.getX(), packet.pos.getY(), myVR.getOrigin_X(), myVR.getOrigin_Y());

        Loc loc = new Loc(myVR.getCurregion(), seg_pos, iter);
        loc.setPos_X(Pos_x); loc.setPos_Y(Pos_y);

        return loc;
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
    public string setdirectory(int pos_x, string region)
    {
        string dir = "";
        if (pos_x < 9)
            dir = "C:\\LFDATA\\EVEN\\" + region + "\\" + "000" + (pos_x + 1).ToString() + ".jpg";
        else if (pos_x < 99)
            dir = "C:\\LFDATA\\EVEN\\" + region + "\\" + "00" + (pos_x + 1).ToString() + ".jpg";
        else if (pos_x < 999)
            dir = "C:\\LFDATA\\EVEN\\" + region + "\\" + "0" + (pos_x + 1).ToString() + ".jpg";
        UnityEngine.Debug.Log(dir);
        return dir;
    }

    public string setdirectory(ref byte[] buffer, int pos_x, string region, int direction, QUALITY digit)
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
    void ReceiveSync()
    {
        sync = readPosByteSize(messageByteLength);

    }
    void SendSync()
    {
        frameBytesLength = new byte[messageByteLength];
        byteLengthToFrameByteArray(1, frameBytesLength);
        tex_stream.Write(frameBytesLength, 0, frameBytesLength.Length);
    }
    void ReceivePacket()
    {
        int PosDataSize = readPosByteSize(messageByteLength);
        readFrameByteArray(PosDataSize);
    }


    #region Network functions
    void EstablishConn()
    {
        #region Postional data connection
        posClient = new TcpClient();
        posClient.Connect(IPAddress.Parse(Pos_client_IP), Pos_port);
        pos_stream = posClient.GetStream();
        UnityEngine.Debug.Log("Ready for receiving pos data");
        #endregion

        Thread.Sleep(1000);

        #region view data connection
        textureServer = new TcpListener(IPAddress.Any, Texture_port);
        textureServer.Start();
        Task serverThread = new Task(() =>
        {
            UnityEngine.Debug.Log("Wait for sending texture data");
            texClient = textureServer.AcceptTcpClient();
            UnityEngine.Debug.Log("Ready for sending texture data");
            tex_stream = texClient.GetStream();
        });
        serverThread.Start();

        UnityEngine.Debug.Log("My ip : " + GetLocalIP());
        #endregion

    }

    public string GetLocalIP()
    {
        string localIP = "Not available, please check your network seetings!";
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    private void readClientInfo(int size)
    {
        bool disconnected = false;

        byte[] clientinfo = new byte[size];
        posClient.ReceiveBufferSize = size;
        posClient.NoDelay = true;
        NetworkStream pos_stream = posClient.GetStream();
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
        posClient.ReceiveBufferSize = size;
        posClient.NoDelay = true;
        NetworkStream pos_stream = posClient.GetStream();
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
        posClient.ReceiveBufferSize = size;
        posClient.NoDelay = true;
        NetworkStream pos_stream = posClient.GetStream();
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

}
