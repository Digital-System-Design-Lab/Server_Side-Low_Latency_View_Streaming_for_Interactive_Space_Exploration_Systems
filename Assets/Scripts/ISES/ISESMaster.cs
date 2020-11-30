using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ISESClient;
using ISESServer;
using ISESStructure;
using ISESESAS;
using System.IO;
using System.Threading;
using System.Diagnostics;
using ISESThreadInfoWrite;
using ISESProfiler;
using System.Net.Sockets;
using Born2Code.Net;
using UnityEditor;

public class ISESMaster : MonoBehaviour
{
    Server server;
    Client client;
    Cacheinfo cacheinfo;
    ESASinfo esasinfo;
    viewinfo view_info;

    subseg_container subcontainer;
    pre_renderViews pre_container;
    view_container container;
    jpeg_container jpeg_container;
    ThreadInfoWrite threadinfo;
    Profiler profiler;
    ISESESAS.Path path;
    Loc loc;
    Pos pos;
    Pos est_pos; //estimated position (future view position)
    Point2D[] predict_p;
    List<Point2D> testlist; 
    out_cache_search result_cache;
    int esl;
    string reqlist;
    Velocity velo;

    RequestPacket temp_packet; // 동일한 sub-segment내에서는 head direction 변경과 ES level 변경을 반영하지 않는다.
    RequestPacket packet;

    Thread loadthread;
    Thread fillthread;


    int time = 0;
    double reversePercent = 0.0f;

    // segment_id
    int prev_seg_x;
    int prev_seg_y;


    public GameObject Render_tex = null;
    public GameObject Equi = null;
    Texture2D tex;
    Texture2D pre_tex;

    public byte[] framebuffer;
    public int[] time_hist;

    string reversefilename;
    int reverseoffset;

    public void TrafficShaping()
    {
        //Stream originDestinatoinStream = new NetworkStream(mysocket, false);
        //Stream destinationStream = new ThrottledStream(originDestinatoinStream, 512000);
    }



