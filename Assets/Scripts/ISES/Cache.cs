using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISESStructure;
using System.Threading;

namespace ISESCache
{
    class GDC
    {
        public List<Record> table;
        int[,] Row_index_table;
        int[,] Col_index_table;
        int[,] index_table;
        int row_seg_size = 0;
        int col_seg_size = 0;
        RequestPacket prev_Packet;
        Point2D[] predicted_pos;


        public Pathcalculator pathcalc;
        public Cacheinfo cacheinfo;

        public int cntRecords(int idx)
        {
            int total_cnt = 0;
            for(int i = 0; i < cacheinfo.seg_size; i++)
            {
                int f_cnt=0;
                int l_cnt=0;
                int r_cnt=0;
                int b_cnt=0;
                if (table[idx].Lsubview[i].Length == 1)
                {
                    l_cnt = 1;
                }
                if (table[idx].Fsubview[i].Length == 1)
                {
                    f_cnt = 1;
                }
                if (table[idx].Rsubview[i].Length == 1)
                {
                    r_cnt = 1;
                }
                if (table[idx].Bsubview[i].Length == 1)
                {
                    b_cnt = 1;
                }

                if ((l_cnt + f_cnt + r_cnt + b_cnt) <= 1)
                {
                    total_cnt += 1;
                }
            }
            return total_cnt;
        }

        public void init(Cacheinfo cacheinfo)
        {
            int unit_r_size = 6;
            int unit_c_size = 56;
            this.cacheinfo = cacheinfo;
            pathcalc = new Pathcalculator();
            pathcalc.init();
            table = new List<Record>();
            prev_Packet = new RequestPacket();
            Row_index_table = new int[(cacheinfo.row_size / cacheinfo.seg_size), (cacheinfo.col_size / 120)];
            Col_index_table = new int[(cacheinfo.row_size / 120), (cacheinfo.col_size / cacheinfo.seg_size)];
            //index_table = new int[(cacheinfo.row_size / cacheinfo.seg_size), (cacheinfo.col_size / cacheinfo.seg_size)];
            //row_seg_size = (cacheinfo.row_size / unit_r_size) + ((cacheinfo.row_size / unit_r_size) - 1) * (120 / cacheinfo.seg_size);
            //col_seg_size = (cacheinfo.col_size / unit_c_size) + ((cacheinfo.col_size / unit_c_size) - 1) * (120 / cacheinfo.seg_size);

            row_seg_size = unit_r_size*(120/ cacheinfo.seg_size + 1)+1;
            col_seg_size = unit_c_size*(120/ cacheinfo.seg_size + 1)+1;
            predicted_pos = new Point2D[cacheinfo.predict_cnt];
            UnityEngine.Debug.Log("size : " + row_seg_size + " " + col_seg_size);
            index_table = new int[row_seg_size, col_seg_size];
            int r = 0; int c = 0;
            //for (r=0; r < Row_index_table.GetLength(0); r++)
            //{
            //    for(c=0;c<Row_index_table.GetLength(1); c++)
            //    {
            //        Row_index_table[r, c] = -1;
            //    }
            //}
            //for (r = 0; r < Col_index_table.GetLength(0); r++)
            //{
            //    for (c = 0; c < Col_index_table.GetLength(1); c++)
            //    {
            //        Col_index_table[r, c] = -1;
            //    }
            //}
            for (r = 0; r < row_seg_size; r++)
            {
                for (c = 0; c < col_seg_size; c++)
                {
                    index_table[r, c] = -1;
                }
            }
        }

        public void printTable()
        {
            for(int iter = 0; iter < table.Count; iter++)
            {
                UnityEngine.Debug.LogErrorFormat("{0} seg ({1}, {2})", iter, table[iter].seg_x, table[iter].seg_y);
            }
        }

        public void printTable(int time)
        {
            for (int iter = 0; iter < table.Count; iter++)
            {
                UnityEngine.Debug.LogErrorFormat("{3} {0} seg ({1}, {2}) time : {4}", iter, table[iter].seg_x, table[iter].seg_y, time, table[iter].read_time);
            }
        }


