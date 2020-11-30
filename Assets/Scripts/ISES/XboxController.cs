using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using _NS_5DoFVR;

public class XboxController : MonoBehaviour
{
    #region Objects
    private GameObject main_camera;
    private GameObject minimap_player = null;
    private GameObject minimap_cam = null;
    private GameObject minimap_image = null;
    private GameObject minimap_bg = null;
    private GameObject entiremap_bg = null;
    private GameObject entiremap_image = null;
    private GameObject Front_Arrow = null;
    private GameObject Right_Arrow = null;
    private GameObject Left_Arrow = null;
    private GameObject[] Line_list;
    #endregion

    #region Vars
    public NS_5DoFVR myVR;

    public bool btn_X;
    public bool xbox_A;
    public bool xbox_Y;
    public float pos_h = 0.0f;
    public float pos_v = 0.0f;
    public float dir_h = 0.0f;
    public float dir_v = 0.0f;
    public bool switching_lock = false;
    public bool map_state = false;// false : minimap, true : entiremap 

    private float hd = 90; //head direction

    private float hor_deg = 0.0f;
    private float ver_deg = 0.0f;
    private int stride = 1;
    private int real_dir;
    private int camera_dir;

    private int componentY;
    private int componentX;

    private int cur_GLO_X;
    private int cur_GLO_Y;
    #endregion 


    #region functions
    public void GetinputData()
    {
        btn_X = Input.GetButtonDown("Button_X");
        xbox_A = Input.GetButtonDown("XboxA");
        xbox_Y = Input.GetButtonDown("XboxY");
        pos_h = Input.GetAxisRaw("Horizontal_pad");
        pos_v = Input.GetAxisRaw("Vertical_pad");
        dir_h = Input.GetAxis("myPad_X");
        dir_v = Input.GetAxis("myPad_Y");

        dir_h = (float)Math.Round(dir_h, 2);
        dir_v = (float)Math.Round(dir_v, 2);

        if (Mathf.Abs(pos_h) > Mathf.Abs(pos_v))
        {
            pos_v = 0.0f;
        }
        else
        {
            pos_h = 0.0f;
        }

    }

    public float Getspeed()
    {
        float result = 0.0f;
        //result = Mathf.Sqrt(Mathf.Pow(pos_h, 2) + Mathf.Pow(pos_v, 2));
        result = Mathf.Abs(pos_h) + Mathf.Abs(pos_v);
        return result;
    }

    public void rotateMiniCam()
    {
        Quaternion temp_qu = minimap_player.transform.localRotation;
        Vector3 miniplayer_dir = temp_qu.eulerAngles;
        miniplayer_dir.y = 270 - real_dir;
        minimap_player.transform.localRotation = Quaternion.Euler(miniplayer_dir);
    }
    public void getCurPos(ref int x, ref int y)
    {
        x = cur_GLO_X;
        y = cur_GLO_Y;
    }

    public void rotate_camera()
    {

        //Maximum degree : 30
        hor_deg = 1 * dir_h;
        ver_deg = 1 * dir_v;

        Quaternion temp_qu = main_camera.transform.localRotation;
        Vector3 main_camera_dir = temp_qu.eulerAngles;
        main_camera_dir.y = main_camera_dir.y + hor_deg;
        //UnityEngine.Debug.Log("main_camera_dir.x " + main_camera_dir.x);
        main_camera_dir.x = main_camera_dir.x + ver_deg;
        camera_dir = (int)main_camera_dir.y % 360;
        main_camera.transform.localRotation = Quaternion.Euler(main_camera_dir);
    }

    public void move_miniplayer()
    {
        Vector3 minimap_pos = minimap_player.transform.position;
        //minimap_pos.x = minimap_pos.x + (myVR.GLO_X - prev_GLO_X);
        //minimap_pos.z = minimap_pos.z + (myVR.GLO_Y - prev_GLO_Y);
        minimap_pos.x = 3300 + cur_GLO_X;
        minimap_pos.z = 2300 + cur_GLO_Y;

        //UnityEngine.Debug.Log(string.Format("{0} {1}", minimap_pos.x, minimap_pos.z));

        minimap_player.transform.position = minimap_pos;
    }

    public void calc_component()
    {
        componentY = (int)Mathf.Round((stride) * Mathf.Sin((real_dir + 90) * Mathf.Deg2Rad));
        componentX = -1 * (int)Mathf.Round((stride) * Mathf.Cos((real_dir + 90) * Mathf.Deg2Rad));
    }

    public void transform_direction()
    {
        if (0 <= camera_dir && camera_dir < 90)
        {
            //0<=theta<90 => 0<=theta<90
            real_dir = 90 - camera_dir;
        }
        else if (90 <= camera_dir && camera_dir < 180)
        {
            //90<theta<=180 => 270<=theta'<360
            real_dir = 450 - camera_dir;
        }
        else if (180 <= camera_dir && camera_dir < 270)
        {
            //180<=theta<270 => 180<= theta'<270
            real_dir = 270 + 180 - camera_dir;
        }
        else if (270 <= camera_dir && camera_dir < 360)
        {
            real_dir = -camera_dir + 450;
            // 270<=theta<360 => 90<theta'<= 180
        }
    }
    public void getHeadDirection()
    {
        if ((real_dir >= 0 && real_dir < 30) || (real_dir >= 330 && real_dir <= 360))
        {
            hd = 0;
        }
        else if ((real_dir >= 60 && real_dir < 120))
        {
            hd = 90;
        }
        else if ((real_dir >= 150 && real_dir < 210))
        {
            hd = 180;
        }
        else if ((real_dir >= 240 && real_dir < 300))
        {
            hd = 270;
        }
    }

