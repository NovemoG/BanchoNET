using BanchoNET.Objects.Players;
using BanchoNET.Services;

namespace BanchoNET.Packets;

public partial class ClientPackets : Packet
{
	private static readonly BanchoSession Session = BanchoSession.Instance;
	
	public async Task<ClientPackets> CopyStream(Stream stream)
	{
		await stream.CopyToAsync(DataBuffer);
		DataBuffer.Position = 0;
		return this;
	}

	public void ReadPackets(Player player)
	{
		using var br = new BinaryReader(DataBuffer);
		
		Console.WriteLine("\nStarted reading packets");
		Console.WriteLine($"Initial buffer position: {br.BaseStream.Position}, length: {br.BaseStream.Length}");

		do
		{
			Console.WriteLine($"Buffer position: {br.BaseStream.Position}, length: {br.BaseStream.Length}");

			var packetId = (ClientPacketId)br.ReadInt16();
			
			if (!Enum.IsDefined(typeof(ClientPacketId), packetId))
				throw new Exception("Unresolvable packet id");

			br.ReadByte();
			var packetSize = br.ReadInt32();

			var dataBuffer = new byte[packetSize];
			var bytesRead = br.Read(dataBuffer, 0, packetSize);
			
			Console.WriteLine($"Packet: {packetId}, Length: {dataBuffer.Length}, Read: {bytesRead}, To read: {packetSize}");
			if (bytesRead != packetSize)
				throw new Exception("Packet size mismatch");

			DataBuffer.Position -= bytesRead;

			_packetHandlersMap[packetId](player, br);
		} while (br.BaseStream.Position < br.BaseStream.Length - 7);
		
		Console.WriteLine($"Final buffer position: {br.BaseStream.Position}, length: {br.BaseStream.Length}");
		Console.WriteLine("Stopped reading packets");
	}
	
	#region PacketHandlersMap

	private readonly Dictionary<ClientPacketId, Action<Player, BinaryReader>> _packetHandlersMap = new()
	{
		{ ClientPacketId.ChangeAction, ChangeAction },
		{ ClientPacketId.SendPublicMessage, SendPublicMessage },
		{ ClientPacketId.Logout, (player, br) => { br.ReadInt32(); BanchoSession.Instance.LogoutPlayer(player); } },
		{ ClientPacketId.RequestStatusUpdate, RequestStatusUpdate },
		{ ClientPacketId.Ping, (_, _) => { } },
		{ ClientPacketId.StartSpectating, (player, br) => { } },
		{ ClientPacketId.StopSpectating, (player, br) => { } },
		{ ClientPacketId.SpectateFrames, (player, br) => { } },
		{ ClientPacketId.ErrorReport, (player, br) => { } },
		{ ClientPacketId.CantSpectate, (player, br) => { } },
		{ ClientPacketId.SendPrivateMessage, (player, br) => { } },
		{ ClientPacketId.PartLobby, (player, br) => { } },
		{ ClientPacketId.JoinLobby, (player, br) => { } },
		{ ClientPacketId.CreateMatch, (player, br) => { } },
		{ ClientPacketId.JoinMatch, (player, br) => { } },
		{ ClientPacketId.PartMatch, (player, br) => { } },
		{ ClientPacketId.MatchChangeSlot, (player, br) => { } },
		{ ClientPacketId.MatchReady, (player, br) => { } },
		{ ClientPacketId.MatchLock, (player, br) => { } },
		{ ClientPacketId.MatchChangeSettings, (player, br) => { } },
		{ ClientPacketId.MatchStart, (player, br) => { } },
		{ ClientPacketId.MatchScoreUpdate, (player, br) => { } },
		{ ClientPacketId.MatchComplete, (player, br) => { } },
		{ ClientPacketId.MatchChangeMods, (player, br) => { } },
		{ ClientPacketId.MatchLoadComplete, (player, br) => { } },
		{ ClientPacketId.MatchNoBeatmap, (player, br) => { } },
		{ ClientPacketId.MatchNotReady, (player, br) => { } },
		{ ClientPacketId.MatchFailed, (player, br) => { } },
		{ ClientPacketId.MatchHasBeatmap, (player, br) => { } },
		{ ClientPacketId.MatchSkipRequest, (player, br) => { } },
		{ ClientPacketId.ChannelJoin, ChannelJoin },
		{ ClientPacketId.BeatmapInfoRequest, (player, br) => { } },
		{ ClientPacketId.MatchTransferHost, (player, br) => { } },
		{ ClientPacketId.FriendAdd, (player, br) => { } },
		{ ClientPacketId.FriendRemove, (player, br) => { } },
		{ ClientPacketId.MatchChangeTeam, (player, br) => { } },
		{ ClientPacketId.ChannelPart, (player, br) => { } },
		{ ClientPacketId.ReceiveUpdates, ReceiveUpdates },
		{ ClientPacketId.SetAwayMessage, (player, br) => { } },
		{ ClientPacketId.IrcOnly, (player, br) => { } },
		{ ClientPacketId.UserStatsRequest, UserStatsRequest },
		{ ClientPacketId.MatchInvite, (player, br) => { } },
		{ ClientPacketId.MatchChangePassword, (player, br) => { } },
		{ ClientPacketId.TournamentMatchInfoRequest, (player, br) => { } },
		{ ClientPacketId.UserPresenceRequest, (player, br) => { } },
		{ ClientPacketId.UserPresenceRequestAll, (player, br) => { } },
		{ ClientPacketId.ToggleBlockNonFriendDms, (player, br) => { } },
		{ ClientPacketId.TournamentJoinMatchChannel, (player, br) => { } },
		{ ClientPacketId.TournamentLeaveMatchChannel, (player, br) => { } },
	};

	#endregion
}