        public void addRecord(Record r)
        {
            table.Add(r);
        }

        public void addRecord(Record r, int idx)
        {
            table.Insert(idx, r);
        }
        public void recordPrevPacket(RequestPacket Packet)
        {
            prev_Packet = Packet;
        }

        #region Get methods
        public Record GetRecord(int idx) { return table[idx]; }
        public byte[] GetFsubview(int idx, int offset) { return table[idx].Fsubview[offset]; }
        public byte[] GetRsubview(int idx, int offset) { return table[idx].Rsubview[offset]; }
        public byte[] GetBsubview(int idx, int offset) { return table[idx].Bsubview[offset]; }
        public byte[] GetLsubview(int idx, int offset) { return table[idx].Lsubview[offset]; }
        public byte[] GetView(int idx, int offset, int digit)
        {
            byte[] temp = null;
            switch (digit)
            {
                case 0:
                    temp = GetLsubview(idx, offset);
                    break;
                case 1:
                    temp = GetFsubview(idx, offset);
                    break;
                case 2:
                    temp = GetRsubview(idx, offset);
                    break;
                case 3:
                    temp = GetBsubview(idx, offset);
                    break;
            }
            return temp;
        }
        #endregion

        public void update_viewlist(int idx, string list){table[idx].setViewlist(list);}
        public string getviewlist(int idx) { return table[idx].viewlist; }

        public void printCache(int time)
        {
            UnityEngine.Debug.LogError("***************************************************************");
            for(int i = 0; i < table.Count; i++)
            {
                UnityEngine.Debug.LogErrorFormat("{3} Segmentation info : ({0}, {1}) {2}", table[i].seg_x, table[i].seg_y, table[i].path, time);
            }
            UnityEngine.Debug.LogError("***************************************************************");
        }



        public void checkStat(int idx, string reqlist, ref out_cache_search result_cache)
        {
            // Substring(start, length); start로부터 length만큼을 잘라서 반환
            string cachedlist = null;
            if (idx != -1)
            {
                cachedlist = table[idx].viewlist;
            }
            else
            {
                cachedlist = "0000";
            }
            //UnityEngine.Debug.Log("request list : " + reqlist);
            
            int hitCnt = 0;
            int cacheddigit = 0;
            int reqdigit = 0;

            int missdigit = 0;
            int hitdigit = 0;
            int renderdigit = 0;
            for(int i = 0; i < 4; i++)
            {
                cacheddigit = Convert.ToInt32((cachedlist.Substring(i, 1)));
                reqdigit = Convert.ToInt32(reqlist.Substring(i, 1));
                if (reqdigit != 0)
                {
                    if (cacheddigit !=0)
                    {
                        if((cacheddigit - reqdigit) >= 0)
                        {
                            hitCnt++;
                            missdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * 0;
                            renderdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * cacheddigit;
                            hitdigit+= (int)Math.Pow(10.0f, (float)(3 - i)) * cacheddigit;
                        }
                        else
                        {
                            missdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * 2;
                            renderdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * 2;
                        }
                    }
                    else
                    {
                        missdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * reqdigit;
                        renderdigit += (int)Math.Pow(10.0f, (float)(3 - i)) * reqdigit;
                    }
                }
            }

            if(hitCnt == 3)
            {
                result_cache.setStat(1);
                
            }
            else if(hitCnt == 0)
            {
                if (isFull())
                {
                    result_cache.setStat(3);
                }
                else
                {
                    result_cache.setStat(0);
                }
            }
            else
            {
                result_cache.setStat(2);
                UnityEngine.Debug.LogFormat("cachedlist : {0}, reqlist : {1}", cachedlist, reqlist);
            }

            string misslist = missdigit.ToString();
            string hitlist = hitdigit.ToString();
            string renderlist = renderdigit.ToString();

            misslist = paddingzeros(misslist);
            hitlist = paddingzeros(hitlist);
            renderlist = paddingzeros(renderlist);

            result_cache.setMisslist(misslist);
            result_cache.setHitlist(hitlist);
            result_cache.setRenderlist(renderlist);

           
        }


