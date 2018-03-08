using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogComponent : MonoBehaviour
{
    public LogWriter.LogWriter logWriter;

    void Start()
    {
        logWriter = new LogWriter.LogWriter("C:/tmp/", "Test_", ".log");
        string msg = "Start Msg";
        logWriter.WriteToLog(msg);
    }

    // Update is called once per frame
    void Update()
    {
        logWriter.WriteToLog(Time.frameCount.ToString() + ' ' + Time.deltaTime);
    }
}
