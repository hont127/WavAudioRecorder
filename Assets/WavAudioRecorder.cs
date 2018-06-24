using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavAudioRecorder : MonoBehaviour
{
    FileStream mFileStream;
    BinaryWriter mBinaryWriter;
    short[] mCacheSamplesOutput;


    void OnEnable()
    {
        var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Record");

        try
        {
            var filePath = Path.Combine(saveDirectory, "wav1.wav");

            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            if (File.Exists(filePath))
                File.Delete(filePath);

            mFileStream = File.Create(filePath);

            var sampleRate = 48000;
            var channels = (short)2;
            var bitPerSample = (short)16;
            var bytePerSec = sampleRate * bitPerSample * channels / 8;
            var blockSize = (short)(bitPerSample * channels / 8);

            mFileStream.Write(Encoding.Default.GetBytes(new char[] { 'R', 'I', 'F', 'F' }), 0, 4);//rtff tag.资源交换文件标志。
            mFileStream.Write(new byte[4], 0, 4);//file size.从下一个地址开始到文件尾的总字节数
            mFileStream.Write(Encoding.Default.GetBytes(new char[] { 'W', 'A', 'V', 'E' }), 0, 4);//wave tag.
            //rtff 代码块.

            mFileStream.Write(Encoding.Default.GetBytes(new char[] { 'f', 'm', 't', ' ' }), 0, 4);//fmt tag.
            mFileStream.Write(BitConverter.GetBytes(16), 0, 4);//fmt size.
            mFileStream.Write(BitConverter.GetBytes((short)1), 0, 2);//fmt id(compression).
            mFileStream.Write(BitConverter.GetBytes(channels), 0, 2);//ch. 单声道还是双声道
            mFileStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);//Sample rate. 每秒样本数
            mFileStream.Write(BitConverter.GetBytes(bytePerSec), 0, 4);//BytePerSec. 通道数×每秒样本数×每样本的数据位数／8
            mFileStream.Write(BitConverter.GetBytes(blockSize), 0, 2);//BlockSize. 通道数×每样本的数据位值／8
            mFileStream.Write(BitConverter.GetBytes(bitPerSample), 0, 2);//BitPerSample. 采样大小
            //fmt 代码块.

            mFileStream.Write(Encoding.Default.GetBytes(new char[] { 'd', 'a', 't', 'a' }), 0, 4);
            mFileStream.Write(new byte[4], 0, 4);//SubchunkSize.音频数据的大小
        }
        catch (System.Exception e)
        {
            Debug.Log("Error! " + e);
            Destroy(gameObject);
            throw;
        }
    }

    void OnDisable()
    {
        if (mFileStream != null)
        {
            var streamLength = mFileStream.Length;
            var fileSize = (int)streamLength - 8;
            var dataSize = (int)streamLength - 44;

            mFileStream.Seek(4, SeekOrigin.Begin);
            mFileStream.Write(BitConverter.GetBytes(fileSize), 0, 4);

            mFileStream.Seek(40, SeekOrigin.Begin);
            mFileStream.Write(BitConverter.GetBytes(dataSize), 0, 4);

            mFileStream.Close();
            mFileStream.Dispose();
            mFileStream = null;
        }
    }

    void OnAudioFilterRead(float[] samples, int channels)
    {
        if (mFileStream == null) return;

        if (mCacheSamplesOutput == null)
            mCacheSamplesOutput = new short[samples.Length];

        Encode(samples, mCacheSamplesOutput);

        for (int i = 0, iMax = mCacheSamplesOutput.Length; i < iMax; i++)
        {
            var samplePoint = mCacheSamplesOutput[i];
            mFileStream.Write(BitConverter.GetBytes(samplePoint), 0, 2);
        }
    }

    void Encode(float[] inSamples, short[] outSamples)
    {
        //注意，这里只有16位的.
        //如需扩展请参考https://github.com/unity3d-jp/FrameCapturer.

        for (int i = 0, iMax = inSamples.Length; i < iMax; i++)
        {
            mCacheSamplesOutput[i] = (short)(inSamples[i] * 32767.0f);
        }
    }
}