        public string paddingzeros(string str)
        {
            string result = "";
            switch (str.Length)
            {
                case 0:
                    result = "0000";
                    break;
                case 1:
                    result = "000" + str;
                    break;
                case 2:
                    result = "00" + str;
                    break;
                case 3:
                    result = "0" + str;
                    break;
                default:
                    result = str;
                    break;
            }
            return result;
        }

        public void reset_index(int seg_x, int seg_y)
        {
            index_table[seg_x, seg_y] = -1;
        }


        public void record_index(Loc loc, int idx)
        {
            //if (loc.dir.Equals("R"))
            //{
            //    //Row index table에서 searching
            //    Row_index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y] = idx;
            //}
            //else if (loc.dir.Equals("C"))
            //{
            //    //Column index table에서 searching
            //    Col_index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y] = idx;
            //}
            index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y] = idx;
        }
        public int find_records(Loc loc)
        {
            int idx = -1;
            //if (loc.dir.Equals("R"))
            //{
            //    //Row index table에서 searching
            //    idx = Row_index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y];
            //}
            //else if (loc.dir.Equals("C"))
            //{
            //    //Column index table에서 searching
            //    idx = Col_index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y];
            //}

            idx = index_table[loc.get_seg_pos().seg_pos_x, loc.get_seg_pos().seg_pos_y];
            //UnityEngine.Debug.LogWarningFormat("find_records function idx {0} ", idx);
            return idx;
        }

        public int find_records(int seg_pos_x, int seg_pos_y)
        {
            int idx = -1;
            if((seg_pos_x>=0 && seg_pos_x<row_seg_size) && (seg_pos_y >= 0 && seg_pos_y < col_seg_size))
            {
                idx = index_table[seg_pos_x, seg_pos_y];
            }
            else
            {
            }
            
            return idx;
        }

        public bool isFull()
        {
            if((table.Count+1)*cacheinfo.seg_size < cacheinfo.cachesize)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public int ReplaceLRU()
        {
            int oldest_time = Int32.MaxValue;
            
            int idx = -1;

            for (int i = 0; i < table.Count; i++)
            {
                if (oldest_time > table[i].read_time)
                {
                    oldest_time = table[i].read_time;
                    idx = i;
                }
            }
            reset_index(table[idx].seg_x, table[idx].seg_y);
            return idx;
        }


        public int ReplaceGDC(int cur_x, int cur_y)
        {
            List<float> dist_list = new List<float>();
            List<float> sorted_list = new List<float>();
            List<int> sorted_index = new List<int>();
            int victim_index = -1;
            candidate_[] candidate_list = new candidate_[3];
            int cur_path = pathcalc.calc_path();

            for(int i = 0; i < table.Count; i++)
            {
                float temp = Math.Abs((cur_x - table[i].seg_x)) + Math.Abs((cur_y - table[i].seg_y));
                dist_list.Add(temp);
            }
            index_based_sort(ref dist_list, ref sorted_list, ref sorted_index);

            for(int i = 0; i < table.Count; i++)
            {
                UnityEngine.Debug.LogWarningFormat("{4} {5} segment's : {0} dist lsit : {1} sorted_list {2} sorted_index : {3}",
                    i, dist_list[i], sorted_list[i], sorted_index[i], table[i].seg_x , table[i].seg_y);
            }



            if (table.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    candidate_list[i].candidate_index = sorted_index[i];
                    candidate_list[i].candidate_dist = cur_path - table[candidate_list[i].candidate_index].path;
                }

                float max_path = candidate_list[0].candidate_dist < candidate_list[1].candidate_dist ?
                    (candidate_list[1].candidate_dist < candidate_list[2].candidate_dist ? candidate_list[2].candidate_dist : candidate_list[1].candidate_dist) :
                    (candidate_list[0].candidate_dist < candidate_list[2].candidate_dist ? candidate_list[2].candidate_dist : candidate_list[0].candidate_dist);
                for (int i = 0; i < 3; i++)
                {
                    if (candidate_list[i].candidate_dist == max_path)
                    {
                        //UnityEngine.Debug.LogErrorFormat("{0} {1} segment is replaced!", table[candidate_list[i].candidate_index].seg_x, table[candidate_list[i].candidate_index].seg_y);
                        victim_index = candidate_list[i].candidate_index;
                        reset_index(table[victim_index].seg_x, table[victim_index].seg_y);
                        //table.RemoveAt(victim_index);
                        break;
                    }
                }
            }
            else
            {
                //table.RemoveAt(sorted_index[0]);
                victim_index = sorted_index[0];
                reset_index(table[victim_index].seg_x, table[victim_index].seg_y);
            }
            return victim_index;
        }
        public void index_based_sort(ref List<float> src, ref List<float> dst, ref List<int> output_index)
        {
            var sorted = src

                .Select((x, i) => new { Value = x, OriginalIndex = i })
                .OrderByDescending(x => x.Value)
                .ToList();

            dst = sorted.Select(x => x.Value).ToList();
            output_index = sorted.Select(x => x.OriginalIndex).ToList();

        }


