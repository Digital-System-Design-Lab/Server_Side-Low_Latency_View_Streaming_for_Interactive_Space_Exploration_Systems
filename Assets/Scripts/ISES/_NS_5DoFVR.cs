using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;


namespace _NS_5DoFVR
{
    #region Declare Struct
    public struct Range_info_Tuple
    {
        public string site_name;
        public string region_name;
        public int range_x_start;
        public int range_x_end;
        public int theta;

        public Range_info_Tuple(string site_name, string region_name, int range_x_start, int range_x_end, int theta)
        {
            this.site_name = site_name;
            this.region_name = region_name;
            this.range_x_start = range_x_start;
            this.range_x_end = range_x_end;
            this.theta = theta;
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2} ~ {3}\t{4}", site_name, region_name, range_x_start, range_x_end, theta);
        }
    }

    public struct Link_info_Tuple
    {
        public string site_name;
        public string region_name;
        public int start_x;
        public int start_y;
        public int end_x;
        public int end_y;
        public int theta;
        public int origin_x;
        public int origin_y;

        public Link_info_Tuple(string site_name, string region_name, int theta, int start_x, int end_x, int start_y, int end_y,
            int origin_x, int origin_y)
        {
            this.site_name = site_name;
            this.region_name = region_name;
            this.start_x = start_x;
            this.end_x = end_x;
            this.start_y = start_y;
            this.end_y = end_y;
            this.theta = theta;
            this.origin_x = origin_x;
            this.origin_y = origin_y;
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2} ~ {3}\t{4} ~ {5}\t{6}\t{7} ~ {8}",
                site_name, region_name, start_x, end_x, start_y, end_y, theta, origin_x, origin_y);
        }

    }

    #endregion


    public class Region_Site_information
    {
        #region member_variables
        public List<Range_info_Tuple> Range_info_Table;
        public List<Link_info_Tuple> Link_info_Table;
        #endregion
        #region User-defined Constructor
        public Region_Site_information()
        {
            Range_info_Table = new List<Range_info_Tuple>();
            Link_info_Table = new List<Link_info_Tuple>();
        }
        #endregion
        #region Parsing functions
        public void file_parsing(string region_info, string range_info, string load_info, string link_info)
        {
            // Parse datas from each file.
            parsing_range_info(range_info);
            parsing_link_info(link_info);
        }

        public void parsing_range_info(string filename)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            if (textValue.Length > 0)
            {
                for (int iter = 0; iter < textValue.Length;)
                {
                    myString = textValue[iter++].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    Range_info_Tuple temp_tuple = new Range_info_Tuple(myString[0], myString[1],
                        Int32.Parse(myString[2]), Int32.Parse(myString[3]), Int32.Parse(myString[4]));
                    Range_info_Table.Add(temp_tuple);
                }
            }
        }
        public void parsing_link_info(string filename)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            if (textValue.Length > 0)
            {
                for (int iter = 0; iter < textValue.Length;)
                {
                    Link_info_Tuple tuple;
                    myString = textValue[iter++].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    tuple.site_name = myString[0];
                    tuple.region_name = myString[1];
                    tuple.start_x = Int32.Parse(myString[2]);
                    tuple.end_x = Int32.Parse(myString[3]);
                    tuple.start_y = Int32.Parse(myString[4]);
                    tuple.end_y = Int32.Parse(myString[5]);
                    tuple.theta = Int32.Parse(myString[6]);
                    tuple.origin_x = Int32.Parse(myString[7]);
                    tuple.origin_y = Int32.Parse(myString[8]);
                    Link_info_Table.Add(tuple);
                }
            }
        }
        #endregion
    }

    public class Line_Site_information
    {
        #region member_variables
        public List<Range_info_Tuple> Range_info_Table;
        public List<Link_info_Tuple> Link_info_Table;
        #endregion

        #region User-defined Constructor
        public Line_Site_information()
        {
            Range_info_Table = new List<Range_info_Tuple>();
            Link_info_Table = new List<Link_info_Tuple>();
        }
        #endregion

        #region Parsing functions
        public void fileparsing(string range_path, string link_path)
        {
            parsing_range_info(range_path);
            parsing_link_info(link_path);
        }
        public void parsing_range_info(string filename)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            if (textValue.Length > 0)
            {
                for (int iter = 0; iter < textValue.Length;)
                {
                    myString = textValue[iter++].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    Range_info_Tuple temp_tuple = new Range_info_Tuple(myString[0], myString[1],
                        Int32.Parse(myString[2]), Int32.Parse(myString[3]), Int32.Parse(myString[4]));
                    Range_info_Table.Add(temp_tuple);
                }
            }
        }
        public void parsing_link_info(string filename)
        {
            string[] textValue = System.IO.File.ReadAllLines(filename);
            string[] myString;
            if (textValue.Length > 0)
            {
                for (int iter = 0; iter < textValue.Length;)
                {
                    Link_info_Tuple tuple;
                    myString = textValue[iter++].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    tuple.region_name = myString[0];
                    tuple.site_name = myString[0].Substring(0, 4);
                    tuple.start_x = Int32.Parse(myString[1]);
                    tuple.end_x = Int32.Parse(myString[2]);
                    tuple.start_y = Int32.Parse(myString[3]);
                    tuple.end_y = Int32.Parse(myString[4]);
                    tuple.theta = Int32.Parse(myString[5]);
                    tuple.origin_x = Int32.Parse(myString[6]);
                    tuple.origin_y = Int32.Parse(myString[7]);
                    Link_info_Table.Add(tuple);
                }

            }
        }
        #endregion
    }

    public class _5DoFVRSystem : MonoBehaviour
    {
        #region public variables
        public string[] region_path = { "Relation_info_Region.txt", "Relation_info_Line.txt" };
        public string[] range_path = { "Range_info_Region.txt", "Range_info_Line.txt" };
        public string[] Link_path = { "Link_info_Region.txt", "Link_info_Line.txt" };
        public string Load_path = "Load_info.txt";
        #endregion

        #region premitive member variables
        public int start_x;
        public int start_y;
        int GLO_X;
        int GLO_Y;
        int Pos_x;
        int Pos_y;
        int theta;
        bool Loop;
        int InKey;
        string site;
        string cur_region;
        int origin_X;
        int origin_Y;
        #endregion

        #region object member variables
        Region_Site_information Region;
        Line_Site_information Line;
        #endregion

        #region User-defined Constructor
        public _5DoFVRSystem()
        {
            GLO_X = 0;
            GLO_Y = 0;
            Pos_x = 0; Pos_y = 0;
            theta = 0; Loop = true;
            InKey = 0;
            site = null;
            cur_region = null;
            origin_X = 0;
            origin_Y = 0;

            Region = new Region_Site_information();
            Line = new Line_Site_information();
        }
        #endregion

        #region Parsing functions
        public void parsing_data()
        {
            Region.file_parsing(region_path[0], range_path[0], Load_path, Link_path[0]);
            //Line.fileparsing(range_path[1], Link_path[1]);
        }
        #endregion

        #region Classify function
        public void classify_Location(int cur_X, int cur_Y)
        {
            bool[] Region_Line = new bool[2];


            //Region에 위치하는지 확인
            for (int outter_index = 0; outter_index < Region.Link_info_Table.Count; outter_index++)
            {
                if ((Region.Link_info_Table[outter_index].start_x <= cur_X && Region.Link_info_Table[outter_index].end_x >= cur_X)
                    && (Region.Link_info_Table[outter_index].start_y <= cur_Y && Region.Link_info_Table[outter_index].end_y >= cur_Y))
                {
                    site = Region.Link_info_Table[outter_index].site_name;
                    cur_region = Region.Link_info_Table[outter_index].region_name;
                    theta = Region.Link_info_Table[outter_index].theta;
                    origin_X = Region.Link_info_Table[outter_index].origin_x;
                    origin_Y = Region.Link_info_Table[outter_index].origin_y;
                    Region_Line[0] = true;
                }
            }
            //Line에 위치하는지 확인

            for (int outter_index = 0; outter_index < Line.Link_info_Table.Count; outter_index++)
            {
                if ((Line.Link_info_Table[outter_index].start_x <= cur_X && Line.Link_info_Table[outter_index].end_x >= cur_X)
                    && (Line.Link_info_Table[outter_index].start_y <= cur_Y && Line.Link_info_Table[outter_index].end_y >= cur_Y))
                {
                    site = Line.Link_info_Table[outter_index].site_name;
                    cur_region = Line.Link_info_Table[outter_index].region_name;
                    theta = Line.Link_info_Table[outter_index].theta;
                    origin_X = Line.Link_info_Table[outter_index].origin_x;
                    origin_Y = Line.Link_info_Table[outter_index].origin_y;
                    Region_Line[1] = true;
                }
            }

            if (!Region_Line[0] && !Region_Line[1])
            {
                site = "Out of Region";
                cur_region = "NONE";
                Pos_x = -1;
                Pos_y = -1;
            }
            else
            {
                //int temp_pos_X = cur_X - origin_X;
                //int temp_pos_Y = cur_Y - origin_Y;
                //double temp = Math.PI * theta / 180.0;
                //Pos_x = (int)(Math.Cos(temp) * temp_pos_X) + (int)(Math.Sin(temp) * temp_pos_Y);
                //Pos_y = (int)(-1 * Math.Sin(temp) * temp_pos_X) + (int)(Math.Cos(temp) * temp_pos_Y);
                Pos_x = cur_X - origin_X;
                Pos_y = cur_Y - origin_Y;
            }

        }
        #endregion

        #region Set position info
        public void setPosData(int GLO_X, int GLO_Y)
        {
            this.GLO_X = GLO_X;
            this.GLO_Y = GLO_Y;
        }
        #endregion

        #region Get position info
        public int getPos_X() { return this.Pos_x; }
        public int getPos_Y() { return this.Pos_y; }
        public int getGLO_X() { return GLO_X; }
        public int getGLO_Y() { return GLO_Y; }
        public int getOrigin_X() { return origin_X; }
        public int getOrigin_Y() { return origin_Y; }
        public string getsite() { return site; }
        public string getCurregion() { return cur_region; }
        public int getTheta() { return theta; }
        #endregion

        #region NS_Rendering
        public void NS_Rendering(ref byte[] imageBytes, ref Texture2D sendTexture)
        {
            string file_name = null;
            string directory_name = "Assets/Resources/" + site + "/" + cur_region + "/";
            if (Pos_x < 9)
                file_name = "000" + (Pos_x + 1).ToString() + ".jpg";
            else if (Pos_x < 99)
                file_name = "00" + (Pos_x + 1).ToString() + ".jpg";
            else if (Pos_x < 999)
                file_name = "0" + (Pos_x + 1).ToString() + ".jpg";
            string path = directory_name + file_name;

            UnityEngine.Debug.Log(site + " / " + cur_region + " / " + " ( " + Pos_x + ", " + Pos_y + ")");

            imageBytes = File.ReadAllBytes(path);
            sendTexture = new Texture2D(2, 2);
            sendTexture.LoadImage(imageBytes);
        }
        #endregion
    }
}