    public void init()
    {
        //int segsize = 5;
        //int cache_size = 360;

        const int segment_size = 120;
        const int one_way_length = 5520; // 480, 5520
        double[] seg_percent = { 0.05f, 0.1f, 0.25f, 0.5f }; //sub-segment size percent
        double[] cache_percent = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f }; //sub-segment size percent
        reversePercent = 0.90f;
        reversefilename = string.Format("{2}_{0}_{1}_reverse_list.txt", (int)(reversePercent * 100), (int)(seg_percent[0] * 100), 2* one_way_length);
        int segsize = (int)((double)segment_size * seg_percent[0]); // 다하고 2로 설정해야한다.
        int cache_size = (int)((double)one_way_length * cache_percent[0]);
        time_hist = new int[segsize];   
        predict_p = new Point2D[10];
        testlist = new List<Point2D>();
        client = new Client();
        server = new Server();
        threadinfo = new ThreadInfoWrite();
        cacheinfo = new Cacheinfo(cache_size, segsize, 720, 6720, Policy.LRU, 10);
        esasinfo = new ESASinfo(30, 0.3f, 0.6f);
        view_info = new viewinfo(2048, 4096, 3);
        subcontainer = new subseg_container(segsize);
        container = new view_container(4096, 2048, 4, 2, 3);
        pre_container = new pre_renderViews(container, cacheinfo.seg_size);
        jpeg_container = new jpeg_container();
        prev_seg_x = -1; prev_seg_y = -1;
        reverseoffset = time_hist.Length - 1;
    }

    void Start()
    {
        #region initialize unity objects
        #region prepare rendering
        init();

        profiler = new Profiler();
        Render_tex = GameObject.Find("LFSpace");
        Equi = GameObject.Find("Equi");
        Render_tex.GetComponent<Renderer>().enabled = true;
        Equi.GetComponent<Renderer>().enabled = true;
        Render_tex.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Pano360Shader");
        Equi.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Pano360Shader");
        tex = new Texture2D(view_info.width, view_info.height, TextureFormat.RGB24, false);
        pre_tex = new Texture2D(view_info.width, view_info.height, TextureFormat.RGB24, false);
        Render_tex.GetComponent<Renderer>().material.mainTexture = tex;
        Equi.GetComponent<Renderer>().material.mainTexture = tex;
        #endregion
        #endregion

        #region initialize other c# objects
        client.init(cacheinfo, esasinfo, view_info);
        //client.read_path("path.txt");
        //read_list("480_reverse_list.txt");
        //client.read_path("Simple_circle.txt");
        //client.read_path("Simple_circle.txt", reverselist);
        //client.read_path("Complex_snake.txt");
        client.read_path("Simple_snake.txt");
        //client.read_path("Simple_snake_[test_fast].txt");
        //client.read_path("test_snake.txt");
        //client.read_path("Worst_cycle.txt");
        //client.read_path("Simple_circle_partial_hit_test.txt");
        //client.read_path("Worst_cycle[test_superfast].txt");
        UnityEngine.Debug.LogWarning("Pathlist's length : " + client.pathlist.Length);
        createReverseIdx();
        writeList();
        //printlist();
        #endregion
    }
    public bool _isSameSeg(ref RequestPacket packet)
    {
        if((prev_seg_x == packet.loc.get_seg_pos().seg_pos_x)&& (prev_seg_y == packet.loc.get_seg_pos().seg_pos_y)){
            return true;
        }
        else
        {
            prev_seg_x = packet.loc.get_seg_pos().seg_pos_x;
            prev_seg_y = packet.loc.get_seg_pos().seg_pos_y;

            return false;
        }
    }

    bool flag_fill = false;
    bool render_pre = false;

    public List<int> reverselist; //reverse할 list 개수

    public void writeList()
    {
        using (StreamWriter outputFile = new StreamWriter(reversefilename))
        {
            foreach(int elem in reverselist)
            {
                outputFile.WriteLine(elem.ToString());
            }
        }
    }
    public void read_list(string filename)
    {
        string[] textValue = System.IO.File.ReadAllLines(filename);
        string myString;
        
        if (textValue.Length > 0)
        {
            for (int iter = 0; iter < textValue.Length; iter++)
            {
                myString = textValue[iter];
                reverselist.Add(System.Convert.ToInt32(myString));
            }
        }

    }
    public void createReverseIdx()
    {
        reverselist = new List<int>();
        int reverseL = client.pathlist.Length / time_hist.Length;
        for(int i = 1; i < reverseL; i++)
        {
            reverselist.Add(i);
        }
        Shuffle(reverselist);

        int reverseCnt = (int)((reverselist.Count+1) * reversePercent);
        UnityEngine.Debug.Log("ReverseCnt : " + reverseCnt);
        //for(int iter = reverseCnt-1; iter < reverseL; iter++)
        //{
        //    reverselist.RemoveAt(iter);
        //}
        for(int iter=0;iter<(reverseL- reverseCnt)-1; iter++)
        {
            reverselist.RemoveAt(reverselist.Count-1);
        }
        reverselist.Sort();
    }
    public void printlist()
    {
        UnityEngine.Debug.LogError("Count : " + reverselist.Count);
        for(int i = 0; i < reverselist.Count; i++)
        {
            UnityEngine.Debug.LogErrorFormat("{0} : {1}", i, reverselist[i]);
        }
    }
    public void Shuffle(List<int> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void PathTest()
    {
        path = client.receivePos(time);
        loc = client.TransP2L(path);
        path.hd = client.getHeadDirection(path.hd);
        esl = client.calcESL(path, time);
        reqlist = client.getReqViewlist(esl, path.hd);
        pos = new Pos(path.x, path.y, path.hd, esl);
        result_cache = client.searchCache_view(loc, reqlist);
        packet = client.requestView(pos, result_cache, loc);
        int iter = 0;
        if (loc.getPath().Substring(0, 1).Equals("C"))
        {
            iter = loc.getPos_y();
        }
        else if(loc.getPath().Substring(0, 1).Equals("R"))
        {
            iter = loc.getPos_x();
        }
        server.load_singleview(jpeg_container, packet.result_cache.getMisslist(), iter, 5.0f, loc.getPath());

        client.rendersubviews(jpeg_container, container, packet.result_cache.getRenderlist(), iter, packet);
        tex.LoadRawTextureData(client.render_frame(container));
        tex.Apply();

        time++;
        if(time == client.pathlist.Length)
        {
            EditorApplication.isPlaying = false;
        }
    }


    public void initlist()
    {
        for(int i = 0; i < 10; i++)
        {
            testlist.Add(new Point2D(i, i + 1));
        }

        ListTest();
    }


    public void printList()
    {
        for(int i = 0; i < testlist.Count; i++)
        {
            UnityEngine.Debug.LogFormat("{0} ({1}, {2})", i, testlist[i].getX(), testlist[i].getY());
        }
    }
    public void ListTest()
    {
        UnityEngine.Debug.Log("원본");
        printList();
        testlist.RemoveAt(5);
        UnityEngine.Debug.Log("삭제한 후");
        printList();
        testlist.Insert(5, new Point2D(100, 100));
        UnityEngine.Debug.Log("원본");
        printList();
    }

    bool firstthread = true;
    int predict_cnt = 0;
    int cnt = 0;
    bool reverseFlag = true;
    bool reverseMod = false;
    int reverse_offset = 0;
    public void ISESSystemGo()
    {
        UnityEngine.Debug.Log("=================================================");
        List<Labelinfo>label_delay = new List<Labelinfo>();

        #region Client (Request)
        
        profiler.Start("Client side");
        path = client.receivePos(time);
        loc = client.TransP2L(path);
        path.hd = client.getHeadDirection(path.hd);
        esl = client.calcESL(path);
        reqlist = client.getReqViewlist(esl, path.hd);
        result_cache = client.searchCache(loc, reqlist);
        pos = new Pos(path.x, path.y, path.hd, esl);
        packet = client.requestView(pos, result_cache, loc);

        if (client.cache.cacheinfo.policy == Policy.DR)
        {
            velo = client.cache.calcVelocity(packet);
        }
        
        UnityEngine.Debug.LogFormat("[{0}] cache size : {1} hd : {2} velo : {3}", time, client.cache.table.Count, path.hd, velo.getVelo_y());
        //checkPreRender(packet);
        label_delay.Add(profiler.End());
        //status 확인해서 partial hit이면 미리 rendering
        #endregion

        bool isPredict = true;
        bool isPartial = false;
        if(client.cache.cacheinfo.policy == Policy.DR && time!=0 && time != (client.pathlist.Length-1))
        {
            /*
         * dead reckoning일 때만 호출되는 부분
         * pre-fetching을 진행
         * view prediction을 하고 server에게 알려주는 부분이다.
         * 1차적으로 server에게 요청하는 시늉만 내는 것으로...
         * 
         */
            //step 1. view prediction하는 구간인지 확인 
            
            if (pos.getHead_dir() == 1 || pos.getHead_dir() == 2)
            {
                if (loc.iter == cacheinfo.seg_size - 1)
                {
                    //prediction을 한다.
                    predict_p = client.cache.predictPos(packet, velo);
                    UnityEngine.Debug.LogWarningFormat("{0} {1} {2} {3} predicted position", predict_p[0].getX(), predict_p[0].getY(),
                        predict_p[1].getX(), predict_p[1].getY());
                }
                else if (loc.iter == 0)
                {
                    isPredict = false;
                    for (int i = 0; i < 10; i++)
                    {
                        if (predict_p[i].getX() == packet.pos.getX() && predict_p[i].getY() == packet.pos.getY())
                        {
                            //prediction 성공
                            isPredict = true;
                            predict_cnt += 1;
                            break;
                        }
                    }
                }
            }
            else if (pos.getHead_dir() == 0 || pos.getHead_dir() == 3)
            {
                if (loc.iter == 0)
                {
                    predict_p = client.cache.predictPos(packet, velo);
                    UnityEngine.Debug.LogWarningFormat("{0} {1} {2} {3} predicted position", predict_p[0].getX(), predict_p[0].getY(),
                        predict_p[1].getX(), predict_p[1].getY());
                }
                else if (loc.iter == cacheinfo.seg_size - 1)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        isPredict = false;
                        if (predict_p[i].getX() == packet.pos.getX() && predict_p[i].getY() == packet.pos.getY())
                        {
                            //prediction 성공
                            isPredict = true;
                            break;
                        }
                    }
                }
            }

            if (!isPredict)
            {
                UnityEngine.Debug.LogError("Prediction 실패!");
            }
        }
        

