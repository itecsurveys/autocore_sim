﻿#region License
/*
* Copyright 2018 AutoCore
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using AutoCore.Sim.Autoware.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class LunchUI : MonoBehaviour
    {
        public static LunchUI Instance { get; private set; }
        public Button btn_testConnect;
        public InputField inputField_RosMasterIP;
        public InputField inputField_Port;
        public InputField inputField_rosIP;
        public Text text_tip;
        public Text text_noticeRosIP;
        public Text text_noticeLocalIP;
        public Text text_noticePort;
        public Transform groupMode;
        public GameObject objModeToggle;
        public Sprite nullSprte;
        Uri uriRos;
        IPAddress iPAddressLocal;
        int port;
        public GameObject PanelURL;

        bool isEnterSimu = false;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Instance = this;
        }
        private void Update()
        {
            switch (connectStatus)
            {
                case ConnectStatus.None:
                    text_tip.text = "";
                    break;
                case ConnectStatus.Connecting:
                    text_tip.text = "Connecting----";
                    break;
                case ConnectStatus.Success:
                    text_tip.text = "Connect Success";
                    break;
                case ConnectStatus.failed:
                    text_tip.text = "Connect Failed";
                    break;
            }
            if (isEnterSimu)
            {
                EnterSimu(false);
            }
        }

        // Start is called before the first frame update
        Thread thread;
        void Start()
        {
            connectStatus = ConnectStatus.None;
            TestConfig.LoadAllData();
            SetModeGroup();
            LoadUrl();
            PanelURL.SetActive(!ROS_Config.ROS2);
            btn_testConnect.onClick.AddListener(() => { TestConnect(); });
            inputField_RosMasterIP?.onEndEdit.AddListener((string value) =>
            {
                if (Uri.TryCreate(value, UriKind.Absolute, out uriRos))
                {
                    text_noticeRosIP.text = "OK";
                    text_noticeRosIP.color = Color.green;
                    SetIPConfig();
                }
                else
                {
                    text_noticeRosIP.text = "Address is not legitimate";
                    text_noticeRosIP.color = Color.red;
                }
            });
            inputField_Port?.onEndEdit.AddListener((string value) =>
            {
                if (int.TryParse(value, out int porttemp) && porttemp < 65536 && porttemp > 0)
                {
                    port = porttemp;
                    text_noticePort.text = "ok";
                    text_noticePort.color = Color.green;
                    SetIPConfig();
                }
                else
                {
                    text_noticePort.text = "port is not legitimate";
                    text_noticePort.color = Color.red;
                }
            });
            inputField_rosIP?.onEndEdit.AddListener((string value) =>
            {
                string[] values = value.Split('.');
                if (values.Length == 4)
                {
                    if (IPAddress.TryParse(value, out IPAddress iPAddress))
                    {
                        if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            iPAddressLocal = iPAddress;
                            text_noticeLocalIP.text = "OK";
                            text_noticeLocalIP.color = Color.green;
                            SetIPConfig();
                            return;
                        }
                    }
                }
                text_noticeLocalIP.text = "Address is not legitimate";
                text_noticeLocalIP.color = Color.red;
            });
        }

        private void OnDestroy()
        {
            if (thread != null && thread.IsAlive)
                thread.Abort();
        }

        public void TestConnect(SimuTestMode mode = null)
        {
            TestConfig.TestMode = mode;
            isEnter = mode != null;
            if (uriRos == null && !Uri.TryCreate(inputField_RosMasterIP.text, UriKind.Absolute, out uriRos))
            {
                text_noticeLocalIP.text = "Address is not legitimate";
                text_noticeLocalIP.color = Color.red;
                return;
            }
            thread = new Thread(new ThreadStart(RunConnect));
            thread.Start();
        }

        enum ConnectStatus
        {
            None,
            Connecting,
            Success,
            failed
        }
        private ConnectStatus connectStatus;
        bool isEnter = false;
        private void RunConnect()
        {
            connectStatus = ConnectStatus.Connecting;
            Task.Run(() => ROS_Node.CheckRos1Master(IPAddress.Parse(uriRos.Host), (ushort)uriRos.Port, 2))
                .ContinueWith(ret =>
                {
                    if (ret.Result == true)
                    {
                        connectStatus = ConnectStatus.Success;
                        if (isEnter)
                        {
                            isEnterSimu = true;
                        }
                    }
                    else
                    {
                        connectStatus = ConnectStatus.failed;
                    }
                });
        }
        public void EnterSimu(bool isEdit)
        {
            TestConfig.isEditMode = isEdit;
            string sceneName = TestConfig.TestMode.MapName + (isEdit ? "is" : "");
            SceneManager.LoadScene(sceneName);
        }


        private void SetModeGroup()
        {
            for (int i = 0; i < groupMode.transform.childCount; i++)
            {
                Destroy(groupMode.transform.GetChild(i).gameObject);
            }
            for (int i = 0; i < TestConfig.TestModes.Count; i++)
            {
                SimuTestMode mode = TestConfig.TestModes[i];
                GameObject ModeObj = Instantiate(objModeToggle, groupMode.transform);
                ModeObj.GetComponent<ModeButton>().SetModeButton(mode.TestModeName, mode.MapName, mode.LastTime, mode);
            }
        }

        void LoadUrl()
        {
            inputField_RosMasterIP.text = ROS_Node.Config.ros_master_uri;
            inputField_rosIP.text = ROS_Node.Config.ros_ip;
        }
        void SetIPConfig()
        {
            if (uriRos != null) ROS_Node.Config.ros_master_uri = uriRos.ToString();
            if (iPAddressLocal != null) ROS_Node.Config.ros_ip = iPAddressLocal.ToString();
            string content = JsonUtility.ToJson(ROS_Node.Config);
            TestDataManager.Instance.WriteByLineCover(Application.streamingAssetsPath + "\\ROS_Config.json", content);
        }
    }
}