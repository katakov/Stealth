#!/usr/bin/env python3
# -*- encoding: utf-8 -*-

######
## Unityの初期座標合わせに用いるスクリプト
######

import sys
import AxiDraw

ad = AxiDraw()


# GetP()
#  現在の位置を取得関数（仮）
#  return (x, y, z)
def GetP():
    x = y = z = 0
    return (x, y, z)

# GetR()
#  現在の姿勢を取得関数（仮）
#  return (x, y, z)
def GetR():
    x = y = z = 0
    return (x, y, z)

# 標準ストリームで表示させ、文字列としてUnityでキャプチャ
def PrintAxiValue():
    print("{0[0]}-{0[1]}-{0[2]}:{1[0]}-{1[1]}-{1[2]}".format(GetP(), GetR()))


if __name__ == "__main__":
    PrintAxiValue()