    public void setPos(int x, int y)
    {
        cur_GLO_X = x;
        cur_GLO_Y = y;
    }
    public int getDir()
    {
        return real_dir;
    }

    public void update_posData(ref int GLO_X, ref int GLO_Y)
    {
        int move_speed = 20;

        if (pos_h > 0)
        {
            cur_GLO_Y -= (int)(componentY * Mathf.Abs(pos_h * move_speed));
            cur_GLO_X += (int)(componentX * Mathf.Abs(pos_h * move_speed));
        }
        //left
        else if (pos_h < 0)
        {
            cur_GLO_Y += (int)(componentY * Mathf.Abs(pos_h * move_speed));
            cur_GLO_X -= (int)(componentX * Mathf.Abs(pos_h * move_speed));
        }
        //front
        else if (pos_v < 0)
        {
            cur_GLO_Y += (int)(componentX * Mathf.Abs(pos_v * move_speed));
            cur_GLO_X += (int)(componentY * Mathf.Abs(pos_v * move_speed));
        }
        else if (pos_v > 0)
        {
            cur_GLO_Y -= (int)(componentX * Mathf.Abs(pos_v * move_speed));
            cur_GLO_X -= (int)(componentY * Mathf.Abs(pos_v * move_speed));
        }
        GLO_X = cur_GLO_X;
        GLO_Y = cur_GLO_Y;
    }

    public void switching_map(bool trigger)
    {

        //myDebugger.text = string.Format("{0} {1} {2}", trigger, switching_lock, map_state);
        //minimap -> entiremap
        if (trigger && !switching_lock && !map_state)
        {
            switching_lock = true;
            map_state = true;
            minimap_image.SetActive(false);
            minimap_bg.SetActive(false);
            entiremap_image.SetActive(true);
            entiremap_bg.SetActive(true);

            float x = 9.0f;
            float y = 9.0f;
            float z = 9.0f;

            float _x = 6.0f;
            float _y = 6.0f;
            float _z = 6.0f;

            minimap_player.transform.localScale += new Vector3(_x, _y, _z);
            //Lines.transform.localScale += new Vector3(x, y, z);

            for (int iter = 0; iter < Line_list.Length; iter++)
            {
                Line_list[iter].transform.localScale += new Vector3(x, y, z);
            }



        }
        //entiremap -> minimap
        else if (trigger && !switching_lock && map_state)
        {
            switching_lock = true;
            map_state = false;
            minimap_image.SetActive(true);
            minimap_bg.SetActive(true);
            entiremap_image.SetActive(false);
            entiremap_bg.SetActive(false);

            float x = -9.0f;
            float y = -9.0f;
            float z = -9.0f;

            float _x = -6.0f;
            float _y = -6.0f;
            float _z = -6.0f;

            minimap_player.transform.localScale += new Vector3(_x, _y, _z);
            //Lines.transform.localScale += new Vector3(x, y, z);

            for (int iter = 0; iter < Line_list.Length; iter++)
            {
                Line_list[iter].transform.localScale += new Vector3(x, y, z);
            }
        }

        if (!trigger && switching_lock)
        {
            switching_lock = false;
        }
    }
    #endregion

    void Start()
    {
        main_camera = GameObject.Find("Main Camera");
        minimap_player = GameObject.Find("Player");
        minimap_cam = GameObject.Find("MiniMapCam");
        minimap_image = GameObject.Find("MiniMap_image");
        entiremap_image = GameObject.Find("EntireMap_image");
        entiremap_bg = GameObject.Find("EntireBackground");
        minimap_bg = GameObject.Find("Background");
        Front_Arrow = GameObject.Find("Front_Arrow");
        Right_Arrow = GameObject.Find("Right_Arrow");
        Left_Arrow = GameObject.Find("Left_Arrow");


        Front_Arrow.SetActive(false);
        Right_Arrow.SetActive(false);
        Left_Arrow.SetActive(false);


        #region object_list
        Line_list = GameObject.FindGameObjectsWithTag("Lines");
        #endregion

        minimap_image.SetActive(true);
        minimap_bg.SetActive(true);
        entiremap_image.SetActive(false);
        entiremap_bg.SetActive(false);


        Vector3 minimap_player_pos = minimap_player.transform.position;

        minimap_player_pos.x = myVR.start_x + minimap_player_pos.x;
        minimap_player_pos.z = myVR.start_y + minimap_player_pos.z;

        minimap_player.transform.position = minimap_player_pos;
    }
    private void LateUpdate()
    {
        rotate_camera();
        rotateMiniCam();
        switching_map(btn_X);
    }
}