using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using System.IO;
using NAudio;
using NAudio.Wave;
using WebSocketSharp;
public class statusMsg
{
    public int statusCode;
}

public class onTTSFinishMsg
{
    public string voicePath;
}

public class Msg
{
    public int code;
    public string content;
}

public class MsgChoices
{
    public string[] data;
}



public class python : MonoBehaviour
{
    IPAddress ip;
    Socket sSocket;
    IPEndPoint sEndPoint;
    bool hasConnect = false;
    Socket cSocket;
    Animator ani;
    string nowPlay = "hello";
    string nextPlay = "hello";
    AudioSource audioplay;
    Text label;
    GameObject label_;
    string lastVoiceText = "";
    string nowVoiceText = "1";
    string lastVoice = "";
    string nowVoice = "";
    string lastResult = "";
    string nowResult = "";
    string[] lastData = new string[5];
    string[] nowData = new string[5];
    string lastShow = "";
    string nowShow = "";
    Text[] Button_Texts = new Text[5];
    Button[] buttons = new Button[5];
    int deviceNumber = 0;
    WaveFormat recordingFormat;
    WebSocket ws;
    GameObject buttonObj;
    Button button;
    bool isRecording = false;
    GameObject go;
    WaveIn waveIn;
    CanvasGroup canvasGroup;
    float timer = 0;
    float speed = 2;//渐隐渐显的速度
    float showTime = 2;//渐隐渐显之间的显示时间
    GameObject panel_button;
    // Start is called before the first frame update
    void Start()
    {
        lastData = nowData;
        int btnPos = 200; //第一个Button的Y轴位置
        int btnHeight = 200; //Button的高度
        int btnCount = 5; //Button的数量
        go = GameObject.Find("Canvas/BtnC");
        panel_button = GameObject.Find("Canvas/Panel");
        canvasGroup =panel_button.AddComponent<CanvasGroup>();
        var rectTransform = panel_button.transform.GetComponent<RectTransform>();
        panel_button.transform.localPosition = new Vector3(0, 0 - (((btnHeight * btnCount) / 2) - (rectTransform.rect.height / 2)), 0);
        rectTransform.sizeDelta = new Vector2(rectTransform.rect.width, btnHeight * btnCount);
        for (int i = 0; i < btnCount; i++)
        {
            string text = i.ToString();
            int index = Convert.ToInt32(text);
            GameObject goClone = Instantiate(go);
            goClone.transform.parent = panel_button.transform;
            goClone.transform.localEulerAngles = new Vector3(0, 0, 0);
            goClone.transform.localScale = new Vector3(1, 1, 1);    //由于克隆的Button缩放被设置为0，所以这里要设置为1
            goClone.transform.localPosition = new Vector3(0, btnPos, 0);
            goClone.transform.Find("Text").GetComponent<Text>().text = text;
            buttons[i] = goClone.GetComponent<Button>();
            Button_Texts[i] = goClone.transform.Find("Text").GetComponent<Text>();
            //buttons[i].gameObject.SetActive(false);
            goClone.GetComponent<Button>().onClick.AddListener
            (
                () =>
                {
                    OnChoiceClick(index);    //添加按钮点击事件
                }
            );
            //下一个Button的位置等于当前减去他的高度
            btnPos = btnPos - btnHeight;
        }
        canvasGroup.alpha = 0;
        panel_button.SetActive(false);


        ani = GetComponent<Animator>();
        label_ = GameObject.Find("Canvas/Text");
        label = label_.GetComponent<Text>();
        buttonObj = GameObject.Find("Canvas/Button");
        button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
        audioplay = GetComponent<AudioSource>();
        /*
        int i = 0;
        for (i = 0; i <= 4; i++)
        {
            GameObject.Find("Canvas/Canvas/" + Convert.ToString(i)).GetComponent<Button>().onClick.AddListener(delegate() {
                
            });
        }
        */
        Debug.Log("INIT");
        StartWs();
    }

