using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using StypeGripPacket = Tracking.StypeGrip.PacketHF;
//using StypeGripPacket = Tracking.StypeGrip.PacketA5;

public class TrackingApp
{
    public static void PrintData(StypeGripPacket packet)
    {
        System.Console.WriteLine("---------------------------------------------------");
        System.Console.WriteLine("Counter: {0}", packet.Counter);
        System.Console.WriteLine("Time   : {0}", packet.Timecode);
        System.Console.WriteLine("Pos    : {0}, {1}, {2}", packet.XYZ[0], packet.XYZ[1], packet.XYZ[2]);
        System.Console.WriteLine("Rot    : {0}, {1}, {2}", packet.PTR[0], packet.PTR[1], packet.PTR[2]);
        System.Console.WriteLine("Fov    : {0} {1}", packet.FovX, packet.FovY);
        System.Console.WriteLine("Zoom   : {0}", packet.Zoom);
        System.Console.WriteLine("Focus  : {0}", packet.Focus);
        System.Console.WriteLine("K1/K2  : {0} {1}", packet.K1, packet.K2);
        System.Console.WriteLine("\n---------------------------------------------------");
    }


    public static void PrintStats(Tracking.RingBuffer<StypeGripPacket> ringBuffer)
    {
        System.Console.WriteLine("---------------------------------------------------");
        System.Console.WriteLine("Time   : {0}", DateTime.Now);
        System.Console.WriteLine("Drops  : {0}", ringBuffer.Drops);
        System.Console.Write("Buffer :");
        for (int i = 0; i < ringBuffer.Length; ++i)
            System.Console.Write(" {0}", ringBuffer.Data[i].Counter);
        System.Console.WriteLine("\n---------------------------------------------------");
    }

    public static void PrintConfig(Tracking.StypeGrip.Config config)
    {
        System.Console.WriteLine("\n---------------------------------------------------");
        System.Console.WriteLine("Local Ip              : {0}", config.LocalIp);
        System.Console.WriteLine("Remote Ip             : {0}", config.RemoteIp);
        System.Console.WriteLine("Multicast             : {0}", config.Multicast);
        System.Console.WriteLine("Port                  : {0}", config.Port);
        System.Console.WriteLine("Delay                 : {0}", config.Delay);
        System.Console.WriteLine("ReadIntervalMs        : {0}", config.ReadIntervalMs);
        System.Console.WriteLine("ConsumeWhileAvailable : {0}", config.ConsumeWhileAvailable);
        System.Console.WriteLine("---------------------------------------------------");
    }

    public static void PrintHelp()
    {
        System.Console.WriteLine("\n---------------------------------------------------");
        System.Console.WriteLine("[Enter]   - Show stats");
        System.Console.WriteLine("[Space]   - Start/Stop stats loop");
        System.Console.WriteLine("[C]       - Clear");
        System.Console.WriteLine("[N]       - Network config");
        System.Console.WriteLine("[H]       - Help");
        System.Console.WriteLine("[Esc]     - Exit");
        System.Console.WriteLine("---------------------------------------------------\n");
    }

    

    public static void PrevMain(string[] args)
    {
        System.Console.WriteLine("Usage: TrackingApp.exe Host Port Delay ReadInterval ConsumeWhileAvailable");
        Tracking.StypeGrip.Config config = new Tracking.StypeGrip.Config();
        config.LocalIp = "127.0.0.1";
        config.RemoteIp = "0.0.0.0";
        config.Port = 6301;
        config.Delay = 5;
        config.ReadIntervalMs = 10;
        config.ConsumeWhileAvailable = false;

        if (args.Length > 1)
        {
            config.LocalIp = args[0];
            config.RemoteIp = args[1];
            System.Int32.TryParse(args[2], out config.Port);
            System.Int32.TryParse(args[3], out config.Delay);
            System.Int32.TryParse(args[4], out config.ReadIntervalMs);
            System.Boolean.TryParse(args[5], out config.ConsumeWhileAvailable);
        }
        else
        {
            System.Console.WriteLine("Running Default Configuration");
        }

        PrintConfig(config);


        Tracking.INetReader<StypeGripPacket> netReader
            //= new Tracking.StypeGrip.NetReaderAsync<StypeGripPacket>();
            = new Tracking.StypeGrip.NetReader<StypeGripPacket>();
        Tracking.RingBuffer<StypeGripPacket> ringBuffer = new Tracking.RingBuffer<StypeGripPacket>(config.Delay);

        config.Port = 6302;
        config.RemoteIp = "224.0.0.2";
        netReader.Config = config;
        netReader.Buffer = ringBuffer;

        netReader.Connect(config, ringBuffer);
        ringBuffer.ResetDrops();


        PrintHelp();


        bool exit = false;
        bool continuous_print = false;

        while(!exit)
        {
            if (System.Console.KeyAvailable)
            {
                System.ConsoleKey key_pressed = System.Console.ReadKey(true).Key;
                if (key_pressed == System.ConsoleKey.Spacebar)
                {
                    continuous_print = !continuous_print;
                }
                else if (key_pressed == System.ConsoleKey.Enter)
                {
                    PrintData(ringBuffer.Packet);
                    PrintStats(ringBuffer);
                }
                else if (key_pressed == System.ConsoleKey.C)
                {
                    System.Console.Clear();
                }
                else if (key_pressed == System.ConsoleKey.N)
                {
                    PrintConfig(config);
                }
                else if (key_pressed == System.ConsoleKey.H)
                {
                    PrintHelp();
                }
                else if (key_pressed == System.ConsoleKey.Escape)
                {
                    PrintStats(ringBuffer);
                    exit = true;
                }
            }
            else if (continuous_print)
            {
                PrintStats(ringBuffer);
            }
            
        }

        netReader.Disconnect();
    }

