using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;


//////
// Pythonを実行する
// ref: https://tofgame.hatenablog.com/entry/2019/04/30/011221
//////
[Inspector]
public class PythonProcess
{
    #region インスペクタに表示させるパラメータ
    [Header("実行したいスクリプトがある場所")]
    public string pyCodePath = @"(実行したいスクリプトの場所)\AxiDraw****.py";

    [Header("標準出力文字列")]
    public string  coutStr = "";
    #endregion

    //pythonがある場所
    private string pyExePath = @"(Pythonの実行ファイルが置いてある場所)\python.exe";

    //外部プロセスの設定
    private ProcessStartInfo processStartInfo;

    // プロセス
    private Process process;

    // 引数
    string argv = "";

    // 標準出力ストリームを利用するかどうか
    bool coutFlag = false;

    // プロセスを実行する
    public void StartProcess()
    {
        //外部プロセスの開始
        process = Process.Start(processStartInfo);

        if(coutFlag)
        {
            //ストリームから出力を得る
            StreamReader streamReader = process.StandardOutput;
            coutStr = ""; // 念のための文字列リセット
            coutStr = streamReader.ReadLine();
        }
    }

    // プロセスの終了を待つ
    // async awaitで待つのはなんかすごい面倒らしい
    public void WaitForExit()
    {
        if(process != null)
        {
            //外部プロセスの終了
            process.WaitForExit();
            process.Close();
        }
    }

    // プロセスのオプションを設定する
    public void SetCoutFlag(bool inCountFlag = false)
    {
        processStartInfo.RedirectStandardOutput = inCountFlag;
    }
    public void SetArgv(string inArgv)
    {
        processStartInfo.Arguments = pyCodePath + " " + inArgv;
    }

    // 標準出力を取得
    public string GetStandardOutput()
    {
        if(!coutFlag) return "";

        WaitForExit();
        return coutStr;
    }


    public PythonProcess(string pyexe, string inArgv = "", bool inCoutFlag = false)
    {
        // パラメータ設定
        pyExePath = pyexe;
        argv = inArgv;
        coutFlag = inCoutFlag;

        processStartInfo = new ProcessStartInfo() {
            FileName = pyExePath, //実行するファイル(python)
            UseShellExecute = false,//シェルを使うかどうか
            CreateNoWindow = true, //ウィンドウを開くかどうか
            RedirectStandardOutput = coutFlag, //テキスト出力をStandardOutputストリームに書き込むかどうか
            Arguments = pyCodePath + " " + argv, //実行するスクリプト 引数(複数可)
        };

        StartProcess();
    }

    public ~PythonProcess()
    {
        if(coutFlag)
        {
            WaitForExit();
        }
    }
}
//////
// PythonでServerと通信
//////
[Inspector]
public class PyAxiDraw : PythonProcess
{

    private string axiStr;

    public void refresh()
    {
        StartProcess();
        axiStr = GetStandardOutput();
    }

    // 文字列にて出力されたパラメータを分割し、値へ
    public Vector3[] paramSplitStr()
    {
        refresh();
        Vector3[] vec = new Vector3[2];
        string[] axiStrSplit = axiStr.Split(":");
        string[][] eachValue = new string[axiStrSplit.Length][];
        for(int i = 0; i < axiStrSplit.Length; i++)
        {
            eachValue[i] = axiStrSplit[i].Split("-");
        }
        for(int i = 0; i < 3; i++)
        {
            vec[0][i] = float.Parse(eachValue[0][i]);
            vec[1][i] = float.Parse(eachValue[1][i]);
        }
        // LinQ デバッグしてない
        // var fl = axiStr.Split(":").Select(s => s.split("-").Select(i => float.Parse(i)));
        // for(int i = 0; i < 3; i++)
        // {
        //     axiDrawP[i] = fl[0][i];
        //     axiDrawR[i] = fl[1][i];
        // }

        return vec;
    }

    public PyAxiDraw(string pyExePath) : base(pyExePath, "", true)
    {
        //axiStr = GetStandardOutput();
    }

}
//////
// Socket通信でServerと通信
//////
[Inspector]
public class AxiDrawClient
{
    // 通信でやり取りするデータ数
    const short byteL = 25;

    // Header
    public enum AxiHeader{
        stop = (char)'0', // b'0'
        send = (char)'1', // b'1'
        get  = (char)'2', // b'2'
    }

    // 接続先
    public string hostname = "localhost";

    // ポート番号
    public int port = 12345;

    // Socket通信周り
    private IPEndPoint ipe;
    private Socket socket;

    // ソケットを作成して、接続
    private Socket Connect()
    {
        
        Socket tempSocket = 
            new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        tempSocket.Connect(ipe);

        if(tempSocket.Connected)
        {
            return tempSocket;
        }

        return null;
    }

