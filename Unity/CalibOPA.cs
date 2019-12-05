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
        get {
            return axiDrawVec[0];
        }
    }
    public Vector3 axiDrawR{
        get {
            return axiDrawVec[1];
        }
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


    [Header("Optitrack → Projector")]
    public Mat4x4 opti2ProMat;

    [Header("Optitrack → AxiDraw")]
    public Mat4x4 opti2AxiMat;
    

    // Calib Opti 2 Axi
    private void CalibO2A()
    {
        // 
        if (optiP.Size() == 3)
        {



        }
    }
    #endregion

    private void InputKey()
    {
        if(Input.GetKeyDown() == KeyCode.Space)
        {
            
        }
        if(Input.GetKeyDown() == KeyCode.Return)
        {
            
        }
    }

    private void Start() 
    {

        // StartPython
        StartProcess();

        StartAxiValue();
        
    }

    private void LastUpdate()
    {
        // axiDraw更新
        RefreshAxiValue();


    }

    private void Update()
    {

    }

    public void OnApplicationQuit()
    {

    }

}