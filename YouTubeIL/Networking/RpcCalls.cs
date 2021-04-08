using System.Diagnostics.CodeAnalysis;

namespace YouTubeIL.Networking
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum RpcCalls : byte
    {
        PlayAnimation = 0,
        CompleteTask = 1,
        SyncSettings = 2,
        SetInfected = 3,
        Exiled = 4,
        CheckName = 5,
        SetName = 6,
        CheckColor = 7,
        SetColor = 8,
        SetHat = 9,
        SetSkin = 10, // 0x0A
        ReportDeadBody = 11, // 0x0B
        MurderPlayer = 12, // 0x0C
        SendChat = 13, // 0x0D
        StartMeeting = 14, // 0x0E
        SetScanner = 15, // 0x0F
        SendChatNote = 16, // 0x10
        SetPet = 17, // 0x11
        SetStartCounter = 18, // 0x12
        EnterVent = 19, // 0x13
        ExitVent = 20, // 0x14
        SnapTo = 21, // 0x15
        CloseMeeting = 22, // 0x16
        VotingComplete = 23, // 0x17
        CastVote = 24, // 0x18
        ClearVote = 25, // 0x19
        AddVote = 26, // 0x1A
        CloseDoorsOfType = 27, // 0x1B
        RepairSystem = 28, // 0x1C
        SetTasks = 29, // 0x1D
        ClimbLadder = 31, // 0x1F
        UsePlatform = 32, // 0x20
        
        YouTubeIL = byte.MaxValue - 1
    }
}