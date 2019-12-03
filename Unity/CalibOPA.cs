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
// Socket通信でServerと通信
//////
[Inspector]
public class AxiDrawClient
{
    // 通信でやり取りするデータ数
    const short byteL = 25;

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

        data[0] = 0x31; // b'1'
        Buffer.BlockCopy(p, 0, data,  1, 12);
        Buffer.BlockCopy(r, 0, data, 13, 12);

        socket.Send(data);

    }

    // データを受信
    public Vector3[] Recv()
    {
        byte[] data = new byte[byteL];
        data[0] = 0x32; // b'2'
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
        data[0] = 0x30; // b'0'

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
    public PythonProcess pyAxiDraw;
    public Vector3 axiDrawP;
    public Vector3 axiDrawR;
    private string axiStr;

    private void StartPyAxi()
    {
        pyAxiDraw = new PythonProcess(pyExePath, "", true);
        axiStr = pyAxiDraw.GetStandardOutput();
        paramSplitStr();
    }

    // 文字列にて出力されたパラメータを分割し、値へ
    private void paramSplitStr()
    {
        string[] axiStrSplit = axiStr.Split(":");
        string[][] eachValue = new string[axiStrSplit.Length][];
        for(int i = 0; i < axiStrSplit.Length; i++)
        {
            eachValue[i] = axiStrSplit[i].Split("-");
        }
        for(int i = 0; i < 3; i++)
        {
            axiDrawP[i] = float.Parse(eachValue[0][i]);
            axiDrawR[i] = float.Parse(eachValue[1][i]);
        }
        // LinQ デバッグしてない
        // var fl = axiStr.Split(":").Select(s => s.split("-").Select(i => float.Parse(i)));
        // for(int i = 0; i < 3; i++)
        // {
        //     axiDrawP[i] = fl[0][i];
        //     axiDrawR[i] = fl[1][i];
        // }
    }

    // Refresh AxiDraw Value
    private void RefreshAxiValue()
    {
        pyAxiDraw.StartProcess();
        axiStr = pyAxiDraw.GetStandardOutput();
        paramSplitStr();
    }
    #endregion
    #region Socket通信
    AxiDrawClient axiDrawClient;

    void StartAxiClient()
    {
        axiDrawClient = new AxiDrawClient();
    }

    #endregion
    [Space(2)]

    [Header("Optitrack → Projector")]
    public Mat4x4 opti2ProMat;

    [Header("Optitrack → AxiDraw")]
    public Mat4x4 opti2AxiMat;

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
    private void append()
    {
        optiP.append(optiP());
        optiR.append(optiR());

        Vector3[] axi = axiDrawClient.GetVec();

        axiP.append(axi[0]);
        axiR.append(axi[1]);

    }

    // Calib Opti 2 Axi
    private void StartCalibO2A()
    {
        // Opti 2 Axi
        Vector3 optiPs = getOptiP();
        Quaternion optiQs = getOptiQ();

        Vector3 axiPs = getAxiP();
        Quaternion axiQs = getAxiQ();
    }
    #endregion

    private void InputKey()
    {
        if(Input.GetKeyDown() == KeyCode.Space)
        {

        }
    }

    private void Start() 
    {

        // StartPython
        StartProcess();

        StartCalibO2A();


    }

    private void Update()
    {

    }

    public void OnApplicationQuit()
    {

    }

}