    // データ送信
    public void Send(Vector3 p, Vector3 r)
    {
        byte[] data = new byte[byteL];

        data[0] = AxiHeader.send;
        Buffer.BlockCopy(p, 0, data,  1, 12);
        Buffer.BlockCopy(r, 0, data, 13, 12);

        socket.Send(data);

    }

    // データを受信
    public Vector3[] Recv()
    {
        byte[] data = new byte[byteL];
        data[0] = AxiHeader.get; // b'2'
        socket.Send(data);

        socket.Receive(data);

        Vector3[] vec = new Vector3[2];


        Buffer.BlockCopy(data,  1, vec[0],  0, 12);
        Buffer.BlockCopy(data, 13, vec[1],  0, 12);

        return vec;
    }
    // 関数名をわかりやすくするための関数
    public Vector3[] GetVec() { return Recv(); }

    // 接続を終了する
    public void Close()
    {
        byte[] data = new byte[byteL];
        data[0] = AxiHeader.stop; // b'0'

        socket.Send(data);
    }
    
    // Vectorデータをbyte配列に変換
    private byte[] vec2bytes(Vector vec)
    {
        byte[] data = new byte[3 * 4]; // sizeof(float) * 3
        Buffer.BlockCopy(vec, 0, data, 0, data.Length);
        
        return data;
    }

    public AxiDrawClient()
    {
        ipe = new IPEndPoint(hostname, port);
        socket = Connect();

        if(socket == null)
        {
            Debug.Log("socket null!");
        }
    }

    public ~AxiDrawClient()
    {
        Close();
        socket.Dispose();
    }
}

public class CalibOPA : MonoBehavior
{
    //pythonがある場所
    public string pyExePath = @"(Pythonの実行ファイルが置いてある場所)\python.exe";

    // swich how to access AxiDraw by python or socket
    public enum switchAccessAxiDraw
    {
        virtua = -1,
        socket = 0,
        python = 1

    }
    public switchAccessAxiDraw switchAxiDraw = switchAccessAxiDraw.socket;

    [Header("ローカルで保持するAxiDrawの位置姿勢")]
    public Vector3 localAxiDrawP;
    public Vector3 localAxiDrawR;

    private Vector3[] axiDrawVec = new Vector3[2];
    public Vector3 axiDrawP{
        set {
            axiDrawVec[0] = value;
        }
        get {
            return axiDrawVec[0];
        }
    }
    public Vector3 axiDrawR{
        set {
            axiDrawVec[1] = value;
        }
        get {
            return axiDrawVec[1];
        }
    }

    // AxiDrawを動かす関数
    public void moveAxi(Vector3? p = null, Vector3? r = null)
    {
        if(p != null) localAxiDrawP = p;
        if(r != null) localAxiDrawR = r;
        
        axiDrawClient.Send(localAxiDrawP, localAxiDrawR);
    }

    #region PythonServer実行

    [Header("AxiDrawへ座標を送信するためのポート番号"), Range(1000, 65535)]
    public int port = 12345;

    // Pythonで作られたサーバスクリプト管理インスタンス
    public PythonProcess pyServer;

    // Serverスクリプトを実行
    private void StartPyServer()
    {
        pyServer = new PythonProcess(pyExePath, port.ToString());
    }
    
    #endregion
    #region AxiDrawの現在地を取得 by Python with StandardOutput
    public PyAxiDraw pyAxiDraw;

    void StartPyAxi()
    {
        PyAxiDraw = new PyAxiDraw(pyExePath);
    }
    #endregion
    #region Socket通信
    AxiDrawClient axiDrawClient;

    void StartAxiClient()
    {
        axiDrawClient = new AxiDrawClient();
    }

    #endregion

    private void StartAxiValue()
    {
        StartPyAxi();
        StartAxiClient();
    }

    // Refresh AxiDraw Value
    private void RefreshAxiValue()
    {
        switch(switchAxiDraw)
        {
        case switchAccessAxiDraw.virtua:
            axiDrawVec[0] = localAxiDrawP;
            axiDrawVec[1] = localAxiDrawR;
            break;
        case switchAccessAxiDraw.socket:
            axiDrawVec = AxiDrawClient.GetVec();
            break;
        case switchAccessAxiDraw.python:
            axiDrawVec = pyAxiDraw.paramSplitStr();
            break;

        }

    }

    [Space(2)]


//////////////////////////////////////////
/// OptiTrack AxiDraw の位置姿勢取得
//////////////////////////////////////////
    #region OptiTrack 位置姿勢取得プログラム
    // 位置
    private Vector3 getOptiP()
    {

        return new Vector3();
    }
    // 姿勢
    private Vector3 getOptiR()
    {

        return new Vector3();
    }
    #endregion
    #region AxiDrawit 位置姿勢取得プログラム
    // 位置
    private Vector3 getAxiP()
    {

        return axiDrawP;
    }
    // 姿勢
    private Vector3 getAxiQ()
    {

        return axiDrawR;
    }
    #endregion