        public void updateReadtime(int segidx, int time)
        {   //hit 발생했을 때 호출
            table[segidx].setReadtime(time);
        }
        public void updateSubview(int segidx, string misslist)
        {

            //각 digit마다 큰 수를 새로운 viewlist로 만들기...
            int[] view_lists = new int[4];
            int[] miss_lists = new int[4];
            int[] new_lists = new int[4];
            string new_viewlist = "";
            int new_digits = 0;
            for (int i = 0; i < 4; i++)
            {
                view_lists[i] = Convert.ToInt32(table[segidx].viewlist.Substring(i, 1));
                miss_lists[i] = Convert.ToInt32(misslist.Substring(i, 1));
                new_lists[i] = view_lists[i] >= miss_lists[i] ? view_lists[i] : miss_lists[i];
                new_digits += (int)Math.Pow(10.0f, (float)(3 - i)) * new_lists[i];
            }
            new_viewlist = new_digits.ToString();
            UnityEngine.Debug.Log("[Partial Hit] 새로운 cached list : " + new_viewlist);
            table[segidx].setViewlist(new_viewlist);
        }

        public void updateSubview(int segidx, string misslist, subseg_container subcontainer, int iter)
        {
            int digit = -1;
            table[segidx].setViews(misslist, subcontainer, iter);


            //각 digit마다 큰 수를 새로운 viewlist로 만들기...
            int[] view_lists = new int[4];
            int[] miss_lists = new int[4];
            int[] new_lists = new int[4];
            string new_viewlist = "";
            int new_digits = 0;
            for (int i = 0; i < 4; i++)
            {
                view_lists[i] = Convert.ToInt32(table[segidx].viewlist.Substring(i, 1));
                miss_lists[i] = Convert.ToInt32(misslist.Substring(i, 1));
                new_lists[i] = view_lists[i] >= miss_lists[i] ? view_lists[i] : miss_lists[i];
                new_digits += (int)Math.Pow(10.0f, (float)(3 - i)) * new_lists[i];
            }
            new_viewlist = new_digits.ToString();
            table[segidx].setViewlist(new_viewlist);
        }

        public void fillCache(RequestPacket packet, ref subseg_container subcontainer, string misslist)
        {
            for (int i = subcontainer.offset_s; i <= subcontainer.offset_e; i++)
            {
                //UnityEngine.Debug.LogFormat("{0} view cached, misslist : {1}", i, misslist);
                table[find_records(packet.loc)].setViews(misslist, subcontainer, i);
            }
        }

