/*
 * This file is part of Moviebattles II Event Notification Library.
 * Licensed under the GNU Lesser General Public License, version 2.1.
 */

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace MB2EventReceiver
{
    public class MB2EventListener
    {
        public enum MBIINotificationType : int
        {
            MBII_RENS_CLIENTCONNECT = 0,
            MBII_RENS_CLIENTDISCONNECT,
            MBII_RENS_CLIENTBEGIN,
            MBII_RENS_TEAMCHANGE,
            MBII_RENS_SPEECH,
            MBII_RENS_KILL,
            MBII_RENS_SERVERSTART,
            MBII_RENS_SERVERSHUTDOWN,
            MBII_RENS_PRIVATEDUELEVENT,
            MBII_RENS_MAPCHANGE,
            MBII_RENS_MODECHANGE,
            MBII_RENS_SMODCMD,
            MBII_RENS_SMODLOGIN,
            MBII_RENS_NAMECHANGE,
            MBII_RENS_BAN,
            MBII_RENS_INTERMISSION,
            MBII_RENS_OBJCOMPLETE
        };

        public delegate void onClientConnectHandler(object sender, MBIINotificationEvent e);
        public delegate void onClientDisconnectHandler(object sender, MBIINotificationEvent e);
        public delegate void onClientBeginHandler(object sender, MBIINotificationEvent e);
        public delegate void onTeamChangeHandler(object sender, MBIINotificationEvent e);
        public delegate void onSpeachHandler(object sender, MBIINotificationEvent e);
        public delegate void onKillsHandler(object sender, MBIINotificationEvent e);
        public delegate void onServerStartHandler(object sender, MBIINotificationEvent e);
        public delegate void onServerShutdownHandler(object sender, MBIINotificationEvent e);
        public delegate void onPrivateDuelEventHandler(object sender, MBIINotificationEvent e);
        public delegate void onSMODCommandHandler(object sender, MBIINotificationEvent e);
        public delegate void onSMODLoginHandler(object sender, MBIINotificationEvent e);
        public delegate void onMapchangeHandler(object sender, MBIINotificationEvent e);
        public delegate void onModechangeHandler(object sender, MBIINotificationEvent e);
        public delegate void onNameChangeHandler(object sender, MBIINotificationEvent e);
        public delegate void onBanHandler(object sender, MBIINotificationEvent e);
        public delegate void onIntermissionHandler(object sender, MBIINotificationEvent e);
        public delegate void onObjCompleteHandler(object sender, MBIINotificationEvent e);

        public event onClientConnectHandler? onClientConnect;
        public event onClientDisconnectHandler? onClientDisconnect;
        public event onClientBeginHandler? onClientBegin;
        public event onTeamChangeHandler? onTeamChange;
        public event onSpeachHandler? onSpeach;
        public event onKillsHandler? onKills;
        public event onServerStartHandler? onServerStart;
        public event onServerShutdownHandler? onServerShutdown;
        public event onPrivateDuelEventHandler? onPrivateDuelEvent;
        public event onSMODCommandHandler? onSMODCommand;
        public event onSMODLoginHandler? onSMODLogin;
        public event onMapchangeHandler? onMapchange;
        public event onModechangeHandler? onModechange;
        public event onNameChangeHandler? onNameChange;
        public event onBanHandler? onBan;
        public event onIntermissionHandler? onIntermission;
        public event onObjCompleteHandler? onObjComplete;


        //private UdpClient client = null!;
        private Socket udpSocket = null!;
        private List<IPAddress> sourceWhitelist = new List<IPAddress>();

        CancellationTokenSource TokenSource = new CancellationTokenSource();
        private CancellationToken Token;
        private Task? listenTask;
        public MB2EventListener(int port)
            : this(port, null)
        {

        }

        public MB2EventListener(int port, IEnumerable<string>? SourceWhitelist)
        {
            onServerStart = null;

            if (SourceWhitelist != null)
            {
                foreach (string s in SourceWhitelist)
                {
                    IPAddress newAddr = IPAddress.Parse(s);
                    sourceWhitelist.Add(newAddr);
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
            Token = TokenSource.Token;

            udpSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.DualMode = true;
            udpSocket.Bind(localEndPoint);
        }

        public async Task StartListen()
        {
            Console.WriteLine("StartListen");
            if (listenTask != null)
            {
                Console.WriteLine("Already Listening");
                //Already listening
                return;
            }
            listenTask = ListenAsync(Token);

            await Task.Run(async () =>
            {
                try
                {
                    while (await udpProcessingChannel.Reader.WaitToReadAsync() && !Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Channel is waking up!");
                        try
                        {
                            byte[] packet = await udpProcessingChannel.Reader.ReadAsync();
                            InterpretData(packet);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Exception reading updProcessingChannel {ex.Message}{Environment.NewLine}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Exception reading updProcessingChannel {ex.Message}{Environment.NewLine}{Environment.NewLine}{ex.StackTrace}");
                }
            }
            );

        }

        private bool AcceptFromIPAddress(IPAddress srcAddr)
        {
            if(sourceWhitelist.Count > 0)
            {
                foreach(IPAddress whitelistedAddr in sourceWhitelist)
                {
                    if(srcAddr.Equals(whitelistedAddr))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private Channel<byte[]> udpProcessingChannel = Channel.CreateUnbounded<byte[]>();
        private async Task ListenAsync(CancellationToken token)
        {
            Console.WriteLine("Listen Thread running!");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    byte[] data = new byte[512];

                    SocketReceiveMessageFromResult result = await udpSocket.ReceiveMessageFromAsync(new ArraySegment<byte>(data), SocketFlags.None, RemoteEndPoint, token);
                    IPEndPoint ep = (IPEndPoint)result.RemoteEndPoint;

                    string recvAddr;
                    if(ep.Address.IsIPv4MappedToIPv6)
                    {
                        recvAddr = ep.Address.MapToIPv4().ToString();
                        ep.Address = ep.Address.MapToIPv4();
                    }
                    else
                    {
                        recvAddr = ep.Address.ToString();
                    }

                    if(result.SocketFlags.HasFlag(SocketFlags.Truncated))
                    {
                        Console.WriteLine($"Oversized packet detected! {data.Length} bytes from {recvAddr}");
                        continue;
                    }

                    if (!AcceptFromIPAddress(ep.Address))
                    {
                        Console.WriteLine($"Got {data.Length} bytes from {recvAddr}. Address is not whitelisted, therefore this data has been rejected");
                        continue;
                    }

                    Console.WriteLine($"Got {data.Length} bytes from {recvAddr}. Writing to UDP Processing Channel");

                    byte[] packet = data.AsSpan(0, result.ReceivedBytes).ToArray();
                    udpProcessingChannel.Writer.TryWrite(packet);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Exception in Listen Loop " + ex.Message);
                }
            }
        }

        private void InterpretData(byte[] data)
        {
            Console.WriteLine("InterpretData");

            if(data.Length <  Marshal.SizeOf(typeof(MBIINotificationHeader)))
            {
                throw new InvalidOperationException($"Packet was too short! {data.Length} bytes");
            }

            if(data.Length > 512)
            {
                throw new InvalidOperationException($"Oversized packet detected! {data.Length} bytes");
            }

            //Validate the Magic
            if (data[0] != 'M' || data[1] != 'B' || data[2] != 'I' || data[3] != 'I')
            {
                Console.WriteLine($"Bad Packet Identifier. Packet bytes({data.Length}):");
                foreach(byte b in data)
                {
                    Console.Write($"{(uint)b} ");
                }
                Console.WriteLine();
                throw new InvalidDataException($"Packet Identifier was incorrect! Bytes were {data[0]} {data[1]} {data[2]} {data[3]}.");
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            MBIINotificationType packetType = (MBIINotificationType)BitConverter.ToInt32(data, 4);

            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                Console.WriteLine($"PacketType: {packetType}");
                switch (packetType)
                {
                    case MBIINotificationType.MBII_RENS_CLIENTCONNECT:
                        {
                            if(onClientConnect != null)
                            {
                                onClientConnect(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientConnect>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_CLIENTDISCONNECT:
                        {
                            if (onClientDisconnect != null)
                            {
                                onClientDisconnect(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientDisconnect>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_CLIENTBEGIN:
                        {
                            if (onClientBegin != null)
                            {
                                onClientBegin(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientBegin>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_TEAMCHANGE:
                        {
                            if (onTeamChange != null)
                            {
                                onTeamChange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SwitchTeams>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SPEECH:
                        {
                            if (onSpeach != null)
                            {
                                onSpeach(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Speech>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_KILL:
                        {
                            if (onKills != null)
                            {
                                onKills(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_PlayerDeath>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SERVERSTART:
                        {
                            if (onServerStart != null)
                            {
                                onServerStart(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ServerStart>(ptr) });
                            }
                            
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SERVERSHUTDOWN:
                        {
                            if (onServerShutdown != null)
                            {
                                onServerShutdown(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ServerShutdown>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_PRIVATEDUELEVENT:
                        {
                            if (onPrivateDuelEvent != null)
                            {
                                onPrivateDuelEvent(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_PrivateDuelEvent>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_MAPCHANGE:
                        {
                            if (onMapchange != null)
                            {
                                onMapchange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_MapChange>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_MODECHANGE:
                        {
                            if (onModechange != null)
                            {
                                onModechange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ModeChange>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SMODCMD:
                        {
                            if (onSMODCommand != null)
                            {
                                onSMODCommand(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SmodCommand>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SMODLOGIN:
                        {
                            if (onSMODLogin != null)
                            {
                                onSMODLogin(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SmodLogin>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_NAMECHANGE:
                        {
                            if(onNameChange != null)
                            {
                                onNameChange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_NameChange>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_BAN:
                        {
                            if (onBan != null)
                            {
                                onBan(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Ban>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_INTERMISSION:
                        {
                            if (onIntermission != null)
                            {
                                onIntermission(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Intermission>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_OBJCOMPLETE:
                        {
                            if (onObjComplete != null)
                            {
                                onObjComplete(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ObjectiveComplete>(ptr) });
                            }
                            break;
                        }
                    default:
                        {
                            throw new InvalidDataException($"Unknown MBIINotificationType {(int)packetType}");
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}: {e.Message}");
            }
            finally
            {
                handle.Free();
            }
        }

        public async void EndListen()
        {
            TokenSource.Cancel();
            if (listenTask != null)
            {
                await listenTask;
            }
            udpSocket.Dispose();
            udpProcessingChannel.Writer.TryComplete();
        }
    }
}
