/*
 * This file is part of Moviebattles II Event Notification Library.
 * Licensed under the GNU Lesser General Public License, version 2.1.
 */

using System.Runtime.InteropServices;
using static MB2EventReceiver.MB2EventListener;

namespace MB2EventReceiver
{
    public struct MBIINotificationEvent
    {
        public object Notification { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotificationHeader 
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] MBIIIdentifier;
        public MBIINotificationType PacketBodyType;
        public uint LevelTime;
        public ushort Port;
        public byte Version;
        public byte Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ClientConnect
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
        public uint IPAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Guid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string PlayerName;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ClientDisconnect
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ClientBegin
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_SwitchTeams
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
        public byte NewTeam;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_PlayerDeath
    {
        public MBIINotificationHeader Header;
        public byte ClientKilledId;
        public byte ClientKilledById;
        public byte ClientAssistId;
        public int MeansOfDeath;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ServerStart
    {
        public MBIINotificationHeader Header;
        public byte MBMode;
        public int ruleset;
        public bool respawnMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Map;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ServerShutdown
    {
        public MBIINotificationHeader Header;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_Speech
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
        public byte MessageMode;
        public sbyte TargetClientId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 150)]
        public string Text;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_SmodLogin
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
        public byte adminNum;
        public bool LoginSuccess;
        public uint IPAddress;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_SmodCommand
    {
        public MBIINotificationHeader Header;
        public byte ClientId;
        public byte SmodCommandId;
        public byte ClientTargetId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_PrivateDuelEvent
    {
        public MBIINotificationHeader Header;
        public byte ClientId1;
        public byte ClientId2;
        public bool endDuel;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_MapChange
    {
        public MBIINotificationHeader Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public string NewMap;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ModeChange
    {
        public MBIINotificationHeader Header;
        public byte NewMode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_Ban
    {
        public MBIINotificationHeader Header;
        public byte clientId;
        public uint IPAddress;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_NameChange
    {
        public MBIINotificationHeader Header;
        public byte clientId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string NewName;
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_Intermission
    {
        public MBIINotificationHeader Header;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MBIINotification_ObjectiveComplete
    {
        public MBIINotificationHeader Header;
        public byte clientId;
    }
}
