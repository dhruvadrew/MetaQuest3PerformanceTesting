using UnityEngine;
using UnityEngine.UI;  // Use this if you're using UI.Text
using TMPro;       // Uncomment this if you use TextMeshPro

public class DisplayPosRotation : MonoBehaviour
{
    public OVRCameraRig cameraRig; // Assign your OVRCameraRig (or its centerEyeAnchor) in the Inspector
    public TextMeshProUGUI posRotText;        // For UI.Text; if using TextMeshPro, use: public TextMeshProUGUI posRotText;

    void Update()
    {
        /*
        if (cameraRig != null && posRotText != null)
        {
            // Get the headset's position and rotation
            Vector3 pos = cameraRig.centerEyeAnchor.position;
            Quaternion rot = cameraRig.centerEyeAnchor.rotation;

            // Format the position and rotation. You can choose to display Euler angles.
            string posString = $"Pos: {pos.x:F2}, {pos.y:F2}, {pos.z:F2}";
            string rotString = $"Rot: {rot.eulerAngles.x:F2}, {rot.eulerAngles.y:F2}, {rot.eulerAngles.z:F2}";

            // Update the text element
            posRotText.text = posString + "\n" + rotString;
        }
        */
    }
}
