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

        public delegate void OnClientConnectHandler(object sender, MBIINotificationEvent e);
        public delegate void OnClientDisconnectHandler(object sender, MBIINotificationEvent e);
        public delegate void OnClientBeginHandler(object sender, MBIINotificationEvent e);
        public delegate void OnTeamChangeHandler(object sender, MBIINotificationEvent e);
        public delegate void OnSpeechHandler(object sender, MBIINotificationEvent e);
        public delegate void OnKillsHandler(object sender, MBIINotificationEvent e);
        public delegate void OnServerStartHandler(object sender, MBIINotificationEvent e);
        public delegate void OnServerShutdownHandler(object sender, MBIINotificationEvent e);
        public delegate void OnPrivateDuelEventHandler(object sender, MBIINotificationEvent e);
        public delegate void OnSMODCommandHandler(object sender, MBIINotificationEvent e);
        public delegate void OnSMODLoginHandler(object sender, MBIINotificationEvent e);
        public delegate void OnMapchangeHandler(object sender, MBIINotificationEvent e);
        public delegate void OnModechangeHandler(object sender, MBIINotificationEvent e);
        public delegate void OnNameChangeHandler(object sender, MBIINotificationEvent e);
        public delegate void OnBanHandler(object sender, MBIINotificationEvent e);
        public delegate void OnIntermissionHandler(object sender, MBIINotificationEvent e);
        public delegate void OnObjCompleteHandler(object sender, MBIINotificationEvent e);

        public event OnClientConnectHandler? OnClientConnect;
        public event OnClientDisconnectHandler? OnClientDisconnect;
        public event OnClientBeginHandler? OnClientBegin;
        public event OnTeamChangeHandler? OnTeamChange;
        public event OnSpeechHandler? OnSpeech;
        public event OnKillsHandler? OnKills;
        public event OnServerStartHandler? OnServerStart;
        public event OnServerShutdownHandler? OnServerShutdown;
        public event OnPrivateDuelEventHandler? OnPrivateDuelEvent;
        public event OnSMODCommandHandler? OnSMODCommand;
        public event OnSMODLoginHandler? OnSMODLogin;
        public event OnMapchangeHandler? OnMapchange;
        public event OnModechangeHandler? OnModechange;
        public event OnNameChangeHandler? OnNameChange;
        public event OnBanHandler? OnBan;
        public event OnIntermissionHandler? OnIntermission;
        public event OnObjCompleteHandler? OnObjComplete;


        //private UdpClient client = null!;
        private Socket _udpSocket = null!;
        private List<IPAddress> _sourceWhitelist = new List<IPAddress>();

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _token;
        private Task? _listenTask;
        private Channel<byte[]> _udpProcessingChannel = Channel.CreateUnbounded<byte[]>();

        public MB2EventListener(int port)
            : this(port, null)
        {

        }

        public MB2EventListener(int port, IEnumerable<string>? SourceWhitelist)
        {
            OnServerStart = null;

            if (SourceWhitelist is not null)
            {
                foreach (string s in SourceWhitelist)
                {
                    IPAddress newAddr = IPAddress.Parse(s);
                    _sourceWhitelist.Add(newAddr);
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
            _token = _tokenSource.Token;

            _udpSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            _udpSocket.DualMode = true;
            _udpSocket.Bind(localEndPoint);
        }

        public async Task StartListen()
        {
            Console.WriteLine("StartListen");
            if (_listenTask is not null)
            {
                Console.WriteLine("Already Listening");
                //Already listening
                return;
            }
            _listenTask = ListenAsync(_token);

            await Task.Run(async () =>
            {
                try
                {
                    while (await _udpProcessingChannel.Reader.WaitToReadAsync() && !_token.IsCancellationRequested)
                    {
                        Console.WriteLine("Channel is waking up!");
                        try
                        {
                            byte[] packet = await _udpProcessingChannel.Reader.ReadAsync();
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
            if(_sourceWhitelist.Count > 0)
            {
                foreach(IPAddress whitelistedAddr in _sourceWhitelist)
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
        
        private async Task ListenAsync(CancellationToken token)
        {
            Console.WriteLine("Listen Thread running!");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    byte[] data = new byte[512];

                    SocketReceiveMessageFromResult result = await _udpSocket.ReceiveMessageFromAsync(new ArraySegment<byte>(data), SocketFlags.None, RemoteEndPoint, token);
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
                    _udpProcessingChannel.Writer.TryWrite(packet);
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
                            if(OnClientConnect is not null)
                            {
                                OnClientConnect(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientConnect>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_CLIENTDISCONNECT:
                        {
                            if (OnClientDisconnect is not null)
                            {
                                OnClientDisconnect(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientDisconnect>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_CLIENTBEGIN:
                        {
                            if (OnClientBegin is not null)
                            {
                                OnClientBegin(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ClientBegin>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_TEAMCHANGE:
                        {
                            if (OnTeamChange is not null)
                            {
                                OnTeamChange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SwitchTeams>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SPEECH:
                        {
                            if (OnSpeech is not null)
                            {
                                OnSpeech(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Speech>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_KILL:
                        {
                            if (OnKills is not null)
                            {
                                OnKills(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_PlayerDeath>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SERVERSTART:
                        {
                            if (OnServerStart is not null)
                            {
                                OnServerStart(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ServerStart>(ptr) });
                            }
                            
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SERVERSHUTDOWN:
                        {
                            if (OnServerShutdown is not null)
                            {
                                OnServerShutdown(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ServerShutdown>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_PRIVATEDUELEVENT:
                        {
                            if (OnPrivateDuelEvent is not null)
                            {
                                OnPrivateDuelEvent(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_PrivateDuelEvent>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_MAPCHANGE:
                        {
                            if (OnMapchange is not null)
                            {
                                OnMapchange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_MapChange>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_MODECHANGE:
                        {
                            if (OnModechange is not null)
                            {
                                OnModechange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ModeChange>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SMODCMD:
                        {
                            if (OnSMODCommand is not null)
                            {
                                OnSMODCommand(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SmodCommand>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_SMODLOGIN:
                        {
                            if (OnSMODLogin is not null)
                            {
                                OnSMODLogin(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_SmodLogin>(ptr) });
                            }

                            break;
                        }
                    case MBIINotificationType.MBII_RENS_NAMECHANGE:
                        {
                            if(OnNameChange is not null)
                            {
                                OnNameChange(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_NameChange>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_BAN:
                        {
                            if (OnBan is not null)
                            {
                                OnBan(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Ban>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_INTERMISSION:
                        {
                            if (OnIntermission is not null)
                            {
                                OnIntermission(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_Intermission>(ptr) });
                            }
                            break;
                        }
                    case MBIINotificationType.MBII_RENS_OBJCOMPLETE:
                        {
                            if (OnObjComplete is not null)
                            {
                                OnObjComplete(this, new MBIINotificationEvent() { Notification = Marshal.PtrToStructure<MBIINotification_ObjectiveComplete>(ptr) });
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
            _tokenSource.Cancel();
            if (_listenTask is not null)
            {
                await _listenTask;
            }
            _udpSocket.Dispose();
            _udpProcessingChannel.Writer.TryComplete();
        }
    }
}
