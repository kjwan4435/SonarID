import os
import socket
import socketserver
import scipy.io.wavfile as wav
import numpy as np
from numpy import correlate, array, zeros, roll, exp, pi, arange, fft, sin, cos, imag, pad, concatenate, savetxt, empty, convolve, atleast_1d, linalg, absolute

# For recording and play
import wave
import sys

# For analyzing sound signal
import math
import pandas as pd
# import seaborn as sns
import matplotlib.pyplot as plt
from matplotlib import cm

from pydub import AudioSegment
from scipy import signal
import scipy.io.wavfile as wav
import librosa
import librosa.display

fileFreq = 48000
sub_id = "9999"
timestamp = "-"
predicted = "HI"


def demodzc(y):
    carrierFreq = 20250.0
    fileFreq = 48000.0
    x = arange(len(y))
    idown = y * cos(2 * pi * carrierFreq * x / fileFreq)
    # idown = y * cos(2 * pi * -carrierFreq * x / fileFreq)
    qdown = y * (-sin(2 * pi * carrierFreq * x / fileFreq))
    nyq_rate = fileFreq / 2.0   # The Nyquist rate of the signal.
    cutoff_hz = 3500.0
    width = 1000.0
    atten_db = 40.0
    N = round((fileFreq/width) * (atten_db/22))
    taps = signal.firwin(N, cutoff_hz/nyq_rate)

    idown_lp = signal.lfilter(taps, 1.0, idown)
    qdown_lp = signal.lfilter(taps, 1.0, qdown)
    zcseqReceived = idown_lp + 1j*qdown_lp

    return np.array(zcseqReceived)


def zcsequence(u, seq_length):
    zcseq = exp((-1j * pi * u * arange(seq_length) *
                 (arange(seq_length)+1)) / seq_length)
    return zcseq


def normalize(data):
    return (data - np.min(data))/np.ptp(data)  # simple 0-1 normaliztion.


fileFreq = 48000.0
zcseq = zcsequence(63, 127)
zcfft = fft.fft(zcseq)
zcfftPadded = concatenate(
    (pad(array(zcfft[:64]), (0, 897), 'constant'), array(zcfft[64:])), axis=0)
zcseqUpscaled = fft.ifft(zcfftPadded)
zcseqUpscaled = np.array(zcseqUpscaled)


def zcseqDivider(n=4):
    result = []
    q = int(len(zcseqUpscaled)/n)
    for i in range(n):
        result.append(np.concatenate(
            (zcseqUpscaled[i*q:], zcseqUpscaled[:i*q])))
    return result


zcseqList = zcseqDivider(2)


def chunkBySize(lst, n, d):
    for x in range(0, len(lst), n):
        for y in range(d):
            each_chunk = lst[x + int(n/d)*y: n+x + int(n/d)*y]
            if len(each_chunk) < n:
                return
            yield each_chunk


# Generate sonar fingerprint using sound
def plotACSpect(fnIn):
    samplingFrequency, signalData = wav.read(fnIn)

    zcLen = 1024
    chunkedData = list(chunkBySize(signalData, zcLen, 2))
    signalParts = chunkedData[-61:-1]                       # Retain nSeqs

    # how much of the ZC signal do we want to plot? Each step is 0.72 cm.
    retain = 300                                            # try 300, which gives a coverage of ~2.1 meters.
    data = np.zeros([len(signalParts), retain])             # Retain nSamples
    # get the biggest peak in the first chunk for an offset
    deMod = demodzc(signalParts[0])                         # demodulate

    autoC = np.correlate(zcseqUpscaled, deMod, mode="same")
    autoC = np.absolute(autoC)                              # get abs value
    autoC = (autoC - np.min(autoC))/np.ptp(autoC)

    allPeaks, props = signal.find_peaks(autoC, height=0)    # all the peaks
    NPeaks = 1
    topPeak = allPeaks[np.sort(np.argpartition(
        props['peak_heights'], -NPeaks)[-NPeaks:])][0]   # the top NPeaks peaks
    offset = 512-topPeak

    for i in range(0, len(signalParts)):
        deMod = demodzc(signalParts[i])                         # demodulate
        autoC = np.correlate(zcseqList[i % 2], deMod, mode="same")
        autoC = np.absolute(autoC)                              # get abs value
        autoC = (autoC - np.min(autoC))/np.ptp(autoC)
        autoC = np.roll(autoC, offset)
        data[i] = autoC[512-int(round(retain/2)):512+int(round(retain/2))]

    data = np.array(data)
    data = np.atleast_3d(data)

    return data


class MyTCPHandler(socketserver.BaseRequestHandler):
    def handle(self):  # self.request is the TCP socket connected to the client
        global sub_id
        global timestamp

        total = 0
        chunks = []
        count = 0
        while True:
            count += 1
            self.data = self.request.recv(8192)
            total += len(self.data)
            if count == 1:
                headerPacket = self.data[0:10].decode("utf-8")
            if not self.data:
                count = 0
                break
            else:
                chunks.append(self.data)

        fullPacket = b''.join(chunks)
        header = fullPacket[0:10].decode("utf-8")
        print(header)
        print("Recd", total, len(chunks), len(fullPacket), "bytes from", self.client_address[0])

        if (header[0:5] == "SUBID"):
            sub_id = header[6:10]
            timestamp = fullPacket[10:].decode("utf-8")
            sub_dir = "sub" + sub_id + "_" + timestamp
            try:
                os.mkdir(sub_dir)
                os.chdir(sub_dir)
                print("Working directory set as %s\n" % os.getcwd())
                file_object = open(sub_id + "_" + timestamp + ".csv", 'a')
                file_object.write(
                    "index,targetPosture,targetNum,finger,startTime,downTime,endTime,correctDown,ptsDownX,ptsDownY,ptsDownT,ptsUpX,ptsUpY,ptsUpT,ptss\n")
                file_object.close()
                print("Fileheader writen")
            except:
                print("Exception: working directory remains %s\n" % os.getcwd())

        elif (header[0:5] == "BLOCK"):
            try:
                file_object = open(sub_id + "_" + timestamp + ".csv", 'a')
                txt = fullPacket[6:].decode("utf-8")
                file_object.write(txt + "\n")
                file_object.close()
            except:
                print("Data file write failed. ERROR!")

        elif (header[0:5] == "SOUND"):
            if (timestamp == "-"):  # not yet started
                print("Test sound recevied: connected.")
            else:
                id = header[6:10]
                fullPacket = fullPacket[10:]
                sz = int(len(fullPacket)/2)
                soundFile = np.empty([sz, 1], dtype=np.int16)
                for index in range(0, sz):
                    i = index * 2
                    soundFile[index][0] = (
                        fullPacket[i+1] << 8) + fullPacket[i]    # little endian

                wav.write('trial' + id + '.wav', int(fileFreq), soundFile)

                # image = plotACSpect('trial' + id + '.wav')

        # elif (header[0:5] == "RECEV"):
        #     self.request.send("HI".encode())


if __name__ == "__main__":
    print("\n-------------\n")
    path = os.getcwd()
    print("The current working directory is %s\n" % path)
    print("Starting up a TCP server - ")
    host_ip = 'localhost'
    host_port = 50005
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        host_ip = (s.getsockname()[0])

        print("IP : ", host_ip)
    except:
        print("Unable to get Hostname and IP")
    print()
    server_address = (host_ip, host_port)

    with socketserver.TCPServer(server_address, MyTCPHandler) as server:
        # Activate the server; this will keep running until you
        # interrupt the program with Ctrl-C
        server.serve_forever()