        public void fillCacheBackup(RequestPacket packet, ref subseg_container subcontainer, string misslist)
        {
            //UnityEngine.Debug.LogErrorFormat("offset_s/_e {0} {1}", subcontainer.offset_s, subcontainer.offset_e);
            if (subcontainer.offset_e > 0)
            {
                for (int i = subcontainer.offset_s; i <= subcontainer.offset_e; i++)
                {
                    //UnityEngine.Debug.LogFormat("{0} view cached, misslist : {1}", i, misslist);
                    table[find_records(packet.loc)].setViews(misslist, subcontainer, i);
                }
                subcontainer.offset_s = subcontainer.offset_e;
            }
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



        public void updateCache(RequestPacket packet, subseg_container subcontainer, int iter, int time)
        {
            Record r = new Record(packet.loc, subcontainer.segsize);
            r.read_time = time;
            switch (packet.result_cache.getStat())
            {
                case 0:
                    //tuple 만들고 path 계산 후 넣어주기
                    //r.setViews(packet.result_cache.getMisslist(), subcontainer, iter);
                    //UnityEngine.Debug.LogWarningFormat("Cache miss 발생 그리고 record 생성");
                    r.setViewlist(packet.result_cache.getMisslist());
                    addRecord(r);
                    record_index(packet.loc, table.Count-1);
                    break;
                case 1:
                    updateReadtime(packet.result_cache.getIdx(), time);
                    //updateReadtime();
                    break;
                case 2:
                    updateSubview(packet.result_cache.getIdx(), packet.result_cache.getMisslist());
                    //updateSubview();
                    break;
                case 3:
                    UnityEngine.Debug.Log("Here is part for alarm Cache FULL");
                    int victim_idx = -1;
                    switch (cacheinfo.policy)
                    {
                        case Policy.LRU:
                            victim_idx = ReplaceLRU();
                            UnityEngine.Debug.LogErrorFormat("victim index : {0}", victim_idx);
                            table.RemoveAt(victim_idx);
                            r.setViewlist(packet.result_cache.getMisslist());
                            addRecord(r, victim_idx);
                            record_index(packet.loc, victim_idx);
                            break;
                        case Policy.DR:
                            /*
                             * Step 1. 속도 계산하기(segment 단위)
                             * Step 2. position 계산하기 (segment 단위)
                             * Step 3. 해당 segment가 cache에 있는지 확인 후 있다면 교체순위를 낮춘다.
                             */
                            double[] prob_ = new double[cacheinfo.predict_cnt];
                            int[] idx_list = new int[cacheinfo.predict_cnt];
                            int idx_offset = 0;
                            int oldest_time = Int32.MaxValue;
                            int miss_cnt = 0;


                            
                            Point2D cur_seg_pos = new Point2D(packet.loc.get_seg_pos().seg_pos_x, packet.loc.get_seg_pos().seg_pos_y);
                            predictPos(predicted_pos, cur_seg_pos, calcVelocity(cur_seg_pos, packet));
                            for(int i = 0; i < cacheinfo.predict_cnt; i++)
                            {
                                UnityEngine.Debug.LogFormat("Predicted position : {0}, {1}", predicted_pos[i].getX(), predicted_pos[i].getY());
                            }
                            for(int i = 0; i < cacheinfo.predict_cnt; i++)
                            {
                                int idx = find_records(predicted_pos[i].getX(), predicted_pos[i].getY());
                                if(idx == -1)
                                {
                                    prob_[i] = 0.0f;
                                    miss_cnt += 1;
                                }
                                else
                                {
                                    prob_[i] = 1.0f / (Math.Pow(2.0f, i));
                                }
                                idx_list[i] = idx;
                            }
                            for(int i = 0; i < table.Count; i++)
                            {
                                if (idx_offset < cacheinfo.predict_cnt)
                                {
                                    if (i != idx_list[idx_offset])
                                    {
                                        if (oldest_time > table[i].read_time)
                                        {
                                            oldest_time = table[i].read_time;
                                            victim_idx = i;
                                        }
                                    }
                                    else
                                    {
                                        idx_offset++;
                                    }
                                }
                            }
                            UnityEngine.Debug.LogError("Victim idx : " + victim_idx);
                            reset_index(table[victim_idx].seg_x, table[victim_idx].seg_y);
                            table.RemoveAt(victim_idx);
                            r.setViewlist(packet.result_cache.getMisslist());
                            addRecord(r, victim_idx);
                            updateReadtime(victim_idx, time);
                            record_index(packet.loc, victim_idx);
                            break;
                        case Policy.GDC:
                            victim_idx = ReplaceGDC(packet.loc.get_seg_pos().seg_pos_x, packet.loc.get_seg_pos().seg_pos_y);
                            table.RemoveAt(victim_idx);
                            r.setViewlist(packet.result_cache.getMisslist());
                            //r.setViews(packet.result_cache.getMisslist(), subcontainer, iter);
                            addRecord(r, victim_idx);
                            updateReadtime(victim_idx, time);
                            record_index(packet.loc, victim_idx);
                            break;
                    }
                    break;
            }
            recordPrevPacket(packet);
        }

#region DeadReckoning
        double alpha = 0.5f;
        double prev_velo_x = 0.0f;
        double prev_velo_y = 0.0f;
        double prev_seg_velo_x = 0.0f;
        double prev_seg_velo_y = 0.0f;
        public Velocity calcVelocity(Point2D position, RequestPacket packet)
        {
            Velocity velocity = new Velocity(0.0f, 0.0f);
            double velo_x = 0.0f;
            double velo_y = 0.0f;
            if (table.Count == 0)
            {
            }
            else
            {
                velo_x = alpha * ((position.getX() - prev_Packet.loc.get_seg_pos().seg_pos_x)) + (1 - alpha) * prev_seg_velo_x;
                velo_y = alpha * ((position.getY() - prev_Packet.loc.get_seg_pos().seg_pos_y)) + (1 - alpha) * prev_seg_velo_y;
                if (prev_Packet.loc.getPath().Substring(0, 1).Equals("C"))
                {
                    velo_x = 0.0f;
                }
                else if (prev_Packet.loc.getPath().Substring(0, 1).Equals("R"))
                {
                    velo_y = 0.0f;
                }
                prev_seg_velo_x = velo_x;
                prev_seg_velo_y = velo_y;
                velocity.setVelo(velo_x, velo_y);
            }
            
            return velocity;
        }

        public Velocity calcVelocity(RequestPacket packet)
        {
            Velocity velocity = new Velocity(0.0f, 0.0f);
            double velo_x = 0.0f;
            double velo_y = 0.0f;
            if (table.Count == 0)
            {
            }
            else
            {
                velo_x = alpha * ((packet.pos.getX() - prev_Packet.pos.getX())) + (1 - alpha) * prev_velo_x;
                velo_y = alpha * ((packet.pos.getY() - prev_Packet.pos.getY())) + (1 - alpha) * prev_velo_y;
                if (prev_Packet.loc.getPath().Substring(0, 1).Equals("C"))
                {
                    velo_x = 0.0f;
                }
                else if (prev_Packet.loc.getPath().Substring(0, 1).Equals("R"))
                {
                    velo_y = 0.0f;
                }
                prev_velo_x = velo_x;
                prev_velo_y = velo_y;
                velocity.setVelo(velo_x, velo_y);
            }
            return velocity;
        }


        public Point2D estimatedP(Point2D p_k, Velocity velo, int m)
        {
            Point2D result = new Point2D(0, 0);

            int estimated_x = (int)(p_k.getX() + (m * velo.getVelo_x()));
            int estimated_y = (int)(p_k.getY() + (m * velo.getVelo_y()));

            result.setPoint(estimated_x, estimated_y);
            return result;
        }
        public Point2D[] predictPos(RequestPacket packet, Velocity velo)
        {
            Point2D[] predicted_pos = new Point2D[10];
            Point2D p_k = new Point2D(packet.pos.getX(), packet.pos.getY());

            for(int i = 0; i < 10; i++)
            {
                predicted_pos[i] = estimatedP(p_k, velo, i+1);
                //UnityEngine.Debug.LogErrorFormat("velo : {2} {0} {1} predicted", predicted_pos[i].getX(), predicted_pos[i].getY(), velo.getVelo_y());
            }
            return predicted_pos;
        }
        public void predictPos(Point2D[] predict_pos, Point2D p_k, Velocity velo)
        {
            for(int i = 0; i < cacheinfo.predict_cnt; i++)
            {
                predicted_pos[i] = estimatedP(p_k, velo, i+1);
            }
        }
        #endregion

        public void printCache()
        {
            for (int i = 0; i < table.Count; i++)
            {
                table[i].print();
            }
        }   
    }

    
}