    // Update is called once per frame
    void Update()
    {
        if (nowPlay != nextPlay)
        {
            nowPlay = nextPlay;
            if(nowPlay == "think")
            {
                //ani.SetBool("isThinking", true);
            }
        }
        if (lastVoiceText != nowVoiceText)
        {
            lastVoiceText = nowVoiceText;
            label.text = lastVoiceText;
        }
        if (lastVoice != nowVoice) {
            lastVoice = nowVoice;
            button.interactable = true;
            waveIn.StopRecording();
            isRecording = false;
            StartCoroutine(LoadAudio(nowVoice));
        }
        if (lastResult != nowResult) {
            lastResult = nowResult;
            label.text = lastResult;
            waveIn.StopRecording();
            isRecording = false;
            nextPlay = "think";
        }
        if (lastShow != nowShow)
        {
            lastShow = nowShow;
            label.text = lastShow;
        }
        if(nowData != lastData)
        {
            lastData = nowData;
            int i = 0;
            for (i = 0; i <= 4; i++)
            {
                Button_Texts [i].text = lastData[i];
            }
            panel_button.SetActive(true);
            StartCoroutine(TipsAnim_show());

        }
    }

    void OnChoiceClick(int i) {
        Debug.Log(i);
    }


    void StartWs() {
        ws = new WebSocket("ws://127.0.0.1:41085");
        ws.OnMessage += OnWsMsg;
        ws.Connect();
        sendWsMessage(10001, "hello");
        hasConnect = true;
    }

    void OnWsMsg(object sender, MessageEventArgs e)
    {
        if (e.IsText)
        {
            Debug.Log(e.Data);
            Msg oMsg = JsonUtility.FromJson<Msg>(e.Data);
            switch (oMsg.code)
            {
                case 20001:
                    break;
                case 20002:
                    // choices
                    MsgChoices dataMsg = JsonUtility.FromJson<MsgChoices>(oMsg.content);
                    nowData = dataMsg.data;
                    break;
                case 20003:
                    nowVoiceText = oMsg.content;
                    break;
                case 20004:
                    nowResult = oMsg.content;        
                    break;
                case 20005:
                    nowVoice = oMsg.content;
                    break;
                case 20006:
                    nowShow = oMsg.content;
                    break;
                case 20007:
                    //choices 
                    break;
            }
        }
    }
    void OnButtonClick()
    {
        Debug.Log("1111");
        button.interactable = false;
        StartRecorder();

       //panel_button.SetActive(true);
        //StartCoroutine(TipsAnim_show());
    }
    internal bool StartRecorder()
    {
        sendWsMessage(10002, "start");
        // 设置录音格式
        recordingFormat = new WaveFormat(16000, 1);
        // 设置麦克风操作对象
        waveIn = new WaveIn();
        waveIn.DeviceNumber = deviceNumber;    // 设置使用的录音设备
        waveIn.DataAvailable += OnDataAviailable;        // 接收到音频数据时，写入文件
        waveIn.RecordingStopped += OnRecordingStopped;   // 录音结束时执行
        waveIn.WaveFormat = recordingFormat;
        isRecording = true;
        waveIn.StartRecording();

        return true;
    }

    IEnumerator TipsAnim_show()
    {
        while (canvasGroup.alpha < 1f)
        {
            
            canvasGroup.alpha += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        while (timer < showTime)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }


    }

    IEnumerator TipsAnim_hidden()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        while (timer < showTime)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }



    }


    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
       
    }


    private void OnDataAviailable(object sender, WaveInEventArgs e)
    {
        if (isRecording)
        {
            byte[] buffer = e.Buffer;
            int bytesRecorded = e.BytesRecorded;
            string base64String = Convert.ToBase64String(buffer);
            sendWsMessage(10003, base64String);
        }

    }

    public IEnumerator LoadAudio(string recordPath)
    {
        // www 加载音频
        WWW www = new WWW(recordPath);
        yield return www;
        var clipTemp = www.GetAudioClip();
        audioplay.clip = clipTemp;
        audioplay.Play();
    }
    void OnDestroy() {
        waveIn.StopRecording();
    }

    void sendWsMessage(int code, string content)
    {
        var msgObj = new Msg();
        msgObj.code = code;
        msgObj.content = content;
        string sendJson = JsonUtility.ToJson(msgObj, true);
        ws.Send(sendJson);
    }
}
