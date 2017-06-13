using UnityEngine;
using System.Collections;

public class StypeGripClientUI : MonoBehaviour
{
    public StypeGripClient client = null;

#if true //STYPE_HF
    [HideInInspector]
    public Tracking.NetReader<Tracking.StypeGrip.PacketHF> netReader = null;
#else
    public Tracking.NetReader<Tracking.StypeGrip.PacketA5> netReader = null;
#endif

    public UnityEngine.UI.Button connectButton;
    public UnityEngine.UI.Button disconnectButton;

    public UnityEngine.UI.InputField port;
    public UnityEngine.UI.InputField delay;
    public UnityEngine.UI.Text dropsText;
    private uint lastDropCount = 0;

    public UnityEngine.UI.Image statusImage;
    public UnityEngine.UI.Text statusText;

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

    void Start()
    {
        if (client == null)
            client = FindObjectOfType<StypeGripClient>();

        if (client == null)
        {
            Debug.LogError("Could not fin StypeGripClient object");
            enabled = false;
            return;
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
        UpdateUI();
    }

    public void OnConnect()
    {
        netReader.Connect();
    }


    public void OnDisconnect()
    {
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

    public void OnDropPressed()
    {
        netReader.Buffer.ResetDrops();
    }

    public void UpdateUI()
    {
        connectButton.interactable = !netReader.IsReading;
        disconnectButton.interactable = netReader.IsReading;

        //if (netReader.RingBuffer.Packet.Repeats == 0)
        //{
        //    statusImage.color = new Color(0.0f, 0.5f, 0.0f);
        //    statusText.text = "Connected";
        //}
        //else
        //{
        //    statusImage.color = new Color(0.5f, 0.0f, 0.0f);
        //    statusText.text = "Disconnected";
        //}

        counterText.text = netReader.Buffer.Packet.Counter.ToString();
        dropsText.text = netReader.Buffer.Drops.ToString();

        if (netReader.Buffer.Drops != lastDropCount)
        {
            statusImage.color = new Color(0.5f, 0.0f, 0.0f);
            statusText.text = "Status: Fail";
            lastDropCount = netReader.Buffer.Drops;
        }
        else
        {
            statusImage.color = new Color(0.0f, 0.5f, 0.0f);
            statusText.text = "Status: Ok";
        }

        positionText.text = netReader.Buffer.Packet.Position.ToString();
        rotationText.text = netReader.Buffer.Packet.EulerAngles.ToString();

        //imageSizeText.text = "(" + netReader.Config.ImageWidth.ToString() + ", " + netReader.Config.ImageHeight.ToString() + ")";
        chipSizeText.text = new Vector2(netReader.Buffer.Packet.ChipWidth, netReader.Buffer.Packet.ChipHeight).ToString("0.00");
        centerShiftText.text = new Vector2(netReader.Buffer.Packet.CenterX, netReader.Buffer.Packet.CenterY).ToString("0.00");

        fovText.text = netReader.Buffer.Packet.FovY.ToString("0.000");
        aspectText.text = netReader.Buffer.Packet.AspectRatio.ToString("0.000");

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