    public static void TutMain(string[] args)
    {
        //PrevMain(args);

            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 6302);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEp);

            IPAddress multicastaddress = IPAddress.Parse("224.0.0.2");
            client.JoinMulticastGroup(multicastaddress);

            Console.WriteLine("Listening this will never quit so you will need to ctrl-c it");

            while (true)
            {
                Byte[] data = client.Receive(ref localEp);
                string strData = System.Text.Encoding.Unicode.GetString(data);
                Console.WriteLine(strData);
            }
    }

    public static void Main(string[] args)
    {
        System.Console.WriteLine("Usage: TrackingApp.exe Host Port Delay ReadInterval ConsumeWhileAvailable");
        Tracking.StypeGrip.Config config = new Tracking.StypeGrip.Config();
        config.LocalIp = "0.0.0.0";
        config.RemoteIp = "0.0.0.0"; //!"224.0.0.2";
        config.Multicast = false;
        config.Port = 6301;
        config.Delay = 0;
        config.ReadIntervalMs = 10;
        config.ConsumeWhileAvailable = false;


        PrintConfig(config);


        Tracking.INetReader<StypeGripPacket> netReader
            //= new Tracking.StypeGrip.NetReaderAsync<StypeGripPacket>();
            //= new Tracking.StypeGrip.NetReader<StypeGripPacket>();
            = new Tracking.StypeGrip.NetReaderSync<StypeGripPacket>();
        Tracking.RingBuffer<StypeGripPacket> ringBuffer = new Tracking.RingBuffer<StypeGripPacket>(config.Delay);

        netReader.Config = config;
        netReader.Buffer = ringBuffer;

        netReader.Connect(config, ringBuffer);
        ringBuffer.ResetDrops();


        PrintHelp();


        bool exit = false;
        bool continuous_print = false;

        long last_timecode = 0;

        while (!exit)
        {
            if (System.Console.KeyAvailable)
            {
                System.ConsoleKey key_pressed = System.Console.ReadKey(true).Key;
                if (key_pressed == System.ConsoleKey.Spacebar)
                {
                    continuous_print = !continuous_print;
                }
                else if (key_pressed == System.ConsoleKey.Enter)
                {
                    PrintData(ringBuffer.Packet);
                    PrintStats(ringBuffer);
                }
                else if (key_pressed == System.ConsoleKey.C)
                {
                    System.Console.Clear();
                }
                else if (key_pressed == System.ConsoleKey.N)
                {
                    PrintConfig(config);
                }
                else if (key_pressed == System.ConsoleKey.H)
                {
                    PrintHelp();
                }
                else if (key_pressed == System.ConsoleKey.Escape)
                {
                    PrintStats(ringBuffer);
                    exit = true;
                }
            }
            else if (continuous_print)
            {
                //PrintStats(ringBuffer);
                PrintData(ringBuffer.Packet);
            }


            if (netReader.ReadNow() > 0)
            {
                long now = ringBuffer.Packet.Timecode;
                long elapsedTicks = now - last_timecode;
                last_timecode = ringBuffer.Packet.Timecode;

                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                Console.WriteLine("   {0:N0} ticks", elapsedTicks);
                Console.WriteLine("   {0:N0} nanoseconds", elapsedTicks * 100);
                Console.WriteLine("   {0:N0} milliseconds", elapsedSpan.Milliseconds);
                Console.WriteLine("   {0:N2} seconds", elapsedSpan.TotalSeconds);
                Console.WriteLine();
            }

        }

        netReader.Disconnect();
    }
}
