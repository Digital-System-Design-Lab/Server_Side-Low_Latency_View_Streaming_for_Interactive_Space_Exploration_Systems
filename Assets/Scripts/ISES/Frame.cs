using UnityEngine;
using System.Collections;

public class Frame : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 100;
    }
}