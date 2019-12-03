#!/usr/bin/env python3
# -*- encoding: utf-8 -*-

import sys
import struct, binascii
import socket


class AxiServer():
    def __init__(self, port=12345, host="localhost"):
        self.port = int(port)
        print(port)
        self.host = host
        self.daemon = True

    def run(self):
        # ソケット通信作成
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.bind((self.host, self.port))
            # 接続待ち開始
            s.listen()
            while self.daemon:
                try:
                    print("wait..")
                    conn, addr = s.accept()
                except:
                    break
                
                # 接続がきた時
                with conn:
                    while self.daemon:
                        data = conn.recv(25)
                        if not data: break
                        # データ整形
                        # header, position, rotation
                        # b'0', byte-float x3, byte-float x3 
                        # 1 byte 4byte x3 4byte x3

                        # Headerチェック
                        header = data[0:1]
                        print("header {}".format(header))
                        if header == b'0': 
                            # 終了
                            self.daemon = False
                            conn.close()
                            s.close()
                            break
                        
                        elif header == b'1':
                            # AxiDraw動作
                            self.dataRev(s, data[1:25])

                        elif header == b'2':
                            # 座標送信
                            self.dataSend(conn)
                            #break

                # 本当は接続が切れても再接続待ちをした方がいいと思うが、念の為
                # self.daemon = False
            s.close()

    # データ受信後
    def dataRev(self, s, data):
        p = data[0:12] 
        r = data[12:24]
        position = [p[x*4:x*4 + 4] for x in range(3)]
        rotation = [r[x*4:x*4 + 4] for x in range(3)]
        # p: [x, y, z] , r: [x, y, z]

        p = [hex_to_float(x.hex()) for x in position]
        r = [hex_to_float(x.hex()) for x in rotation]
        print(p)
        print(r)

        ##########
        # ここにAxiDrawの移動を書く
        ##########

    def dataSend(self, conn):
        ##########
        # ここにAxiDrawの位置姿勢取得を書く
        # getP() # 位置
        # getR() # 回転
        ##########
        data = b'2'
        d = 0.2
        print(joinData((d, d, d)) )
        data += joinData((d, d, d)) #joinData(getP()) #
        data += joinData((d, d, d)) #joinData(getR()) #
        conn.sendall(data)

# タプルで受け取った3値をbyte文字列にして、結合
def joinData(tupleData):
    return float_to_hex(tupleData[0]) + float_to_hex(tupleData[1]) + float_to_hex(tupleData[2])


###
# 4byte → float
# ref: https://note.nkmk.me/python-float-hex/
###
def hex_to_float(s):
    if s.startswith('0x'):
        s = s[2:]
    s = s.replace(' ', '')
    return struct.unpack('>f', binascii.unhexlify(s))[0]

def float_to_hex(f):
    if(f == 0):
        return bytes(4)
    return bytes.fromhex(hex(struct.unpack('>I', struct.pack('>f', f))[0])[2:])

if __name__ == "__main__":
    # 引数がない時はポート番号12345、何かある時はその番号で待ち受け
    axi = AxiServer(port=12345 if len(sys.argv) < 2 else sys.argv[1])
    axi.run()
