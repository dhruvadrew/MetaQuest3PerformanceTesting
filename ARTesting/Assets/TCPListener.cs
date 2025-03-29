using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpViconSync : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private List<float> offsetArray = new List<float>();
    private bool isSyncing = true;
    private float averageOffset = 0;
    private string csvFilePath;
    public GameObject cameraRig;

    private const int udpPort = 8888;
    private const string sbcServerIp = "10.197.190.79";
    private const int sbcServerPort = 6666;
    private const int ftpPort = 6667;
    private const string ftpUsername = "i3tlab";
    private const string ftpPassword = "iotlab443";

    void Start()
    {
        udpClient = new UdpClient(udpPort);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(sbcServerIp), sbcServerPort);
        string deviceMessage = $"MetaQuest:{GetLocalIPAddress()}";
        SendUdpMessage(deviceMessage);
        StartListening();
    }

    void StartListening()
    {
        Thread listenerThread = new Thread(() =>
        {
            while (true)
            {
                byte[] receivedData = udpClient.Receive(ref serverEndPoint);
                string message = Encoding.UTF8.GetString(receivedData).Trim();
                ProcessMessage(message);
            }
        });
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void ProcessMessage(string message)
    {
        if (isSyncing)
        {
            if (message.StartsWith("Stop Sync"))
            {
                isSyncing = false;
                averageOffset = ComputeAverageOffset();
                string timestamp = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm");
                csvFilePath = $"meta_quest_{timestamp}.csv";
            }
            else if (float.TryParse(message, out float viconTimestamp))
            {
                float xrTimestamp = Time.time;
                offsetArray.Add(viconTimestamp - xrTimestamp);
            }
        }
        else
        {
            if (message.StartsWith("Stop Collection"))
            {
                UploadCsv();
            }
            else
            {
                AppendTrackingDataToCsv();
            }
        }
    }

    float ComputeAverageOffset()
    {
        float sum = 0;
        foreach (var offset in offsetArray)
        {
            sum += offset;
        }
        return sum / offsetArray.Count;
    }

    void AppendTrackingDataToCsv()
    {
        OVRCameraRig cameraRigInstance = cameraRig.GetComponent<OVRCameraRig>();
        Transform centerEye = cameraRigInstance.centerEyeAnchor;
        Vector3 position = centerEye.position;
        Quaternion rotation = centerEye.rotation;

        float correctedTimestamp = Time.time + averageOffset;
        string csvLine = $"{correctedTimestamp}, {position.x}, {position.y}, {position.z}, {rotation.x}, {rotation.y}, {rotation.z}, {rotation.w}";
        File.AppendAllText(csvFilePath, csvLine + "\n");
    }

    void UploadCsv()
    {
        string ftpUri = $"ftp://{sbcServerIp}:{ftpPort}/{Path.GetFileName(csvFilePath)}";
        WebClient client = new WebClient { Credentials = new NetworkCredential(ftpUsername, ftpPassword) };
        client.UploadFile(ftpUri, WebRequestMethods.Ftp.UploadFile, csvFilePath);
    }

    void SendUdpMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, serverEndPoint);
    }

    string GetLocalIPAddress()
    {
        string localIP = "";
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}
