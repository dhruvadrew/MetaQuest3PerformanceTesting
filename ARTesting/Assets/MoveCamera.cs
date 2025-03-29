using UnityEngine;
using System.IO;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;

public class MoveCamera : MonoBehaviour
{
    public float moveSpeed;
    private string fname;
    private string path;
    public OVRCameraRig cameraRig;
    public float flag;

    private TcpListener listener8765;
    private TcpListener listener8764;
    private CancellationTokenSource cancellationTokenSource;
    public TextMeshProUGUI tcpText;        // For UI.Text; if using TextMeshPro, use: public TextMeshProUGUI posRotText;

    // Start is called before the first frame update
    void Start()
    {
        tcpText.text = "TEST";
        flag = 0;
        moveSpeed = 7f;

        Debug.Log("###################################" +
                  "\n" + "START TO UPDATE OF TIME" +
                  "\n" + "####################################");

        // Setup file name and path
        fname = System.DateTime.Now.ToString("HH-mm-ss") + ".txt";
        path = Path.Combine(Application.persistentDataPath, "CameraPaths", fname);

        // Ensure CameraPaths directory exists
        string directoryPath = Path.Combine(Application.persistentDataPath, "CameraPaths");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Write initial content using StreamWriter
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            WriteContent(writer);
        }

        // Start TCP listeners
        cancellationTokenSource = new CancellationTokenSource();
        StartTCPListeners();
    }

    // Update is called once per frame
    void Update()
    {
        flag += 1;
        if (flag == 10)
        {
            Debug.Log("###################################" +
                      "\n" + "TESTING FOR UPDATE OF TIME" +
                      "\n" + "####################################");
        }

        // Append content using StreamWriter in append mode
        using (StreamWriter writer = new StreamWriter(path, true))
        {
            WriteContent(writer);
        }
    }

    void WriteContent(StreamWriter writer)
    {
        long timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
        string adjustedTimestamp = ((timestamp * 1000L) / 1000000.0).ToString("F6");

        // Get the position of the headset
        Vector3 headsetPosition = cameraRig.centerEyeAnchor.position;
        // Get the rotation of the headset
        Quaternion headsetRotation = cameraRig.centerEyeAnchor.rotation;

        // Convert position from Unity left-handed coordinate system to Vicon right-handed coordinate system
        Vector3 convertedPosition = CoordinateConverter.ConvertPosition(headsetPosition);
        // Convert rotation from Unity left-handed coordinate system to Vicon right-handed coordinate system
        Quaternion convertedRotation = CoordinateConverter.ConvertRotation(headsetRotation);

        // Convert position components to string with 6 digits after the decimal
        string posX = convertedPosition.x.ToString("F6");
        string posY = convertedPosition.y.ToString("F6");
        string posZ = convertedPosition.z.ToString("F6");

        // Convert rotation components to string with 6 digits after the decimal
        string rotX = convertedRotation.x.ToString("F6");
        string rotY = convertedRotation.y.ToString("F6");
        string rotZ = convertedRotation.z.ToString("F6");
        string rotW = convertedRotation.w.ToString("F6");

        // Debug output of position and rotation in Unity's console
        Debug.Log($"Position: X={posX}, Y={posY}, Z={posZ}");
        Debug.Log($"Rotation: X={rotX}, Y={rotY}, Z={rotZ}, W={rotW}");

        string content = adjustedTimestamp + " " + posX + " " + posY + " " +
                         posZ + " " + rotX + " " + rotY + " " +
                         rotZ + " " + rotW + Environment.NewLine;

        writer.Write(content);
        Debug.Log("Content written to: " + path);
    }

    void StartTCPListeners()
    {
        listener8765 = new TcpListener(IPAddress.Any, 8765);
        listener8764 = new TcpListener(IPAddress.Any, 8764);

        listener8765.Start();
        listener8764.Start();

        Debug.Log("TCP Listeners started on ports 8765 and 8764");

        Task.Run(() => ListenForClients(listener8765, 8765, cancellationTokenSource.Token));
        Task.Run(() => ListenForClients(listener8764, 8764, cancellationTokenSource.Token));
    }

    async Task ListenForClients(TcpListener listener, int port, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Debug.Log($"Client connected on port {port}");

                    _ = Task.Run(() => HandleClient(client, port, token));
                }
                await Task.Delay(100);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error on port {port}: {e.Message}");
        }
    }

    async Task HandleClient(TcpClient client, int port, CancellationToken token)
    {
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break; // Client disconnected

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    tcpText.text = message;
                    Debug.Log($"Received on port {port}: {message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling client on port {port}: {e.Message}");
            }
        }

        client.Close();
        Debug.Log($"Client disconnected from port {port}");
    }

    void OnApplicationQuit()
    {
        cancellationTokenSource.Cancel();
        listener8765.Stop();
        listener8764.Stop();
        Debug.Log("TCP Listeners stopped");
    }
}

public class CoordinateConverter
{
    // Convert position from Unity left-handed coordinate system to Vicon right-handed coordinate system
    public static Vector3 ConvertPosition(Vector3 position)
    {
        // Invert Unity's Z-axis and swap Y-axis for Vicon's coordinate system
        return new Vector3(position.x, position.z, -position.y);
    }

    // Convert rotation from Unity left-handed coordinate system to Vicon right-handed coordinate system
    public static Quaternion ConvertRotation(Quaternion rotation)
    {
        // Typically, Unity's rotation is already suitable; adjust if needed.
        return rotation;
    }
}
