/**
 *  the author: D2og
 *  date: 2019-03-06
 *  what it does: lens control (mimic the Unity editor)
 *  how to use it: just put the script on the camera
 *  operation method:   1. Right click and press + mouse to move so that the lens to rotate
 *                      2. Press the mouse wheel + mouse to move so that the lens to translation
 *                      3. Right mouse button + keyboard w s a d (+leftShift) so that the lens to move
 *                      4. the mouse wheel rolling so that the lens forward and backward
 *                      
 *  作用:镜头控制（模仿Unity编辑器的操作进行控制）
 *  使用方法:   将该脚本放到摄像机上即可
 *  操作方法:   1、右键按下+鼠标移动 镜头旋转
 *              2、鼠标滚轮按下+鼠标移动 镜头平移
 *              3、鼠标右键按下+键盘wsad (+leftShift) 镜头前进，后退，向左，向右（2倍速）
 *              4、鼠标滚轮滚动 镜头前进，后退                    
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOperate : MonoBehaviour
{
    [Tooltip("Mouse wheel rolling control lens please enter, the speed of the back")]
    [Range(0.5f, 2f)] public float scrollSpeed = 1f;
    [Tooltip("Right mouse button control lens X axis rotation speed")]
    [Range(0.5f, 2f)] public float rotateXSpeed = 1f;
    [Tooltip("Right mouse button control lens Y axis rotation speed")]
    [Range(0.5f, 2f)] public float rotateYSpeed = 1f;
    [Tooltip("Mouse wheel press, lens translation speed")]
    [Range(0.5f, 2f)] public float moveSpeed = 1f;
    [Tooltip("The keyboard controls how fast the camera moves")]
    [Range(0.5f, 2f)] public float keyMoveSpeed = 1f;

    //Whether the lens control operation is performed
    //是否进行镜头控制操作
    public bool operate = true;

    //Whether keyboard control lens operation is performed
    //是否进行键盘控制镜头操作
    public bool isKeyOperate = true;

    //Whether currently in rotation
    //目前是否在轮换
    private bool isRotate = false;

    //Is currently in panning
    //目前在平移吗
    private bool isMove = false;

    //Camera transform component cache
    //摄像机转换组件缓存
    private Transform m_transform;

    //The initial position of the camera at the beginning of the operation
    //摄像机在开始进行操作时摄像机初始的位置
    private Vector3 traStart;

    //The initial position of the mouse as the camera begins to operate
    //摄像机在开始进行操作时鼠标初始的位置
    private Vector3 mouseStart;

    //Is the camera facing down
    //摄像机是否镜头朝下
    private bool isDown = false;


    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (operate)
        {
            //When in the rotation state, and the right mouse button is released, then exit the rotation state
            //当处于旋转状态，且鼠标右键放开，则退出旋转状态
            if (isRotate && Input.GetMouseButtonUp(1))
            {
                isRotate = false;
            }
            //When it is in the translation state, and the mouse wheel is released, it will exit the translation state
            //当处于平移状态，且鼠标滚轮放开，则退出平移状态
            if (isMove && Input.GetMouseButtonUp(2))
            {
                isMove = false;
            }

            //Whether it's in a rotational state
            //是否处于旋转状态
            if (isRotate)
            {
                //Gets the offset of the mouse on the screen
                //获取鼠标在屏幕上的偏移量
                Vector3 offset = Input.mousePosition - mouseStart;

                // whether the lens is facing down
                //是否镜头朝下
                if (isDown)
                {
                    // the final rotation Angle = initial Angle + offset, 0.3f coefficient makes the rotation speed normal when rotateYSpeed, rotateXSpeed is 1
                    //最后的旋转角度 = 初始角度 + 偏移量，0.3f系数使得当rotateYSpeed，rotateXSpeed为1的时候，旋转速度正常
                    m_transform.rotation = Quaternion.Euler(traStart + new Vector3(offset.y * 0.3f * rotateYSpeed, -offset.x * 0.3f * rotateXSpeed, 0));
                }
                else
                {
                    // final rotation Angle = initial Angle + offset
                    //最后的旋转角度 = 初始角度 + 偏移量
                    m_transform.rotation = Quaternion.Euler(traStart + new Vector3(-offset.y * 0.3f * rotateYSpeed, offset.x * 0.3f * rotateXSpeed, 0));
                }

                // simulate the unity editor operation: right click, the keyboard can control the lens movement
                //模仿unity编辑器操作：右键按下，键盘可以控制镜头移动
                if (isKeyOperate)
                {
                    float speed = keyMoveSpeed;
                    // press LeftShift to make speed *2
                    //按下LeftShift使得速度*2
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        speed = 2f * speed;
                    }
                    // press W on the keyboard to move the camera forward
                    //键盘W键按下，镜头前进
                    if (Input.GetKey(KeyCode.W))
                    {
                        m_transform.position += m_transform.forward * Time.deltaTime * 10f * speed;
                    }
                    // press the S key on the keyboard to back up the camera
                    //键盘S键按下，镜头后退
                    if (Input.GetKey(KeyCode.S))
                    {
                        m_transform.position -= m_transform.forward * Time.deltaTime * 10f * speed;
                    }
                    // press A on the keyboard and the camera will turn left
                    //键盘A键按下，镜头向左
                    if (Input.GetKey(KeyCode.A))
                    {
                        m_transform.position -= m_transform.right * Time.deltaTime * 10f * speed;
                    }
                    // press D on the keyboard to turn the camera to the right
                    //键盘D键按下，镜头向右
                    if (Input.GetKey(KeyCode.D))
                    {
                        m_transform.position += m_transform.right * Time.deltaTime * 10f * speed;
                    }
                }
            }
            // press the right mouse button to enter the rotation state
            //鼠标右键按下，表示进入旋转状态
            else if (Input.GetMouseButtonDown(1))
            {
                // enter the rotation state
                //进入旋转状态
                isRotate = true;
                // record the initial position of the mouse in order to calculate the offset
                //记录鼠标初始位置，为了计算偏移量
                mouseStart = Input.mousePosition;
                // record the initial mouse Angle
                //记录鼠标初始角度
                traStart = m_transform.rotation.eulerAngles;
                // to determine whether the lens is facing down (the Y-axis is <0 according to the position of the object facing up),-0.0001f is a special case when x rotates 90
                //判断镜头是否朝下(根据物体朝上的位置的y轴是<0),-0.0001f是x旋转90的时候的特例
                isDown = m_transform.up.y < -0.0001f ? true : false;
            }

            // whether it is in the translation state
            //是否处于平移状态
            if (isMove)
            {
                // mouse offset on the screen
                //鼠标在屏幕上的偏移量
                Vector3 offset = Input.mousePosition - mouseStart;
                // final position = initial position + offset
                //最终位置 = 初始位置 + 偏移量
                m_transform.position = traStart + m_transform.up * -offset.y * 0.1f * moveSpeed + m_transform.right * -offset.x * 0.1f * moveSpeed;
            }
            // click the mouse wheel to enter translation mode
            //鼠标滚轮按下，进入平移模式
            else if (Input.GetMouseButtonDown(2))
            {
                // translation begins
                //平移开始
                isMove = true;
                // record the initial position of the mouse
                //记录鼠标初始位置
                mouseStart = Input.mousePosition;
                // record the initial position of the camera
                //记录摄像机初始位置
                traStart = m_transform.position;
            }

            // how much did the roller roll
            //滚轮滚动了多少
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            // scroll to scroll or not
            //滚动是否滚动
            if (scroll != 0)
            {
                // position = current position + scroll amount
                //位置 = 当前位置 + 滚动量
                m_transform.position += m_transform.forward * scroll * 1000f * Time.deltaTime * scrollSpeed;
            }


        }
    }
}


