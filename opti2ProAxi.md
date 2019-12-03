<h1>Update to git</h1>
[Link](https://github.com/meto4d/opti2ProAxi)

# 問題

- 画像生成(Unity)
- アクチュエータ制御(Python-CUI)

# 要素

- 実物体
    - AIプレートの上
    - 自由移動
    - センサ（OptiTrack）にて位置姿勢取得可能
        - 精度高い

- 仮想物体
    - AIプレートの下
    - アクチュエータ（AxiDraw）で位置姿勢が制御される
    - プロジェクタで投影される

- プロジェクタ
    - 仮想物体を照らす
    - 位置姿勢半固定（初期位置のみ移動）
    - 位置姿勢の取得方法未定

# シーケンス

0. 初期位置あわせ
    1. 実物体の位置姿勢取得（Opti）
    2. 仮想物体の位置姿勢取得（Axi）
        - OptiとAxiのキャリブレーション（必要？）
    3. プロジェクタの位置姿勢取得
    4. 仮想物体とプロジェクタのキャリブレーション
        - 透視投影変換行列?

1. Opti→実物体の位置姿勢取得

2. 座標変換：実物体→仮想物体

    2.1. 仮想物体の位置移動

    2.2. 座標変換：仮想物体→プロジェクタ座標

3. レンダリング

4. goto 1.

# ソース

- OptitrackStreamingClient.cs  
    - Unity
    - Optitrack

- python_example_xy.py  
    - Python(CUI)
    - AxiDraw3D
    - pyaxidraw/axidraw.py : class core

# 実装イメージ

- AxiDraw Core  
python  
AxiDrawServer.py  
    - socket server
    - recv
        - 3D+3R?3D+4Q?
        - 3D+3R

- Calibration [Optitrack + Projector + AxiDraw]  
Unity  
CalibOPA.cs  
    - member  
        - port : int
        - conv Opti 2 Pro mat : Mat4x4
        - conv Opti 2 Axi mat : Mat4x4
    - Start  
        run AxiDrawServer.py
    - OnQuit  
        stop AxiDrawServer.py

- Projection  
Unity  
DynamicProjection.cs  
    - Start
        ref CalibOPA.cs for Value
    - Update  
        モロモロ


---
### 原文 

> 主に投影用の画像生成の問題と，アクチュエータ制御（Python）の問題があります  
> 投影画像のずれ（ProCamシステム導入）関連で...  
> Unity内の  
> カメラの位置→現実のプロジェクタの位置  
> bunnyの3Dmodelの位置→実物体のbunny（ダミー物体側）の位置  
> を対応させたい  
> Optitrackからストリーミングしている剛体の位置，移動距離（左右と奥行き方向の並進移動）も投影したときに実物体のバニーの移動に合わせて動くようにしたい．  
> 総じて，プロジェクタから動くバニーの絵を投影して，実物体のバニーを動かしたときにマッピングさせた状態を保ちたい．  
>   
> アクチュエータ`<AxiDrawV3>`の駆動関連で...  
> Pythonを実行したい．  
> やったこと  
> 　⇒IronPythonの導入（C#⇔Pythonのスコープ関連で躓く）  
> 	→IronPythonで実装する必要は現状なさそうです．  
> 　⇒Python3.8.0ダウンロード  
> 　⇒OptitrackのPythonAPIを見てみる，開いたままの状態でエラー発生中（APIファイル送ります）  
> 　⇒そもそもPythonで何か作ったりしたこと自体無いので初歩的なプログラムの動かし方から学ぶ必要  
>  
> したいこと  
> 投影画像の動き…  
> Optitrack(Motive)  
> ↓  
> (UnityへストリーミングできるAssets,API)  
> ↓  
> Unity側の3Dmodelが動く  
> アクチュエータの動き…  
> Optitrack(Motive)  
> ↓  
> (OptitrackのPythonAPI,剛体のXYZ座標を取得できる？)  
> ↓  
> (アクチュエータのPythonAPI,座標の位置を入れ続ける,加速度も指定)  
> ↓  
> アクチュエータが動く  