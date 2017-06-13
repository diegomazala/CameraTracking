using UnityEngine;
using System.Collections;

public class XcitoClientUI : MonoBehaviour
{
    public XcitoClient client = null;

    public Tracking.INetReader<Tracking.Xcito.Packet> netReader = null;

    public UnityEngine.UI.Button connectButton;
    public UnityEngine.UI.Button disconnectButton;

    public UnityEngine.UI.InputField port;
    public UnityEngine.UI.InputField delay;
    public UnityEngine.UI.InputField drops;

    public UnityEngine.UI.Image statusImage;
    public UnityEngine.UI.Text statusText;

    public UnityEngine.UI.Image syncImage;

    public UnityEngine.UI.Text counterText;

    public UnityEngine.UI.Text positionText;
    public UnityEngine.UI.Text rotationText;

    public UnityEngine.UI.Text imageSizeText;
    public UnityEngine.UI.Text chipSizeText;
    public UnityEngine.UI.Text centerShiftText;

    public UnityEngine.UI.Text fovText;
    public UnityEngine.UI.Text aspectText;

    public UnityEngine.UI.Text k1Text;
    public UnityEngine.UI.Text k2Text;

    const int MaxCountersUI = 8;
    public UnityEngine.UI.Text[] bufferCounterText = new UnityEngine.UI.Text[MaxCountersUI];

    IEnumerator Start()
    {
        if (client == null)
            client = FindObjectOfType<XcitoClient>();

        if (client == null)
        {
            Debug.LogError("Could not fin StypeGripClient object");
            enabled = false;
            yield return null;
        }

        client.UI = this;

        port.text = netReader.Config.Port.ToString();
        delay.text = netReader.Config.Delay.ToString();
        positionText.text = Vector3.zero.ToString();
        rotationText.text = Vector3.zero.ToString();

        UpdateUI();
    }

    void OnDisable()
    {
        OnDisconnect();
    }

    void Update()
    {       
        if (client.IsUpdating())
            statusImage.color = new Color(0.5f, 0.0f, 0.0f);
        else
            statusImage.color = new Color(0.0f, 0.5f, 0.0f);

        if (client.IsSynced())
            syncImage.color = new Color(0.0f, 0.5f, 0.0f);
        else
            syncImage.color = new Color(0.5f, 0.0f, 0.0f);

        UpdateUI();
    }

    public void OnConnect()
    {
        netReader.Connect();
    }


    public void OnDisconnect()
    {
        if (netReader != null)
            netReader.Disconnect();
    }

    public void OnPortChange()
    {
        netReader.Config.Port = System.Convert.ToInt32(port.text);
    }

    public void OnDelayChange()
    {
        netReader.Config.Delay = System.Convert.ToInt32(delay.text);
    }

    public void OnResetDropPressed()
    {
        netReader.Buffer.ResetDrops();
    }

    public void UpdateUI()
    {
        connectButton.interactable = !netReader.IsReading;
        disconnectButton.interactable = netReader.IsReading;

        counterText.text = netReader.Buffer.Packet.Counter.ToString();
        drops.text = netReader.Buffer.Drops.ToString();

        positionText.text = netReader.Buffer.Packet.Position.ToString();
        rotationText.text = netReader.Buffer.Packet.EulerAngles.ToString();

        imageSizeText.text = "(" + client.CameraConsts.ImageWidth.ToString() + ", " + client.CameraConsts.ImageHeight.ToString() + ")";
        chipSizeText.text = new Vector2(client.CameraConsts.ChipWidth, client.CameraConsts.ChipHeight).ToString("0.00");
        centerShiftText.text = new Vector2(netReader.Buffer.Packet.CenterX, netReader.Buffer.Packet.CenterY).ToString("0.00");

        fovText.text = netReader.Buffer.Packet.FovY.ToString("0.000");
        aspectText.text = client.CameraConsts.AspectRatio.ToString("0.000");

        k1Text.text = netReader.Buffer.Packet.K1.ToString("0.000000");
        k2Text.text = netReader.Buffer.Packet.K2.ToString("0.000000");

#if false
        int total = Mathf.Min(netReader.Buffer.Length, MaxCountersUI);
        int it = 0;
        for (it = 0; it < total; ++it)
            bufferCounterText[it].text = netReader.Buffer.Data[it].Counter.ToString();

        for (int i = it; i < MaxCountersUI; ++i)
            bufferCounterText[i].text = "x";
#endif
    }
}