#region Network(C->S)
        //Request view images to server. When the system is local environment, you can simply send a request packet structure as function parameter.
        #endregion

        UnityEngine.Debug.LogWarningFormat("[Pos] ({0}, {1}), [Loc] ({4}, {5}) iter : {3} [Region] {2}", pos.getX(), pos.getY(), loc.getPath(), loc.iter, loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y);
        profiler.Start("Searching Table");
        //if(packet.result_cache.getStat() == CacheStatus.PARTIAL_HIT)
        //{
        //    reqlist = client.getReqViewlist(packet.pos.getEslevel(), packet.pos.getHead_dir());
        //    result_cache = client.searchCache(loc, reqlist);
        //    pos = new Pos(path.x, path.y, path.hd, esl);
        //    packet = client.requestView(pos, result_cache, loc);
        //}
        client.updateCache(packet, subcontainer, packet.loc.iter, time);
        label_delay.Add(profiler.End());

#region Server (Response)
        if (!_isSameSeg(ref packet) && packet.result_cache.getStat() != 1)
        {
            
            #region codebackup
            #region partial hit pre-rendering
            //client.cache.printCache();
            if (packet.result_cache.getStat() == 2)
            {
                //client.pre_rendersubviews(pre_container, view_info, packet.result_cache.getHitlist(), packet.loc.iter, packet.result_cache.getIdx());
                client.preRendering(packet, pre_container, view_info);
                isPartial = true;
            }

            #endregion
            temp_packet = packet;
            UnityEngine.Debug.Log("!!!!! Here is region for calling a load function!!!!!!!!");
            //client.updateCache(packet, subcontainer, packet.loc.iter, time);
            //Thread loadthread = new Thread(()=>server.loadSubSeg(packet, subcontainer));
            UnityEngine.Debug.Log("[" + time + "]" + "cache status : " + packet.result_cache.getStat());
            if (!(packet.result_cache.getStat() == 1))
            {
                server.loadSubSeg(packet, ref subcontainer);
                flag_fill = true;
                render_pre = true;
                firstthread = false;
            }
            //Thread updatethread = new Thread(() =>
            //{
            //    client.cache.fillCache(packet, subcontainer);
            //});
            //updatethread.IsBackground = true;
            //updatethread.Start();
            //server.loadSubSeg(packet, subcontainer);
#endregion
        }
        
        if (flag_fill)
        {
            client.cache.fillCache(packet, ref subcontainer, temp_packet.result_cache.getMisslist());
            flag_fill = false;
        }

        #endregion
        int cachesize = client.cache.table.Count*cacheinfo.seg_size;
        //UnityEngine.Debug.Log("Cache size : " + cachesize + " Current time " + time);
