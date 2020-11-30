using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISESStructure;
using System.IO;

namespace ISESProfiler
{
    struct Summary
    {
        public double avg_e2e;
        public double hit_rate;
        public double miss_rate;
        public int request_cnt;
    }
    struct Detail
    {
        public RequestPacket packet;
        public string profiled_str;
        public double framedelay;
        public bool isPredict; // true일때는 server burden을 굳이 count하지 않는다. false일 때만 count하자.
        public bool isPartial; // true일때는 partial hit로 request했으니 request cnt 증가
        public Detail(RequestPacket packet, double framedelay)
        {
            this.packet = packet;
            this.framedelay = framedelay;
            isPredict = true;
            isPartial = false;
            //Region    segment position    view position   hd  esl stat
            profiled_str = 
                string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8:f3}",
                packet.loc.getPath(),
                packet.loc.get_seg_pos().seg_pos_x,
                packet.loc.get_seg_pos().seg_pos_y,
                packet.pos.getX(),
                packet.pos.getY(),
                packet.pos.getHead_dir(),
                packet.pos.getEslevel(),
                packet.result_cache.getStat(),
                framedelay
                );
        }

        public void setIspredict(bool isPredict)
        {
            this.isPredict = isPredict;
        }
    }

    struct Labelinfo
    {
        string label;
        double end2end;

        public void setLabel(string label)
        {
            this.label = label;
        }
        public void setEnd2end(double end2end)
        {
            this.end2end = end2end;
        }

        public string getString()
        {
            return string.Format("{0}\tdelay:\t{1}", label, end2end);
        }

        public double getEnd2end() { return end2end; }
    }


    class Profiler
    {
        public Summary _summary;
        public List<Detail> _detail;
        public List<List<Labelinfo>> _delay;
        public DateTime[] stopwatch;
        public Labelinfo _tempdelay;



        public Profiler()
        {
            _detail = new List<Detail>();
            _delay = new List<List<Labelinfo>>();
            stopwatch = new DateTime[2];
        }


        public void Start(string label)
        {
            _tempdelay.setLabel(label);
            stopwatch[0] = DateTime.Now;
        }

        public Labelinfo End()
        {
            stopwatch[1] = DateTime.Now;
            double e2edelay = (stopwatch[1] - stopwatch[0]).TotalMilliseconds;
            _tempdelay.setEnd2end(e2edelay);
            return _tempdelay;
        }

        public void recordLabel(List<Labelinfo> label_delay)
        {
            _delay.Add(label_delay);
        }

        public void recording(RequestPacket packet, List<Labelinfo> label_delay)
        {
            double total_delay = 0.0f;
            for (int i = 0; i < label_delay.Count; i++)
            {
                total_delay += label_delay[i].getEnd2end();
            }
            Detail _deta = new Detail(packet, total_delay);
            _detail.Add(_deta);
        }

        // deed reckoning 전용 recording
        public void recording(RequestPacket packet, List<Labelinfo> label_delay, bool isPredict, bool isPartial)
        {
            double total_delay = 0.0f;
            for (int i = 0; i < label_delay.Count; i++)
            {
                total_delay += label_delay[i].getEnd2end();
            }
            Detail _deta = new Detail(packet, total_delay);
            _deta.setIspredict(isPredict);
            _detail.Add(_deta);
        }

        public void writeSummary()
        {
            string HeadLine = "===============S U M M A R Y===============";
            string[] mystr = new string[4];
            using (StreamWriter outputFile = new StreamWriter(string.Format("LOG/summary_{0}.txt", DateTime.Now.ToString("HHmmss"))))
            {
                mystr[0] = string.Format("Average e2e delay:\t{0:f3}", _summary.avg_e2e);
                mystr[1] = string.Format("Hit rate:\t{0:f3}", _summary.hit_rate);
                mystr[2] = string.Format("Miss rate:\t{0:f3}", _summary.miss_rate);
                mystr[3] = string.Format("Request count:\t{0}", _summary.request_cnt);
                outputFile.WriteLine(HeadLine);
                for(int i = 0; i < 4; i++)
                {
                    outputFile.WriteLine(mystr[i]);
                }
            }
        }
        public void writeLabel()
        {
            string HeadLine = "================L A B E L================";
            using (StreamWriter outputFile = new StreamWriter(string.Format("LOG/label_{0}.txt", DateTime.Now.ToString("HHmmss"))))
            {
                int iter = 0;
                outputFile.WriteLine(HeadLine);
                for(int i = 0; i < _delay.Count; i++)
                {
                    for(int j = 0; j < _delay[i].Count; j++)
                    {
                        outputFile.WriteLine(iter + "\t" + _delay[i][j].getString());
                    }
                    iter++;
                }
            }

        }

        public void writeDetail()
        {
            string HeadLine = "===============D e t a i l================";
            string Attribute = "ID\tRegion\tSeg_X\tSeg_Y\tPos_X\tPos_Y\tHD\tESL\tStat\tFrame Delay";
            using (StreamWriter outputFile = new StreamWriter(string.Format("LOG/detail_{0}.txt", DateTime.Now.ToString("HHmmss"))))
            {
                int iter = 0;
                outputFile.WriteLine(HeadLine);
                outputFile.WriteLine(Attribute);

                foreach(Detail elem in _detail)
                {
                    outputFile.WriteLine(iter + "\t" + elem.profiled_str);
                    iter++;
                }
            }
        }

        public void getSummary()
        {
            int hit_cnt = 0;
            int miss_cnt = 0;
            int miss_pred_cnt = 0;
            int partial_cnt = 0;
            double total_delay = 0.0f;
            double frame_delay = 0.0f;
            for(int i = 0; i < _detail.Count; i++)
            {
                int stat = _detail[i].packet.result_cache.getStat();
                switch (stat)
                {
                    case 1:
                        hit_cnt++;
                        break;
                    case 2:
                        hit_cnt++;
                        break;
                    case 0:
                        if (_detail[i].isPredict)
                        {
                            hit_cnt++;
                            miss_cnt++;
                        }
                        else
                        {
                            miss_cnt++;
                        }
                        break;

                    case 3:
                        miss_cnt++;
                        break;
                }
                if (!_detail[i].isPredict)
                {
                    miss_pred_cnt++;
                }
                if (_detail[i].isPartial)
                {
                    partial_cnt++;
                }
            }
            int delay_cnt=0;
            for(int i = 0; i < _detail.Count; i++)
            {
                total_delay += _detail[i].framedelay;
            }

            //for(int i = 0; i < _delay.Count; i++)
            //{
            //    for (int j = 0; j < _delay[i].Count; j++)
            //    {
            //        frame_delay = _delay[i][j].getEnd2end();
            //    }
            //    total_delay += frame_delay;
            //}
            UnityEngine.Debug.LogError("Total delay_cnt : " + _delay.Count);
            

            _summary.avg_e2e = (total_delay / (double)_delay.Count);
            _summary.request_cnt = miss_cnt + miss_pred_cnt + partial_cnt;
            _summary.hit_rate = ((double)hit_cnt / (double)_detail.Count);
            _summary.miss_rate = ((double)miss_cnt / (double)_detail.Count);
        }

    }
}