    [Space(5)]
    #region キャリブレーション用パラメータ

    // 本当はこっちでやりたい
    //public List<Transform> optiT;
    //public List<Transform> optiT;

    public List<Vector3> optiP;
    public List<Vector3> optiR;

    public List<Vector3> axiP;
    public List<Vector3> axiR;

    // 座標追加
    private void appendOpti()
    {
        optiP.append(getOptiP());
        optiR.append(getOptiR());
    }
    private void appendAxi()
    {
        axiP.append(getAxiP());
        axiR.append(getAxiQ());
    }
    private void append()
    {
        appendOpti();
        appendAxi();
    }

    private void calcO2A()
    {
        Matrix4x4 tmpP = Matrix4x4.identity;

        tmpP[0, 3] = optiP[0].x;
        tmpP[1, 3] = optiP[0].y;
        tmpP[2, 3] = optiP[0].z;

        float tmp0 = axiP[1].y * axiP[2].x - axiP[1].x * axiP[2].y;
        float tmp1 = axiP[1].x - axiP[2].x;
        float tmp2 = axiP[1].y - axiP[2].y;

        float htmp1 = optiP[1].x * axiP[2].y - optiP[2].x * axiP[1].y;
        float htmp2 = optiP[1].x * axiP[2].x - optiP[2].x * axiP[1].x;
        float htmp3 = optiP[1].y * axiP[2].y - optiP[2].y * axiP[1].y;
        float htmp4 = optiP[1].y * axiP[2].x - optiP[2].y * axiP[1].x;
        
        tmpP[0, 0] = htmp1 + axiP[0].x * tmp2;
        tmpP[0, 1] = htmp2 + axiP[0].x * tmp1;
        tmpP[1, 0] = htmp1 + axiP[0].y * tmp2;
        tmpP[1, 1] = htmp2 + axiP[0].y * tmp1;
        
        tmpP[0, 0] /= tmp0;
        tmpP[0, 1] /= tmp0;
        tmpP[1, 0] /= tmp0;
        tmpP[1, 1] /= tmp0;

        opti2AxiMat = tmpP.inverse;
    }


    [Header("Optitrack → Projector")]
    public Matrix4x4 opti2ProMat;

    [Header("Optitrack → AxiDraw")]
    public Matrix4x4 opti2AxiMat;
    
    [Header("Apply opti → Axi")]
    public bool opti2AxiFlag;

    public enum CalibStats : int
    {
        start = 0,
        move00,
        getOpti00,
        move01,
        getOpti01,
        move10,
        getOpti10,
        end
    }

    public CalibStats calibStats = CalibStats.start;

    // Calib Opti 2 Axi
    private void CalibO2A()
    {
        switch(calibStats)
        {
        case CalibStats.start:
            Debug.Log("Plz, set Axi & Opti");
            calibStats++;
            break;
        case CalibStats.move00:
            moveAxi(new Vector3(0, 0, 0));
            calibStats++;
            break;
        case CalibStats.getOpti00:
            append();
            calibStats++;
            break;
        case CalibStats.move01:
            moveAxi(new Vector3(0, 1, 0));
            calibStats++;
            break;
        case CalibStats.getOpti01:
            append();
            calibStats++;
            break;
        case CalibStats.move10:
            moveAxi(new Vector3(1, 0, 0));
            calibStats++;
            break;
        case CalibStats.getOpti10:
            append();
            calibStats++;
            calcO2A();
            endCalibAxi = true;
            break;
            
        case CalibStats.end:
            break;
            
        default:
            break;
        }
    }

    public void ApplyMat()
    {
        

        if(calibStats == CalibStats.end && opti2AxiFlag)
        {
            moveAxi(opti2ProMat.MultiplyPoint(getOptiP()));

        }
    }
    #endregion

    #region model

    public GameObject modelObj;

    public bool moveModelFlag;

    private bool endCalibAxi;

    private void ApplyAxi2Model()
    {
        if(moveModelFlag && endCalibAxi)
        {
            modelObj.transform.localPosition = getAxiP;
        }
    }

    #endregion

    private void InputKey()
    {
        if(Input.GetKeyDown() == KeyCode.Space)
        {
            if (calibStats % 2 == 0)
            {
                CalibO2A();
            }
        }
        if(Input.GetKeyDown() == KeyCode.Return)
        {
            if (calibStats % 2 == 1)
            {
                CalibO2A();
            }
        }
        if(InputKey.GetKeyDown() == KeyCode.Esc)
        {
            if(calibStats == CalibStats.start)
            {
                calibStats = CalibStats.end;
            }
        }
    }

    private void Start() 
    {

        // StartPython
        StartProcess();

        StartAxiValue();
        
    }

    private void FixedUpdate()
    {
        // axiDraw更新
        RefreshAxiValue();


    }

    private void Update()
    {
        InputKey();

        ApplyMat();
    }

    public void OnApplicationQuit()
    {

    }

}