#region Network (S->C)
        //Response view images to client. When the system is local environment, view image will be loaded from disk.

#endregion

#region Client (Rendering)
        profiler.Start("Rendering");
        if (render_pre && packet.result_cache.getStat() != 1)
        {
            UnityEngine.Debug.LogError("Time : " + time + " Rendering previous frame");
            //tex.LoadRawTextureData(container.framebuffer);
            tex.Apply();
            render_pre = false;
        }
        else
        {
            tex.LoadRawTextureData(client.RenderView(packet, pre_container, subcontainer, container, packet.loc.iter));
            tex.Apply();
        }
        //UnityEngine.Debug.LogFormat("Thread count :{0}", Process.GetCurrentProcess().Threads.Count);
        label_delay.Add(profiler.End());
#endregion

        profiler.recording(packet, label_delay, isPredict, isPartial);
        profiler.recordLabel(label_delay);
        //client.updateCache(packet, subcontainer, packet.loc.iter, time);
        time++;
        
        if (time == (client.pathlist.Length))
        {
            UnityEngine.Debug.LogError("Application was terminated!!");
            //Application.Quit();
            EditorApplication.isPlaying = false;
            //UnityEditor.EditorApplication.Exit(0);
            
        }
        //UnityEngine.Debug.Log("=================================================");
    }

    public void ISESSystemGoBackup()
    {
        //UnityEngine.Debug.Log("=================================================");
        List<Labelinfo> label_delay = new List<Labelinfo>();

        #region Client (Request)
        UnityEngine.Debug.LogFormat("[{0}] cache size : {1}", time, client.cache.table.Count);
        profiler.Start("Client side");
        path = client.receivePos(time);
        loc = client.TransP2L(path);
        path.hd = client.getHeadDirection(path.hd);
        esl = client.calcESL(path, time);
        reqlist = client.getReqViewlist(esl, path.hd);
        result_cache = client.searchCache(loc, reqlist);
        pos = new Pos(path.x, path.y, path.hd, esl);
        packet = client.requestView(pos, result_cache, loc);

        checkPreRenderBackup(packet);
        label_delay.Add(profiler.End());
        //status 확인해서 partial hit이면 미리 rendering
        #endregion
        bool isPartial = false;
        bool isPredict = true;
#if false
        /*
         * dead reckoning일 때만 호출되는 부분
         * pre-fetching을 진행
         * view prediction을 하고 server에게 알려주는 부분이다.
         * 1차적으로 server에게 요청하는 시늉만 내는 것으로...
         * 
         */
        //step 1. view prediction하는 구간인지 확인 
        if (pos.getHead_dir() <=1)
        {
            if(loc.iter == cacheinfo.seg_size-1)
            {
                //prediction을 한다.
                predict_p = client.cache.predictPos(packet, client.cache.calcVelocity(packet));
                UnityEngine.Debug.LogWarningFormat("{0} {1} {2} {3} predicted position", predict_p[0].getX(), predict_p[0].getY(),
                    predict_p[1].getX(), predict_p[1].getY());
            }
            else if(loc.iter == 0)
            {
                isPredict = false;
                for (int i = 0; i < 2; i++)
                {
                    if(predict_p[i].getX() == packet.pos.getX() && predict_p[i].getY() == packet.pos.getY())
                    {
                        //prediction 성공
                        isPredict = true;
                        break; 
                    }
                }
            }
        }
        else if (pos.getHead_dir() > 1)
        {
            if(loc.iter == 0)
            {
                predict_p = client.cache.predictPos(packet, client.cache.calcVelocity(packet));
                UnityEngine.Debug.LogWarningFormat("{0} {1} {2} {3} predicted position", predict_p[0].getX(), predict_p[0].getY(),
                    predict_p[1].getX(), predict_p[1].getY());
            }
            else if (loc.iter == cacheinfo.seg_size - 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    isPredict = false;
                    if (predict_p[i].getX() == packet.pos.getX() && predict_p[i].getY() == packet.pos.getY())
                    {
                        //prediction 성공
                        isPredict = true;
                        break;
                    }
                }
            }
        }
        
