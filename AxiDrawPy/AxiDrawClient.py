#!/usr/bin/env python3
# -*- encoding: utf-8 -*-

import socket
import struct, binascii

port = 12345

def main():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect(("localhost", port))

        print("Please input value")

        x, y = 0.0, 0.0
        while 1:
            try:
                print("X:", end="")
                x=float(input())
                
                print("Y:", end="")
                y=float(input())

                send(s, (x, y))

            except EOFError:
                close(s)
                print("\nbye.")
                break

def send(sock, p):
    data = bytearray(25)
    data[0:1] = b'1'
    data[1:9] = float_to_hex(p[0])+float_to_hex(p[1])
    print(len(data))

    sock.send(data)

def close(sock):
    data = bytearray(25)
    data[0:1] = b'0'
    sock.send(data)
    sock.close()

def debugConst():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect(("localhost", port))
        
        h0 = b'0'
        h1 = b'1'
        h2 = b'2'
        p0 = b'\x3d\xcc\xcc\xcd'
        p1 = [p0 for _ in range(3)]
        p2 = b''
        for d in p1:
            p2 += d
        for i in range(5):
            a = h1 + p2 + p2
            print(a.hex())
            s.send(a)

            # 強制切断テスト（終了ヘッダを受け取らずに終了）
            if i == -1:
                break

            if i == 4:
                print("recv")
                a = h2 + p2 + p2
                s.send(a)
                d = s.recv(25)
                print(d.hex())

                print("end header")
                a = h0 + p2 + p2
                print(a.hex())
                s.send(a)
                break
        s.close()
    print("close")

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
    main()