#endif

        #region Network(C->S)
        //Request view images to server. When the system is local environment, you can simply send a request packet structure as function parameter.
        #endregion

        UnityEngine.Debug.LogWarningFormat("[Pos] ({0}, {1}), [Loc] ({4}, {5}) iter : {3} [Region] {2}", pos.getX(), pos.getY(), loc.getPath(), loc.iter, loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y);
        profiler.Start("Searching Table");
        if (packet.result_cache.getStat() == 2)
        {
            reqlist = client.getReqViewlist(temp_packet.pos.getEslevel(), temp_packet.pos.getHead_dir());
            result_cache = client.searchCache(loc, reqlist);
            pos = new Pos(path.x, path.y, path.hd, esl);
            packet = client.requestView(pos, result_cache, loc);
        }
        client.updateCache(packet, subcontainer, packet.loc.iter, time);
        label_delay.Add(profiler.End());

        #region Server (Response)
        if (!_isSameSeg(ref packet) && packet.result_cache.getStat() != 1)
        {
            #region codebackup
            temp_packet = packet;
            UnityEngine.Debug.Log("!!!!! Here is region for calling a load function!!!!!!!!");
            //client.updateCache(packet, subcontainer, packet.loc.iter, time);
            //Thread loadthread = new Thread(()=>server.loadSubSeg(packet, subcontainer));
            UnityEngine.Debug.Log("[" + time + "]" + "cache status : " + packet.result_cache.getStat());
            if (!(packet.result_cache.getStat() == 1))
            {
                loadthread = new Thread(() =>
                {
                    server.loadSubSeg(packet, ref subcontainer);
                });
                loadthread.IsBackground = true;
                loadthread.Start();
                flag_fill = true;
                render_pre = true;
                UnityEngine.Debug.LogFormat("load thraed's status : {0}", loadthread.IsAlive);
                firstthread = false;
            }
            //Thread updatethread = new Thread(() =>
            //{
            //    client.cache.fillCache(packet, subcontainer);
            //});
            //updatethread.IsBackground = true;
            //updatethread.Start();
            //server.loadSubSeg(packet, subcontainer);
            #endregion
        }

        if (flag_fill)
        {
            fillthread = new Thread(() =>
            {
                if (subcontainer.offset_s < subcontainer.segsize - 1)
                {
                    client.cache.fillCacheBackup(packet, ref subcontainer, temp_packet.result_cache.getMisslist());
                }
                else
                {
                    if (subcontainer.offset_s == (subcontainer.segsize - 1) && subcontainer.offset_e == (subcontainer.segsize - 1))
                    {
                        subcontainer.offset_s = subcontainer.offset_e = 0;
                        flag_fill = false;
                    }
                }
            });
            fillthread.IsBackground = true;
            fillthread.Start();
        }

        #endregion
        int cachesize = client.cache.table.Count * cacheinfo.seg_size;
        //UnityEngine.Debug.Log("Cache size : " + cachesize + " Current time " + time);
        #region Network (S->C)
        //Response view images to client. When the system is local environment, view image will be loaded from disk.

        #endregion

        #region Client (Rendering)
        profiler.Start("Rendering");
        if (render_pre && packet.result_cache.getStat() != 1)
        {
            UnityEngine.Debug.LogError("Time : " + time + " Rendering previous frame");
            //tex.LoadRawTextureData(container.framebuffer);
            tex.Apply();
            render_pre = false;
        }
        else
        {
            tex.LoadRawTextureData(client.RenderView(packet, pre_container, subcontainer, container, packet.loc.iter));
            tex.Apply();
        }
        //UnityEngine.Debug.LogFormat("Thread count :{0}", Process.GetCurrentProcess().Threads.Count);
        label_delay.Add(profiler.End());
        #endregion

        profiler.recording(packet, label_delay, isPredict, isPartial);
        profiler.recordLabel(label_delay);
        //client.updateCache(packet, subcontainer, packet.loc.iter, time);
        time++;

        if (time == (client.pathlist.Length))
        {
            UnityEngine.Debug.LogError("Application was terminated!!");
            //Application.Quit();
            EditorApplication.isPlaying = false;
            //UnityEditor.EditorApplication.Exit(0);

        }
        //UnityEngine.Debug.Log("=================================================");
    }


    public void manual_viewload()
    {

        byte[] left_img = File.ReadAllBytes("left_image_0001.jpg");
        byte[] front_img = File.ReadAllBytes("front_image_0001.jpg");
        byte[] right_img = File.ReadAllBytes("right_image_0001.jpg");

        subcontainer.setlview(left_img);
        subcontainer.setfview(front_img);
        subcontainer.setrview(right_img);

    }

    public void renderingTest()
    {
        manual_viewload();
        //client.rendersubviews(subcontainer, container, "2220", 0);
        framebuffer = client.render_frame(container);
        tex.LoadRawTextureData(framebuffer);
        tex.Apply();
    }

    public byte[] naiveRender()
    {
        byte[] framebuffer = new byte[4096 * 2048 * 3];
        byte[] left_img = new byte[1024 * 2048 * 3];
        byte[] front_img = new byte[1024 * 2048 * 3];
        byte[] right_img = new byte[1024 * 2048 * 3];
        byte[] empty_img = new byte[1024 * 2048 * 3];

        for(int i = 0; i < empty_img.Length; i++)
        {
            empty_img[i] = 0;
        }

        byte[] limg = File.ReadAllBytes("left_image_0001.jpg");
        byte[] fimg = File.ReadAllBytes("front_image_0001.jpg");
        byte[] rimg = File.ReadAllBytes("right_image_0001.jpg");

        int err = Client.decoding(limg, left_img, limg.Length, 1024, 2048, 3);
        err = Client.decoding(fimg, front_img, fimg.Length, 1024, 2048, 3);
        err = Client.decoding(rimg, right_img, rimg.Length, 1024, 2048, 3);

        int one_eighth_length = (1024 / 2) * 3;
        int quater_length = 1024 * 3;
        int back_offset = one_eighth_length;
        int front_offset = 0;
        int left_offset = 0;
        int right_offset = 0;
        int offset = 0;

        for (int h = 0; h < container.oheight; h++)
        {
            System.Buffer.BlockCopy(empty_img, back_offset, framebuffer, offset, one_eighth_length);
            back_offset -= one_eighth_length;
            offset += one_eighth_length;
            System.Buffer.BlockCopy(left_img, left_offset, framebuffer, offset, quater_length);
            left_offset += quater_length;
            offset += quater_length;
            System.Buffer.BlockCopy(front_img, front_offset, framebuffer, offset, quater_length);
            front_offset += quater_length;
            offset += quater_length;
            System.Buffer.BlockCopy(right_img, right_offset, framebuffer, offset, quater_length);
            right_offset += quater_length;
            offset += quater_length;
            System.Buffer.BlockCopy(empty_img, back_offset, framebuffer, offset, one_eighth_length);
            back_offset += (quater_length + one_eighth_length);
            offset += one_eighth_length;
        }

        return framebuffer;

    }



    // Update is called once per frame
    void Update()
    {
        ISESSystemGo();
        //ISESSystemGoBackup();
        //PathTest();


        //renderingTest();
        //tex.LoadRawTextureData(naiveRender());
        //tex.Apply();

        /*
         * 08.24 업무일지
         * sub-segment를 background에서 load하는 작업은 완료 하지만, load한 것을 cache에 채우고 hit를 발생하도록 하는 작업이 필요...
         * cache의 update 함수를 수정해야한다.
         * 
         * 
         * 08.25 업무일지
         * cache status 중 miss와 hit에 대해서는 문제없이 수정했다.
         * partial hit와 full 경우를 정상 작동시켜야 한다.
         * 그리고 역방향의 load 또한 진행할 수 있어야한다.
         * 
         */

    }


    private void OnApplicationQuit()
    {
        profiler.getSummary();
        profiler.writeSummary();
        profiler.writeDetail();
        profiler.writeLabel();
    }

    public void checkPreRender(RequestPacket packet)
    {
        //if(packet.result_cache.getStat() == CacheStatus.PARTIAL_HIT && (packet.loc.iter == 0 || packet.loc.iter==cacheinfo.seg_size-1))
        //{
        //    UnityEngine.Debug.LogError("PreRender function ");
        //    client.pre_rendersubviews(pre_container, view_info, packet.result_cache.getHitlist(), packet.loc.iter, packet.result_cache.getIdx());
        //}

        if(packet.result_cache.getStat() == 2)
        {
            UnityEngine.Debug.LogError("PreRender function ");
            client.pre_rendersubviews(pre_container, packet.result_cache.getHitlist(), packet.loc.iter, packet.result_cache.getIdx());
        }
    }
    public void checkPreRenderBackup(RequestPacket packet)
    {
        if (packet.result_cache.getStat() == 2 && (packet.loc.iter == 0 || packet.loc.iter == cacheinfo.seg_size - 1))
        {
            Thread pre_decThread = new Thread(() =>
            {
                client.pre_rendersubviews(pre_container, packet.result_cache.getHitlist(), packet.loc.iter, packet.result_cache.getIdx());
            });
            pre_decThread.IsBackground = true;
            pre_decThread.Start();
        }
    }
}
